using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;

        }

        //Get summary
        public async Task<NotificationDTO.NotificationSummaryDTO> GetSummaryAsync(string userId)
        {
            var all = await _notificationRepository.GetByUserIdAsync(userId);

            return new NotificationDTO.NotificationSummaryDTO
            {
                UnreadCount = all.Count(n => !n.IsRead),
                Recent = all.Take(10).Select(MapToNotificationDTO).ToList()
            };
        }

        //Get all notifications for a user
        public async Task<List<NotificationDTO.NotificationResponseDTO>> GetAllAsync(string userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(MapToNotificationDTO).ToList();
        }

        //Mark a notification as read
        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);

            if (notification == null || notification.UserId != userId)
                throw new KeyNotFoundException("Notification not found.");

            if (notification.IsRead) return;

            notification.IsRead = true;
            await _notificationRepository.SaveChangesAsync();
        }

        //Mark all notifications as read
        public async Task MarkAllAsReadAsync(string userId)
        {
            var all = await _notificationRepository.GetByUserIdAsync(userId);
            var unread = all.Where(n => !n.IsRead).ToList();

            if (!unread.Any()) return;

            foreach (var n in unread)
                n.IsRead = true;

            await _notificationRepository.SaveChangesAsync();
        }

        //send notificaiton
        public async Task SendAsync(
            string userId,
            NotificationType type,
            string message,
            int? referenceId = null,
            NotificationReferenceType? referenceType = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();
        }

        //Map to DTO
        private static NotificationDTO.NotificationResponseDTO MapToNotificationDTO(Notification n)
        {
            return new NotificationDTO.NotificationResponseDTO
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Message = n.Message,
                ReferenceId = n.ReferenceId,
                ReferenceType = n.ReferenceType?.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            };
        }

    }
}
