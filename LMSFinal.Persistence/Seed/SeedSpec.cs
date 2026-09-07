using LMSFinal.Domain.Enums;

namespace LMSFinal.Persistence.Seed
{
 
    public sealed class SeedCatalogFile
    {
        public List<CategorySpec> Categories { get; init; } = new();
        public List<UserSpec> Instructors { get; init; } = new();
        public List<UserSpec> Students { get; init; } = new();
    }
    public sealed class LocalizedText
    {
        public string Az { get; init; } = string.Empty;
        public string En { get; init; } = string.Empty;
        public string Ru { get; init; } = string.Empty;

        public string For(LanguageCode language) => language switch
        {
            LanguageCode.AZ => Az,
            LanguageCode.RU => Ru,
            _ => En
        };
    }

    public sealed class LocalizedList
    {
        public List<string> Az { get; init; } = new();
        public List<string> En { get; init; } = new();
        public List<string> Ru { get; init; } = new();

        public List<string> For(LanguageCode language) => language switch
        {
            LanguageCode.AZ => Az,
            LanguageCode.RU => Ru,
            _ => En
        };
    }

    public sealed class CategorySpec
    {
        public string Slug { get; init; } = string.Empty;
        public string? IconUrl { get; init; }
        public LocalizedText Name { get; init; } = new();
        public LocalizedText Description { get; init; } = new();
    }

    public sealed class UserSpec
    {
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }
        public bool IsPrimaryDemo { get; init; }
    }

    public sealed class CourseSpec
    {
        public string CategorySlug { get; init; } = string.Empty;
        public string InstructorEmail { get; init; } = string.Empty;
        public CourseLevel Level { get; init; } = CourseLevel.Beginner;
        public CourseStatus Status { get; init; } = CourseStatus.Published;
        public decimal Price { get; init; }
        public string? ThumbnailUrl { get; init; }
        public int CreatedDaysAgo { get; init; } = 90;

        public LocalizedText Title { get; init; } = new();
        public LocalizedText ShortDescription { get; init; } = new();
        public LocalizedText Description { get; init; } = new();
        public LocalizedList WhatYouWillLearn { get; init; } = new();

        public List<ModuleSpec> Modules { get; init; } = new();
    }

    public sealed class ModuleSpec
    {
        public LocalizedText Title { get; init; } = new();
        public LocalizedText? Description { get; init; }
        public List<LessonSpec> Lessons { get; init; } = new();
    }

    public sealed class LessonSpec
    {
        public LocalizedText Title { get; init; } = new();
        public LocalizedText Content { get; init; } = new();
        public string? VideoUrl { get; init; }
        public int DurationMinutes { get; init; } = 10;
        public QuizSpec? Quiz { get; init; }

        public List<ResourceSpec> Resources { get; init; } = new();
    }

    public sealed class ResourceSpec
    {
        /// <summary>Язык материала. Разбирается из строки "RU" — см. JsonStringEnumConverter в SeedCatalog.</summary>
        public LanguageCode LanguageCode { get; init; } = LanguageCode.EN;

        public string FileName { get; init; } = string.Empty;
        public string FileUrl { get; init; } = string.Empty;
        public string FileType { get; init; } = string.Empty;
    }

    public sealed class QuizSpec
    {
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int PassingScore { get; init; } = 60;
        public int? TimeLimitMinutes { get; init; }
        public List<QuestionSpec> Questions { get; init; } = new();
    }

    public sealed class QuestionSpec
    {
        public string Text { get; init; } = string.Empty;
        public QuestionType Type { get; init; } = QuestionType.SingleChoice;
        public int Points { get; init; } = 1;
        public List<AnswerSpec> Answers { get; init; } = new();
    }

    public sealed class AnswerSpec
    {
        public string Text { get; init; } = string.Empty;
        public bool IsCorrect { get; init; }
    }
}
