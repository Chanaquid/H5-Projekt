using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserRecentlyViewedRepository : IUserRecentlyViewedRepository
    {
        private readonly AppDbContext _context;

        public UserRecentlyViewedRepository(AppDbContext context)
        {
            _context = context;
        }

        //Get recently viewed items for a user, most recent first, capped at limit
        public async Task<List<UserRecentlyViewedItem>> GetAllByUserIdAsync(string userId, int limit = 20)
        {
            return await _context.UserRecentlyViewedItems
                .Where(r => r.UserId == userId)
                .Include(r => r.Item)
                    .ThenInclude(i => i.Photos)
                .Include(r => r.Item)
                    .ThenInclude(i => i.Category)
                .Include(r => r.Item)
                    .ThenInclude(i => i.Owner)
                .Include(r => r.Item)
                    .ThenInclude(i => i.Reviews)
                .Include(r => r.Item)
                    .ThenInclude(i => i.Loans)
                .IgnoreQueryFilters()
                .OrderByDescending(r => r.ViewedAt)
                .Take(limit)
                .ToListAsync();
        }

        // Get a single record for upsert check
        public async Task<UserRecentlyViewedItem?> GetAsync(string userId, int itemId)
        {
            return await _context.UserRecentlyViewedItems
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ItemId == itemId);
        }

        public async Task AddAsync(UserRecentlyViewedItem entry)
        {
            await _context.UserRecentlyViewedItems.AddAsync(entry);
        }

        public void Remove(UserRecentlyViewedItem entry)
        {
            _context.UserRecentlyViewedItems.Remove(entry);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}