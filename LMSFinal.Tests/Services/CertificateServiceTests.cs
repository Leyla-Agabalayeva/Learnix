using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Certificates;
using LMSFinal.Application.Services;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Tests.Common;
using Moq;

namespace LMSFinal.Tests.Services
{
   
    public class CertificateServiceTests
    {
        private readonly Mock<ICertificateRepository> _certificates = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly Mock<IQuizAttemptRepository> _attempts = new();
        private readonly Mock<ICertificatePdfGenerator> _pdf = new();
        private readonly Mock<INotificationService> _notifications = new();
        private readonly Mock<IFileStorageService> _fileStorage = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private readonly ApplicationUser _student = TestData.User("Leyla", "Agabalayeva");
        private readonly Course _course;

        public CertificateServiceTests()
        {
            _course = TestData.Course(Guid.NewGuid(), title: "C# Fundamentals");
        }

        private CertificateService CreateSut() => new(
            _certificates.Object, _enrollments.Object, _quizzes.Object, _attempts.Object,
            _pdf.Object, _notifications.Object, _fileStorage.Object, _unitOfWork.Object);

        private void GivenIssuanceSucceeds()
        {
            _certificates
                .Setup(r => r.ExistsAsync(_student.Id, _course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _certificates.Setup(r => r.CountByYearAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

            Certificate? saved = null;
            _certificates
                .Setup(r => r.AddAsync(It.IsAny<Certificate>(), It.IsAny<CancellationToken>()))
                .Callback<Certificate, CancellationToken>((certificate, _) =>
                {
                    certificate.Student = _student;
                    certificate.Course = _course;
                    saved = certificate;
                })
                .Returns(Task.CompletedTask);

            _certificates
                .Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => saved);
        }

        private void GivenEnrollment(EnrollmentStatus status, double progress) =>
            _enrollments
                .Setup(r => r.GetAsync(_student.Id, _course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.Enrollment(_student.Id, _course.Id, status, progress));

        private void GivenCourseQuizzes(params Quiz[] quizzes) =>
            _quizzes
                .Setup(r => r.GetByCourseIdAsync(_course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(quizzes);

        private void GivenStudentAttempts(params QuizAttempt[] attempts) =>
            _attempts
                .Setup(r => r.GetByStudentIdAsync(_student.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(attempts);

        // ------------------------------------------------------------------
        // Условия выдачи
        // ------------------------------------------------------------------

        [Fact]
        public async Task IssueIfEligibleAsync_CourseCompletedAndNoQuizzes_IssuesCertificate()
        {
            GivenEnrollment(EnrollmentStatus.Completed, 100);
            GivenCourseQuizzes();
            GivenIssuanceSucceeds();

            var result = await CreateSut().IssueIfEligibleAsync(_student.Id, _course.Id);

            Assert.NotNull(result);
            Assert.Equal("Leyla Agabalayeva", result!.StudentName);
            Assert.Equal("C# Fundamentals", result.CourseTitle);
        }

        [Fact]
        public async Task IssueIfEligibleAsync_CourseNotFinished_IssuesNothing()
        {
            GivenEnrollment(EnrollmentStatus.Active, 80);

            var result = await CreateSut().IssueIfEligibleAsync(_student.Id, _course.Id);

            Assert.Null(result);
            _certificates.Verify(r => r.AddAsync(It.IsAny<Certificate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task IssueIfEligibleAsync_StudentNotEnrolled_IssuesNothing()
        {
            _enrollments
                .Setup(r => r.GetAsync(_student.Id, _course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Enrollment?)null);

            var result = await CreateSut().IssueIfEligibleAsync(_student.Id, _course.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task IssueIfEligibleAsync_AllLessonsDoneButAQuizNotPassed_IssuesNothing()
        {
            TestData.CourseWithLessons(Guid.NewGuid(), 1, out var lessons);
            var quiz = TestData.Quiz(lessons[0]);

            GivenEnrollment(EnrollmentStatus.Completed, 100);
            GivenCourseQuizzes(quiz);
            GivenStudentAttempts(new QuizAttempt { QuizId = quiz.Id, StudentId = _student.Id, Passed = false, Percentage = 40 });
            _certificates.Setup(r => r.ExistsAsync(_student.Id, _course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await CreateSut().IssueIfEligibleAsync(_student.Id, _course.Id);

            Assert.Null(result);
            _certificates.Verify(r => r.AddAsync(It.IsAny<Certificate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task IssueIfEligibleAsync_AllQuizzesPassed_IssuesCertificate()
        {
            TestData.CourseWithLessons(Guid.NewGuid(), 2, out var lessons);
            var firstQuiz = TestData.Quiz(lessons[0]);
            var secondQuiz = TestData.Quiz(lessons[1]);

            GivenEnrollment(EnrollmentStatus.Completed, 100);
            GivenCourseQuizzes(firstQuiz, secondQuiz);
            GivenStudentAttempts(
                new QuizAttempt { QuizId = firstQuiz.Id, StudentId = _student.Id, Passed = false },
                new QuizAttempt { QuizId = firstQuiz.Id, StudentId = _student.Id, Passed = true },
                new QuizAttempt { QuizId = secondQuiz.Id, StudentId = _student.Id, Passed = true });
            GivenIssuanceSucceeds();

            var result = await CreateSut().IssueIfEligibleAsync(_student.Id, _course.Id);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task IssueIfEligibleAsync_CalledTwice_DoesNotIssueASecondCertificate()
        {
            _certificates
                .Setup(r => r.ExistsAsync(_student.Id, _course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await CreateSut().IssueIfEligibleAsync(_student.Id, _course.Id);

            Assert.Null(result);
            _certificates.Verify(r => r.AddAsync(It.IsAny<Certificate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------------
        // Номер сертификата и уведомление
        // ------------------------------------------------------------------

        [Fact]
        public async Task IssueIfEligibleAsync_GeneratesSequentialNumberInTheExpectedFormat()
        {
            GivenEnrollment(EnrollmentStatus.Completed, 100);
            GivenCourseQuizzes();
            GivenIssuanceSucceeds();
            _certificates.Setup(r => r.CountByYearAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(41);

            var result = await CreateSut().IssueIfEligibleAsync(_student.Id, _course.Id);

            Assert.NotNull(result);
            Assert.Equal($"LMS-{DateTime.UtcNow.Year}-000042", result!.CertificateNumber);
        }

        [Fact]
        public async Task IssueIfEligibleAsync_NotifiesTheStudent()
        {
            GivenEnrollment(EnrollmentStatus.Completed, 100);
            GivenCourseQuizzes();
            GivenIssuanceSucceeds();

            await CreateSut().IssueIfEligibleAsync(_student.Id, _course.Id);

            _notifications.Verify(n => n.NotifyAsync(
                _student.Id, It.IsAny<string>(), It.IsAny<string>(),
                NotificationType.CertificateIssued, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------------
        // Доступ к чужим сертификатам
        // ------------------------------------------------------------------

        [Fact]
        public async Task GetByIdAsync_CertificateOfAnotherStudent_ThrowsForbidden()
        {
            var certificate = TestData.Certificate(_student, _course);
            _certificates
                .Setup(r => r.GetWithDetailsAsync(certificate.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(certificate);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().GetByIdAsync(Guid.NewGuid(), certificate.Id));
        }

        [Fact]
        public async Task GeneratePdfAsync_CertificateOfAnotherStudent_ThrowsForbiddenAndGeneratesNothing()
        {
            var certificate = TestData.Certificate(_student, _course);
            _certificates
                .Setup(r => r.GetWithDetailsAsync(certificate.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(certificate);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().GeneratePdfAsync(Guid.NewGuid(), certificate.Id));

            _pdf.Verify(p => p.Generate(It.IsAny<CertificateDto>()), Times.Never);
        }

        [Fact]
        public async Task VerifyAsync_UnknownNumber_ThrowsNotFound()
        {
            _certificates
                .Setup(r => r.GetByCertificateNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Certificate?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().VerifyAsync("LMS-2026-999999"));
        }

        [Fact]
        public async Task VerifyAsync_ExistingNumber_ReturnsCertificateWithoutAuthentication()
        {
            var certificate = TestData.Certificate(_student, _course);
            _certificates
                .Setup(r => r.GetByCertificateNumberAsync(certificate.CertificateNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(certificate);

            var result = await CreateSut().VerifyAsync(certificate.CertificateNumber);

            Assert.Equal(certificate.CertificateNumber, result.CertificateNumber);
            Assert.Equal("Leyla Agabalayeva", result.StudentName);
        }

        [Fact]
        public async Task VerifyAsync_ReturnsOnlyWhatIsPrintedOnTheCertificate()
        {
            var certificate = TestData.Certificate(_student, _course);
            _certificates
                .Setup(r => r.GetByCertificateNumberAsync(certificate.CertificateNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(certificate);

            CertificateVerificationDto result = await CreateSut().VerifyAsync(certificate.CertificateNumber);

            var exposed = typeof(CertificateVerificationDto)
                .GetProperties()
                .Select(p => p.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    nameof(CertificateVerificationDto.CertificateNumber),
                    nameof(CertificateVerificationDto.CompletionDate),
                    nameof(CertificateVerificationDto.CourseTitle),
                    nameof(CertificateVerificationDto.InstructorName),
                    nameof(CertificateVerificationDto.IssuedAt),
                    nameof(CertificateVerificationDto.StudentName)
                }.OrderBy(name => name),
                exposed);
            Assert.Equal(_course.Translations.First().Title, result.CourseTitle);
        }

        [Fact]
        public async Task GetByIdAsync_ForeignCertificate_ThrowsForbidden()
        {
            var certificate = TestData.Certificate(_student, _course);
            _certificates
                .Setup(r => r.GetWithDetailsAsync(certificate.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(certificate);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().GetByIdAsync(Guid.NewGuid(), certificate.Id));
        }
    }
}
