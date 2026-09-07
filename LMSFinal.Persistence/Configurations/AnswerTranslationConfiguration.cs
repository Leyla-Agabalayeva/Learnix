using LMSFinal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMSFinal.Persistence.Configurations
{
    public class AnswerTranslationConfiguration : IEntityTypeConfiguration<AnswerTranslation>
    {
        public void Configure(EntityTypeBuilder<AnswerTranslation> builder)
        {
            builder.ToTable("AnswerTranslations");

            builder.Property(t => t.LanguageCode).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(t => t.AnswerText).HasMaxLength(500).IsRequired();

            builder.HasIndex(t => new { t.AnswerId, t.LanguageCode }).IsUnique();
        }
    }
}
