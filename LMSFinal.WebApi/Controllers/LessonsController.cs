using LMSFinal.Application.Common;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Lessons;
using LMSFinal.Domain.Enums;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api")]
    [Authorize]
    [Produces("application/json")]
    public class LessonsController : ApiControllerBase
    {
        private const long MaxResourceSizeBytes = 20 * 1024 * 1024; // 20 МБ

        private readonly ILessonService _lessonService;
        private readonly IFileStorageService _fileStorage;

        public LessonsController(ILessonService lessonService, IFileStorageService fileStorage)
        {
            _lessonService = lessonService;
            _fileStorage = fileStorage;
        }

        /// <param name="moduleId">Идентификатор модуля.</param>
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список уроков.</response>
        /// <response code="403">Пользователь не владелец курса и не записан на него.</response>
        /// <response code="404">Модуль не найден.</response>
        [HttpGet("modules/{moduleId:guid}/lessons")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LessonDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByModule(Guid moduleId, [FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localized = await _lessonService.GetByModuleIdLocalizedAsync(User.GetUserId(), moduleId, lang.Value, cancellationToken);
                return Success(localized);
            }

            var lessons = await _lessonService.GetByModuleIdAsync(User.GetUserId(), moduleId, cancellationToken);
            return Success(lessons);
        }

        /// <param name="moduleId">Идентификатор модуля.</param>
        /// <param name="request">Данные урока и его переводы.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Урок создан.</response>
        /// <response code="403">Модуль принадлежит курсу другого инструктора.</response>
        /// <response code="404">Модуль не найден.</response>
        [HttpPost("modules/{moduleId:guid}/lessons")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<LessonDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(Guid moduleId, [FromBody] CreateLessonRequest request, CancellationToken cancellationToken)
        {
            var lesson = await _lessonService.CreateAsync(User.GetUserId(), moduleId, request, cancellationToken);
            return Created(lesson);
        }
        /// <param name="id">Идентификатор урока.</param>
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Данные урока.</response>
        /// <response code="403">Пользователь не записан на курс.</response>
        /// <response code="404">Урок не найден.</response>
        [HttpGet("lessons/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<LessonDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localizedLesson = await _lessonService.GetByIdLocalizedAsync(User.GetUserId(), id, lang.Value, cancellationToken);
                return Success(localizedLesson);
            }

            var lesson = await _lessonService.GetByIdAsync(User.GetUserId(), id, cancellationToken);
            return Success(lesson);
        }
        /// <param name="id">Идентификатор урока.</param>
        /// <param name="request">Новые данные урока.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Урок обновлён.</response>
        /// <response code="403">Урок принадлежит курсу другого инструктора.</response>
        /// <response code="404">Урок не найден.</response>
        [HttpPut("lessons/{id:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<LessonDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLessonRequest request, CancellationToken cancellationToken)
        {
            var lesson = await _lessonService.UpdateAsync(User.GetUserId(), id, request, cancellationToken);
            return Success(lesson);
        }

        /// <param name="id">Идентификатор урока.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Урок удалён.</response>
        /// <response code="403">Урок принадлежит курсу другого инструктора.</response>
        /// <response code="404">Урок не найден.</response>
        [HttpDelete("lessons/{id:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _lessonService.DeleteAsync(User.GetUserId(), id, cancellationToken);
            return NoContent();
        }

      
        /// <param name="request">Новый порядок уроков.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Порядок сохранён.</response>
        /// <response code="403">Хотя бы один урок принадлежит чужому курсу.</response>
        /// <response code="404">Один или несколько уроков не найдены.</response>
        /// <response code="409">Уроки из разных модулей в одном запросе.</response>
        [HttpPut("lessons/reorder")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reorder([FromBody] ReorderLessonsRequest request, CancellationToken cancellationToken)
        {
            await _lessonService.ReorderAsync(User.GetUserId(), request, cancellationToken);
            return NoContent();
        }

        /// <param name="id">Идентификатор урока.</param>
        /// <param name="languageCode">Язык, к переводу урока на котором привязан материал.</param>
        /// <param name="file">PDF-файл материала, максимум 20 МБ.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Материал загружен.</response>
        /// <response code="400">Файл не выбран, слишком большой или не PDF.</response>
        /// <response code="403">Урок принадлежит курсу другого инструктора.</response>
        /// <response code="404">Урок не найден.</response>
        [HttpPost("lessons/{id:guid}/resources")]
        [Authorize(Roles = "Instructor")]
        [RequestSizeLimit(MaxResourceSizeBytes)]
        [ProducesResponseType(typeof(ApiResponse<LessonResourceDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadResource(
            Guid id, [FromForm] LanguageCode languageCode, IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Fail("Выберите PDF-файл.");
            }

            if (file.Length > MaxResourceSizeBytes)
            {
                return Fail("Файл слишком большой — максимум 20 МБ.");
            }

            if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Fail("Материалы урока принимаются только в формате PDF.");
            }

            var objectName = $"{id}/{Guid.NewGuid():N}.pdf";

            using (var stream = file.OpenReadStream())
            {
                await _fileStorage.UploadAsync(StorageBuckets.LessonMaterials, objectName, stream, "application/pdf", cancellationToken);
            }

            var fileUrl = _fileStorage.GetPublicUrl(StorageBuckets.LessonMaterials, objectName);
            var resource = await _lessonService.AddResourceAsync(
                User.GetUserId(), id, languageCode, file.FileName, fileUrl, "pdf", cancellationToken);

            return Created(resource);
        }

        /// <param name="id">Идентификатор урока.</param>
        /// <param name="resourceId">Идентификатор материала.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Материал удалён.</response>
        /// <response code="403">Урок принадлежит курсу другого инструктора.</response>
        /// <response code="404">Урок или материал не найден.</response>
        [HttpDelete("lessons/{id:guid}/resources/{resourceId:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteResource(Guid id, Guid resourceId, CancellationToken cancellationToken)
        {
            var fileUrl = await _lessonService.RemoveResourceAsync(User.GetUserId(), id, resourceId, cancellationToken);

            var objectName = fileUrl[(fileUrl.LastIndexOf($"/{StorageBuckets.LessonMaterials}/", StringComparison.Ordinal)
                + StorageBuckets.LessonMaterials.Length + 2)..];
            await _fileStorage.DeleteAsync(StorageBuckets.LessonMaterials, objectName, cancellationToken);

            return NoContent();
        }
    }
}
