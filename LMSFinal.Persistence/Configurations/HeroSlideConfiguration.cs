using LMSFinal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMSFinal.Persistence.Configurations
{
    public class HeroSlideConfiguration : IEntityTypeConfiguration<HeroSlide>
    {
        public void Configure(EntityTypeBuilder<HeroSlide> builder)
        {
            builder.ToTable("HeroSlides");

            builder.Property(s => s.ImageUrl).HasMaxLength(500).IsRequired();
            builder.Property(s => s.LinkUrl).HasMaxLength(500);
        }
    }
}
