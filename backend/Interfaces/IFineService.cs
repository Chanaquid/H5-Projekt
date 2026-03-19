using backend.DTOs;

namespace backend.Interfaces
{
    public interface IFineService
    {
        //User actions
        Task<List<FineDTO.FineResponseDTO>> GetUserFinesAsync(string userId);
        Task<FineDTO.FineResponseDTO> MarkAsPaidAsync(string userId, FineDTO.PayFineDTO dto);
        Task<List<FineDTO.FineResponseDTO>> GetByDisputeIdAsync(int disputeId);


        //Admin actions
        Task<List<FineDTO.FineResponseDTO>> GetAllUnpaidAsync();
        Task<List<FineDTO.FineResponseDTO>> GetPendingVerificationAsync();
        Task<FineDTO.FineResponseDTO> AdminIssueFineAsync(FineDTO.AdminIssueFineDTO dto); //Custom fine 
        Task<FineDTO.FineResponseDTO> AdminUpdateFineAsync(int fineId, FineDTO.AdminUpdateFineDTO dto);
        Task<FineDTO.FineResponseDTO> AdminConfirmPaymentAsync(string adminId, FineDTO.AdminFineVerificationDTO dto);
        Task<FineDTO.FineResponseDTO> IssueLateReturnFineAsync(int loanId);
        Task<FineDTO.FineResponseDTO> IssueDamagedFineAsync(int loanId, int? disputeId = null);
        Task<FineDTO.FineResponseDTO> IssueLostFineAsync(int loanId, int? disputeId = null);
        Task<FineDTO.FineResponseDTO?> IssueCustomFineAsync(int loanId, int disputeId, string userId, decimal? amount, int? scoreAdjustment);
    }
}
