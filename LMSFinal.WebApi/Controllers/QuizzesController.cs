using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Quizzes;
using LMSFinal.Domain.Enums;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api/quizzes")]
    [Authorize]
    [Produces("application/json")]
    public class QuizzesController : ApiControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly IQuizGenerationService _quizGenerationService;

        public QuizzesController(IQuizService quizService, IQuizGenerationService quizGenerationService)
        {
            _quizService = quizService;
            _quizGenerationService = quizGenerationService;
        }

        /// <param name="request">Текст урока и количество вопросов.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Черновик вопросов на AZ/EN/RU, сгенерированный AI — ещё не сохранён.</response>
        [HttpPost("generate")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QuestionInput>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Generate([FromBody] GenerateQuizRequest request, CancellationToken cancellationToken)
        {
            var questions = await _quizGenerationService.GenerateAsync(
                request.LessonContent, request.QuestionCount, cancellationToken);
            return Success(questions);
        }

        /// <param name="request">Тест, вопросы и варианты ответов.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Тест создан.</response>
        /// <response code="403">Урок принадлежит курсу другого инструктора.</response>
        /// <response code="404">Урок не найден.</response>
        /// <response code="409">У урока уже есть тест.</response>
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<QuizDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateQuizRequest request, CancellationToken cancellationToken)
        {
            var quiz = await _quizService.CreateAsync(User.GetUserId(), request, cancellationToken);
            return Created(quiz);
        }
        /// <param name="id">Идентификатор теста.</param>
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Данные теста.</response>
        /// <response code="403">Пользователь не владелец курса и не записан на него.</response>
        /// <response code="404">Тест не найден.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<QuizDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localizedQuiz = await _quizService.GetByIdLocalizedAsync(User.GetUserId(), id, lang.Value, cancellationToken);
                return Success(localizedQuiz);
            }

            var quiz = await _quizService.GetByIdAsync(User.GetUserId(), id, cancellationToken);
            return Success(quiz);
        }
        /// <param name="id">Идентификатор теста.</param>
        /// <param name="request">Новые данные теста.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Тест обновлён.</response>
        /// <response code="403">Тест принадлежит курсу другого инструктора.</response>
        /// <response code="404">Тест не найден.</response>
        /// <response code="409">Тест уже проходили студенты — такое изменение запрещено.</response>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<QuizDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuizRequest request, CancellationToken cancellationToken)
        {
            var quiz = await _quizService.UpdateAsync(User.GetUserId(), id, request, cancellationToken);
            return Success(quiz);
        }

        /// <param name="id">Идентификатор теста.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Тест удалён.</response>
        /// <response code="403">Тест принадлежит курсу другого инструктора.</response>
        /// <response code="404">Тест не найден.</response>
        /// <response code="409">Тест уже проходили студенты.</response>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _quizService.DeleteAsync(User.GetUserId(), id, cancellationToken);
            return NoContent();
        }

        /// <param name="id">Идентификатор теста.</param>
        /// <param name="request">Выбранные варианты ответов.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Результат попытки: балл, процент, passed.</response>
        /// <response code="403">Студент не записан на курс этого теста.</response>
        /// <response code="404">Тест не найден.</response>
        [HttpPost("{id:guid}/submit")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<QuizResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitQuizRequest request, CancellationToken cancellationToken)
        {
            var result = await _quizService.SubmitAsync(User.GetUserId(), id, request, cancellationToken);
            return Success(result);
        }

    
        /// <param name="id">Идентификатор теста.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список попыток.</response>
        /// <response code="404">Тест не найден.</response>
        [HttpGet("{id:guid}/results")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QuizResultDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResults(Guid id, CancellationToken cancellationToken)
        {
            var results = await _quizService.GetResultsAsync(User.GetUserId(), id, cancellationToken);
            return Success(results);
        }
    }
}
