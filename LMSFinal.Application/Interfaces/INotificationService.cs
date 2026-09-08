using LMSFinal.Contracts.DTOs.Notifications;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    public interface INotificationService
    {
        Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

        Task NotifyAsync(Guid userId, string title, string message, NotificationType type, CancellationToken cancellationToken = default);
    }

}
