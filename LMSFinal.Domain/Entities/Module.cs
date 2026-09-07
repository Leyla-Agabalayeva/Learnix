using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class Module : BaseEntity
    {
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public int OrderIndex { get; set; }

        // Навигационные свойства
        public ICollection<ModuleTranslation> Translations { get; set; } = new List<ModuleTranslation>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }

}
