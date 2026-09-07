using LMSFinal.Application.Common;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Admin;
using LMSFinal.Contracts.DTOs.Home;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class AdminController : ApiControllerBase
    {
        private static readonly HashSet<string> AllowedThumbnailExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".svg"
        };

        private const long MaxThumbnailSizeBytes = 5 * 1024 * 1024; // 5 МБ
        private const long MaxHeroSlideSizeBytes = 8 * 1024 * 1024; // 8 МБ — баннеры обычно тяжелее обложек курса

        private readonly IAdminService _adminService;
        private readonly IHeroSlideService _heroSlideService;
        private readonly IFileStorageService _fileStorage;

        public AdminController(IAdminService adminService, IHeroSlideService heroSlideService, IFileStorageService fileStorage)
        {
            _adminService = adminService;
            _heroSlideService = heroSlideService;
            _fileStorage = fileStorage;
        }

        /// <response code="200">Сводная статистика по платформе.</response>
        [HttpGet("dashboard-stats")]
        [ProducesResponseType(typeof(ApiResponse<AdminDashboardStatsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
        {
            var stats = await _adminService.GetDashboardStatsAsync(cancellationToken);
            return Success(stats);
        }

        /// <param name="request">Поиск по имени/email, фильтр по роли, страница.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Страница пользователей.</response>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AdminUserDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers([FromQuery] AdminUserSearchRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminService.SearchUsersAsync(request, cancellationToken);
            return Success(result);
        }

        /// <param name="id">Идентификатор пользователя.</param>
        /// <param name="request">Locked = true — заблокировать вход, false — снять блокировку.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Готово.</response>
        /// <response code="404">Пользователь не найден.</response>
        /// <response code="409">Нельзя заблокировать самого себя.</response>
        [HttpPatch("users/{id:guid}/lock")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SetUserLock(Guid id, [FromBody] SetUserLockRequest request, CancellationToken cancellationToken)
        {
            await _adminService.SetUserLockAsync(User.GetUserId(), id, request.Locked, cancellationToken);
            return NoContent();
        }

        /// <param name="id">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Пользователь удалён.</response>
        /// <response code="404">Пользователь не найден.</response>
        /// <response code="409">Нельзя удалить себя или последнего администратора.</response>
        [HttpDelete("users/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            await _adminService.DeleteUserAsync(User.GetUserId(), id, cancellationToken);
            return NoContent();
        }

        /// <param name="request">Поиск по названию, фильтр по статусу, страница.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Страница курсов (любого статуса, любого автора).</response>
        [HttpGet("courses")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AdminCourseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourses([FromQuery] AdminCourseSearchRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminService.SearchCoursesAsync(request, cancellationToken);
            return Success(result);
        }

        /// <param name="id">Идентификатор курса.</param>
        /// <param name="request">Новый статус.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Статус изменён.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpPatch("courses/{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetCourseStatus(Guid id, [FromBody] SetCourseStatusRequest request, CancellationToken cancellationToken)
        {
            await _adminService.SetCourseStatusAsync(id, request.Status, cancellationToken);
            return NoContent();
        }

        /// <param name="id">Идентификатор курса.</param>
        /// <param name="file">Новое изображение обложки — JPG, PNG, WEBP или SVG, максимум 5 МБ.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Обложка обновлена, в data лежит новый URL.</response>
        /// <response code="400">Файл не выбран, слишком большой или недопустимого формата.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpPost("courses/{id:guid}/thumbnail")]
        [RequestSizeLimit(MaxThumbnailSizeBytes)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetCourseThumbnail(Guid id, IFormFile? file, CancellationToken cancellationToken)
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
            await _adminService.SetCourseThumbnailAsync(id, url, cancellationToken);

            return Success<object>(new { url });
        }

        /// <param name="id">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Курс удалён.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpDelete("courses/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCourse(Guid id, CancellationToken cancellationToken)
        {
            await _adminService.DeleteCourseAsync(id, cancellationToken);
            return NoContent();
        }

        /// <param name="file">Изображение баннера — JPG, PNG или WEBP, максимум 8 МБ.</param>
        /// <param name="linkUrl">Необязательная ссылка — куда ведёт клик по слайду.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Слайд добавлен в конец карусели.</response>
        /// <response code="400">Файл не выбран, слишком большой или недопустимого формата.</response>
        [HttpPost("hero-slides")]
        [RequestSizeLimit(MaxHeroSlideSizeBytes)]
        [ProducesResponseType(typeof(ApiResponse<HeroSlideDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateHeroSlide(IFormFile? file, [FromForm] string? linkUrl, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Fail("Выберите файл изображения.");
            }

            if (file.Length > MaxHeroSlideSizeBytes)
            {
                return Fail("Файл слишком большой — максимум 8 МБ.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedThumbnailExtensions.Contains(extension) || string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
            {
                return Fail("Поддерживаются только изображения: JPG, PNG, WEBP.");
            }

            var objectName = $"{Guid.NewGuid():N}{extension}";

            using (var stream = file.OpenReadStream())
            {
                await _fileStorage.UploadAsync(StorageBuckets.HeroSlides, objectName, stream, file.ContentType, cancellationToken);
            }

            var url = _fileStorage.GetPublicUrl(StorageBuckets.HeroSlides, objectName);
            var slide = await _heroSlideService.CreateAsync(url, string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl, cancellationToken);

            return Created(slide);
        }

        /// <param name="id">Идентификатор слайда.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Слайд удалён.</response>
        /// <response code="404">Слайд не найден.</response>
        [HttpDelete("hero-slides/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteHeroSlide(Guid id, CancellationToken cancellationToken)
        {
            var imageUrl = await _heroSlideService.DeleteAsync(id, cancellationToken);

            var objectName = imageUrl[(imageUrl.LastIndexOf($"/{StorageBuckets.HeroSlides}/", StringComparison.Ordinal)
                + StorageBuckets.HeroSlides.Length + 2)..];
            await _fileStorage.DeleteAsync(StorageBuckets.HeroSlides, objectName, cancellationToken);

            return NoContent();
        }
    }
}
