using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class Course : BaseEntity
    {
        public Guid InstructorId { get; set; }
        public ApplicationUser Instructor { get; set; } = null!;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string? ThumbnailUrl { get; set; }

        public decimal Price { get; set; }

        public int DurationMinutes { get; set; }

        public CourseLevel Level { get; set; } = CourseLevel.Beginner;

        public CourseStatus Status { get; set; } = CourseStatus.Draft;

        // Навигационные свойства
        public ICollection<CourseTranslation> Translations { get; set; } = new List<CourseTranslation>();
        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<CourseReview> Reviews { get; set; } = new List<CourseReview>();
        public ICollection<Wishlist> WishlistedBy { get; set; } = new List<Wishlist>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }

}
