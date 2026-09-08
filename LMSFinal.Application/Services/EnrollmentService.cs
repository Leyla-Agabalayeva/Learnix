using AutoMapper;
using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Enrollments;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LMSFinal.Application.Common;

namespace LMSFinal.Application.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public EnrollmentService(
            IEnrollmentRepository enrollmentRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService)
        {
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<StudentEnrollmentDto> EnrollAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetWithDetailsAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            if (course.Status != CourseStatus.Published)
            {
                throw new ConflictException("Нельзя записаться на курс, который не опубликован.");
            }

            var existing = await _enrollmentRepository.GetAsync(studentId, courseId, cancellationToken);

            Enrollment enrollment;

            if (existing is null)
            {
                enrollment = new Enrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    Status = EnrollmentStatus.Active
                };

                await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            }
            else if (existing.Status == EnrollmentStatus.Active)
            {
                throw new ConflictException("Вы уже записаны на этот курс.");
            }
            else if (existing.Status == EnrollmentStatus.Completed)
            {
                throw new ConflictException("Вы уже завершили этот курс — повторная запись не требуется.");
            }
            else
            {
                // Status == Cancelled: реактивируем существующую строку вместо INSERT новой —
                // иначе нарушится unique index (StudentId, CourseId), раздел 49 ТЗ.
                existing.Status = EnrollmentStatus.Active;
                existing.EnrolledAt = DateTime.UtcNow;
                existing.CompletedAt = null;
                existing.ProgressPercentage = 0;

                _enrollmentRepository.Update(existing);
                enrollment = existing;
            }

            // course уже загружен с переводами одним запросом выше — переиспользуем его для
            // навигации, а не делаем ещё один SELECT ради DTO.
            enrollment.Course = course;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var courseTitle = TranslationResolver.Resolve(course.Translations, LanguageCode.EN, t => t.LanguageCode)?.Title
                ?? course.Id.ToString();

            await _notificationService.NotifyAsync(
                course.InstructorId,
                "New enrollment",
                $"A new student enrolled in \"{courseTitle}\".",
                NotificationType.NewEnrollment,
                cancellationToken);

            return _mapper.Map<StudentEnrollmentDto>(enrollment);
        }

        public async Task<IReadOnlyList<StudentEnrollmentDto>> GetMyEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var enrollments = await _enrollmentRepository.GetByStudentIdAsync(studentId, cancellationToken);
            return _mapper.Map<IReadOnlyList<StudentEnrollmentDto>>(enrollments);
        }

        public async Task<IReadOnlyList<StudentEnrollmentLocalizedDto>> GetMyEnrollmentsLocalizedAsync(
            Guid studentId, LanguageCode language, CancellationToken cancellationToken = default)
        {
            var enrollments = await _enrollmentRepository.GetByStudentIdAsync(studentId, cancellationToken);

            // Агрегаты одним запросом на весь список, а не на каждый курс.
            var stats = await CourseSummaryMapper.LoadStatsAsync(
                _courseRepository, enrollments.Select(e => e.Course), cancellationToken);

            return enrollments
                .Select(e => new StudentEnrollmentLocalizedDto(
                    e.Id,
                    e.EnrolledAt,
                    e.CompletedAt,
                    e.ProgressPercentage,
                    e.Status.ToString(),
                    CourseSummaryMapper.ToLocalized(e.Course, language, stats.GetValueOrDefault(e.CourseId))))
                .ToList();
        }

        public async Task<IReadOnlyList<CourseEnrollmentDto>> GetCourseStudentsAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            if (course.InstructorId != instructorId)
            {
                throw new ForbiddenAccessException("Вы можете просматривать студентов только своих курсов.");
            }

            var enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId, cancellationToken);
            return _mapper.Map<IReadOnlyList<CourseEnrollmentDto>>(enrollments);
        }

        public async Task<StudentEnrollmentDto> CancelAsync(Guid studentId, Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken)
                ?? throw new NotFoundException("Enrollment", enrollmentId);

            if (enrollment.StudentId != studentId)
            {
                throw new ForbiddenAccessException("Вы можете отменять только свои записи на курсы.");
            }

            if (enrollment.Status != EnrollmentStatus.Active)
            {
                throw new ConflictException("Отменить можно только активную запись на курс.");
            }

            enrollment.Status = EnrollmentStatus.Cancelled;
            _enrollmentRepository.Update(enrollment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var course = await _courseRepository.GetWithDetailsAsync(enrollment.CourseId, cancellationToken);
            enrollment.Course = course!;

            return _mapper.Map<StudentEnrollmentDto>(enrollment);
        }
    }

}
