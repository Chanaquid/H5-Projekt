using backend.Interfaces;
using backend.Models;
using static backend.DTOs.ChatDTO;

namespace backend.Services
{
    public class SupportChatService : ISupportChatService
    {
        private readonly ISupportChatRepository _supportChatRepository;
        private readonly INotificationService _notificationService;

        public SupportChatService(
            ISupportChatRepository supportChatRepository,
            INotificationService notificationService)
        {
            _supportChatRepository = supportChatRepository;
            _notificationService = notificationService;
        }

        // User creates a new support thread with an initial message
        public async Task<SupportChatDTO.SupportThreadDetailDTO> CreateThreadAsync(string userId, SupportChatDTO.CreateSupportThreadDTO dto)
        {
            // FIX 1: Validate message is not empty or whitespace
            if (string.IsNullOrWhiteSpace(dto.InitialMessage))
                throw new ArgumentException("Initial message is required.");

            var thread = new SupportThread
            {
                UserId = userId,
                Status = SupportThreadStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _supportChatRepository.AddThreadAsync(thread);
            await _supportChatRepository.SaveChangesAsync();

            var message = new SupportMessage
            {
                SupportThreadId = thread.Id,
                SenderId = userId,
                Content = dto.InitialMessage.Trim(),
                SentAt = DateTime.UtcNow
            };

            await _supportChatRepository.AddMessageAsync(message);
            await _supportChatRepository.SaveChangesAsync();

            var created = await _supportChatRepository.GetThreadWithMessagesAsync(thread.Id);
            return MapToDetailDTO(created!);
        }

        // Either party sends a message in an existing thread
        public async Task<SupportChatDTO.SupportMessageResponseDTO> SendMessageAsync(string senderId, SupportChatDTO.SendSupportMessageDTO dto)
        {
            // FIX 1: Validate message is not empty or whitespace
            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("Message content is required.");

            var thread = await _supportChatRepository.GetThreadByIdAsync(dto.SupportThreadId);
            if (thread == null)
                throw new KeyNotFoundException("Support thread not found.");

            if (thread.Status == SupportThreadStatus.Closed)
                throw new InvalidOperationException("Cannot send messages in a closed support thread.");

            var isThreadOwner = thread.UserId == senderId;
            var isClaimingAdmin = thread.ClaimedByAdminId == senderId;

            if (!isThreadOwner && !isClaimingAdmin)
                throw new UnauthorizedAccessException("You do not have access to this support thread.");

            var message = new SupportMessage
            {
                SupportThreadId = thread.Id,
                SenderId = senderId,
                Content = dto.Content.Trim(),
                SentAt = DateTime.UtcNow
            };

            await _supportChatRepository.AddMessageAsync(message);
            await _supportChatRepository.SaveChangesAsync();

            // Notify the other party
            var recipientId = isThreadOwner ? thread.ClaimedByAdminId : thread.UserId;
            if (recipientId != null)
            {
                await _notificationService.SendAsync(
                    recipientId,
                    NotificationType.SupportMessageReceived,
                    "You have a new support message.",
                    thread.Id,
                    NotificationReferenceType.SupportThread
                );
            }

            // FIX 3: Return the already-constructed message directly — no need to
            // reload the thread just to find the message we just built.
            return MapToMessageDTO(message, thread.ClaimedByAdminId);
        }

        // Get full thread with messages
        public async Task<SupportChatDTO.SupportThreadDetailDTO> GetThreadAsync(int threadId, string requestingUserId, bool isAdmin)
        {
            var thread = await _supportChatRepository.GetThreadWithMessagesAsync(threadId);
            if (thread == null)
                throw new KeyNotFoundException("Support thread not found.");

            if (!isAdmin && thread.UserId != requestingUserId)
                throw new UnauthorizedAccessException("You do not have access to this support thread.");

            await _supportChatRepository.MarkMessagesReadAsync(threadId, requestingUserId);
            await _supportChatRepository.SaveChangesAsync();

            return MapToDetailDTO(thread);
        }

        // User gets their own threads
        public async Task<List<SupportChatDTO.SupportThreadSummaryDTO>> GetMyThreadsAsync(string userId)
        {
            var threads = await _supportChatRepository.GetThreadsByUserIdAsync(userId);
            return threads.Select(t => MapToSummaryDTO(t, userId)).ToList();
        }

        // Admin gets all open/claimed threads.
        // UnreadCount is not calculated here — no viewing user context is available
        // without an interface change. Admins see unread counts per-thread only
        // when opening a thread via GetThreadAsync.
        public async Task<List<SupportChatDTO.SupportThreadSummaryDTO>> GetAllOpenThreadsAsync()
        {
            var threads = await _supportChatRepository.GetAllOpenThreadsAsync();
            return threads.Select(t => MapToSummaryDTO(t, null)).ToList();
        }

        // Admin claims a thread
        public async Task<SupportChatDTO.SupportThreadDetailDTO> ClaimThreadAsync(int threadId, string adminId)
        {
            var thread = await _supportChatRepository.GetThreadWithMessagesAsync(threadId);
            if (thread == null)
                throw new KeyNotFoundException("Support thread not found.");

            if (thread.Status == SupportThreadStatus.Closed)
                throw new InvalidOperationException("Cannot claim a closed thread.");

            if (thread.Status == SupportThreadStatus.Claimed && thread.ClaimedByAdminId != adminId)
                throw new InvalidOperationException("This thread is already claimed by another admin.");

            thread.ClaimedByAdminId = adminId;
            thread.Status = SupportThreadStatus.Claimed;
            thread.ClaimedAt = DateTime.UtcNow;

            await _supportChatRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                thread.UserId,
                NotificationType.SupportMessageReceived,
                "An admin has joined your support thread.",
                thread.Id,
                NotificationReferenceType.SupportThread
            );

            return MapToDetailDTO(thread);
        }

        // Admin closes a thread
        public async Task<SupportChatDTO.SupportThreadDetailDTO> CloseThreadAsync(int threadId, string adminId)
        {
            var thread = await _supportChatRepository.GetThreadWithMessagesAsync(threadId);
            if (thread == null)
                throw new KeyNotFoundException("Support thread not found.");

            if (thread.Status == SupportThreadStatus.Closed)
                throw new InvalidOperationException("Thread is already closed.");

            thread.Status = SupportThreadStatus.Closed;
            thread.ClosedAt = DateTime.UtcNow;

            await _supportChatRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                thread.UserId,
                NotificationType.SupportMessageReceived,
                "Your support thread has been closed.",
                thread.Id,
                NotificationReferenceType.SupportThread
            );

            return MapToDetailDTO(thread);
        }

        // Admin reopens a closed thread — always reopens as Claimed under the
        // calling admin to avoid threads being silently re-claimed under a stale admin.
        public async Task<SupportChatDTO.SupportThreadDetailDTO> ReopenThreadAsync(int threadId, string adminId)
        {
            var thread = await _supportChatRepository.GetThreadWithMessagesAsync(threadId);
            if (thread == null)
                throw new KeyNotFoundException("Support thread not found.");

            if (thread.Status != SupportThreadStatus.Closed)
                throw new InvalidOperationException("Only closed threads can be reopened.");

            thread.ClaimedByAdminId = adminId;
            thread.Status = SupportThreadStatus.Claimed;
            thread.ClosedAt = null;

            await _supportChatRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                thread.UserId,
                NotificationType.SupportMessageReceived,
                "Your support thread has been reopened.",
                thread.Id,
                NotificationReferenceType.SupportThread
            );

            return MapToDetailDTO(thread);
        }

        // Mark messages as read — intentionally allowed on closed threads so users
        // can mark closing messages as read after the thread is resolved.
        public async Task MarkReadAsync(int threadId, string userId)
        {
            var thread = await _supportChatRepository.GetThreadByIdAsync(threadId);
            if (thread == null)
                throw new KeyNotFoundException("Support thread not found.");

            if (thread.UserId != userId && thread.ClaimedByAdminId != userId)
                throw new UnauthorizedAccessException("You do not have access to this support thread.");

            await _supportChatRepository.MarkMessagesReadAsync(threadId, userId);
            await _supportChatRepository.SaveChangesAsync();
        }

        // ── Mappers ──────────────────────────────────────────────────

        private static SupportChatDTO.SupportThreadDetailDTO MapToDetailDTO(SupportThread t)
        {
            return new SupportChatDTO.SupportThreadDetailDTO
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = t.User?.FullName ?? string.Empty,
                UserAvatarUrl = t.User?.AvatarUrl,
                Status = t.Status.ToString(),
                ClaimedByAdminId = t.ClaimedByAdminId,
                ClaimedByAdminName = t.ClaimedByAdmin?.FullName,
                ClaimedAt = t.ClaimedAt,
                ClosedAt = t.ClosedAt,
                CreatedAt = t.CreatedAt,
                Messages = t.Messages?
                    .OrderBy(m => m.SentAt)
                    .Select(m => MapToMessageDTO(m, t.ClaimedByAdminId))
                    .ToList() ?? new()
            };
        }

        private static SupportChatDTO.SupportThreadSummaryDTO MapToSummaryDTO(SupportThread t, string? viewingUserId)
        {
            var lastMessage = t.Messages?.OrderByDescending(m => m.SentAt).FirstOrDefault();
            var unreadCount = viewingUserId != null
                ? t.Messages?.Count(m => m.SenderId != viewingUserId && !m.IsRead) ?? 0
                : 0;

            return new SupportChatDTO.SupportThreadSummaryDTO
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = t.User?.FullName ?? string.Empty,
                UserAvatarUrl = t.User?.AvatarUrl,
                Status = t.Status.ToString(),
                ClaimedByAdminName = t.ClaimedByAdmin?.FullName,
                LastMessageContent = lastMessage?.Content,
                LastMessageAt = lastMessage?.SentAt,
                UnreadCount = unreadCount,
                CreatedAt = t.CreatedAt
            };
        }

        private static SupportChatDTO.SupportMessageResponseDTO MapToMessageDTO(SupportMessage m, string? claimedByAdminId)
        {
            return new SupportChatDTO.SupportMessageResponseDTO
            {
                Id = m.Id,
                SupportThreadId = m.SupportThreadId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.FullName ?? string.Empty,
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                IsAdminSender = claimedByAdminId != null && m.SenderId == claimedByAdminId,
                Content = m.Content,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            };
        }
    }
}