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
    public class ModuleTranslationConfiguration : IEntityTypeConfiguration<ModuleTranslation>
    {
        public void Configure(EntityTypeBuilder<ModuleTranslation> builder)
        {
            builder.ToTable("ModuleTranslations");

            builder.Property(t => t.LanguageCode).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
            builder.Property(t => t.Description).HasMaxLength(1000);

            builder.HasIndex(t => new { t.ModuleId, t.LanguageCode }).IsUnique();
        }
    }

}
