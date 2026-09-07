using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Modules;
using LMSFinal.Domain.Enums;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    
    [Route("api")]
    [Authorize]
    [Produces("application/json")]
    public class ModulesController : ApiControllerBase
    {
        private readonly IModuleService _moduleService;

        public ModulesController(IModuleService moduleService)
        {
            _moduleService = moduleService;
        }
        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список модулей.</response>
        /// <response code="403">Пользователь не владелец курса и не записан на него.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpGet("courses/{courseId:guid}/modules")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ModuleDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByCourse(Guid courseId, [FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localized = await _moduleService.GetByCourseIdLocalizedAsync(User.GetUserId(), courseId, lang.Value, cancellationToken);
                return Success(localized);
            }

            var modules = await _moduleService.GetByCourseIdAsync(User.GetUserId(), courseId, cancellationToken);
            return Success(modules);
        }
        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="request">Название модуля на разных языках.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Модуль создан.</response>
        /// <response code="403">Курс принадлежит другому инструктору.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpPost("courses/{courseId:guid}/modules")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<ModuleDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(Guid courseId, [FromBody] CreateModuleRequest request, CancellationToken cancellationToken)
        {
            var module = await _moduleService.CreateAsync(User.GetUserId(), courseId, request, cancellationToken);
            return Created(module);
        }
        /// <param name="id">Идентификатор модуля.</param>
        /// <param name="request">Новые данные модуля.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Модуль обновлён.</response>
        /// <response code="403">Модуль принадлежит курсу другого инструктора.</response>
        /// <response code="404">Модуль не найден.</response>
        [HttpPut("modules/{id:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<ModuleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateModuleRequest request, CancellationToken cancellationToken)
        {
            var module = await _moduleService.UpdateAsync(User.GetUserId(), id, request, cancellationToken);
            return Success(module);
        }
        /// <param name="id">Идентификатор модуля.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Модуль удалён.</response>
        /// <response code="403">Модуль принадлежит курсу другого инструктора.</response>
        /// <response code="404">Модуль не найден.</response>
        [HttpDelete("modules/{id:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _moduleService.DeleteAsync(User.GetUserId(), id, cancellationToken);
            return NoContent();
        }
        /// <param name="request">Новый порядок модулей.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Порядок сохранён.</response>
        /// <response code="403">Хотя бы один модуль принадлежит чужому курсу.</response>
        /// <response code="404">Один или несколько модулей не найдены.</response>
        /// <response code="409">Модули из разных курсов в одном запросе.</response>
        [HttpPut("modules/reorder")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reorder([FromBody] ReorderModulesRequest request, CancellationToken cancellationToken)
        {
            await _moduleService.ReorderAsync(User.GetUserId(), request, cancellationToken);
            return NoContent();
        }
    }
}
