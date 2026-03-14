using backend.DTOs;

namespace backend.Interfaces
{
    public interface ILoanMessageService
    {
        Task<ChatDTO.LoanMessageDTO.LoanMessageResponseDTO> SendAsync(string senderId, ChatDTO.LoanMessageDTO.SendLoanMessageDTO dto);
        Task<ChatDTO.LoanMessageDTO.LoanMessageThreadDTO> GetThreadAsync(int loanId, string requestingUserId);
        Task MarkThreadAsReadAsync(int loanId, string userId);
        Task<bool> IsPartyToLoanAsync(int loanId, string userId);  //Used by ChatHub to validate connection

    }
}
