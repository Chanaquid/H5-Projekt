using backend.Models;

namespace backend.Interfaces
{
    public interface IDisputeRepository
    {
        Task<Dispute?> GetByIdAsync(int disputeId);
        Task<Dispute?> GetByIdWithDetailsAsync(int disputeId); //Includes Loan, FiledBy, Photos, SnapshotPhotos
        Task<List<Dispute>> GetByUserIdAsync(string userId);
        Task<List<Dispute>> GetAllOpenAsync();
        Task<List<Dispute>> GetDisputeHistoryByItemIdAsync(int itemId);

        Task AddAsync(Dispute dispute);
        Task AddPhotoAsync(DisputePhoto photo);
        void Update(Dispute dispute);
        Task SaveChangesAsync();
    }
}
