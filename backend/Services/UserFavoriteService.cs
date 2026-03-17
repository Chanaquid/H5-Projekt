using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class UserFavoriteService : IUserFavoriteService
    {
        private readonly IUserFavoriteRepository _favoriteRepository;
        private readonly IItemRepository _itemRepository;

        public UserFavoriteService(
            IUserFavoriteRepository favoriteRepository,
            IItemRepository itemRepository)
        {
            _favoriteRepository = favoriteRepository;
            _itemRepository = itemRepository;
        }

        //Get all favorites for the logged-in user
        public async Task<List<FavoriteDTO.FavoriteResponseDTO>> GetMyFavoritesAsync(string userId)
        {
            var favorites = await _favoriteRepository.GetAllByUserIdAsync(userId);
            return favorites.Select(MapToDTO).ToList();
        }

        //Add an item to favorites
        public async Task AddAsync(string userId, int itemId)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Item not found.");

            //Cannot favorite your own item
            //if (item.OwnerId == userId)
            //    throw new InvalidOperationException("You cannot favorite your own item.");

            var alreadyExists = await _favoriteRepository.ExistsAsync(userId, itemId);
            if (alreadyExists)
                throw new InvalidOperationException("Item is already in your favorites.");

            var favorite = new UserFavoriteItem
            {
                UserId = userId,
                ItemId = itemId,
                NotifyWhenAvailable = false,
                SavedAt = DateTime.UtcNow
            };

            await _favoriteRepository.AddAsync(favorite);
            await _favoriteRepository.SaveChangesAsync();
        }

        // Remove an item from favorites
        public async Task RemoveAsync(string userId, int itemId)
        {
            var favorite = await _favoriteRepository.GetAsync(userId, itemId);
            if (favorite == null)
                throw new KeyNotFoundException("Favorite not found.");

            _favoriteRepository.Remove(favorite);
            await _favoriteRepository.SaveChangesAsync();
        }

        // Toggle the notify-when-available flag on a favorite
        public async Task<FavoriteDTO.FavoriteResponseDTO> ToggleNotifyAsync(string userId, int itemId, bool notifyWhenAvailable)
        {
            var favorite = await _favoriteRepository.GetAsync(userId, itemId);
            if (favorite == null)
                throw new KeyNotFoundException("Favorite not found. Add the item to favorites first.");

            favorite.NotifyWhenAvailable = notifyWhenAvailable;
            await _favoriteRepository.SaveChangesAsync();

            // Reload with full item details for the response
            var favorites = await _favoriteRepository.GetAllByUserIdAsync(userId);
            var updated = favorites.First(f => f.ItemId == itemId);
            return MapToDTO(updated);
        }

        // Mapper — reuses the same logic as ItemService.MapToSummaryDTO
        private static FavoriteDTO.FavoriteResponseDTO MapToDTO(UserFavoriteItem f)
        {
            var item = f.Item;
            var reviews = item.Reviews?.ToList() ?? new();
            var activeLoans = item.Loans?.Where(l => l.Status == LoanStatus.Active).ToList() ?? new();

            return new FavoriteDTO.FavoriteResponseDTO
            {
                NotifyWhenAvailable = f.NotifyWhenAvailable,
                SavedAt = f.SavedAt,
                Item = new ItemDTO.ItemSummaryDTO
                {
                    Id = item.Id,
                    Title = item.Title,
                    Condition = item.Condition.ToString(),
                    Status = item.Status.ToString(),
                    PickupAddress = item.PickupAddress,
                    PickupLatitude = item.PickupLatitude,
                    PickupLongitude = item.PickupLongitude,
                    AvailableFrom = item.AvailableFrom,
                    AvailableUntil = item.AvailableUntil,
                    PrimaryPhotoUrl = item.Photos?.FirstOrDefault(p => p.IsPrimary)?.PhotoUrl,
                    CategoryName = item.Category?.Name ?? string.Empty,
                    CategoryIcon = item.Category?.Icon,
                    OwnerId = item.OwnerId,
                    OwnerName = item.Owner?.FullName ?? string.Empty,
                    AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0,
                    ReviewCount = reviews.Count,
                    IsActive = item.IsActive,
                    IsCurrentlyOnLoan = activeLoans.Any()
                }
            };
        }
    }
}