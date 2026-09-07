using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class LessonProgress : BaseEntity
    {
        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public Guid LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        /// <summary>Позиция в видео/уроке (секунды), чтобы можно было "продолжить с места остановки".</summary>
        public int LastPosition { get; set; } = 0;

        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    }

}
