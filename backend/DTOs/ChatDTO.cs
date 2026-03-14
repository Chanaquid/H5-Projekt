namespace backend.DTOs
{
    public class ChatDTO
    {
        public class LoanMessageDTO
        {
            //Requests
            public class SendLoanMessageDTO
            {
                public int LoanId { get; set; }
                public string Content { get; set; } = string.Empty;
            }

            //Responses
            public class LoanMessageResponseDTO
            {
                public int Id { get; set; }
                public int LoanId { get; set; }
                public string SenderId { get; set; } = string.Empty;
                public string SenderName { get; set; } = string.Empty;
                public string? SenderAvatarUrl { get; set; }
                public string Content { get; set; } = string.Empty;
                public bool IsRead { get; set; }
                public DateTime SentAt { get; set; }
            }

            //Full thread returned when opening a loan's chat
            public class LoanMessageThreadDTO
            {
                public int LoanId { get; set; }
                public string ItemTitle { get; set; } = string.Empty;
                public string OtherPartyName { get; set; } = string.Empty;
                public string? OtherPartyAvatarUrl { get; set; }
                public List<LoanMessageResponseDTO> Messages { get; set; } = new();
            }
        }


        public class DirectMessageDTO
        {
            //Requests

            //Send dm to a user
            public class SendDirectMessageDTO
            {
                public string RecipientUsernameOrEmail { get; set; } = string.Empty;
                public string Content { get; set; } = string.Empty;
            }

            //Responses

            //Single direct message
            public class DirectMessageResponseDTO
            {
                public int Id { get; set; }
                public int ConversationId { get; set; }
                public string SenderId { get; set; } = string.Empty;
                public string SenderName { get; set; } = string.Empty;
                public string? SenderAvatarUrl { get; set; }
                public string Content { get; set; } = string.Empty;
                public bool IsRead { get; set; }
                public DateTime SentAt { get; set; }
            }

            //Compact conversation — used in the inbox list
            public class DirectConversationSummaryDTO
            {
                public int Id { get; set; }
                public string OtherUserId { get; set; } = string.Empty;
                public string OtherUserName { get; set; } = string.Empty;
                public string? OtherUserAvatarUrl { get; set; }
                public string? LastMessageContent { get; set; }
                public DateTime? LastMessageAt { get; set; }
                public int UnreadCount { get; set; }
                public bool IsHidden { get; set; }  //Whether the requesting user has hidden this conversation
                public DateTime CreatedAt { get; set; }
            }

            //Full thread returned when opening a conversation
            public class DirectMessageThreadDTO
            {
                public int ConversationId { get; set; }
                public string OtherUserId { get; set; } = string.Empty;
                public string OtherUserName { get; set; } = string.Empty;
                public string? OtherUserAvatarUrl { get; set; }
                public List<DirectMessageResponseDTO> Messages { get; set; } = new();
            }
        }


        public class SupportChatDTO
        {
            //Requests

            //User opens a new support thread
            public class CreateSupportThreadDTO
            {
                public string InitialMessage { get; set; } = string.Empty;
            }

            //Either party sends a message in an existing thread
            public class SendSupportMessageDTO
            {
                public int SupportThreadId { get; set; }
                public string Content { get; set; } = string.Empty;
            }

            //Admin claims or unclaims a thread
            public class ClaimThreadDTO
            {
                public int SupportThreadId { get; set; }
            }

            //Admin closes a thread  
            public class CloseThreadDTO
            {
                public int SupportThreadId { get; set; }
                public string? ClosingNote { get; set; }
            }

            //Responses

            //Single support message
            public class SupportMessageResponseDTO
            {
                public int Id { get; set; }
                public int SupportThreadId { get; set; }
                public string SenderId { get; set; } = string.Empty;
                public string SenderName { get; set; } = string.Empty;
                public string? SenderAvatarUrl { get; set; }
                public bool IsAdminSender { get; set; }  //True if the sender is an admin
                public string Content { get; set; } = string.Empty;
                public bool IsRead { get; set; }
                public DateTime SentAt { get; set; }
            }

            //Compact thread — used in user's support list and admin queue
            public class SupportThreadSummaryDTO
            {
                public int Id { get; set; }
                public string UserId { get; set; } = string.Empty;
                public string UserName { get; set; } = string.Empty;
                public string? UserAvatarUrl { get; set; }
                public string Status { get; set; } = string.Empty; //"Open", "Claimed", "Closed"
                public string? ClaimedByAdminName { get; set; }
                public string? LastMessageContent { get; set; }
                public DateTime? LastMessageAt { get; set; }
                public int UnreadCount { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            //Full thread detail — shown when opening a support conversation
            public class SupportThreadDetailDTO
            {
                public int Id { get; set; }
                public string UserId { get; set; } = string.Empty;
                public string UserName { get; set; } = string.Empty;
                public string? UserAvatarUrl { get; set; }
                public string Status { get; set; } = string.Empty;
                public string? ClaimedByAdminId { get; set; }
                public string? ClaimedByAdminName { get; set; }
                public DateTime? ClaimedAt { get; set; }
                public DateTime? ClosedAt { get; set; }
                public DateTime CreatedAt { get; set; }
                public List<SupportMessageResponseDTO> Messages { get; set; } = new();
            }
        }


        public class UserBlockDTO
        {
            //Requests
            public class BlockUserDTO
            {
                public string BlockedUserId { get; set; } = string.Empty;
            }

            //Responses
            public class BlockResponseDTO
            {
                public string BlockerId { get; set; } = string.Empty;
                public string BlockedId { get; set; } = string.Empty;
                public string BlockedUserName { get; set; } = string.Empty;
                public string? BlockedUserAvatarUrl { get; set; }
                public DateTime CreatedAt { get; set; }
            }
        }


    }
}
