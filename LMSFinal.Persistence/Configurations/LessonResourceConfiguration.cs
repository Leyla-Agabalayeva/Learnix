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
    public class LessonResourceConfiguration : IEntityTypeConfiguration<LessonResource>
    {
        public void Configure(EntityTypeBuilder<LessonResource> builder)
        {
            builder.ToTable("LessonResources");

            builder.Property(r => r.LanguageCode).HasConversion<string>().HasMaxLength(10).IsRequired();

            builder.Property(r => r.FileName).HasMaxLength(255).IsRequired();
            builder.Property(r => r.FileUrl).HasMaxLength(500).IsRequired();
            builder.Property(r => r.FileType).HasMaxLength(50).IsRequired();

            builder.HasIndex(r => new { r.LessonId, r.LanguageCode });
        }
    }

}
