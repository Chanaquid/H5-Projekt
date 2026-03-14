using backend.DTOs;
using backend.Models;

namespace backend.Interfaces
{
    public interface INotificationService
    {
        //User actions
        Task<NotificationDTO.NotificationSummaryDTO> GetSummaryAsync(string userId);
        Task<List<NotificationDTO.NotificationResponseDTO>> GetAllAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);

        //Internal — called by other services, not controllers
        Task SendAsync(string userId, NotificationType type, string message, int? referenceId = null, NotificationReferenceType? referenceType = null);
    }
}
