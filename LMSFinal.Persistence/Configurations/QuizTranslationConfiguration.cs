using LMSFinal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMSFinal.Persistence.Configurations
{
    public class QuizTranslationConfiguration : IEntityTypeConfiguration<QuizTranslation>
    {
        public void Configure(EntityTypeBuilder<QuizTranslation> builder)
        {
            builder.ToTable("QuizTranslations");

            builder.Property(t => t.LanguageCode).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
            builder.Property(t => t.Description).HasMaxLength(1000);

            builder.HasIndex(t => new { t.QuizId, t.LanguageCode }).IsUnique();
        }
    }
}
