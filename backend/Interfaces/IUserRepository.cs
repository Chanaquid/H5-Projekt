using backend.Models;

namespace backend.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string userId);
        Task<ApplicationUser?> GetByIdIgnoreFiltersAsync(string userId);  //Admin use — includes soft-deleted users
        Task<ApplicationUser?> GetByIdWithDetailsAsync(string userId); //Includes ScoreHistory
        Task<List<ApplicationUser>> GetAllAsync(); //Admin — includes deleted
        Task<List<ScoreHistory>> GetScoreHistoryAsync(string userId);

        Task AddScoreHistoryAsync(ScoreHistory entry);
        Task UpdateAsync(ApplicationUser user);
        Task SaveChangesAsync();
    }
}
