using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Progress;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api")]
    [Authorize(Roles = "Student")]
    [Produces("application/json")]
    public class ProgressController : ApiControllerBase
    {
        private readonly IProgressService _progressService;

        public ProgressController(IProgressService progressService)
        {
            _progressService = progressService;
        }

 
        /// <param name="lessonId">Идентификатор урока.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Урок отмечен пройденным.</response>
        /// <response code="403">Студент не записан на курс этого урока.</response>
        /// <response code="404">Урок не найден.</response>
        [HttpPost("lessons/{lessonId:guid}/complete")]
        [ProducesResponseType(typeof(ApiResponse<LessonProgressDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkComplete(Guid lessonId, CancellationToken cancellationToken)
        {
            var progress = await _progressService.MarkCompleteAsync(User.GetUserId(), lessonId, cancellationToken);
            return Success(progress);
        }

 
        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Прогресс по курсу.</response>
        /// <response code="403">Студент не записан на этот курс.</response>
        [HttpGet("courses/{courseId:guid}/progress")]
        [ProducesResponseType(typeof(ApiResponse<CourseProgressDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProgress(Guid courseId, CancellationToken cancellationToken)
        {
            var progress = await _progressService.GetCourseProgressAsync(User.GetUserId(), courseId, cancellationToken);
            return Success(progress);
        }

  
        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Урок для продолжения либо признак завершения курса.</response>
        /// <response code="403">Студент не записан на этот курс.</response>
        [HttpGet("courses/{courseId:guid}/continue")]
        [ProducesResponseType(typeof(ApiResponse<ContinueLearningDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Continue(Guid courseId, CancellationToken cancellationToken)
        {
            var result = await _progressService.GetContinueLearningAsync(User.GetUserId(), courseId, cancellationToken);
            return Success(result);
        }
    }
}
