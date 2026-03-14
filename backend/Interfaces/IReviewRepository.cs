using backend.Models;

namespace backend.Interfaces
{
    public interface IReviewRepository
    {
        //Item reviews
        Task<ItemReview?> GetItemReviewByIdAsync(int reviewId);
        Task<ItemReview?> GetItemReviewByLoanIdAsync(int loanId);
        Task<List<ItemReview>> GetItemReviewsByItemIdAsync(int itemId);
        Task AddItemReviewAsync(ItemReview review);
        Task LoadReviewerAsync(ItemReview review);
        void DeleteItemReview(ItemReview review);

        //User reviews
        Task<UserReview?> GetUserReviewByIdAsync(int reviewId);
        Task<UserReview?> GetUserReviewByLoanAndReviewerAsync(int loanId, string reviewerId);
        Task<List<UserReview>> GetUserReviewsByReviewedUserIdAsync(string userId);
        Task AddUserReviewAsync(UserReview review);
        Task LoadUserReviewDetailsAsync(UserReview review);
        Task LoadUserReviewerAsync(UserReview review);

        void DeleteUserReview(UserReview review);

        Task SaveChangesAsync();
    }
}
