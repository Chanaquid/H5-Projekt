using backend.DTOs;
using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace backend.Services
{
    public class LoanMessageService : ILoanMessageService
    {
        private readonly ILoanMessageRepository _loanMessageRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IOnlineTracker _onlineTracker;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoanMessageService(
            ILoanMessageRepository loanMessageRepository,
            ILoanRepository loanRepository,
            IHubContext<ChatHub> hubContext,
            INotificationService notificationService,
            IOnlineTracker onlineTracker,
            UserManager<ApplicationUser> userManager)
        {
            _loanMessageRepository = loanMessageRepository;
            _loanRepository = loanRepository;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _onlineTracker = onlineTracker;
            _userManager = userManager;
        }

        //Send a message in a loan chat thread
        public async Task<ChatDTO.LoanMessageDTO.LoanMessageResponseDTO> SendAsync(string senderId, ChatDTO.LoanMessageDTO.SendLoanMessageDTO dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(dto.LoanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {dto.LoanId} not found.");

            var isOwner = loan.Item.OwnerId == senderId;
            var isBorrower = loan.BorrowerId == senderId;

            if (!isOwner && !isBorrower)
                throw new UnauthorizedAccessException("You are not a party to this loan.");

            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ArgumentException("Message content cannot be empty.");

            //lock chat agter a week
            var terminalStatuses = new[] { LoanStatus.Returned, LoanStatus.Cancelled, LoanStatus.Rejected };
            if (terminalStatuses.Contains(loan.Status))
            {
                var lockedAt = loan.ActualReturnDate ?? loan.UpdatedAt ?? loan.CreatedAt;
                if (lockedAt < DateTime.UtcNow.AddDays(-7))
                    throw new InvalidOperationException("This loan chat has been locked. Chats are disabled 1 week after a loan is completed.");
            }


            var message = new LoanMessage
            {
                LoanId = dto.LoanId,
                SenderId = senderId,
                Content = dto.Content.Trim(),
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            await _loanMessageRepository.AddAsync(message);
            await _loanMessageRepository.SaveChangesAsync();

            //Load only the sender reference instead of reloading the entire thread
            await _loanMessageRepository.LoadSenderAsync(message);

            var response = MapToLoanMessageDTO(message);

            //Push to loan group in real time
            await _hubContext.Clients
                .Group($"loan_{dto.LoanId}")
                .SendAsync("ReceiveMessage", response);

            //If other party is NOT in the chat group, send a notification
            var otherPartyId = isOwner ? loan.BorrowerId : loan.Item.OwnerId;
            var otherPartyOnline = _onlineTracker.IsUserInLoanGroup(otherPartyId, dto.LoanId);

            if (otherPartyOnline)
            {
                //Other party is viewing the chat — mark the message as read immediately
                message.IsRead = true;
                await _loanMessageRepository.SaveChangesAsync();

                //Push read receipt to the sender immediately
                await _hubContext.Clients
                    .Group($"loan_{dto.LoanId}")
                    .SendAsync("MessagesRead", new { loanId = dto.LoanId, readBy = otherPartyId });


            }

            else
            {
                await _notificationService.SendAsync(
                    otherPartyId,
                    NotificationType.MessageReceived,
                    $"New message from {message.Sender?.FullName} about '{loan.Item.Title}'.",
                    dto.LoanId,
                    NotificationReferenceType.Loan
                );

                //Also push a real-time toast to their personal notification group
                //so they see a red dot even while browsing other pages
                await _hubContext.Clients
                    .Group($"user_{otherPartyId}")
                    .SendAsync("NewMessageNotification", new
                    {
                        LoanId = dto.LoanId,
                        ItemTitle = loan.Item.Title,
                        From = message.Sender?.FullName
                    });
            }

            return response;
        }

        //Get full loan chat thread, auto-marks incoming messages as read
        public async Task<ChatDTO.LoanMessageDTO.LoanMessageThreadDTO> GetThreadAsync(int loanId, string requestingUserId, bool isAdmin = false)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            var isOwner = loan.Item.OwnerId == requestingUserId;
            var isBorrower = loan.BorrowerId == requestingUserId;

            if (!isOwner && !isBorrower && !isAdmin)
                throw new UnauthorizedAccessException("You are not a party to this loan.");

            var messages = await _loanMessageRepository.GetByLoanIdAsync(loanId);

            // Only mark as read if the user is an actual party, not admin viewing
            if (!isAdmin)
            {
                var unread = messages.Where(m => m.SenderId != requestingUserId && !m.IsRead).ToList();
                foreach (var m in unread)
                    m.IsRead = true;

                if (unread.Any())
                {
                    await _loanMessageRepository.SaveChangesAsync();
                    await _hubContext.Clients
                        .Group($"loan_{loanId}")
                        .SendAsync("MessagesRead", new { LoanId = loanId, ReadBy = requestingUserId });
                }
            }

            var otherParty = isOwner ? loan.Borrower : loan.Item.Owner;

            return new ChatDTO.LoanMessageDTO.LoanMessageThreadDTO
            {
                LoanId = loanId,
                ItemTitle = loan.Item.Title,
                OtherPartyName = otherParty.FullName,
                OtherPartyAvatarUrl = otherParty.AvatarUrl,
                Messages = messages.Select(MapToLoanMessageDTO).ToList()
            };
        }

        //Mark all unread messages in a thread as read, pushes read receipt via SignalR
        public async Task MarkThreadAsReadAsync(int loanId, string userId)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            var isOwner = loan.Item.OwnerId == userId;
            var isBorrower = loan.BorrowerId == userId;

            if (!isOwner && !isBorrower)
                throw new UnauthorizedAccessException("You are not a party to this loan.");

            var messages = await _loanMessageRepository.GetByLoanIdAsync(loanId);
            var unread = messages.Where(m => m.SenderId != userId && !m.IsRead).ToList();

            if (!unread.Any()) return;

            foreach (var m in unread)
                m.IsRead = true;

            await _loanMessageRepository.SaveChangesAsync();

            //Push read receipt so sender's UI can show a checkmark
            await _hubContext.Clients
                .Group($"loan_{loanId}")
                .SendAsync("MessagesRead", new { loanId = loanId, readBy = userId });
        }

        //Used by ChatHub to validate the connecting user before joining a loan group
        public async Task<bool> IsPartyToLoanAsync(int loanId, string userId)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null) return false;

            var isParty = loan.Item.OwnerId == userId || loan.BorrowerId == userId;
            if (isParty) return true;

            // Allow admins to join for read-only real-time viewing
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && await _userManager.IsInRoleAsync(user, "Admin");
        }

        //Mapper
        private static ChatDTO.LoanMessageDTO.LoanMessageResponseDTO MapToLoanMessageDTO(LoanMessage m)
        {
            return new ChatDTO.LoanMessageDTO.LoanMessageResponseDTO
            {
                Id = m.Id,
                LoanId = m.LoanId,
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