using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace LMSFinal.WebApi.Hubs;

public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public SignalRNotificationPublisher(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(userId.ToString())
            .SendAsync("ReceiveNotification", notification, cancellationToken);
}
