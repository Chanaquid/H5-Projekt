using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class AppealRepository : IAppealRepository
    {
        private readonly AppDbContext _context;

        public AppealRepository(AppDbContext context)
        {
            _context = context;
        }

        //Get appeal by id
        public async Task<Appeal?> GetByIdAsync(int appealId)
        {
            return await _context.Appeals
                .FirstOrDefaultAsync(a => a.Id == appealId);
        }

        //Get appeal by id with details
        public async Task<Appeal?> GetByIdWithDetailsAsync(int appealId)
        {
            return await _context.Appeals
                .Include(a => a.User)
                .Include(a => a.ResolvedByAdmin)
                .Include(a => a.Fine)
                .FirstOrDefaultAsync(a => a.Id == appealId);
        }

        //Get pending appeal by user id 
        public async Task<Appeal?> GetPendingByUserIdAsync(string userId)
        {
            return await _context.Appeals
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Status == AppealStatus.Pending);
        }

        //Get all pending appeals
        public async Task<List<Appeal>> GetAllPendingAsync()
        {
            return await _context.Appeals
                .Include(a => a.User)
                .Where(a => a.Status == AppealStatus.Pending)
                .OrderBy(a => a.CreatedAt)   //Oldest first — FIFO
                .ToListAsync();
        }

        //Get all appeals of an user
        public async Task<List<Appeal>> GetAllByUserIdAsync(string userId)
        {
            return await _context.Appeals
                .Include(a => a.User)
                .Include(a => a.ResolvedByAdmin)
                .Include(a => a.Fine)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Appeal?> GetPendingFineAppealByFineIdAsync(int fineId)
        {
            return await _context.Appeals
                .FirstOrDefaultAsync(a => a.FineId == fineId && a.Status == AppealStatus.Pending);
        }



        public async Task AddAsync(Appeal appeal)
        {
            await _context.Appeals.AddAsync(appeal);
        }

        public void Update(Appeal appeal)
        {
            _context.Appeals.Update(appeal);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }




    }
}
