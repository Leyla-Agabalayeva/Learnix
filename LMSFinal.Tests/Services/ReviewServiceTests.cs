using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Services;
using LMSFinal.Contracts.DTOs.Reviews;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Tests.Common;
using Moq;

namespace LMSFinal.Tests.Services
{
    public class ReviewServiceTests
    {
        private readonly Mock<ICourseReviewRepository> _reviews = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private readonly Guid _studentId = Guid.NewGuid();
        private readonly Guid _otherStudentId = Guid.NewGuid();
        private readonly Guid _courseId = Guid.NewGuid();

        private ReviewService CreateSut() =>
            new(_reviews.Object, _enrollments.Object, _courses.Object, _unitOfWork.Object);

        private static CreateReviewRequest Request(int rating = 5, string? comment = "Great course") =>
            new() { Rating = rating, Comment = comment };

        private void GivenEnrollment(EnrollmentStatus status = EnrollmentStatus.Active) =>
            _enrollments
                .Setup(r => r.GetAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.Enrollment(_studentId, _courseId, status));

        private CourseReview GivenExistingReview(Guid ownerId)
        {
            var review = new CourseReview
            {
                CourseId = _courseId,
                StudentId = ownerId,
                Rating = 4,
                Comment = "Original comment",
                Student = TestData.User(id: ownerId)
            };

            _reviews.Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>())).ReturnsAsync(review);
            return review;
        }

        // ------------------------------------------------------------------
        // Кто может оставить отзыв
        // ------------------------------------------------------------------

        [Fact]
        public async Task CreateAsync_EnrolledStudent_CreatesReview()
        {
            GivenEnrollment();
            _reviews
                .Setup(r => r.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseReview?)null);

            CourseReview? saved = null;
            _reviews
                .Setup(r => r.AddAsync(It.IsAny<CourseReview>(), It.IsAny<CancellationToken>()))
                .Callback<CourseReview, CancellationToken>((review, _) =>
                {
                    review.Student = TestData.User(id: _studentId);
                    saved = review;
                })
                .Returns(Task.CompletedTask);

            _reviews
                .SetupSequence(r => r.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseReview?)null)
                .ReturnsAsync(() => saved);

            var result = await CreateSut().CreateAsync(_studentId, _courseId, Request(rating: 5));

            Assert.NotNull(saved);
            Assert.Equal(5, saved!.Rating);
            Assert.Equal(_studentId, saved.StudentId);
            Assert.Equal(5, result.Rating);
        }

        [Fact]
        public async Task CreateAsync_StudentIsNotEnrolled_ThrowsForbidden()
        {
            // Без этой проверки рейтинг курса можно было бы накрутить аккаунтами,
            // которые курс даже не открывали.
            _enrollments
                .Setup(r => r.GetAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Enrollment?)null);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().CreateAsync(_studentId, _courseId, Request()));

            _reviews.Verify(r => r.AddAsync(It.IsAny<CourseReview>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_EnrollmentWasCancelled_ThrowsForbidden()
        {
            GivenEnrollment(EnrollmentStatus.Cancelled);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().CreateAsync(_studentId, _courseId, Request()));
        }

        [Fact]
        public async Task CreateAsync_CompletedCourse_IsAllowed()
        {
            GivenEnrollment(EnrollmentStatus.Completed);
            _reviews
                .SetupSequence(r => r.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseReview?)null)
                .ReturnsAsync(new CourseReview
                {
                    CourseId = _courseId,
                    StudentId = _studentId,
                    Rating = 5,
                    Student = TestData.User(id: _studentId)
                });

            var result = await CreateSut().CreateAsync(_studentId, _courseId, Request());

            Assert.Equal(5, result.Rating);
        }

        [Fact]
        public async Task CreateAsync_SecondReviewOnTheSameCourse_ThrowsConflict()
        {
            GivenEnrollment();
            _reviews
                .Setup(r => r.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseReview { CourseId = _courseId, StudentId = _studentId, Rating = 4 });

            await Assert.ThrowsAsync<ConflictException>(
                () => CreateSut().CreateAsync(_studentId, _courseId, Request()));

            _reviews.Verify(r => r.AddAsync(It.IsAny<CourseReview>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------------
        // Кто может править и удалять
        // ------------------------------------------------------------------

        [Fact]
        public async Task UpdateAsync_ReviewOfAnotherStudent_ThrowsForbiddenAndChangesNothing()
        {
            var review = GivenExistingReview(_otherStudentId);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().UpdateAsync(_studentId, review.Id, new UpdateReviewRequest { Rating = 1, Comment = "Hacked" }));

            Assert.Equal(4, review.Rating);
            Assert.Equal("Original comment", review.Comment);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_OwnReview_UpdatesRatingAndComment()
        {
            var review = GivenExistingReview(_studentId);
            _reviews
                .Setup(r => r.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(review);

            var result = await CreateSut().UpdateAsync(
                _studentId, review.Id, new UpdateReviewRequest { Rating = 2, Comment = "Changed my mind" });

            Assert.Equal(2, review.Rating);
            Assert.Equal("Changed my mind", review.Comment);
            Assert.NotNull(review.UpdatedAt);
            Assert.Equal(2, result.Rating);
        }

        [Fact]
        public async Task DeleteAsync_ReviewOfAnotherStudent_ThrowsForbiddenAndDeletesNothing()
        {
            var review = GivenExistingReview(_otherStudentId);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().DeleteAsync(_studentId, review.Id));

            _reviews.Verify(r => r.Remove(It.IsAny<CourseReview>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_OwnReview_RemovesIt()
        {
            var review = GivenExistingReview(_studentId);

            await CreateSut().DeleteAsync(_studentId, review.Id);

            _reviews.Verify(r => r.Remove(review), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UnknownReview_ThrowsNotFound()
        {
            _reviews
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseReview?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => CreateSut().UpdateAsync(_studentId, Guid.NewGuid(), new UpdateReviewRequest { Rating = 5 }));
        }
    }
}
