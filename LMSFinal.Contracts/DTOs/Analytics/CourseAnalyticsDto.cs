using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Analytics
{
    public record CourseAnalyticsDto(
        Guid CourseId,
        int StudentsEnrolled,
        int StudentsCompleted,
        double AverageProgress,
        double AverageQuizScore,
        double AverageRating,
        int ReviewsCount);
}
