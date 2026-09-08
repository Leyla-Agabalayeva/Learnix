using LMSFinal.Contracts.DTOs.Notifications;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    /// <summary>
    /// Живая доставка уведомления (SignalR) — отдельно от NotifyAsync,
    /// который отвечает за сохранение в БД. NotificationService вызывает оба:
    /// сначала сохраняет, потом публикует, чтобы live-пуш не терялся при сбое рассылки.
    /// </summary>
    public interface INotificationPublisher
    {
        Task PublishAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);
    }
}
