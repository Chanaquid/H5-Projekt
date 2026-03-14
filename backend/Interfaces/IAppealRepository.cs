using backend.Models;

namespace backend.Interfaces
{
    public interface IAppealRepository
    {
        Task<Appeal?> GetByIdAsync(int appealId);
        Task<Appeal?> GetByIdWithDetailsAsync(int appealId); //Includes User, ResolvedByAdmin
        Task<Appeal?> GetPendingByUserIdAsync(string userId); //Only one pending appeal at a time
        Task<Appeal?> GetPendingFineAppealByFineIdAsync(int fineId);
        Task<List<Appeal>> GetAllByUserIdAsync(string userId);
        Task<List<Appeal>> GetAllPendingAsync();
        Task AddAsync(Appeal appeal);
        void Update(Appeal appeal);
        Task SaveChangesAsync();
    }
}
