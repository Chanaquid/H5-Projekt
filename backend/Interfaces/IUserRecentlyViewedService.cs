using backend.DTOs;

namespace backend.Interfaces
{
    public interface IUserRecentlyViewedService
    {
        Task<List<RecentlyViewedDTO.RecentlyViewedResponseDTO>> GetMyRecentlyViewedAsync(string userId);
        Task TrackViewAsync(string userId, int itemId);
    }
}