using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserBlockRepository : IUserBlockRepository
    {
        private readonly AppDbContext _context;

        public UserBlockRepository(AppDbContext context)
        {
            _context = context;
        }

        //Get a specific block record 
        public async Task<UserBlock?> GetAsync(string blockerId, string blockedId)
        {
            return await _context.UserBlocks
                .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
        }


        //Get all users blocked by userId
        public async Task<List<UserBlock>> GetBlocksByUserIdAsync(string userId)
        {
            return await _context.UserBlocks
                .Include(b => b.Blocked)
                .Where(b => b.BlockerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }


        //Check if a block exists in either direction — A blocks B or B blocks A
        public async Task<bool> IsBlockedAsync(string userAId, string userBId)
        {
            return await _context.UserBlocks
                .AnyAsync(b =>
                    (b.BlockerId == userAId && b.BlockedId == userBId) ||
                    (b.BlockerId == userBId && b.BlockedId == userAId));
        }


        public async Task AddAsync(UserBlock block)
        {
            await _context.UserBlocks.AddAsync(block);
        }

        public void Remove(UserBlock block)
        {
            _context.UserBlocks.Remove(block);
        }

        public async Task RemoveAsync(UserBlock block)
        {
            _context.UserBlocks.Remove(block);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


    }
}
