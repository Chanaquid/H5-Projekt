using backend.DTOs;

namespace backend.Interfaces
{
    public interface IVerificationService
    {
        //User actions
        Task<VerificationDTO.VerificationRequestResponseDTO> SubmitRequestAsync(string userId, VerificationDTO.CreateVerificationRequestDTO dto);
        Task<VerificationDTO.VerificationRequestResponseDTO> GetUserRequestAsync(string userId);

        //Admin actions
        Task<List<VerificationDTO.VerificationRequestResponseDTO>> GetAllPendingAsync();
        Task<List<VerificationDTO.VerificationRequestResponseDTO>> GetAllAsync();

        Task<VerificationDTO.VerificationRequestResponseDTO> DecideAsync(int requestId, string adminId, VerificationDTO.AdminVerificationDecisionDTO dto);
    }
}
