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
    public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
    {
        public void Configure(EntityTypeBuilder<QuizAttempt> builder)
        {
            builder.ToTable("QuizAttempts");

            builder.HasIndex(a => a.StudentId);
            builder.HasIndex(a => a.QuizId);

            builder.HasOne(a => a.Student)
                .WithMany(u => u.QuizAttempts)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.AttemptAnswers)
                .WithOne(aa => aa.QuizAttempt)
                .HasForeignKey(aa => aa.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
