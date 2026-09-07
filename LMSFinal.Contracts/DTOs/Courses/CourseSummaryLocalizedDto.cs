using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Courses
{


    public record CourseSummaryLocalizedDto(
        Guid Id,
        string? ThumbnailUrl,
        decimal Price,
        string Level,
        string Status,
        string ResolvedLanguage,
        string Title,
        string ShortDescription,
        string InstructorName,
        double AverageRating,
        int ReviewCount,
        int EnrollmentCount,
        int LessonCount,
        int DurationMinutes);
}
