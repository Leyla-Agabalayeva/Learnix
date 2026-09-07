using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Courses
{
    public record CourseSummaryDto(
        Guid Id,
        string? ThumbnailUrl,
        decimal Price,
        string Level,
        string Status,
        IReadOnlyList<CourseTranslationDto> Translations);
}
