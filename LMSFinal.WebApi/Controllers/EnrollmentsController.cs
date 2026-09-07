using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Enrollments;
using LMSFinal.Domain.Enums;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api")]
    [Produces("application/json")]
    public class EnrollmentsController : ApiControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }
        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Запись создана или реактивирована.</response>
        /// <response code="404">Курс не найден.</response>
        /// <response code="409">Курс не опубликован, либо студент уже записан или уже завершил курс.</response>
        [HttpPost("courses/{courseId:guid}/enroll")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<StudentEnrollmentDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enroll(Guid courseId, CancellationToken cancellationToken)
        {
            var enrollment = await _enrollmentService.EnrollAsync(User.GetUserId(), courseId, cancellationToken);
            return Created(enrollment);
        }
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список записей студента.</response>
        [HttpGet("enrollments/my")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentEnrollmentDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMy([FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localized = await _enrollmentService.GetMyEnrollmentsLocalizedAsync(
                    User.GetUserId(), lang.Value, cancellationToken);
                return Success(localized);
            }

            var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(User.GetUserId(), cancellationToken);
            return Success(enrollments);
        }

        /// <param name="id">Идентификатор записи (Enrollment), не курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Запись отменена.</response>
        /// <response code="403">Запись принадлежит другому студенту.</response>
        /// <response code="404">Запись не найдена.</response>
        /// <response code="409">Запись уже не активна.</response>
        [HttpPost("enrollments/{id:guid}/cancel")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<StudentEnrollmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            var enrollment = await _enrollmentService.CancelAsync(User.GetUserId(), id, cancellationToken);
            return Success(enrollment);
        }

        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список студентов курса.</response>
        /// <response code="403">Курс принадлежит другому инструктору.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpGet("courses/{courseId:guid}/students")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CourseEnrollmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourseStudents(Guid courseId, CancellationToken cancellationToken)
        {
            var students = await _enrollmentService.GetCourseStudentsAsync(User.GetUserId(), courseId, cancellationToken);
            return Success(students);
        }
    }
}
