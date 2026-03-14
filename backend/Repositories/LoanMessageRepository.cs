using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class LoanMessageRepository : ILoanMessageRepository
    {
        private readonly AppDbContext _context;

        public LoanMessageRepository(AppDbContext context)
        {
            _context = context;
        }

        //Get all messages in a loan by loan id
        public async Task<List<LoanMessage>> GetByLoanIdAsync(int loanId)
        {
            return await _context.LoanMessages
                .Include(m => m.Sender)
                .Where(m => m.LoanId == loanId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        //Get all loan messages sent by a specific user — used by admin detail view
        public async Task<List<LoanMessage>> GetByUserIdAsync(string userId)
        {
            return await _context.LoanMessages
                .Include(m => m.Sender)
                .Where(m => m.SenderId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        public async Task AddAsync(LoanMessage message)
        {
            await _context.LoanMessages.AddAsync(message);
        }

        //Instead of getbyloanidasync, fetch one specific record
        public async Task LoadSenderAsync(LoanMessage message)
        {
            await _context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}
