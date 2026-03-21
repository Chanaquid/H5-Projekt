using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class VerificationRepository : IVerificationRepository
    {
        private readonly AppDbContext _context;

        public VerificationRepository(AppDbContext context)
        {
            _context = context;
        }

        //get verification req by id
        public async Task<VerificationRequest?> GetByIdAsync(int requestId)
        {
            return await _context.VerificationRequests
                .FirstOrDefaultAsync(v => v.Id == requestId);
        }

        //get verification req by id WITH DETAILS
        public async Task<VerificationRequest?> GetByIdWithDetailsAsync(int requestId)
        {
            return await _context.VerificationRequests
                .Include(v => v.User)
                .Include(v => v.ReviewedByAdmin)
                .FirstOrDefaultAsync(v => v.Id == requestId);
        }

        //Get pending verification requests by user id
        public async Task<VerificationRequest?> GetPendingByUserIdAsync(string userId)
        {
            return await _context.VerificationRequests
                .FirstOrDefaultAsync(v => v.UserId == userId && v.Status == VerificationStatus.Pending);
        }

        //get most recent verification req
        public async Task<VerificationRequest?> GetLatestByUserIdAsync(string userId)
        {
            return await _context.VerificationRequests
                .Include(v => v.ReviewedByAdmin)
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.SubmittedAt)
                .FirstOrDefaultAsync();
        }

        //get all verification request 
        public async Task<List<VerificationRequest>> GetAllPendingAsync()
        {
            return await _context.VerificationRequests
                .Include(v => v.User)
                .Where(v => v.Status == VerificationStatus.Pending)
                .OrderBy(v => v.SubmittedAt)   //Oldest first — FIFO
                .ToListAsync();
        }

        public async Task<List<VerificationRequest>> GetAllAsync()
        {
            return await _context.VerificationRequests
                .Include(v => v.User)
                .Include(v => v.ReviewedByAdmin)
                .OrderByDescending(v => v.SubmittedAt)
                .ToListAsync();
        }

        //Admin use — full verification history for a specific user
        public async Task<List<VerificationRequest>> GetAllByUserIdAsync(string userId)
        {
            return await _context.VerificationRequests
                .Include(v => v.ReviewedByAdmin)
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.SubmittedAt)
                .ToListAsync();
        }


        public async Task AddAsync(VerificationRequest request)
        {
            await _context.VerificationRequests.AddAsync(request);
        }

        public void Update(VerificationRequest request)
        {
            _context.VerificationRequests.Update(request);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }





    }
}
