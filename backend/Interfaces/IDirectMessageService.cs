using static backend.DTOs.ChatDTO;

namespace backend.Interfaces
{
    public interface IDirectMessageService
    {
        Task<DirectMessageDTO.DirectMessageResponseDTO> SendAsync(string senderId, DirectMessageDTO.SendDirectMessageDTO dto);

        //Get all conversations for the inbox list
        Task<List<DirectMessageDTO.DirectConversationSummaryDTO>> GetInboxAsync(string userId);

        //Get full thread for a conversation - excludes the deleted chat messages for that user
        Task<DirectMessageDTO.DirectMessageThreadDTO> GetThreadAsync(int conversationId, string userId);

        //Mark all unread messages in a conversation as read
        Task MarkAsReadAsync(int conversationId, string userId);

        //Hide a conversation — sets hidden flag and records deletedAt cutoff for this user
        Task HideConversationAsync(int conversationId, string userId);

        //Used by ChatHub to validate before joining a direct chat group
        Task<bool> IsParticipantAsync(int conversationId, string userId);
    }
}
