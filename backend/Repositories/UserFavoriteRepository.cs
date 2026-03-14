using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserFavoriteRepository : IUserFavoriteRepository
    {
        private readonly AppDbContext _context;

        public UserFavoriteRepository(AppDbContext context)
        {
            _context = context;
        }

        // Get all favorites for a user, newest first, with full item details for mapping
        public async Task<List<UserFavoriteItem>> GetAllByUserIdAsync(string userId)
        {
            return await _context.UserFavoriteItems
                .Where(f => f.UserId == userId)
                .Include(f => f.Item)
                    .ThenInclude(i => i.Photos)
                .Include(f => f.Item)
                    .ThenInclude(i => i.Category)
                .Include(f => f.Item)
                    .ThenInclude(i => i.Owner)
                .Include(f => f.Item)
                    .ThenInclude(i => i.Reviews)
                .Include(f => f.Item)
                    .ThenInclude(i => i.Loans)
                .IgnoreQueryFilters()
                .OrderByDescending(f => f.SavedAt)
                .ToListAsync();
        }

        // Get a single favorite record
        public async Task<UserFavoriteItem?> GetAsync(string userId, int itemId)
        {
            return await _context.UserFavoriteItems
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ItemId == itemId);
        }

        // Check if a favorite already exists
        public async Task<bool> ExistsAsync(string userId, int itemId)
        {
            return await _context.UserFavoriteItems
                .AnyAsync(f => f.UserId == userId && f.ItemId == itemId);
        }

        public async Task AddAsync(UserFavoriteItem favorite)
        {
            await _context.UserFavoriteItems.AddAsync(favorite);
        }

        public void Remove(UserFavoriteItem favorite)
        {
            _context.UserFavoriteItems.Remove(favorite);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}