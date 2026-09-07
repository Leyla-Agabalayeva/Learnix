using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class Certificate : BaseEntity
    {
        /// <summary>Уникальный номер вида LMS-2026-000001. Генерируется в Application-слое.</summary>
        public string CertificateNumber { get; set; } = string.Empty;

        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public DateTime CompletionDate { get; set; }
    }

}
