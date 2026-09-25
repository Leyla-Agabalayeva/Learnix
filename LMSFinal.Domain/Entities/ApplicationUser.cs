using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        /// <summary>Краткая биография — заполняется в основном инструкторами.</summary>
        public string? Bio { get; set; }

        // Профессиональный профиль — заполняется только преподавателями при регистрации.
        // У студентов остаётся null: эти данные им не нужны и не запрашиваются.

        /// <summary>Должность или профессиональный заголовок, например «Senior .NET Developer».</summary>
        public string? ProfessionalTitle { get; set; }

        /// <summary>Основная область экспертизы, о которой преподаватель будет вести курсы.</summary>
        public string? Specialization { get; set; }

        public int? YearsOfExperience { get; set; }

        /// <summary>Уровень образования: Bachelor, Master, Doctorate и т.д.</summary>
        public string? EducationLevel { get; set; }

        /// <summary>Текущее место работы (необязательно).</summary>
        public string? Organization { get; set; }

        /// <summary>Ссылка на LinkedIn, GitHub или личный сайт (необязательно).</summary>
        public string? ProfileUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public ICollection<Course> CoursesAsInstructor { get; set; } = new List<Course>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
        public ICollection<CourseReview> Reviews { get; set; } = new List<CourseReview>();
        public ICollection<Wishlist> WishlistItems { get; set; } = new List<Wishlist>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }

}
