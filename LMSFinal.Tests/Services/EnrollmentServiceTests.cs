using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Services;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Tests.Common;
using Moq;

namespace LMSFinal.Tests.Services
{
    public class EnrollmentServiceTests
    {
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private readonly Guid _studentId = Guid.NewGuid();

        private EnrollmentService CreateSut() =>
            new(_enrollments.Object, _courses.Object, _unitOfWork.Object, TestMapper.Create());

        private Course GivenCourse(CourseStatus status = CourseStatus.Published)
        {
            var course = TestData.Course(Guid.NewGuid(), status);
            _courses.Setup(r => r.GetWithDetailsAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
            return course;
        }

        [Fact]
        public async Task EnrollAsync_PublishedCourseFirstTime_CreatesActiveEnrollmentWithZeroProgress()
        {
            var course = GivenCourse();
            _enrollments.Setup(r => r.GetAsync(_studentId, course.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Enrollment?)null);

            Enrollment? saved = null;
            _enrollments
                .Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
                .Callback<Enrollment, CancellationToken>((enrollment, _) => saved = enrollment)
                .Returns(Task.CompletedTask);

            await CreateSut().EnrollAsync(_studentId, course.Id);

            Assert.NotNull(saved);
            Assert.Equal(_studentId, saved!.StudentId);
            Assert.Equal(course.Id, saved.CourseId);
            Assert.Equal(EnrollmentStatus.Active, saved.Status);
            Assert.Equal(0, saved.ProgressPercentage);
        }

        [Theory]
        [InlineData(CourseStatus.Draft)]
        [InlineData(CourseStatus.Archived)]
        public async Task EnrollAsync_CourseIsNotPublished_ThrowsConflict(CourseStatus status)
        {
            var course = GivenCourse(status);

            await Assert.ThrowsAsync<ConflictException>(() => CreateSut().EnrollAsync(_studentId, course.Id));

            _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EnrollAsync_UnknownCourse_ThrowsNotFound()
        {
            _courses
                .Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().EnrollAsync(_studentId, Guid.NewGuid()));
        }

        [Fact]
        public async Task EnrollAsync_AlreadyActivelyEnrolled_ThrowsConflict()
        {
            var course = GivenCourse();
            _enrollments
                .Setup(r => r.GetAsync(_studentId, course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.Enrollment(_studentId, course.Id));

            await Assert.ThrowsAsync<ConflictException>(() => CreateSut().EnrollAsync(_studentId, course.Id));

            _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EnrollAsync_AlreadyCompleted_ThrowsConflict()
        {
            var course = GivenCourse();
            _enrollments
                .Setup(r => r.GetAsync(_studentId, course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.Enrollment(_studentId, course.Id, EnrollmentStatus.Completed, 100));

            await Assert.ThrowsAsync<ConflictException>(() => CreateSut().EnrollAsync(_studentId, course.Id));
        }

        [Fact]
        public async Task EnrollAsync_PreviouslyCancelled_ReactivatesTheSameRowInsteadOfInserting()
        {
            var course = GivenCourse();
            var cancelled = TestData.Enrollment(_studentId, course.Id, EnrollmentStatus.Cancelled, 40);

            _enrollments.Setup(r => r.GetAsync(_studentId, course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cancelled);

            await CreateSut().EnrollAsync(_studentId, course.Id);

            Assert.Equal(EnrollmentStatus.Active, cancelled.Status);
            Assert.Null(cancelled.CompletedAt);
            _enrollments.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
            _enrollments.Verify(r => r.Update(cancelled), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_EnrollmentOfAnotherStudent_ThrowsForbidden()
        {
            var enrollment = TestData.Enrollment(Guid.NewGuid(), Guid.NewGuid());
            _enrollments.Setup(r => r.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().CancelAsync(_studentId, enrollment.Id));

            Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
        }

        [Fact]
        public async Task GetCourseStudentsAsync_CourseOfAnotherInstructor_ThrowsForbidden()
        {
            var course = TestData.Course(Guid.NewGuid());
            _courses.Setup(r => r.GetByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().GetCourseStudentsAsync(Guid.NewGuid(), course.Id));
        }
    }
}
