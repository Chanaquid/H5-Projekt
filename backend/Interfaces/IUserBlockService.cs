using static backend.DTOs.ChatDTO;

namespace backend.Interfaces
{
    public interface IUserBlockService
    {

        Task<UserBlockDTO.BlockResponseDTO> BlockAsync(string blockerId, string blockedUserId);
        Task UnblockAsync(string blockerId, string blockedUserId);
        Task<List<UserBlockDTO.BlockResponseDTO>> GetBlockedUsersAsync(string userId);
    }
}
