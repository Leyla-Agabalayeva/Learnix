using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class LessonTranslation : BaseEntity
    {
        public Guid LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public LanguageCode LanguageCode { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>HTML-контент урока.</summary>
        public string Content { get; set; } = string.Empty;
    }

}
