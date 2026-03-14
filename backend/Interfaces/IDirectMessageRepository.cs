using backend.Models;

namespace backend.Interfaces
{
    public interface IDirectMessageRepository
    {
        Task<DirectConversation?> GetConversationAsync(string userAId, string userBId);

        //Get conversation by id with both user details included
        Task<DirectConversation?> GetConversationByIdAsync(int conversationId);

        //Get all conversations for a user, excluding ones they have deleted
        Task<List<DirectConversation>> GetConversationsByUserIdAsync(string userId);

        //Check if a user is a participant of a conversation — used by ChatHub
        Task<bool> IsParticipantAsync(int conversationId, string userId);

        //Get messages in a conversation, doesnt show all previous messages deleted by that user
        Task<List<DirectMessage>> GetMessagesAsync(int conversationId, DateTime? after);

        Task AddConversationAsync(DirectConversation conversation);
        Task AddMessageAsync(DirectMessage message);
        Task LoadMessageSenderAsync(DirectMessage message);
        Task SaveChangesAsync();

    }
}
