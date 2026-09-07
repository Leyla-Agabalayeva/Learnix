using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class Lesson : BaseEntity
    {
        public Guid ModuleId { get; set; }
        public Module Module { get; set; } = null!;

        public string? VideoUrl { get; set; }

        public int DurationMinutes { get; set; }

        public int OrderIndex { get; set; }

        public bool IsPublished { get; set; } = false;

        // Навигационные свойства
        public ICollection<LessonTranslation> Translations { get; set; } = new List<LessonTranslation>();
        public ICollection<LessonResource> Resources { get; set; } = new List<LessonResource>();
        public ICollection<LessonProgress> ProgressRecords { get; set; } = new List<LessonProgress>();

        /// <summary>Квиз урока — необязателен (0..1). Внешний ключ хранится на стороне Quiz (Quiz.LessonId, unique).</summary>
        public Quiz? Quiz { get; set; }
    }

}
