using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Analytics
{
    public record InstructorDashboardStatsDto(
        int TotalCourses,
        int TotalStudents,
        int TotalEnrollments,
        double AverageRating,
        double AverageCompletionRate);
}
