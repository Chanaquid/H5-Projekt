using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly AppDbContext _context;

        public LoanRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Loan?> GetByIdAsync(int loanId)
        {
            return await _context.Loans
                .FirstOrDefaultAsync(l => l.Id == loanId);
        }

        //Get loan by id with all details
        public async Task<Loan?> GetByIdWithDetailsAsync(int loanId)
        {
            return await _context.Loans
                .Include(l => l.Item)
                .ThenInclude(i => i.Owner)
                .Include(l => l.Item)
                .ThenInclude(i => i.Photos)
                .Include(l => l.Item)
                .ThenInclude(i => i.Category)
                .Include(l => l.Borrower)
                .Include(l => l.Disputes)
                .Include(l => l.Fines)
                .Include(l => l.SnapshotPhotos)
                .FirstOrDefaultAsync(l => l.Id == loanId);
        }

        //Get loans by borrowedId
        public async Task<List<Loan>> GetByBorrowerIdAsync(string borrowerId)
        {
            return await _context.Loans
                .Include(l => l.Item)
                .ThenInclude(i => i.Photos)
                .Include(l => l.Item)
                .ThenInclude(i => i.Owner)
                .Include(l => l.Fines)
                .Include(l => l.Messages)
                .Where(l => l.BorrowerId == borrowerId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        //Get all loans by owner id
        public async Task<List<Loan>> GetByOwnerIdAsync(string ownerId)
        {
            return await _context.Loans
                .Include(l => l.Item)
                .ThenInclude(i => i.Photos)
                .Include(l => l.Borrower)
                .Include(l => l.Fines)
                .Include(l => l.Messages)
                .Where(l => l.Item.OwnerId == ownerId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        //Gett all loans
        public async Task<List<Loan>> GetAllAsync()
        {
            return await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        //Get all loans pending for approval
        public async Task<List<Loan>> GetPendingAdminApprovalsAsync()
        {
            return await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .Where(l => l.Status == LoanStatus.AdminPending)
                .OrderBy(l => l.CreatedAt)   //Oldest first — FIFO queue
                .ToListAsync();
        }

        //Get all overdue loans - Active and late
        public async Task<List<Loan>> GetActiveAndOverdueAsync()
        {
            var overdueStatuses = new[] { LoanStatus.Active, LoanStatus.Late };

            return await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                    .ThenInclude(b => b.ScoreHistory.Where(s =>
                        s.Reason == ScoreChangeReason.LateReturn &&
                        s.LoanId != null))
                .Include(l => l.Fines)
                .Where(l => overdueStatuses.Contains(l.Status) && l.EndDate < DateTime.UtcNow)
                .ToListAsync();
        }

        //checks if user has active loans
        public async Task<bool> HasActiveLoansAsync(string userId)
        {
            var activeStatuses = new[]
            {
                LoanStatus.Pending,
                LoanStatus.AdminPending,
                LoanStatus.Approved,
                LoanStatus.Active
            };

            return await _context.Loans
                .AnyAsync(l =>
                    (l.BorrowerId == userId || l.Item.OwnerId == userId) &&
                    activeStatuses.Contains(l.Status));
        }

        public async Task AddAsync(Loan loan)
        {
            await _context.Loans.AddAsync(loan);
        }

        public void Update(Loan loan)
        {
            _context.Loans.Update(loan);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }




    }

}
