using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        //Get user by id
        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        //Get user by id with details
        public async Task<ApplicationUser?> GetByIdWithDetailsAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.ScoreHistory)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        //Get user by id — ignores soft-delete filter (admin use)
        public async Task<ApplicationUser?> GetByIdIgnoreFiltersAsync(string userId)
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        //Get all users including deleted — admin
        public async Task<List<ApplicationUser>> GetAllAsync()
        {
            return await _context.Users
                .IgnoreQueryFilters()
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        //Get score history by user id
        public async Task<List<ScoreHistory>> GetScoreHistoryAsync(string userId)
        {
            return await _context.ScoreHistories
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        //Get score history by loaIn
        public async Task<List<ScoreHistory>> GetScoreHistoryByLoanIdAsync(int loanId)
        {
            return await _context.ScoreHistories
                .Where(s => s.LoanId == loanId)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task AddScoreHistoryAsync(ScoreHistory entry)
        {
            await _context.ScoreHistories.AddAsync(entry);
        }

        //Entity is already tracked from GetByIdAsync — no need to call Update().
        public Task UpdateAsync(ApplicationUser user)
        {
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}