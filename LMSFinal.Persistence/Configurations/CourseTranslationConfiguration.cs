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
    public class CourseTranslationConfiguration : IEntityTypeConfiguration<CourseTranslation>
    {
        public void Configure(EntityTypeBuilder<CourseTranslation> builder)
        {
            builder.ToTable("CourseTranslations");

            builder.Property(t => t.LanguageCode).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
            builder.Property(t => t.ShortDescription).HasMaxLength(500).IsRequired();
            builder.Property(t => t.Description).HasColumnType("nvarchar(max)").IsRequired();
            builder.Property(t => t.WhatYouWillLearn).HasColumnType("nvarchar(max)");

            builder.HasIndex(t => new { t.CourseId, t.LanguageCode }).IsUnique();
        }
    }

}
