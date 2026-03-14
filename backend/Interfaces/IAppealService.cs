using backend.DTOs;

namespace backend.Interfaces
{
    public interface IAppealService
    {
        //User
        Task<AppealDTO.AppealResponseDTO> CreateScoreAppealAsync(string userId, AppealDTO.CreateScoreAppealDTO dto);
        Task<AppealDTO.AppealResponseDTO> CreateFineAppealAsync(string userId, AppealDTO.CreateFineAppealDTO dto);
        Task<List<AppealDTO.AppealResponseDTO>> GetMyAppealsAsync(string userId);
        //Admin
        Task<List<AppealDTO.AppealResponseDTO>> GetAllPendingAsync();
        Task<AppealDTO.AppealResponseDTO> DecideScoreAppealAsync(int appealId, string adminId, AppealDTO.AdminScoreAppealDecisionDTO dto);
        Task<AppealDTO.AppealResponseDTO> DecideFineAppealAsync(int appealId, string adminId, AppealDTO.AdminFineAppealDecisionDTO dto);
    }
}
