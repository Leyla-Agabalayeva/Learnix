using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Application.Services;
using LMSFinal.Contracts.DTOs.Courses;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Tests.Common;
using Moq;

namespace LMSFinal.Tests.Services
{

    public class CourseServiceTests
    {
        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IWishlistRepository> _wishlist = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<INotificationService> _notifications = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private readonly Guid _instructorId = Guid.NewGuid();
        private readonly Guid _otherInstructorId = Guid.NewGuid();

        private CourseService CreateSut() => new(
            _courses.Object, _categories.Object, _wishlist.Object, _enrollments.Object,
            _notifications.Object, _unitOfWork.Object, TestMapper.Create());

        private static CreateCourseRequest CreateRequest(Guid categoryId) => new()
        {
            CategoryId = categoryId,
            Price = 49.99m,
            DurationMinutes = 120,
            Level = CourseLevel.Beginner,
            Translations = new[]
            {
                new CourseTranslationInput(LanguageCode.EN, "New Course", "Short", "Description", new[] { "First" })
            }
        };

        // ------------------------------------------------------------------
        // Создание
        // ------------------------------------------------------------------

        [Fact]
        public async Task CreateAsync_AssignsInstructorFromCaller_AndStartsAsDraft()
        {
            var category = TestData.Category();
            _categories.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

            Course? saved = null;
            _courses
                .Setup(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()))
                .Callback<Course, CancellationToken>((course, _) => saved = course)
                .Returns(Task.CompletedTask);
            _courses
                .Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => saved);

            await CreateSut().CreateAsync(_instructorId, CreateRequest(category.Id));

            Assert.NotNull(saved);
            Assert.Equal(_instructorId, saved!.InstructorId);
            Assert.Equal(CourseStatus.Draft, saved.Status);
            Assert.Equal("New Course", Assert.Single(saved.Translations).Title);
        }

        [Fact]
        public async Task CreateAsync_UnknownCategory_ThrowsNotFound()
        {
            _categories
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => CreateSut().CreateAsync(_instructorId, CreateRequest(Guid.NewGuid())));

            _courses.Verify(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------------
        // Проверка владения (раздел 52 ТЗ)
        // ------------------------------------------------------------------

        [Fact]
        public async Task UpdateAsync_CourseOfAnotherInstructor_ThrowsForbidden()
        {
            var course = TestData.Course(_otherInstructorId);
            _courses.Setup(r => r.GetWithDetailsAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().UpdateAsync(_instructorId, course.Id, new UpdateCourseRequest()));

            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_CourseOfAnotherInstructor_ThrowsForbiddenAndDeletesNothing()
        {
            var course = TestData.Course(_otherInstructorId);
            _courses.Setup(r => r.GetByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().DeleteAsync(_instructorId, course.Id));

            _courses.Verify(r => r.Remove(It.IsAny<Course>()), Times.Never);
        }

        [Fact]
        public async Task PublishAsync_CourseOfAnotherInstructor_ThrowsForbidden()
        {
            var course = TestData.Course(_otherInstructorId, CourseStatus.Draft);
            _courses.Setup(r => r.GetWithDetailsAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().PublishAsync(_instructorId, course.Id));
        }

        // ------------------------------------------------------------------
        // Публикация
        // ------------------------------------------------------------------

        [Fact]
        public async Task PublishAsync_OwnCourse_SetsStatusPublished()
        {
            var course = TestData.Course(_instructorId, CourseStatus.Draft);
            _courses.Setup(r => r.GetWithDetailsAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
            _wishlist.Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Wishlist>());

            await CreateSut().PublishAsync(_instructorId, course.Id);

            Assert.Equal(CourseStatus.Published, course.Status);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PublishAsync_CourseWithoutTitle_ThrowsConflict()
        {
            var course = TestData.Course(_instructorId, CourseStatus.Draft);
            course.Translations.Clear();

            _courses.Setup(r => r.GetWithDetailsAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            await Assert.ThrowsAsync<ConflictException>(() => CreateSut().PublishAsync(_instructorId, course.Id));

            Assert.Equal(CourseStatus.Draft, course.Status);
        }

        [Fact]
        public async Task PublishAsync_NotifiesEveryStudentWhoWishlistedTheCourse()
        {
            var course = TestData.Course(_instructorId, CourseStatus.Draft);
            var firstStudent = Guid.NewGuid();
            var secondStudent = Guid.NewGuid();

            _courses.Setup(r => r.GetWithDetailsAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
            _wishlist
                .Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Wishlist { StudentId = firstStudent, CourseId = course.Id },
                    new Wishlist { StudentId = secondStudent, CourseId = course.Id }
                });

            await CreateSut().PublishAsync(_instructorId, course.Id);

            _notifications.Verify(n => n.NotifyAsync(
                firstStudent, It.IsAny<string>(), It.IsAny<string>(),
                NotificationType.CoursePublished, It.IsAny<CancellationToken>()), Times.Once);

            _notifications.Verify(n => n.NotifyAsync(
                secondStudent, It.IsAny<string>(), It.IsAny<string>(),
                NotificationType.CoursePublished, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------------
        // Видимость черновиков
        // ------------------------------------------------------------------

        [Fact]
        public async Task GetByIdAsync_DraftCourse_IsHiddenFromStrangersAsNotFound()
        {
            var course = TestData.Course(_otherInstructorId, CourseStatus.Draft);
            _courses.Setup(r => r.GetWithDetailsAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            await Assert.ThrowsAsync<NotFoundException>(
                () => CreateSut().GetByIdAsync(course.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task GetByIdAsync_DraftCourse_IsVisibleToItsOwner()
        {
            var course = TestData.Course(_instructorId, CourseStatus.Draft);
            _courses.Setup(r => r.GetWithDetailsAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            var result = await CreateSut().GetByIdAsync(course.Id, _instructorId);

            Assert.Equal(course.Id, result.Id);
        }

        // ------------------------------------------------------------------
        // Публичная страница курса: программа, агрегаты, признак записи
        // ------------------------------------------------------------------

        private Course GivenCourseWithCurriculum(Guid instructorId, CourseStatus status = CourseStatus.Published)
        {
            var course = TestData.CourseWithLessons(instructorId, 3, out _);
            course.Status = status;

            _courses
                .Setup(r => r.GetWithCurriculumAsync(course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);
            _courses
                .Setup(r => r.GetStatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Guid, CourseStats> { [course.Id] = new(4.5, 2, 7, 3) });

            return course;
        }

        [Fact]
        public async Task GetByIdLocalizedAsync_DraftCourse_IsHiddenFromStrangersAsNotFound()
        {
            // Та же защита, что и в GetByIdAsync, но это отдельный метод с другим
            // запросом к репозиторию — без своего теста регрессия здесь прошла бы незаметно.
            var course = GivenCourseWithCurriculum(_otherInstructorId, CourseStatus.Draft);

            await Assert.ThrowsAsync<NotFoundException>(
                () => CreateSut().GetByIdLocalizedAsync(course.Id, Guid.NewGuid(), LanguageCode.EN));
        }

        [Fact]
        public async Task GetByIdLocalizedAsync_ReturnsCurriculumAndAggregates()
        {
            var course = GivenCourseWithCurriculum(_instructorId);

            var result = await CreateSut().GetByIdLocalizedAsync(course.Id, null, LanguageCode.EN);

            var module = Assert.Single(result.Curriculum);
            Assert.Equal(3, module.Lessons.Count);
            Assert.Equal(4.5, result.AverageRating);
            Assert.Equal(7, result.EnrollmentCount);
        }

        [Fact]
        public async Task GetByIdLocalizedAsync_UnpublishedLessonsAreNotShownInTheCurriculum()
        {
            var course = TestData.CourseWithLessons(_instructorId, 3, out var lessons);
            lessons[2].IsPublished = false;

            _courses.Setup(r => r.GetWithCurriculumAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
            _courses
                .Setup(r => r.GetStatsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Guid, CourseStats>());

            var result = await CreateSut().GetByIdLocalizedAsync(course.Id, null, LanguageCode.EN);

            Assert.Equal(2, Assert.Single(result.Curriculum).Lessons.Count);
        }

        [Fact]
        public async Task GetByIdLocalizedAsync_GuestIsNeverMarkedAsEnrolled()
        {
            var course = GivenCourseWithCurriculum(_instructorId);

            var result = await CreateSut().GetByIdLocalizedAsync(course.Id, currentUserId: null, LanguageCode.EN);

            Assert.False(result.IsEnrolled);
            _enrollments.Verify(
                r => r.IsEnrolledAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByIdLocalizedAsync_EnrolledStudent_IsMarkedAsEnrolled()
        {
            var course = GivenCourseWithCurriculum(_instructorId);
            var studentId = Guid.NewGuid();

            _enrollments
                .Setup(r => r.IsEnrolledAsync(studentId, course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await CreateSut().GetByIdLocalizedAsync(course.Id, studentId, LanguageCode.EN);

            Assert.True(result.IsEnrolled);
        }

        // ------------------------------------------------------------------
        // Удаление курса (Phase 30 — закрыт TODO из ранних фаз)
        // ------------------------------------------------------------------

        [Fact]
        public async Task DeleteAsync_CourseHasEnrollments_ThrowsConflict()
        {
            var course = TestData.Course(_instructorId);
            _courses.Setup(r => r.GetByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
            _enrollments
                .Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Enrollment> { TestData.Enrollment(Guid.NewGuid(), course.Id) });

            await Assert.ThrowsAsync<ConflictException>(
                () => CreateSut().DeleteAsync(_instructorId, course.Id));

            _courses.Verify(r => r.Remove(It.IsAny<Course>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_NoEnrollments_RemovesCourse()
        {
            var course = TestData.Course(_instructorId);
            _courses.Setup(r => r.GetByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);
            _enrollments
                .Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Enrollment>());

            await CreateSut().DeleteAsync(_instructorId, course.Id);

            _courses.Verify(r => r.Remove(course), Times.Once);
        }
    }
}
