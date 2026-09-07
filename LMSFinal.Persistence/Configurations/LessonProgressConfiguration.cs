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
    public class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
    {
        public void Configure(EntityTypeBuilder<LessonProgress> builder)
        {
            builder.ToTable("LessonProgresses");

            builder.HasIndex(p => new { p.StudentId, p.LessonId }).IsUnique();
            builder.HasIndex(p => p.StudentId);
            builder.HasIndex(p => p.LessonId);

            builder.HasOne(p => p.Student)
                .WithMany(u => u.LessonProgresses)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
