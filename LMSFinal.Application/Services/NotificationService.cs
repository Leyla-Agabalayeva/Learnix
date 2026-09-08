using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Notifications;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Services
{

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationPublisher _notificationPublisher;

        public NotificationService(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            INotificationPublisher notificationPublisher)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _notificationPublisher = notificationPublisher;
        }

        public async Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId, cancellationToken);

            return notifications
                .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type.ToString(), n.IsRead, n.CreatedAt))
                .ToList();
        }

        public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken)
                ?? throw new NotFoundException("Notification", notificationId);

            if (notification.UserId != userId)
            {
                throw new ForbiddenAccessException("Вы можете отмечать прочитанными только свои уведомления.");
            }

            await _notificationRepository.MarkAsReadAsync(notificationId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken)
                ?? throw new NotFoundException("Notification", notificationId);

            if (notification.UserId != userId)
            {
                throw new ForbiddenAccessException("Вы можете удалять только свои уведомления.");
            }

            if (!notification.IsRead)
            {
                throw new ConflictException("Сначала отметьте уведомление прочитанным, затем можно удалить.");
            }

            _notificationRepository.Remove(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task NotifyAsync(Guid userId, string title, string message, NotificationType type, CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false
            };

            await _notificationRepository.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Живая доставка — после сохранения, чтобы уведомление в любом случае
            // осталось в БД, даже если у пользователя нет активного SignalR-соединения.
            var dto = new NotificationDto(notification.Id, notification.Title, notification.Message,
                notification.Type.ToString(), notification.IsRead, notification.CreatedAt);
            await _notificationPublisher.PublishAsync(userId, dto, cancellationToken);
        }
    }

}
