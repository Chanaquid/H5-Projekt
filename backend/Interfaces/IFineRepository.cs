using backend.Models;

namespace backend.Interfaces
{
    public interface IFineRepository
    {
        Task<List<Fine>> GetByUserIdAsync(string userId);
        Task<List<Fine>> GetAllUnpaidAsync();
        Task<Fine?> GetByIdAsync(int fineId);
        Task<List<Fine>> GetByDisputeIdAsync(int disputeId);
        Task<List<Fine>> GetAllAsync();

        Task<Fine?> GetByIdWithDetailsAsync(int fineId); //Includes Loan + Loan.Item
        Task<List<Fine>> GetPendingVerificationAsync();
        Task AddAsync(Fine fine);
        void Update(Fine fine);
        Task SaveChangesAsync();
    }
}
