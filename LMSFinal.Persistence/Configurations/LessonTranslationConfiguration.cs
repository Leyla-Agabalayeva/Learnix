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
    public class LessonTranslationConfiguration : IEntityTypeConfiguration<LessonTranslation>
    {
        public void Configure(EntityTypeBuilder<LessonTranslation> builder)
        {
            builder.ToTable("LessonTranslations");

            builder.Property(t => t.LanguageCode).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
            builder.Property(t => t.Description).HasMaxLength(1000);
            builder.Property(t => t.Content).HasColumnType("nvarchar(max)");

            builder.HasIndex(t => new { t.LessonId, t.LanguageCode }).IsUnique();
        }
    }

}
