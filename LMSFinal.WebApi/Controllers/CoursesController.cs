using LMSFinal.Application.Common;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Courses;
using LMSFinal.Domain.Enums;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api/courses")]
    [Produces("application/json")]
    public class CoursesController : ApiControllerBase
    {
        private static readonly HashSet<string> AllowedThumbnailExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".svg"
        };

        private const long MaxThumbnailSizeBytes = 5 * 1024 * 1024; // 5 МБ

        private readonly ICourseService _courseService;
        private readonly IFileStorageService _fileStorage;

        public CoursesController(ICourseService courseService, IFileStorageService fileStorage)
        {
            _courseService = courseService;
            _fileStorage = fileStorage;
        }

        /// <param name="file">Изображение обложки — JPG, PNG, WEBP или SVG, максимум 5 МБ.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Файл загружен, в data лежит его URL.</response>
        /// <response code="400">Файл не выбран, слишком большой или недопустимого формата.</response>
        [HttpPost("thumbnail")]
        [Authorize(Roles = "Instructor")]
        [RequestSizeLimit(MaxThumbnailSizeBytes)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadThumbnail(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Fail("Выберите файл изображения.");
            }

            if (file.Length > MaxThumbnailSizeBytes)
            {
                return Fail("Файл слишком большой — максимум 5 МБ.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedThumbnailExtensions.Contains(extension))
            {
                return Fail("Поддерживаются только изображения: JPG, PNG, WEBP, SVG.");
            }

            var objectName = $"{Guid.NewGuid():N}{extension}";

            using (var stream = file.OpenReadStream())
            {
                await _fileStorage.UploadAsync(StorageBuckets.CourseThumbnails, objectName, stream, file.ContentType, cancellationToken);
            }

            var url = _fileStorage.GetPublicUrl(StorageBuckets.CourseThumbnails, objectName);
            return Success<object>(new { url });
        }
        /// <param name="request">Параметры поиска, фильтрации и пагинации.</param>
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Страница каталога.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CourseSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] CourseSearchRequest request, [FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localized = await _courseService.SearchLocalizedAsync(request, lang.Value, cancellationToken);
                return Success(localized);
            }

            var result = await _courseService.SearchAsync(request, cancellationToken);
            return Success(result);
        }

        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список курсов инструктора.</response>
        [HttpGet("my")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CourseSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMy([FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localized = await _courseService.GetMyCoursesLocalizedAsync(
                    User.GetUserId(), lang.Value, cancellationToken);
                return Success(localized);
            }

            var courses = await _courseService.GetMyCoursesAsync(User.GetUserId(), cancellationToken);
            return Success(courses);
        }

 
        /// <param name="id">Идентификатор курса.</param>
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Данные курса.</response>
        /// <response code="404">Курс не найден или не опубликован.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            var currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;

            if (lang.HasValue)
            {
                var localizedCourse = await _courseService.GetByIdLocalizedAsync(id, currentUserId, lang.Value, cancellationToken);
                return Success(localizedCourse);
            }

            var course = await _courseService.GetByIdAsync(id, currentUserId, cancellationToken);
            return Success(course);
        }

        /// <param name="request">Данные курса и его переводы.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Курс создан в статусе Draft.</response>
        /// <response code="404">Указанная категория не найдена.</response>
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
        {
            var course = await _courseService.CreateAsync(User.GetUserId(), request, cancellationToken);
            return Created(course);
        }
        /// <param name="id">Идентификатор курса.</param>
        /// <param name="request">Новые данные курса и переводов.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Курс обновлён.</response>
        /// <response code="403">Курс принадлежит другому инструктору.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
        {
            var course = await _courseService.UpdateAsync(User.GetUserId(), id, request, cancellationToken);
            return Success(course);
        }

        /// <param name="id">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Курс удалён.</response>
        /// <response code="403">Курс принадлежит другому инструктору.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _courseService.DeleteAsync(User.GetUserId(), id, cancellationToken);
            return NoContent();
        }

        /// <param name="id">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Курс опубликован.</response>
        /// <response code="403">Курс принадлежит другому инструктору.</response>
        /// <response code="404">Курс не найден.</response>
        /// <response code="409">У курса нет названия ни на одном языке.</response>
        [HttpPost("{id:guid}/publish")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
        {
            var course = await _courseService.PublishAsync(User.GetUserId(), id, cancellationToken);
            return Success(course);
        }
        /// <param name="id">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Курс снят с публикации.</response>
        /// <response code="403">Курс принадлежит другому инструктору.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpPost("{id:guid}/unpublish")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
        {
            var course = await _courseService.UnpublishAsync(User.GetUserId(), id, cancellationToken);
            return Success(course);
        }

        /// <param name="id">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Курс архивирован.</response>
        /// <response code="403">Курс принадлежит другому инструктору.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpPost("{id:guid}/archive")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
        {
            var course = await _courseService.ArchiveAsync(User.GetUserId(), id, cancellationToken);
            return Success(course);
        }
    }
}
