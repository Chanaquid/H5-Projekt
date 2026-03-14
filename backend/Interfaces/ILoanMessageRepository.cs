using backend.Models;

namespace backend.Interfaces
{
    public interface ILoanMessageRepository
    {
        Task<List<LoanMessage>> GetByLoanIdAsync(int loanId);

        //Get all loan messages sent by a specific user — used by admin detail view
        Task<List<LoanMessage>> GetByUserIdAsync(string userId);

        Task AddAsync(LoanMessage message);
        Task LoadSenderAsync(LoanMessage message);  //Loads Sender nav property without reloading entire thread
        Task SaveChangesAsync();
    }
}
