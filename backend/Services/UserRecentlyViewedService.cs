using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class UserRecentlyViewedService : IUserRecentlyViewedService
    {
        private readonly IUserRecentlyViewedRepository _recentlyViewedRepository;
        private readonly IItemRepository _itemRepository;

        public UserRecentlyViewedService(
            IUserRecentlyViewedRepository recentlyViewedRepository,
            IItemRepository itemRepository)
        {
            _recentlyViewedRepository = recentlyViewedRepository;
            _itemRepository = itemRepository;
        }

        // Get the last 10 recently viewed items for the logged-in user
        public async Task<List<RecentlyViewedDTO.RecentlyViewedResponseDTO>> GetMyRecentlyViewedAsync(string userId)
        {
            var entries = await _recentlyViewedRepository.GetAllByUserIdAsync(userId, limit: 10);
            return entries.Select(MapToDTO).ToList();
        }

        public async Task TrackViewAsync(string userId, int itemId)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Item not found.");

            // Don't track views on your own items
            if (item.OwnerId == userId) return;

            var existing = await _recentlyViewedRepository.GetAsync(userId, itemId);

            if (existing != null)
            {
                // Already tracked — just update the timestamp
                existing.ViewedAt = DateTime.UtcNow;
            }
            else
            {
                // New item — enforce max 10 cap before inserting
                var allEntries = await _recentlyViewedRepository.GetAllByUserIdAsync(userId, limit: 100);
                var excess = allEntries.Skip(9).ToList(); //keep 9, remove rest, then add 1
                foreach (var old in excess)
                    _recentlyViewedRepository.Remove(old);

                await _recentlyViewedRepository.AddAsync(new UserRecentlyViewedItem
                {
                    UserId = userId,
                    ItemId = itemId,
                    ViewedAt = DateTime.UtcNow
                });
            }

            await _recentlyViewedRepository.SaveChangesAsync();
        }

        //Mapper
        private static RecentlyViewedDTO.RecentlyViewedResponseDTO MapToDTO(UserRecentlyViewedItem r)
        {
            var item = r.Item;
            var reviews = item.Reviews?.ToList() ?? new();
            var activeLoans = item.Loans?.Where(l => l.Status == LoanStatus.Active).ToList() ?? new();

            return new RecentlyViewedDTO.RecentlyViewedResponseDTO
            {
                ViewedAt = r.ViewedAt,
                Item = new ItemDTO.ItemSummaryDTO
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = item.Description, 
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
                    IsCurrentlyOnLoan = activeLoans.Any()
                }
            };
        }
    }
}