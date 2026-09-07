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
    public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
    {
        public void Configure(EntityTypeBuilder<Certificate> builder)
        {
            builder.ToTable("Certificates");

            builder.Property(c => c.CertificateNumber).HasMaxLength(50).IsRequired();

            builder.HasIndex(c => c.CertificateNumber).IsUnique();
            builder.HasIndex(c => c.StudentId);
            builder.HasIndex(c => c.CourseId);

            builder.HasOne(c => c.Student)
                .WithMany(u => u.Certificates)
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
