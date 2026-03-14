using backend.DTOs;

namespace backend.Interfaces
{
    public interface IUserFavoriteService
    {
        Task<List<FavoriteDTO.FavoriteResponseDTO>> GetMyFavoritesAsync(string userId);
        Task AddAsync(string userId, int itemId);
        Task RemoveAsync(string userId, int itemId);
        Task<FavoriteDTO.FavoriteResponseDTO> ToggleNotifyAsync(string userId, int itemId, bool notifyWhenAvailable);
    }
}