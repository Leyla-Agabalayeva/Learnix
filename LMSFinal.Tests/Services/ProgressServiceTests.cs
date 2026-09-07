using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Application.Services;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Tests.Common;
using Moq;

namespace LMSFinal.Tests.Services
{
    public class ProgressServiceTests
    {
        private readonly Mock<ILessonProgressRepository> _progress = new();
        private readonly Mock<ILessonRepository> _lessons = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<ICertificateService> _certificates = new();
        private readonly Mock<INotificationService> _notifications = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private readonly Guid _studentId = Guid.NewGuid();

        private ProgressService CreateSut() => new(
            _progress.Object, _lessons.Object, _enrollments.Object,
            _certificates.Object, _notifications.Object, _unitOfWork.Object);
        private (Course Course, List<Lesson> Lessons, Enrollment Enrollment) GivenCourse(
            int lessonCount, int alreadyCompleted, EnrollmentStatus status = EnrollmentStatus.Active)
        {
            var course = TestData.CourseWithLessons(Guid.NewGuid(), lessonCount, out var lessons);
            var enrollment = TestData.Enrollment(_studentId, course.Id, status);

            _lessons.Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lessons);
            _enrollments.Setup(r => r.IsEnrolledAsync(_studentId, course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _enrollments.Setup(r => r.GetAsync(_studentId, course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);

            var completed = lessons.Take(alreadyCompleted).Select(l => TestData.Progress(_studentId, l.Id)).ToList();
            _progress
                .Setup(r => r.GetByStudentAndCourseAsync(_studentId, course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(completed);

            foreach (var lesson in lessons)
            {
                _lessons.Setup(r => r.GetWithModuleAndCourseAsync(lesson.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lesson);
            }

            return (course, lessons, enrollment);
        }

        // ------------------------------------------------------------------
        // Формула прогресса: пройдено / всего опубликованных * 100
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(4, 1, 25.0)]
        [InlineData(4, 2, 50.0)]
        [InlineData(4, 4, 100.0)]
        [InlineData(5, 4, 80.0)]
        [InlineData(3, 1, 33.33)]   
        [InlineData(3, 2, 66.67)]
        [InlineData(7, 3, 42.86)]
        public async Task GetCourseProgressAsync_ReturnsCompletedDividedByTotal(
            int totalLessons, int completedLessons, double expectedPercentage)
        {
            var (course, _, _) = GivenCourse(totalLessons, completedLessons);

            var result = await CreateSut().GetCourseProgressAsync(_studentId, course.Id);

            Assert.Equal(totalLessons, result.TotalLessons);
            Assert.Equal(completedLessons, result.CompletedLessons);
            Assert.Equal(expectedPercentage, result.ProgressPercentage);
        }

        [Fact]
        public async Task GetCourseProgressAsync_UnpublishedLessonsDoNotCountTowardTheTotal()
        {
         
            var (course, lessons, _) = GivenCourse(4, 2);
            lessons[3].IsPublished = false;

            var result = await CreateSut().GetCourseProgressAsync(_studentId, course.Id);

            Assert.Equal(3, result.TotalLessons);
            Assert.Equal(2, result.CompletedLessons);
            Assert.Equal(66.67, result.ProgressPercentage);
        }

        [Fact]
        public async Task GetCourseProgressAsync_StudentIsNotEnrolled_ThrowsForbidden()
        {
            var course = TestData.CourseWithLessons(Guid.NewGuid(), 3, out _);
            _enrollments
                .Setup(r => r.GetAsync(_studentId, course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Enrollment?)null);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().GetCourseProgressAsync(_studentId, course.Id));
        }

        // ------------------------------------------------------------------
        // Список пройденных уроков  — для галочек в боковой панели
        // страницы урока. 
        // ------------------------------------------------------------------

        [Fact]
        public async Task GetCourseProgressAsync_ReturnsIdsOfCompletedLessons()
        {
            var (course, lessons, _) = GivenCourse(5, 3);

            var result = await CreateSut().GetCourseProgressAsync(_studentId, course.Id);

            Assert.Equal(
                lessons.Take(3).Select(l => l.Id).OrderBy(id => id),
                result.CompletedLessonIds.OrderBy(id => id));
        }

        [Fact]
        public async Task GetCourseProgressAsync_CompletedIdsMatchCompletedCount()
        {
    
            var (course, _, _) = GivenCourse(7, 4);

            var result = await CreateSut().GetCourseProgressAsync(_studentId, course.Id);

            Assert.Equal(result.CompletedLessons, result.CompletedLessonIds.Count);
        }

        [Fact]
        public async Task GetCourseProgressAsync_UnpublishedLessonIsExcludedFromCompletedIds()
        {
         
            var (course, lessons, _) = GivenCourse(4, 3);
            lessons[2].IsPublished = false;

            var result = await CreateSut().GetCourseProgressAsync(_studentId, course.Id);

            Assert.Equal(3, result.TotalLessons);
            Assert.Equal(2, result.CompletedLessons);
            Assert.DoesNotContain(lessons[2].Id, result.CompletedLessonIds);
        }

        [Fact]
        public async Task GetCourseProgressAsync_NothingCompleted_ReturnsEmptyListNotNull()
        {
            var (course, _, _) = GivenCourse(4, 0);

            var result = await CreateSut().GetCourseProgressAsync(_studentId, course.Id);

            Assert.NotNull(result.CompletedLessonIds);
            Assert.Empty(result.CompletedLessonIds);
        }

        [Fact]
        public async Task GetCourseProgressAsync_CourseWithoutLessons_ReturnsEmptyListNotNull()
        {
            var (course, _, _) = GivenCourse(0, 0);

            var result = await CreateSut().GetCourseProgressAsync(_studentId, course.Id);

            Assert.Equal(0, result.TotalLessons);
            Assert.NotNull(result.CompletedLessonIds);
            Assert.Empty(result.CompletedLessonIds);
        }

        // ------------------------------------------------------------------
        // Отметка урока пройденным
        // ------------------------------------------------------------------

        [Fact]
        public async Task MarkCompleteAsync_FirstTime_CreatesCompletedProgressRecord()
        {
            var (_, lessons, _) = GivenCourse(4, 0);
            _progress
                .Setup(r => r.GetAsync(_studentId, lessons[0].Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LessonProgress?)null);

            LessonProgress? saved = null;
            _progress
                .Setup(r => r.AddAsync(It.IsAny<LessonProgress>(), It.IsAny<CancellationToken>()))
                .Callback<LessonProgress, CancellationToken>((progress, _) => saved = progress)
                .Returns(Task.CompletedTask);

            var result = await CreateSut().MarkCompleteAsync(_studentId, lessons[0].Id);

            Assert.NotNull(saved);
            Assert.True(saved!.IsCompleted);
            Assert.NotNull(saved.CompletedAt);
            Assert.True(result.IsCompleted);
        }

        [Fact]
        public async Task MarkCompleteAsync_CalledTwice_KeepsTheOriginalCompletionDate()
        {
            var (_, lessons, _) = GivenCourse(4, 1);
            var originalDate = DateTime.UtcNow.AddDays(-5);
            var existing = TestData.Progress(_studentId, lessons[0].Id);
            existing.CompletedAt = originalDate;

            _progress
                .Setup(r => r.GetAsync(_studentId, lessons[0].Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            await CreateSut().MarkCompleteAsync(_studentId, lessons[0].Id);

            Assert.Equal(originalDate, existing.CompletedAt);
            _progress.Verify(r => r.AddAsync(It.IsAny<LessonProgress>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MarkCompleteAsync_StudentIsNotEnrolled_ThrowsForbidden()
        {
            var course = TestData.CourseWithLessons(Guid.NewGuid(), 2, out var lessons);
            _lessons.Setup(r => r.GetWithModuleAndCourseAsync(lessons[0].Id, It.IsAny<CancellationToken>())).ReturnsAsync(lessons[0]);
            _enrollments.Setup(r => r.IsEnrolledAsync(_studentId, course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().MarkCompleteAsync(_studentId, lessons[0].Id));

            _progress.Verify(r => r.AddAsync(It.IsAny<LessonProgress>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MarkCompleteAsync_UnknownLesson_ThrowsNotFound()
        {
            _lessons
                .Setup(r => r.GetWithModuleAndCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Lesson?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => CreateSut().MarkCompleteAsync(_studentId, Guid.NewGuid()));
        }

        // ------------------------------------------------------------------
        // Переход курса в Completed
        // ------------------------------------------------------------------

        [Fact]
        public async Task MarkCompleteAsync_LastLesson_CompletesEnrollmentAndTriggersCertificateCheck()
        {
            var (course, lessons, enrollment) = GivenCourse(3, 2);
            _progress
                .Setup(r => r.GetByStudentAndCourseAsync(_studentId, course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lessons.Select(l => TestData.Progress(_studentId, l.Id)).ToList());
            _progress
                .Setup(r => r.GetAsync(_studentId, lessons[2].Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LessonProgress?)null);

            await CreateSut().MarkCompleteAsync(_studentId, lessons[2].Id);

            Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
            Assert.Equal(100, enrollment.ProgressPercentage);
            Assert.NotNull(enrollment.CompletedAt);

            _notifications.Verify(n => n.NotifyAsync(
                _studentId, It.IsAny<string>(), It.IsAny<string>(),
                NotificationType.CourseCompleted, It.IsAny<CancellationToken>()), Times.Once);
            _certificates.Verify(c => c.IssueIfEligibleAsync(_studentId, course.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MarkCompleteAsync_OnAnAlreadyCompletedCourse_DoesNotSendTheNotificationAgain()
        {
            var (course, lessons, _) = GivenCourse(3, 3, EnrollmentStatus.Completed);

            _progress
                .Setup(r => r.GetAsync(_studentId, lessons[2].Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.Progress(_studentId, lessons[2].Id));

            await CreateSut().MarkCompleteAsync(_studentId, lessons[2].Id);

            _notifications.Verify(n => n.NotifyAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                NotificationType.CourseCompleted, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MarkCompleteAsync_NotTheLastLesson_LeavesEnrollmentActive()
        {
            var (_, lessons, enrollment) = GivenCourse(4, 1);
            _progress
                .Setup(r => r.GetAsync(_studentId, lessons[1].Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LessonProgress?)null);

            await CreateSut().MarkCompleteAsync(_studentId, lessons[1].Id);

            Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
            Assert.Null(enrollment.CompletedAt);
        }
    }
}
