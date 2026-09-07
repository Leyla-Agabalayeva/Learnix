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
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.ToTable("Courses");

            builder.Property(c => c.ThumbnailUrl).HasMaxLength(500);
            builder.Property(c => c.Price).HasColumnType("decimal(10,2)");

            builder.Property(c => c.Level).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Индексы под фильтры каталога (раздел 49 ТЗ).
            builder.HasIndex(c => c.InstructorId);
            builder.HasIndex(c => c.CategoryId);
            builder.HasIndex(c => c.Status);

            builder.HasMany(c => c.Translations)
                .WithOne(t => t.Course)
                .HasForeignKey(t => t.CourseId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.HasMany(c => c.Modules)
                .WithOne(m => m.Course)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Enrollments)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Reviews)
                .WithOne(r => r.Course)
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.WishlistedBy)
                .WithOne(w => w.Course)
                .HasForeignKey(w => w.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Certificates)
                .WithOne(cert => cert.Course)
                .HasForeignKey(cert => cert.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
