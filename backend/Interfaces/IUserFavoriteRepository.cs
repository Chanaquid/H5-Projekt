using backend.Models;

namespace backend.Interfaces
{
    public interface IUserFavoriteRepository
    {
        Task<List<UserFavoriteItem>> GetAllByUserIdAsync(string userId);
        Task<UserFavoriteItem?> GetAsync(string userId, int itemId);
        Task<bool> ExistsAsync(string userId, int itemId);
        Task AddAsync(UserFavoriteItem favorite);
        void Remove(UserFavoriteItem favorite);
        Task SaveChangesAsync();
    }
}