using LMSFinal.Contracts.DTOs.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{

    public interface IAnalyticsService
    {
        Task<InstructorDashboardStatsDto> GetInstructorDashboardStatsAsync(Guid instructorId, CancellationToken cancellationToken = default);

        Task<CourseAnalyticsDto> GetCourseAnalyticsAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default);
    }
}
