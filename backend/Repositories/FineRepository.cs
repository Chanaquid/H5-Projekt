using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class FineRepository : IFineRepository
    {
        private readonly AppDbContext _context;

        public FineRepository(AppDbContext context)
        {
            _context = context;
        }

        //Get fines by userId
        public async Task<List<Fine>> GetByUserIdAsync(string userId)
        {
            return await _context.Fines
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Item)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        //get all unpaidFines
        public async Task<List<Fine>> GetAllUnpaidAsync()
        {
            return await _context.Fines
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Item)
                .Include(f => f.User)
                .Where(f => f.Status == FineStatus.Unpaid)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        //Get fine by id
        public async Task<Fine?> GetByIdAsync(int fineId)
        {
            return await _context.Fines
                .FirstOrDefaultAsync(f => f.Id == fineId);
        }

        //Get fine by id with loan details
        public async Task<Fine?> GetByIdWithDetailsAsync(int fineId)
        {
            return await _context.Fines
                .Include(f => f.Loan)
                .ThenInclude(l => l.Item)
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == fineId);
        }

        public async Task<List<Fine>> GetPendingVerificationAsync()
        {
            return await _context.Fines
                .Include(f => f.Loan).ThenInclude(l => l.Item)
                .Include(f => f.User)
                .Where(f => f.Status == FineStatus.PendingVerification)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();
        }


        public async Task AddAsync(Fine fine)
        {
            await _context.Fines.AddAsync(fine);
        }

        public void Update(Fine fine)
        {
            _context.Fines.Update(fine);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }




    }
}
