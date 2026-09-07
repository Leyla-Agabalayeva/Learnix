using LMSFinal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMSFinal.Persistence.Configurations
{
    public class QuestionTranslationConfiguration : IEntityTypeConfiguration<QuestionTranslation>
    {
        public void Configure(EntityTypeBuilder<QuestionTranslation> builder)
        {
            builder.ToTable("QuestionTranslations");

            builder.Property(t => t.LanguageCode).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(t => t.QuestionText).HasMaxLength(1000).IsRequired();

            builder.HasIndex(t => new { t.QuestionId, t.LanguageCode }).IsUnique();
        }
    }
}
