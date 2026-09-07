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
    public class QuizAttemptAnswerSelectionConfiguration : IEntityTypeConfiguration<QuizAttemptAnswerSelection>
    {
        public void Configure(EntityTypeBuilder<QuizAttemptAnswerSelection> builder)
        {
            builder.ToTable("QuizAttemptAnswerSelections");

            builder.HasIndex(s => s.QuizAttemptAnswerId);
            builder.HasIndex(s => s.AnswerId);
            builder.HasOne(s => s.Answer)
                .WithMany()
                .HasForeignKey(s => s.AnswerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
