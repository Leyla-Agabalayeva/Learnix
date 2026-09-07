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
    public class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
    {
        public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
        {
            builder.ToTable("CategoryTranslations");

            builder.Property(t => t.LanguageCode)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
            builder.Property(t => t.Description).HasMaxLength(1000);

            builder.HasIndex(t => new { t.CategoryId, t.LanguageCode }).IsUnique();
        }
    }

}
