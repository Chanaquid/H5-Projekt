using backend.DTOs;

namespace backend.Interfaces
{
    public interface IUserService
    {
        //Profile
        Task<UserDTO.UserProfileDTO> GetProfileAsync(string userId);
        Task<UserDTO.UserSummaryDTO> GetPublicProfileAsync(string userId); //Public — safe subset only
        Task<UserDTO.UserProfileDTO> UpdateProfileAsync(string userId, UserDTO.UpdateProfileDTO dto);
        Task DeleteAccountAsync(string userId, UserDTO.DeleteAccountDTO dto);

        //Score
        Task<List<UserDTO.ScoreHistoryDTO>> GetScoreHistoryAsync(string userId);
        Task<List<UserDTO.ScoreHistoryDTO>> GetScoreHistoryByLoanIdAsync(int loanId, string requestingUserId);
        Task AdminAdjustScoreAsync(string targetUserId, UserDTO.AdminScoreAdjustDTO dto);

        //Admin
        Task<UserDTO.AdminUserDTO> AdminEditUserAsync(string targetUserId, string adminId, UserDTO.AdminEditUserDTO dto);
        Task<List<UserDTO.AdminUserDTO>> GetAllUsersAsync();
        Task<UserDTO.AdminUserDetailDTO> GetUserByIdAsync(string userId); //Admin-level detail
        Task<UserDTO.AdminDeleteResultDTO> AdminSoftDeleteUserAsync(string targetUserId, string adminId);
    }
}
