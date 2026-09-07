using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Lessons
{
    public record LessonTranslationInput(LanguageCode LanguageCode, string Title, string? Description, string Content);
}
