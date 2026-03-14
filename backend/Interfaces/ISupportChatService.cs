using static backend.DTOs.ChatDTO;

namespace backend.Interfaces
{
    public interface ISupportChatService
    {
        // User creates a new thread with an initial message
        Task<SupportChatDTO.SupportThreadDetailDTO> CreateThreadAsync(string userId, SupportChatDTO.CreateSupportThreadDTO dto);

        // Either party sends a message
        Task<SupportChatDTO.SupportMessageResponseDTO> SendMessageAsync(string senderId, SupportChatDTO.SendSupportMessageDTO dto);

        // Get full thread with messages — user can only get their own, admin can get any
        Task<SupportChatDTO.SupportThreadDetailDTO> GetThreadAsync(int threadId, string requestingUserId, bool isAdmin);

        // User gets all their own threads
        Task<List<SupportChatDTO.SupportThreadSummaryDTO>> GetMyThreadsAsync(string userId);

        // Admin gets all open/claimed threads
        Task<List<SupportChatDTO.SupportThreadSummaryDTO>> GetAllOpenThreadsAsync();

        // Admin claims a thread
        Task<SupportChatDTO.SupportThreadDetailDTO> ClaimThreadAsync(int threadId, string adminId);

        // Admin closes a thread
        Task<SupportChatDTO.SupportThreadDetailDTO> CloseThreadAsync(int threadId, string adminId);

        // Admin reopens a closed thread
        Task<SupportChatDTO.SupportThreadDetailDTO> ReopenThreadAsync(int threadId, string adminId);

        // Mark messages as read
        Task MarkReadAsync(int threadId, string userId);
    }
}