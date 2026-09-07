using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class LessonResource : BaseEntity
    {
        public Guid LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public LanguageCode LanguageCode { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FileUrl { get; set; } = string.Empty;

        public string FileType { get; set; } = string.Empty;
    }

}
