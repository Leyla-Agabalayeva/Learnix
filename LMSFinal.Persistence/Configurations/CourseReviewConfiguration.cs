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
    public class CourseReviewConfiguration : IEntityTypeConfiguration<CourseReview>
    {
        public void Configure(EntityTypeBuilder<CourseReview> builder)
        {
            builder.ToTable("CourseReviews");

            builder.Property(r => r.Comment).HasMaxLength(2000);
            builder.Property(r => r.InstructorReply).HasMaxLength(2000);

            // 1 <= Rating <= 5 — проверка на уровне БД, дублирует валидацию в Application-слое.
            builder.ToTable(t => t.HasCheckConstraint("CK_CourseReviews_Rating", "[Rating] >= 1 AND [Rating] <= 5"));

            // Один отзыв на курс от одного студента.
            builder.HasIndex(r => new { r.StudentId, r.CourseId }).IsUnique();
            builder.HasIndex(r => r.CourseId);

            builder.HasOne(r => r.Student)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
