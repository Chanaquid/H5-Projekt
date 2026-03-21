using backend.DTOs;
using backend.Models;

namespace backend.Interfaces
{
    public interface ILoanService
    {
        //Borrower actions
        Task<LoanDTO.LoanDetailDTO> CreateAsync(string borrowerId, LoanDTO.CreateLoanDTO dto);
        Task<LoanDTO.LoanDetailDTO> CancelAsync(int loanId, string borrowerId, LoanDTO.CancelLoanDTO dto);
        Task<LoanDTO.LoanDetailDTO> RequestExtensionAsync(int loanId, string borrowerId, LoanDTO.RequestExtensionDTO dto);
        Task<List<LoanDTO.LoanSummaryDTO>> GetBorrowedLoansAsync(string borrowerId);

        //Owner actions
        Task<LoanDTO.LoanDetailDTO> DecideAsync(int loanId, string ownerId, LoanDTO.LoanDecisionDTO dto);
        Task<LoanDTO.LoanDetailDTO> DecideExtensionAsync(int loanId, string ownerId, LoanDTO.ExtensionDecisionDTO dto);
        Task<List<LoanDTO.LoanSummaryDTO>> GetOwnedLoansAsync(string ownerId);
        Task<List<LoanDTO.LoanSummaryDTO>> GetLoanHistoryByItemIdAsync(int itemId, string requestingUserId, bool isAdmin = false);

        //Shared
        Task<LoanDTO.LoanDetailDTO> GetByIdAsync(int loanId, string requestingUserId, bool isAdmin = false);
        Task HandleLoanReturnAsync(Loan loan); // handles loan return

        //Admin actions
        Task<List<LoanDTO.AdminPendingLoanDTO>> GetPendingApprovalsAsync();
        Task<LoanDTO.LoanDetailDTO> AdminDecideAsync(int loanId, string adminId, LoanDTO.LoanDecisionDTO dto);
        Task<List<LoanDTO.LoanSummaryDTO>> GetAllLoansAsync();


        //Late loan - daily background servuce

        Task ProcessLateLoansAsync();
    }
}
