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
    public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
    {
        public void Configure(EntityTypeBuilder<Answer> builder)
        {
            builder.ToTable("Answers");

            builder.HasIndex(a => a.QuestionId);

            builder.HasMany(a => a.Translations)
                .WithOne(t => t.Answer)
                .HasForeignKey(t => t.AnswerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
