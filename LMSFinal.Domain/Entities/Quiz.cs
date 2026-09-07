using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class Quiz : BaseEntity
    {
        public Guid LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        /// <summary>Проходной балл в процентах (0-100).</summary>
        public int PassingScore { get; set; } = 60;

        public int? TimeLimitMinutes { get; set; }

        // Навигационные свойства
        public ICollection<QuizTranslation> Translations { get; set; } = new List<QuizTranslation>();
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    }

}
