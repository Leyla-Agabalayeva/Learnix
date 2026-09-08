using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Reviews;
using LMSFinal.Domain.Enums;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api")]
    [Produces("application/json")]
    public class ReviewsController : ApiControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }
        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список отзывов.</response>
        [HttpGet("courses/{courseId:guid}/reviews")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CourseReviewDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCourse(Guid courseId, CancellationToken cancellationToken)
        {
            var reviews = await _reviewService.GetByCourseIdAsync(courseId, cancellationToken);
            return Success(reviews);
        }
        /// <param name="lang">Язык названий курсов: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список отзывов по курсам преподавателя.</response>
        [HttpGet("instructor/reviews")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InstructorReviewDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInstructorReviews(
            [FromQuery] LanguageCode lang = LanguageCode.EN, CancellationToken cancellationToken = default)
        {
            var reviews = await _reviewService.GetInstructorReviewsAsync(
                User.GetUserId(), lang, cancellationToken);

            return Success(reviews);
        }


        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="request">Оценка (1–5) и текст отзыва.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Отзыв создан.</response>
        /// <response code="403">Студент не записан на этот курс.</response>
        /// <response code="409">Отзыв на этот курс уже существует.</response>
        /// <response code="422">Рейтинг вне диапазона 1–5 или текст не прошёл валидацию.</response>
        [HttpPost("courses/{courseId:guid}/reviews")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<CourseReviewDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(Guid courseId, [FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
        {
            var review = await _reviewService.CreateAsync(User.GetUserId(), courseId, request, cancellationToken);
            return Created(review);
        }

        /// <param name="id">Идентификатор отзыва.</param>
        /// <param name="request">Новая оценка и текст.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Отзыв обновлён.</response>
        /// <response code="403">Отзыв принадлежит другому студенту.</response>
        /// <response code="404">Отзыв не найден.</response>
        [HttpPut("reviews/{id:guid}")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<CourseReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewRequest request, CancellationToken cancellationToken)
        {
            var review = await _reviewService.UpdateAsync(User.GetUserId(), id, request, cancellationToken);
            return Success(review);
        }
        /// <param name="id">Идентификатор отзыва.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Отзыв удалён.</response>
        /// <response code="403">Отзыв принадлежит другому студенту.</response>
        /// <response code="404">Отзыв не найден.</response>
        [HttpDelete("reviews/{id:guid}")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _reviewService.DeleteAsync(User.GetUserId(), id, cancellationToken);
            return NoContent();
        }

        /// <param name="id">Идентификатор отзыва.</param>
        /// <param name="request">Текст ответа преподавателя.</param>
        /// <param name="lang">Язык названия курса в ответе.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Ответ сохранён.</response>
        /// <response code="403">Отзыв относится не к курсу этого преподавателя.</response>
        /// <response code="404">Отзыв не найден.</response>
        [HttpPut("reviews/{id:guid}/reply")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<InstructorReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reply(
            Guid id, [FromBody] ReplyToReviewRequest request, [FromQuery] LanguageCode lang = LanguageCode.EN, CancellationToken cancellationToken = default)
        {
            var review = await _reviewService.ReplyAsync(User.GetUserId(), id, lang, request, cancellationToken);
            return Success(review);
        }
    }
}
