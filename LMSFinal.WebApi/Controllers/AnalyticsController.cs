using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Analytics;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{

    [Route("api")]
    [Authorize(Roles = "Instructor")]
    [Produces("application/json")]
    public class AnalyticsController : ApiControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Сводная статистика инструктора.</response>
        [HttpGet("instructor/dashboard-stats")]
        [ProducesResponseType(typeof(ApiResponse<InstructorDashboardStatsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
        {
            var stats = await _analyticsService.GetInstructorDashboardStatsAsync(User.GetUserId(), cancellationToken);
            return Success(stats);
        }

        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Статистика курса.</response>
        /// <response code="403">Курс принадлежит другому инструктору.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpGet("courses/{courseId:guid}/analytics")]
        [ProducesResponseType(typeof(ApiResponse<CourseAnalyticsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourseAnalytics(Guid courseId, CancellationToken cancellationToken)
        {
            var analytics = await _analyticsService.GetCourseAnalyticsAsync(User.GetUserId(), courseId, cancellationToken);
            return Success(analytics);
        }
    }
}
