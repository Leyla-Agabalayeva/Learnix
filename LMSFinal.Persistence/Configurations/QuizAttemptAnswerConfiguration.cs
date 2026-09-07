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
    public class QuizAttemptAnswerConfiguration : IEntityTypeConfiguration<QuizAttemptAnswer>
    {
        public void Configure(EntityTypeBuilder<QuizAttemptAnswer> builder)
        {
            builder.ToTable("QuizAttemptAnswers");

            builder.HasIndex(aa => aa.QuizAttemptId);
            builder.HasIndex(aa => aa.QuestionId);
            builder.HasOne(aa => aa.Question)
                .WithMany()
                .HasForeignKey(aa => aa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(aa => aa.SelectedAnswers)
                .WithOne(s => s.QuizAttemptAnswer)
                .HasForeignKey(s => s.QuizAttemptAnswerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
