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
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.Property(c => c.Slug).HasMaxLength(150).IsRequired();
            builder.Property(c => c.IconUrl).HasMaxLength(500);

            builder.HasIndex(c => c.Slug).IsUnique();

            builder.HasMany(c => c.Translations)
                .WithOne(t => t.Category)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.HasMany(c => c.Courses)
                .WithOne(course => course.Category)
                .HasForeignKey(course => course.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
