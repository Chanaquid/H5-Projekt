using backend.Models;

namespace backend.Interfaces
{
    public interface ISupportChatRepository
    {
        //Threads
        Task<SupportThread?> GetThreadByIdAsync(int threadId);
        Task<SupportThread?> GetThreadWithMessagesAsync(int threadId);
        Task<List<SupportThread>> GetThreadsByUserIdAsync(string userId);
        Task<List<SupportThread>> GetAllOpenThreadsAsync(); //Admin queue — Open + Claimed
        Task AddThreadAsync(SupportThread thread);

        //Messages
        Task AddMessageAsync(SupportMessage message);
        Task MarkMessagesReadAsync(int threadId, string readByUserId);

        Task SaveChangesAsync();
    }
}
