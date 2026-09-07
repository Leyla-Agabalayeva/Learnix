using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Slug { get; set; } = string.Empty;

        public string? IconUrl { get; set; }

        // Навигационные свойства
        public ICollection<CategoryTranslation> Translations { get; set; } = new List<CategoryTranslation>();
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
