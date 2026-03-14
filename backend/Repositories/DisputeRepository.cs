using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class DisputeRepository : IDisputeRepository
    {
        private readonly AppDbContext _context;

        public DisputeRepository(AppDbContext context)
        {
            _context = context;
        }

        //Get dispute by id
        public async Task<Dispute?> GetByIdAsync(int disputeId)
        {
            return await _context.Disputes
                .FirstOrDefaultAsync(d => d.Id == disputeId);
        }

        //Get dispute by id with details
        public async Task<Dispute?> GetByIdWithDetailsAsync(int disputeId)
        {
            return await _context.Disputes
                .Include(d => d.FiledBy)
                .Include(d => d.Photos)
                .ThenInclude(p => p.SubmittedBy)
                .Include(d => d.Loan)
                .ThenInclude(l => l.Item)
                .ThenInclude(i => i.Owner)
                .Include(d => d.Loan)
                .ThenInclude(l => l.Borrower)
                .Include(d => d.Loan)
                .ThenInclude(l => l.SnapshotPhotos)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
        }

        //Get dispute by user id 
        public async Task<List<Dispute>> GetByUserIdAsync(string userId)
        {
            return await _context.Disputes
                .Include(d => d.Loan)
                .ThenInclude(l => l.Item)
                .Include(d => d.FiledBy)
                .Where(d => d.FiledById == userId || d.Loan.BorrowerId == userId || d.Loan.Item.OwnerId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        //Get all disputes in order by the created time
        public async Task<List<Dispute>> GetAllOpenAsync()
        {
            return await _context.Disputes
                .Include(d => d.Loan)
                    .ThenInclude(l => l.Item)
                .Include(d => d.FiledBy)
                .Where(d => d.Status != DisputeStatus.Resolved)
                .OrderBy(d => d.CreatedAt)  //Oldest first — FIFO
                .ToListAsync();
        }

        //CRUD - cant delete dispute
        public async Task AddAsync(Dispute dispute)
        {
            await _context.Disputes.AddAsync(dispute);
        }

        public async Task AddPhotoAsync(DisputePhoto photo)
        {
            await _context.DisputePhotos.AddAsync(photo);
        }

        public void Update(Dispute dispute)
        {
            _context.Disputes.Update(dispute);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }




    }
}
