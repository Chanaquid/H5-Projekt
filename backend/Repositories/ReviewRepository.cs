using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        //Item reviews
        public async Task<ItemReview?> GetItemReviewByLoanIdAsync(int loanId)
        {
            return await _context.ItemReviews
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.LoanId == loanId);
        }

        //Get Item's review by reviewId
        public async Task<ItemReview?> GetItemReviewByIdAsync(int reviewId)
        {
            return await _context.ItemReviews
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.Id == reviewId);
        }


        //Get all item reviews by itemId
        public async Task<List<ItemReview>> GetItemReviewsByItemIdAsync(int itemId)
        {
            return await _context.ItemReviews
                .Include(r => r.Reviewer)
                .Where(r => r.ItemId == itemId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        //Get all User review by reviewid
        public async Task<UserReview?> GetUserReviewByIdAsync(int reviewId)
        {
            return await _context.UserReviews
                .Include(r => r.Reviewer)
                .Include(r => r.Loan).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(r => r.Id == reviewId);
        }

        public async Task AddItemReviewAsync(ItemReview review)
        {
            await _context.ItemReviews.AddAsync(review);
        }

        public async Task LoadReviewerAsync(ItemReview review)
        {
            await _context.Entry(review)
                .Reference(r => r.Reviewer)
                .LoadAsync();
        }

        //User reviews
        public async Task<UserReview?> GetUserReviewByLoanAndReviewerAsync(int loanId, string reviewerId)
        {
            return await _context.UserReviews
                .FirstOrDefaultAsync(r => r.LoanId == loanId && r.ReviewerId == reviewerId);
        }

        public async Task<List<UserReview>> GetUserReviewsByReviewedUserIdAsync(string userId)
        {
            return await _context.UserReviews
                .Include(r => r.Reviewer)
                .Include(r => r.Loan)
                    .ThenInclude(l => l.Item)
                .Where(r => r.ReviewedUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task AddUserReviewAsync(UserReview review)
        {
            await _context.UserReviews.AddAsync(review);
        }

        public async Task LoadUserReviewerAsync(UserReview review)
        {
            await _context.Entry(review)
                .Reference(r => r.Reviewer)
                .LoadAsync();
        }


        public async Task LoadUserReviewDetailsAsync(UserReview review)
        {
            await _context.Entry(review)
                .Reference(r => r.Reviewer)
                .LoadAsync();

            await _context.Entry(review)
                .Reference(r => r.Loan)
                .LoadAsync();

            await _context.Entry(review.Loan)
                .Reference(l => l.Item)
                .LoadAsync();
        }

        public void DeleteItemReview(ItemReview review)
        {
            _context.ItemReviews.Remove(review);
        }

        public void DeleteUserReview(UserReview review)
        {
            _context.UserReviews.Remove(review);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }



    }
}
