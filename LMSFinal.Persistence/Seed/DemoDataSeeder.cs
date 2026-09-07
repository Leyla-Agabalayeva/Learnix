using System.Text.Json;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Persistence.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMSFinal.Persistence.Seed
{
    public sealed class DemoDataSeeder
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DemoDataSeeder> _logger;

        public DemoDataSeeder(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<DemoDataSeeder> logger)
        {
            _db = db;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (!_configuration.GetValue<bool>("Seed:EnableDemoData"))
            {
                _logger.LogInformation("Демо-данные отключены (Seed:EnableDemoData = false) — сидер пропущен.");
                return;
            }

            var password = _configuration["Seed:DemoPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Seed:DemoPassword не задан в конфигурации — демо-данные не создаются.");
                return;
            }

            if (await _db.Courses.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Курсы в базе уже есть — демо-данные не создаются повторно.");
                return;
            }

            var catalog = SeedCatalog.Load();

            var instructors = await EnsureUsersAsync(catalog.Instructors, nameof(UserRole.Instructor), password);
            var students = await EnsureUsersAsync(catalog.Students, nameof(UserRole.Student), password);

            var categories = BuildCategories(catalog.Categories);
            await _db.Categories.AddRangeAsync(categories.Values, cancellationToken);

            var courses = BuildCourses(catalog.Courses, categories, instructors);
            await _db.Courses.AddRangeAsync(courses, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            var existingCertificates = await _db.Certificates.CountAsync(cancellationToken);
            var activity = new DemoActivityBuilder().Build(courses, catalog.Students, students, existingCertificates);

            await _db.Enrollments.AddRangeAsync(activity.Enrollments, cancellationToken);
            await _db.LessonProgresses.AddRangeAsync(activity.LessonProgresses, cancellationToken);
            await _db.QuizAttempts.AddRangeAsync(activity.QuizAttempts, cancellationToken);
            await _db.CourseReviews.AddRangeAsync(activity.Reviews, cancellationToken);
            await _db.Certificates.AddRangeAsync(activity.Certificates, cancellationToken);
            await _db.Wishlists.AddRangeAsync(activity.WishlistItems, cancellationToken);
            await _db.Notifications.AddRangeAsync(activity.Notifications, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            LogSummary(courses, activity, instructors.Count, students.Count);
        }

        // ------------------------------------------------------------------
        // Пользователи
        // ------------------------------------------------------------------

        private async Task<IReadOnlyDictionary<string, ApplicationUser>> EnsureUsersAsync(
            IReadOnlyList<UserSpec> specs, string role, string password)
        {
            var result = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in specs)
            {
                var existing = await _userManager.FindByEmailAsync(spec.Email);

                if (existing is not null)
                {
                    result[spec.Email] = existing;
                    continue;
                }

                var user = new ApplicationUser
                {
                    UserName = spec.Email,
                    Email = spec.Email,
                    EmailConfirmed = true,
                    FirstName = spec.FirstName,
                    LastName = spec.LastName,
                    Bio = spec.Bio,
                    AvatarUrl = spec.AvatarUrl,
                    CreatedAt = DateTime.UtcNow.AddDays(-180)
                };

                var createResult = await _userManager.CreateAsync(user, password);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Не удалось создать демо-пользователя '{spec.Email}': {errors}");
                }

                await _userManager.AddToRoleAsync(user, role);
                result[spec.Email] = user;
            }

            return result;
        }

        // ------------------------------------------------------------------
        // Каталог
        // ------------------------------------------------------------------

        private static Dictionary<string, Category> BuildCategories(IReadOnlyList<CategorySpec> specs)
        {
            var categories = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in specs)
            {
                var category = new Category
                {
                    Slug = spec.Slug,
                    IconUrl = spec.IconUrl
                };

                foreach (var language in AllLanguages)
                {
                    category.Translations.Add(new CategoryTranslation
                    {
                        CategoryId = category.Id,
                        LanguageCode = language,
                        Name = spec.Name.For(language),
                        Description = spec.Description.For(language)
                    });
                }

                categories[spec.Slug] = category;
            }

            return categories;
        }

        private static List<Course> BuildCourses(
            IReadOnlyList<CourseSpec> specs,
            IReadOnlyDictionary<string, Category> categories,
            IReadOnlyDictionary<string, ApplicationUser> instructors)
        {
            var courses = new List<Course>();

            foreach (var spec in specs)
            {
                var createdAt = DateTime.UtcNow.AddDays(-spec.CreatedDaysAgo);

                var course = new Course
                {
                    CategoryId = categories[spec.CategorySlug].Id,
                    InstructorId = instructors[spec.InstructorEmail].Id,
                    Level = spec.Level,
                    Status = spec.Status,
                    Price = spec.Price,
                    ThumbnailUrl = spec.ThumbnailUrl,
                    CreatedAt = createdAt,
                    // Длительность курса — сумма длительностей уроков, а не отдельно
                    // придуманное число: иначе на карточке курса и в программе были бы
                    // разные цифры, и это первое, что заметит проверяющий.
                    DurationMinutes = spec.Modules.SelectMany(m => m.Lessons).Sum(l => l.DurationMinutes)
                };

                foreach (var language in AllLanguages)
                {
                    course.Translations.Add(new CourseTranslation
                    {
                        CourseId = course.Id,
                        LanguageCode = language,
                        Title = spec.Title.For(language),
                        ShortDescription = spec.ShortDescription.For(language),
                        Description = spec.Description.For(language),
                        WhatYouWillLearn = JsonSerializer.Serialize(spec.WhatYouWillLearn.For(language))
                    });
                }

                for (var moduleIndex = 0; moduleIndex < spec.Modules.Count; moduleIndex++)
                {
                    course.Modules.Add(BuildModule(spec.Modules[moduleIndex], moduleIndex, course.Id, createdAt));
                }

                courses.Add(course);
            }

            return courses;
        }

        private static Module BuildModule(ModuleSpec spec, int orderIndex, Guid courseId, DateTime createdAt)
        {
            var module = new Module
            {
                CourseId = courseId,
                OrderIndex = orderIndex,
                CreatedAt = createdAt
            };

            foreach (var language in AllLanguages)
            {
                module.Translations.Add(new ModuleTranslation
                {
                    ModuleId = module.Id,
                    LanguageCode = language,
                    Title = spec.Title.For(language),
                    Description = spec.Description?.For(language)
                });
            }

            for (var lessonIndex = 0; lessonIndex < spec.Lessons.Count; lessonIndex++)
            {
                module.Lessons.Add(BuildLesson(spec.Lessons[lessonIndex], lessonIndex, module.Id, createdAt));
            }

            return module;
        }

        private static Lesson BuildLesson(LessonSpec spec, int orderIndex, Guid moduleId, DateTime createdAt)
        {
            var lesson = new Lesson
            {
                ModuleId = moduleId,
                OrderIndex = orderIndex,
                VideoUrl = spec.VideoUrl,
                DurationMinutes = spec.DurationMinutes,
                IsPublished = true,
                CreatedAt = createdAt
            };

            foreach (var language in AllLanguages)
            {
                lesson.Translations.Add(new LessonTranslation
                {
                    LessonId = lesson.Id,
                    LanguageCode = language,
                    Title = spec.Title.For(language),
                    Content = spec.Content.For(language)
                });
            }

            foreach (var resource in spec.Resources)
            {
                lesson.Resources.Add(new LessonResource
                {
                    LessonId = lesson.Id,
                    LanguageCode = resource.LanguageCode,
                    FileName = resource.FileName,
                    FileUrl = resource.FileUrl,
                    FileType = resource.FileType,
                    CreatedAt = createdAt
                });
            }

            if (spec.Quiz is not null)
            {
                lesson.Quiz = BuildQuiz(spec.Quiz, lesson.Id, createdAt);
            }

            return lesson;
        }

        private static Quiz BuildQuiz(QuizSpec spec, Guid lessonId, DateTime createdAt)
        {
            var quiz = new Quiz
            {
                LessonId = lessonId,
                PassingScore = spec.PassingScore,
                TimeLimitMinutes = spec.TimeLimitMinutes,
                CreatedAt = createdAt
            };

            quiz.Translations.Add(new QuizTranslation
            {
                QuizId = quiz.Id,
                LanguageCode = LanguageCode.EN,
                Title = spec.Title,
                Description = spec.Description,
                CreatedAt = createdAt
            });

            for (var questionIndex = 0; questionIndex < spec.Questions.Count; questionIndex++)
            {
                var questionSpec = spec.Questions[questionIndex];

                var question = new Question
                {
                    QuizId = quiz.Id,
                    QuestionType = questionSpec.Type,
                    Points = questionSpec.Points,
                    OrderIndex = questionIndex,
                    CreatedAt = createdAt
                };

                question.Translations.Add(new QuestionTranslation
                {
                    QuestionId = question.Id,
                    LanguageCode = LanguageCode.EN,
                    QuestionText = questionSpec.Text,
                    CreatedAt = createdAt
                });

                foreach (var answerSpec in questionSpec.Answers)
                {
                    var answer = new Answer
                    {
                        QuestionId = question.Id,
                        IsCorrect = answerSpec.IsCorrect,
                        CreatedAt = createdAt
                    };

                    answer.Translations.Add(new AnswerTranslation
                    {
                        AnswerId = answer.Id,
                        LanguageCode = LanguageCode.EN,
                        AnswerText = answerSpec.Text,
                        CreatedAt = createdAt
                    });

                    question.Answers.Add(answer);
                }

                quiz.Questions.Add(question);
            }

            return quiz;
        }

        private static readonly LanguageCode[] AllLanguages =
        {
            LanguageCode.AZ, LanguageCode.EN, LanguageCode.RU
        };

        private void LogSummary(IReadOnlyList<Course> courses, DemoActivity activity, int instructorCount, int studentCount)
        {
            var modules = courses.SelectMany(c => c.Modules).ToList();
            var lessons = modules.SelectMany(m => m.Lessons).ToList();
            var quizzes = lessons.Where(l => l.Quiz is not null).Select(l => l.Quiz!).ToList();

            _logger.LogInformation(
                "Демо-данные созданы: {Instructors} инструкторов, {Students} студентов, {Courses} курсов, " +
                "{Modules} модулей, {Lessons} уроков, {Quizzes} квизов, {Questions} вопросов, " +
                "{Enrollments} записей, {Progress} записей прогресса, {Attempts} попыток квизов, " +
                "{Reviews} отзывов, {Certificates} сертификатов, {Wishlist} в избранном, {Notifications} уведомлений.",
                instructorCount, studentCount, courses.Count,
                modules.Count, lessons.Count, quizzes.Count, quizzes.Sum(q => q.Questions.Count),
                activity.Enrollments.Count, activity.LessonProgresses.Count, activity.QuizAttempts.Count,
                activity.Reviews.Count, activity.Certificates.Count, activity.WishlistItems.Count,
                activity.Notifications.Count);
        }
    }
}
