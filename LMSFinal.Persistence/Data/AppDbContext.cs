using LMSFinal.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Persistence.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();

        public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();

        public DbSet<Course> Courses => Set<Course>();
        public DbSet<CourseTranslation> CourseTranslations => Set<CourseTranslation>();

        public DbSet<Module> Modules => Set<Module>();
        public DbSet<ModuleTranslation> ModuleTranslations => Set<ModuleTranslation>();

        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<LessonTranslation> LessonTranslations => Set<LessonTranslation>();
        public DbSet<LessonResource> LessonResources => Set<LessonResource>();

        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();

        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<QuizTranslation> QuizTranslations => Set<QuizTranslation>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<QuestionTranslation> QuestionTranslations => Set<QuestionTranslation>();
        public DbSet<Answer> Answers => Set<Answer>();
        public DbSet<AnswerTranslation> AnswerTranslations => Set<AnswerTranslation>();
        public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
        public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
        public DbSet<QuizAttemptAnswerSelection> QuizAttemptAnswerSelections => Set<QuizAttemptAnswerSelection>();

        public DbSet<CourseReview> CourseReviews => Set<CourseReview>();
        public DbSet<Wishlist> Wishlists => Set<Wishlist>();
        public DbSet<Certificate> Certificates => Set<Certificate>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }

}
