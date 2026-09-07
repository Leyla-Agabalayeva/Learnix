using LMSFinal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Persistence.Configurations
{
    public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
    {
        public void Configure(EntityTypeBuilder<Quiz> builder)
        {
            builder.ToTable("Quizzes");

            // Один квиз на урок.
            builder.HasIndex(q => q.LessonId).IsUnique();

            builder.HasMany(q => q.Translations)
                .WithOne(t => t.Quiz)
                .HasForeignKey(t => t.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(q => q.Questions)
                .WithOne(question => question.Quiz)
                .HasForeignKey(question => question.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(q => q.Attempts)
                .WithOne(a => a.Quiz)
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
