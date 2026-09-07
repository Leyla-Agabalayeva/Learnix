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
    public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
    {
        public void Configure(EntityTypeBuilder<Wishlist> builder)
        {
            builder.ToTable("Wishlists");

            builder.HasIndex(w => new { w.StudentId, w.CourseId }).IsUnique();

            builder.HasOne(w => w.Student)
                .WithMany(u => u.WishlistItems)
                .HasForeignKey(w => w.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
