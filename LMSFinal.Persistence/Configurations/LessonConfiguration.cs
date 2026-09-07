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
    public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
    {
        public void Configure(EntityTypeBuilder<Lesson> builder)
        {
            builder.ToTable("Lessons");

            builder.Property(l => l.VideoUrl).HasMaxLength(500);

            builder.HasIndex(l => l.ModuleId);
            builder.HasIndex(l => new { l.ModuleId, l.OrderIndex });

            builder.HasMany(l => l.Translations)
                .WithOne(t => t.Lesson)
                .HasForeignKey(t => t.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Resources)
                .WithOne(r => r.Lesson)
                .HasForeignKey(r => r.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

     
            builder.HasOne(l => l.Quiz)
                .WithOne(q => q.Lesson)
                .HasForeignKey<Quiz>(q => q.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.ProgressRecords)
                .WithOne(p => p.Lesson)
                .HasForeignKey(p => p.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
