using backend.DTOs;
using backend.Models;

namespace backend.Interfaces
{
    public interface ILoanRepository
    {
        //Queries
        Task<Loan?> GetByIdAsync(int loanId);
        Task<Loan?> GetByIdWithDetailsAsync(int loanId); //Includes Item, Borrower, Owner, Fines, SnapshotPhotos
        Task<List<Loan>> GetByBorrowerIdAsync(string borrowerId);
        Task<List<Loan>> GetByOwnerIdAsync(string ownerId);
        Task<List<Loan>> GetAllAsync();
        Task<List<Loan>> GetPendingAdminApprovalsAsync(); //AdminPending status — low score queue
        Task<List<Loan>> GetActiveAndOverdueAsync(); //For late loan processing job
        Task<bool> HasActiveLoansAsync(string userId); //Used by UserService delete check
        Task<List<Loan>> GetLoanHistoryByItemIdAsync(int itemId);


        //CRUD but no delete since we dont want loan to be deleted
        Task AddAsync(Loan loan);
        void Update(Loan loan);
        Task SaveChangesAsync();
    }
}
