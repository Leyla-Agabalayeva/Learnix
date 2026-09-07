using LMSFinal.Application.Common;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Auth;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LMSFinal.WebApi.Controllers
{

    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ApiControllerBase
    {
        private static readonly HashSet<string> AllowedAvatarExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private const long MaxAvatarSizeBytes = 3 * 1024 * 1024; // 3 МБ

        private readonly IAuthService _authService;
        private readonly IFileStorageService _fileStorage;

        public AuthController(IAuthService authService, IFileStorageService fileStorage)
        {
            _authService = authService;
            _fileStorage = fileStorage;
        }

       
        /// <param name="request">Данные регистрации.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Пользователь создан, в data лежит токен и профиль.</response>
        /// <response code="400">Email уже занят или Identity отклонил пароль.</response>
        /// <response code="422">Ошибка валидации полей.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);

            return result.IsSuccess
                ? Created(result.Value)
                : Fail(result.Error!);
        }

       
        /// Демо-аккаунты после сидирования (:
        ///
        ///     admin@lms.com      / Admin123!
        ///     instructor@lms.com / Instructor123!
        ///     student@lms.com    / Student123!
       
        /// <param name="request">Email и пароль.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Успешный вход, в data лежит токен.</response>
        /// <response code="401">Неверный email или пароль.</response>
        /// <response code="422">Ошибка валидации полей.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);

            return result.IsSuccess
                ? Success(result.Value)
                : Fail(result.Error!, StatusCodes.Status401Unauthorized);
        }

 
        /// <param name="request">Email, на который прислать ссылку.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Запрос принят.</response>
        /// <response code="422">Ошибка валидации полей.</response>
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            await _authService.ForgotPasswordAsync(request, cancellationToken);
            return Success<object?>(null, "Если такой email зарегистрирован, на него отправлено письмо со ссылкой для сброса пароля.");
        }

        /// <param name="request">Email, токен из ссылки и новый пароль.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Пароль изменён.</response>
        /// <response code="400">Ссылка недействительна, устарела или уже использована.</response>
        /// <response code="422">Ошибка валидации полей.</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.ResetPasswordAsync(request, cancellationToken);

            return result.IsSuccess
                ? Success<object?>(null, "Пароль изменён. Теперь можно войти с новым паролем.")
                : Fail(result.Error!);
        }

        /// <response code="204">Выход выполнен.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Logout() => NoContent();

        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Профиль текущего пользователя.</response>
        /// <response code="404">Пользователь из токена больше не существует (например, удалён).</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var result = await _authService.GetCurrentUserAsync(User.GetUserId(), cancellationToken);

            return result.IsSuccess
                ? Success(result.Value)
                : Fail(result.Error!, StatusCodes.Status404NotFound);
        }

        /// <param name="request">Новые данные профиля.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Профиль обновлён, возвращены актуальные данные.</response>
        /// <response code="404">Пользователь из токена больше не существует.</response>
        /// <response code="422">Ошибка валидации полей.</response>
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.UpdateProfileAsync(User.GetUserId(), request, cancellationToken);

            return result.IsSuccess
                ? Success(result.Value)
                : Fail(result.Error!, StatusCodes.Status404NotFound);
        }

  
        /// <response code="200">Аватар обновлён, возвращены актуальные данные профиля.</response>
        /// <response code="400">Файл не выбран, слишком большой или не изображение.</response>
        [HttpPost("me/avatar")]
        [Authorize]
        [RequestSizeLimit(MaxAvatarSizeBytes)]
        [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Fail("Выберите файл изображения.");
            }

            if (file.Length > MaxAvatarSizeBytes)
            {
                return Fail("Файл слишком большой — максимум 3 МБ.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedAvatarExtensions.Contains(extension))
            {
                return Fail("Поддерживаются только изображения: JPG, PNG, WEBP, GIF.");
            }

            var userId = User.GetUserId();

            // Старые аватары того же пользователя больше не нужны — они
            // никак не переиспользуются, а место в бакете лучше не копить.
            await _fileStorage.DeleteByPrefixAsync(StorageBuckets.Avatars, $"{userId}-", cancellationToken);

            var objectName = $"{userId}-{Guid.NewGuid():N}{extension}";

            using (var stream = file.OpenReadStream())
            {
                await _fileStorage.UploadAsync(StorageBuckets.Avatars, objectName, stream, file.ContentType, cancellationToken);
            }

            var avatarUrl = _fileStorage.GetPublicUrl(StorageBuckets.Avatars, objectName);
            var result = await _authService.UpdateAvatarAsync(userId, avatarUrl, cancellationToken);

            return result.IsSuccess
                ? Success(result.Value)
                : Fail(result.Error!, StatusCodes.Status404NotFound);
        }

     
        /// <response code="200">Роль Instructor подтверждена.</response>
        [HttpGet("instructor-only")]
        [Authorize(Roles = "Instructor")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult InstructorOnly() =>
            Success(new { message = $"Доступ разрешён. Роль подтверждена: Instructor. UserId: {User.GetUserId()}" });


        /// <response code="200">Роль Student подтверждена.</response>
        [HttpGet("student-only")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult StudentOnly() =>
            Success(new { message = $"Доступ разрешён. Роль подтверждена: Student. UserId: {User.GetUserId()}" });
    }
}
