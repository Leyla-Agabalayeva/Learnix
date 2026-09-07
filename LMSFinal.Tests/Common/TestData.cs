using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;

namespace LMSFinal.Tests.Common
{
    internal static class TestData
    {
        public static ApplicationUser User(string firstName = "Test", string lastName = "User", Guid? id = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLowerInvariant()}@lms.com",
            UserName = $"{firstName.ToLowerInvariant()}@lms.com"
        };

        public static Category Category(string slug = "backend", string name = "Backend")
        {
            var category = new Category { Slug = slug };

            category.Translations.Add(new CategoryTranslation
            {
                CategoryId = category.Id,
                LanguageCode = LanguageCode.EN,
                Name = name
            });

            return category;
        }

        public static Course Course(
            Guid instructorId,
            CourseStatus status = CourseStatus.Published,
            string title = "Test Course",
            Guid? categoryId = null)
        {
            var category = Category();

            var course = new Course
            {
                InstructorId = instructorId,
                CategoryId = categoryId ?? category.Id,
                Category = category,
                Status = status,
                Level = CourseLevel.Beginner,
                Price = 49.99m,
                Instructor = User("Elvin", "Mammadov", instructorId)
            };

            course.Translations.Add(new CourseTranslation
            {
                CourseId = course.Id,
                LanguageCode = LanguageCode.EN,
                Title = title,
                ShortDescription = "Short",
                Description = "Description",
                WhatYouWillLearn = "[]"
            });

            return course;
        }

        public static Course CourseWithLessons(Guid instructorId, int lessonCount, out List<Lesson> lessons)
        {
            var course = Course(instructorId);

            var module = new Module { CourseId = course.Id, OrderIndex = 0, Course = course };
            lessons = new List<Lesson>();

            for (var i = 0; i < lessonCount; i++)
            {
                var lesson = new Lesson
                {
                    ModuleId = module.Id,
                    Module = module,
                    OrderIndex = i,
                    IsPublished = true,
                    DurationMinutes = 10
                };

                lesson.Translations.Add(new LessonTranslation
                {
                    LessonId = lesson.Id,
                    LanguageCode = LanguageCode.EN,
                    Title = $"Lesson {i + 1}",
                    Content = "<p>Content</p>"
                });

                module.Lessons.Add(lesson);
                lessons.Add(lesson);
            }

            course.Modules.Add(module);
            return course;
        }

        public static Enrollment Enrollment(
            Guid studentId,
            Guid courseId,
            EnrollmentStatus status = EnrollmentStatus.Active,
            double progress = 0) => new()
            {
                StudentId = studentId,
                CourseId = courseId,
                Status = status,
                ProgressPercentage = progress,
                EnrolledAt = DateTime.UtcNow.AddDays(-10),
                CompletedAt = status == EnrollmentStatus.Completed ? DateTime.UtcNow.AddDays(-1) : null
            };

        public static LessonProgress Progress(Guid studentId, Guid lessonId, bool completed = true) => new()
        {
            StudentId = studentId,
            LessonId = lessonId,
            IsCompleted = completed,
            CompletedAt = completed ? DateTime.UtcNow.AddDays(-2) : null,
            LastAccessedAt = DateTime.UtcNow.AddDays(-2)
        };

        public static Quiz Quiz(Lesson lesson, int questionCount = 4, int passingScore = 60)
        {
            var quiz = new Quiz
            {
                LessonId = lesson.Id,
                Lesson = lesson,
                PassingScore = passingScore
            };

            quiz.Translations.Add(new QuizTranslation
            {
                QuizId = quiz.Id,
                LanguageCode = LanguageCode.EN,
                Title = "Test Quiz"
            });

            for (var i = 0; i < questionCount; i++)
            {
                var question = new Question
                {
                    QuizId = quiz.Id,
                    QuestionType = QuestionType.SingleChoice,
                    Points = 1,
                    OrderIndex = i
                };

                question.Translations.Add(new QuestionTranslation
                {
                    QuestionId = question.Id,
                    LanguageCode = LanguageCode.EN,
                    QuestionText = $"Question {i + 1}?"
                });

                for (var a = 0; a < 4; a++)
                {
                    AddAnswer(question, $"Answer {a + 1}", isCorrect: a == 0);
                }

                quiz.Questions.Add(question);
            }

            lesson.Quiz = quiz;
            return quiz;
        }

        public static Question MultipleChoiceQuestion(Quiz quiz, int points = 2)
        {
            var question = new Question
            {
                QuizId = quiz.Id,
                QuestionType = QuestionType.MultipleChoice,
                Points = points,
                OrderIndex = quiz.Questions.Count
            };

            question.Translations.Add(new QuestionTranslation
            {
                QuestionId = question.Id,
                LanguageCode = LanguageCode.EN,
                QuestionText = "Pick all correct options"
            });

            AddAnswer(question, "Correct A", isCorrect: true);
            AddAnswer(question, "Correct B", isCorrect: true);
            AddAnswer(question, "Wrong A", isCorrect: false);
            AddAnswer(question, "Wrong B", isCorrect: false);

            quiz.Questions.Add(question);
            return question;
        }

        private static void AddAnswer(Question question, string text, bool isCorrect)
        {
            var answer = new Answer { QuestionId = question.Id, IsCorrect = isCorrect };

            answer.Translations.Add(new AnswerTranslation
            {
                AnswerId = answer.Id,
                LanguageCode = LanguageCode.EN,
                AnswerText = text
            });

            question.Answers.Add(answer);
        }

        public static Certificate Certificate(ApplicationUser student, Course course, string number = "LMS-2026-000001") => new()
        {
            CertificateNumber = number,
            StudentId = student.Id,
            Student = student,
            CourseId = course.Id,
            Course = course,
            IssuedAt = DateTime.UtcNow,
            CompletionDate = DateTime.UtcNow
        };
    }
}
