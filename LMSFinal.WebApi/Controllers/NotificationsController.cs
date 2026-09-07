using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Notifications;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api/notifications")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationsController : ApiControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список уведомлений текущего пользователя.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
        {
            var notifications = await _notificationService.GetMyNotificationsAsync(User.GetUserId(), cancellationToken);
            return Success(notifications);
        }

        /// <param name="id">Идентификатор уведомления.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Уведомление отмечено прочитанным.</response>
        /// <response code="403">Уведомление принадлежит другому пользователю.</response>
        /// <response code="404">Уведомление не найдено.</response>
        [HttpPut("{id:guid}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
        {
            await _notificationService.MarkAsReadAsync(User.GetUserId(), id, cancellationToken);
            return NoContent();
        }
    }
}
