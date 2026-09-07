using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Courses
{
    public record CourseDto(
        Guid Id,
        Guid InstructorId,
        string InstructorName,
        Guid CategoryId,
        string CategorySlug,
        string? ThumbnailUrl,
        decimal Price,
        int DurationMinutes,
        string Level,
        string Status,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        IReadOnlyList<CourseTranslationDto> Translations);

}
