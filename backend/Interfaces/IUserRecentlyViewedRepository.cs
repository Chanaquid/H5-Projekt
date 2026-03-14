using backend.Models;

namespace backend.Interfaces
{
    public interface IUserRecentlyViewedRepository
    {
        Task<List<UserRecentlyViewedItem>> GetAllByUserIdAsync(string userId, int limit = 20);
        Task<UserRecentlyViewedItem?> GetAsync(string userId, int itemId);
        Task AddAsync(UserRecentlyViewedItem entry);
        void Remove(UserRecentlyViewedItem entry);
        Task SaveChangesAsync();
    }
}