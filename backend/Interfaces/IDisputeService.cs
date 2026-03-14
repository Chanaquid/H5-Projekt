using backend.DTOs;

namespace backend.Interfaces
{
    public interface IDisputeService
    {
        //User actions
        Task<DisputeDTO.DisputeDetailDTO> CreateAsync(string filedById, DisputeDTO.CreateDisputeDTO dto);
        Task<DisputeDTO.DisputeDetailDTO> SubmitResponseAsync(int disputeId, string responderId, DisputeDTO.DisputeResponseDTO dto);
        Task<DisputeDTO.DisputeDetailDTO> GetByIdAsync(int disputeId, string requestingUserId);
        Task<List<DisputeDTO.DisputeSummaryDTO>> GetDisputesByUserIdAsync(string userId);

        //Photo upload
        Task AddPhotoAsync(int disputeId, string submittedById, string photoUrl, string? caption);

        //Admin actions
        Task<List<DisputeDTO.DisputeSummaryDTO>> GetAllOpenAsync();
        Task<DisputeDTO.DisputeDetailDTO> IssueVerdictAsync(int disputeId, string adminId, DisputeDTO.AdminVerdictDTO dto);
    }
}
