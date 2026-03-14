using backend.Models;

namespace backend.Interfaces
{
    public interface IUserBlockRepository
    {

        Task<UserBlock?> GetAsync(string blockerId, string blockedId);
        Task<List<UserBlock>> GetBlocksByUserIdAsync(string userId);

        //Check if a block exists in either direction between two users
        Task<bool> IsBlockedAsync(string userAId, string userBId);

        Task AddAsync(UserBlock block);
        Task RemoveAsync(UserBlock block);
        Task SaveChangesAsync();
    }
}
