using LMSFinal.Application.Common;
using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Certificates;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Services
{

    public class CertificateService : ICertificateService
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IQuizRepository _quizRepository;
        private readonly IQuizAttemptRepository _quizAttemptRepository;
        private readonly ICertificatePdfGenerator _pdfGenerator;
        private readonly INotificationService _notificationService;
        private readonly IFileStorageService _fileStorage;
        private readonly IUnitOfWork _unitOfWork;

        public CertificateService(
            ICertificateRepository certificateRepository,
            IEnrollmentRepository enrollmentRepository,
            IQuizRepository quizRepository,
            IQuizAttemptRepository quizAttemptRepository,
            ICertificatePdfGenerator pdfGenerator,
            INotificationService notificationService,
            IFileStorageService fileStorage,
            IUnitOfWork unitOfWork)
        {
            _certificateRepository = certificateRepository;
            _enrollmentRepository = enrollmentRepository;
            _quizRepository = quizRepository;
            _quizAttemptRepository = quizAttemptRepository;
            _pdfGenerator = pdfGenerator;
            _notificationService = notificationService;
            _fileStorage = fileStorage;
            _unitOfWork = unitOfWork;
        }

        public async Task<CertificateDto?> IssueIfEligibleAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            
            var alreadyIssued = await _certificateRepository.ExistsAsync(studentId, courseId, cancellationToken);
            if (alreadyIssued)
            {
                return null;
            }

            var enrollment = await _enrollmentRepository.GetAsync(studentId, courseId, cancellationToken);
            if (enrollment is null || enrollment.Status != EnrollmentStatus.Completed)
            {
                return null; // прогресс ещё не 100% — рано
            }

            if (!await HasPassedAllRequiredQuizzesAsync(studentId, courseId, cancellationToken))
            {
                return null; //  "100% + выполнения необходимых quizzes" — оба условия обязательны
            }

            var certificateNumber = await GenerateCertificateNumberAsync(cancellationToken);

            var certificate = new Certificate
            {
                CertificateNumber = certificateNumber,
                StudentId = studentId,
                CourseId = courseId,
                IssuedAt = DateTime.UtcNow,
                CompletionDate = enrollment.CompletedAt ?? DateTime.UtcNow
            };

            await _certificateRepository.AddAsync(certificate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _certificateRepository.GetWithDetailsAsync(certificate.Id, cancellationToken)
                ?? throw new NotFoundException("Certificate", certificate.Id);

            var dto = MapToDto(created);

            await _notificationService.NotifyAsync(
                studentId,
                "Certificate issued",
                $"Your certificate for \"{dto.CourseTitle}\" ({dto.CertificateNumber}) is ready to download.",
                NotificationType.CertificateIssued,
                cancellationToken);

            return dto;
        }

        public async Task<IReadOnlyList<CertificateDto>> GetMyCertificatesAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var certificates = await _certificateRepository.GetByStudentIdAsync(studentId, cancellationToken);
            return certificates.Select(MapToDto).ToList();
        }

        public async Task<CertificateDto> GetByIdAsync(Guid studentId, Guid certificateId, CancellationToken cancellationToken = default)
        {
            var certificate = await _certificateRepository.GetWithDetailsAsync(certificateId, cancellationToken)
                ?? throw new NotFoundException("Certificate", certificateId);

            if (certificate.StudentId != studentId)
            {
                throw new ForbiddenAccessException("Вы можете просматривать только свои сертификаты.");
            }

            return MapToDto(certificate);
        }

        public async Task<CertificateVerificationDto> VerifyAsync(string certificateNumber, CancellationToken cancellationToken = default)
        {
            var certificate = await _certificateRepository.GetByCertificateNumberAsync(certificateNumber, cancellationToken)
                ?? throw new NotFoundException($"Сертификат с номером '{certificateNumber}' не найден.");

            return MapToVerification(certificate);
        }

        public async Task<byte[]> GeneratePdfAsync(Guid studentId, Guid certificateId, CancellationToken cancellationToken = default)
        {
            // Переиспользуем GetByIdAsync — ownership-проверка та же самая, дублировать незачем.
            var dto = await GetByIdAsync(studentId, certificateId, cancellationToken);

            var objectName = $"{certificateId}.pdf";
            var cached = await _fileStorage.TryDownloadAsync(StorageBuckets.Certificates, objectName, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            var pdfBytes = _pdfGenerator.Generate(dto);

            using (var stream = new MemoryStream(pdfBytes))
            {
                await _fileStorage.UploadAsync(StorageBuckets.Certificates, objectName, stream, "application/pdf", cancellationToken);
            }

            return pdfBytes;
        }

        // --- helpers ---

        private async Task<bool> HasPassedAllRequiredQuizzesAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken)
        {
            var requiredQuizzes = await _quizRepository.GetByCourseIdAsync(courseId, cancellationToken);
            if (requiredQuizzes.Count == 0)
            {
                return true; // в курсе вообще нет квизов — условие выполнено тривиально
            }

            var attempts = await _quizAttemptRepository.GetByStudentIdAsync(studentId, cancellationToken);
            var passedQuizIds = attempts.Where(a => a.Passed).Select(a => a.QuizId).ToHashSet();

            return requiredQuizzes.All(q => passedQuizIds.Contains(q.Id));
        }

        private async Task<string> GenerateCertificateNumberAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var countThisYear = await _certificateRepository.CountByYearAsync(year, cancellationToken);
            var sequence = countThisYear + 1;

            return $"LMS-{year}-{sequence:D6}";
        }

        /// <summary>Публичная проекция — без внутренних идентификаторов, см. CertificateVerificationDto.</summary>
        private static CertificateVerificationDto MapToVerification(Certificate certificate) => new(
            certificate.CertificateNumber,
            $"{certificate.Student.FirstName} {certificate.Student.LastName}",
            GetCourseTitle(certificate.Course),
            $"{certificate.Course.Instructor.FirstName} {certificate.Course.Instructor.LastName}",
            certificate.IssuedAt,
            certificate.CompletionDate);

        private static CertificateDto MapToDto(Certificate certificate) => new(
            certificate.Id,
            certificate.CertificateNumber,
            certificate.StudentId,
            $"{certificate.Student.FirstName} {certificate.Student.LastName}",
            certificate.CourseId,
            GetCourseTitle(certificate.Course),
            $"{certificate.Course.Instructor.FirstName} {certificate.Course.Instructor.LastName}",
            certificate.IssuedAt,
            certificate.CompletionDate);

        private static string GetCourseTitle(Course course)
        {
            var translation = course.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.EN)
                ?? course.Translations.FirstOrDefault();

            return translation?.Title ?? "—";
        }
    }

}
