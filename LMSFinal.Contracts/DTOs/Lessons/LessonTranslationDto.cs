using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Lessons
{

    public record LessonTranslationDto(string LanguageCode, string Title, string? Description, string Content);
}
