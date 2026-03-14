using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using static backend.DTOs.ChatDTO;

namespace backend.Services
{
    public class DirectMessageService : IDirectMessageService
    {
        private readonly IDirectMessageRepository _directMessageRepository;
        private readonly IUserBlockRepository _userBlockRepository;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public DirectMessageService(
            IDirectMessageRepository directMessageRepository,
            IUserBlockRepository userBlockRepository,
            IHubContext<ChatHub> hubContext,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _directMessageRepository = directMessageRepository;
            _userBlockRepository = userBlockRepository;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _userManager = userManager;
        }


        //Send a message — finds or creates the conversation automatically
        public async Task<DirectMessageDTO.DirectMessageResponseDTO> SendAsync(string senderId, DirectMessageDTO.SendDirectMessageDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("Message content cannot be empty.");

            //Resolve recipient by username or email
            var recipient = await _userManager.FindByNameAsync(dto.RecipientUsernameOrEmail)
                         ?? await _userManager.FindByEmailAsync(dto.RecipientUsernameOrEmail);

            if (recipient == null)
                throw new KeyNotFoundException("Recipient not found.");

            if (recipient.Id == senderId)
                throw new ArgumentException("You cannot send a message to yourself.");

            //Check block in either direction — dont expose who blocked who
            var isBlocked = await _userBlockRepository.IsBlockedAsync(senderId, recipient.Id);
            if (isBlocked)
                throw new InvalidOperationException("Unable to send message.");

            //Find or create conversation
            var conversation = await _directMessageRepository.GetConversationAsync(senderId, recipient.Id);

            if (conversation == null)
            {
                conversation = new DirectConversation
                {
                    InitiatedById = senderId,
                    OtherUserId = recipient.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _directMessageRepository.AddConversationAsync(conversation);
                await _directMessageRepository.SaveChangesAsync();
            }
            else
            {
                //Unhide for both sides if previously hidden — new message makes the conversation reappear
                var isInitiator = conversation.InitiatedById == senderId;

                if (isInitiator && conversation.HiddenForInitiator)
                {
                    conversation.HiddenForInitiator = false;
                    conversation.InitiatorDeletedAt = null;
                }
                else if (!isInitiator && conversation.HiddenForOther)
                {
                    conversation.HiddenForOther = false;
                    conversation.OtherDeletedAt = null;
                }

                //Also unhide for the recipient so the conversation reappears in their inbox
                var isRecipientInitiator = conversation.InitiatedById == recipient.Id;
                if (isRecipientInitiator && conversation.HiddenForInitiator)
                    conversation.HiddenForInitiator = false;
                else if (!isRecipientInitiator && conversation.HiddenForOther)
                    conversation.HiddenForOther = false;
            }

            var message = new DirectMessage
            {
                ConversationId = conversation.Id,
                SenderId = senderId,
                Content = dto.Content.Trim(),
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            await _directMessageRepository.AddMessageAsync(message);
            await _directMessageRepository.SaveChangesAsync();

            await _directMessageRepository.LoadMessageSenderAsync(message);

            var response = MapToDMDTO(message);

            //Push to direct chat group in real time
            await _hubContext.Clients
                .Group($"direct_{conversation.Id}")
                .SendAsync("ReceiveDirectMessage", response);

            //Notify recipient if they are not in the conversation group
            await _notificationService.SendAsync(
                recipient.Id,
                NotificationType.DirectMessageReceived,
                $"New message from {message.Sender?.FullName}.",
                conversation.Id,
                NotificationReferenceType.DirectConversation
            );

            await _hubContext.Clients
                .Group($"user_{recipient.Id}")
                .SendAsync("NewDirectMessageNotification", new
                {
                    ConversationId = conversation.Id,
                    From = message.Sender?.FullName
                });

            return response;
        }


        //Get all conversations for inbox — ordered by most recent message
        public async Task<List<DirectMessageDTO.DirectConversationSummaryDTO>> GetInboxAsync(string userId)
        {
            var conversations = await _directMessageRepository.GetConversationsByUserIdAsync(userId);

            var summaries = new List<DirectMessageDTO.DirectConversationSummaryDTO>();

            foreach (var c in conversations)
            {
                var isInitiator = c.InitiatedById == userId;
                var cutoff = isInitiator ? c.InitiatorDeletedAt : c.OtherDeletedAt;

                var messages = await _directMessageRepository.GetMessagesAsync(c.Id, cutoff);

                var otherUser = isInitiator ? c.OtherUser : c.InitiatedBy;
                var lastMessage = messages.LastOrDefault();
                var unreadCount = messages.Count(m => m.SenderId != userId && !m.IsRead);
                var isHidden = isInitiator ? c.HiddenForInitiator : c.HiddenForOther;

                summaries.Add(new DirectMessageDTO.DirectConversationSummaryDTO
                {
                    Id = c.Id,
                    OtherUserId = otherUser.Id,
                    OtherUserName = otherUser.FullName,
                    OtherUserAvatarUrl = otherUser.AvatarUrl,
                    LastMessageContent = lastMessage?.Content,
                    LastMessageAt = lastMessage?.SentAt,
                    UnreadCount = unreadCount,
                    IsHidden = isHidden,
                    CreatedAt = c.CreatedAt
                });
            }

            return summaries;
        }


        //Get full thread, respects per-user deleted-at cutoff, auto-marks incoming as read
        public async Task<DirectMessageDTO.DirectMessageThreadDTO> GetThreadAsync(int conversationId, string userId)
        {
            var conversation = await _directMessageRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
                throw new KeyNotFoundException($"Conversation {conversationId} not found.");

            var isInitiator = conversation.InitiatedById == userId;
            var isOther = conversation.OtherUserId == userId;

            if (!isInitiator && !isOther)
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            var cutoff = isInitiator ? conversation.InitiatorDeletedAt : conversation.OtherDeletedAt;
            var messages = await _directMessageRepository.GetMessagesAsync(conversationId, cutoff);

            //Auto-mark incoming messages as read on open
            var unread = messages.Where(m => m.SenderId != userId && !m.IsRead).ToList();
            foreach (var m in unread)
                m.IsRead = true;

            if (unread.Any())
            {
                await _directMessageRepository.SaveChangesAsync();

                await _hubContext.Clients
                    .Group($"direct_{conversationId}")
                    .SendAsync("DirectMessagesRead", new { ConversationId = conversationId, ReadBy = userId });
            }

            var otherUser = isInitiator ? conversation.OtherUser : conversation.InitiatedBy;

            return new DirectMessageDTO.DirectMessageThreadDTO
            {
                ConversationId = conversationId,
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.FullName,
                OtherUserAvatarUrl = otherUser.AvatarUrl,
                Messages = messages.Select(MapToDMDTO).ToList()
            };
        }

        //Mark all unread messages as read, push read receipt via SignalR
        public async Task MarkAsReadAsync(int conversationId, string userId)
        {
            var conversation = await _directMessageRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
                throw new KeyNotFoundException($"Conversation {conversationId} not found.");

            var isInitiator = conversation.InitiatedById == userId;
            var isOther = conversation.OtherUserId == userId;

            if (!isInitiator && !isOther)
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            var cutoff = isInitiator ? conversation.InitiatorDeletedAt : conversation.OtherDeletedAt;
            var messages = await _directMessageRepository.GetMessagesAsync(conversationId, cutoff);

            var unread = messages.Where(m => m.SenderId != userId && !m.IsRead).ToList();
            if (!unread.Any()) return;

            foreach (var m in unread)
                m.IsRead = true;

            await _directMessageRepository.SaveChangesAsync();

            await _hubContext.Clients
                .Group($"direct_{conversationId}")
                .SendAsync("DirectMessagesRead", new { ConversationId = conversationId, ReadBy = userId });
        }


        //Hide/Delete conversation for this user — sets hidden flag and records cutoff timestamp
        public async Task HideConversationAsync(int conversationId, string userId)
        {
            var conversation = await _directMessageRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
                throw new KeyNotFoundException($"Conversation {conversationId} not found.");

            var isInitiator = conversation.InitiatedById == userId;
            var isOther = conversation.OtherUserId == userId;

            if (!isInitiator && !isOther)
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            if (isInitiator)
            {
                conversation.HiddenForInitiator = true;
                conversation.InitiatorDeletedAt = DateTime.UtcNow;
            }
            else
            {
                conversation.HiddenForOther = true;
                conversation.OtherDeletedAt = DateTime.UtcNow;
            }

            await _directMessageRepository.SaveChangesAsync();
        }


        //Used by ChatHub to validate before joining a direct chat group
        public async Task<bool> IsParticipantAsync(int conversationId, string userId)
        {
            return await _directMessageRepository.IsParticipantAsync(conversationId, userId);
        }

        //Mapper
        private static DirectMessageDTO.DirectMessageResponseDTO MapToDMDTO(DirectMessage m)
        {
            return new DirectMessageDTO.DirectMessageResponseDTO
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.FullName ?? string.Empty,
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                Content = m.Content,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            };
        }

    }
}
