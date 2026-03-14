using backend.Models;

namespace backend.Interfaces
{
    public interface IVerificationRepository
    {
        Task<VerificationRequest?> GetByIdAsync(int requestId);
        Task<VerificationRequest?> GetByIdWithDetailsAsync(int requestId); //Includes User, ReviewedByAdmin
        Task<VerificationRequest?> GetPendingByUserIdAsync(string userId); //One pending request at a time
        Task<VerificationRequest?> GetLatestByUserIdAsync(string userId); //Most recent regardless of status
        Task<List<VerificationRequest>> GetAllPendingAsync();
        Task<List<VerificationRequest>> GetAllByUserIdAsync(string userId);//Admin use — get full verification history for a user

        Task AddAsync(VerificationRequest request);
        void Update(VerificationRequest request);
        Task SaveChangesAsync();
    }
}
