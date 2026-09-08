using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class CourseReview : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        /// <summary>Оценка от 1 до 5.</summary>
        public int Rating { get; set; }

        public string? Comment { get; set; }

        /// <summary>Ответ преподавателя курса — один на отзыв, необязательный.</summary>
        public string? InstructorReply { get; set; }

        public DateTime? InstructorRepliedAt { get; set; }
    }

}
