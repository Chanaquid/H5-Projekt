using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IUserRepository _userRepository;

        public ReviewService(
            IReviewRepository reviewRepository,
            ILoanRepository loanRepository,
            IUserRepository userRepository)
        {
            _reviewRepository = reviewRepository;
            _loanRepository = loanRepository;
            _userRepository = userRepository;
        }

        //Create item review
        public async Task<ReviewDTO.ItemReviewResponseDTO> CreateItemReviewAsync(string reviewerId, ReviewDTO.CreateItemReviewDTO dto, bool isAdmin = false)
        {
            if (!isAdmin && !dto.LoanId.HasValue)
                throw new ArgumentException("LoanId is required.");

            if (isAdmin && dto.ItemId == 0)
                throw new ArgumentException("ItemId is required for admin reviews.");

            if (!isAdmin)
            {
                var loan = await _loanRepository.GetByIdWithDetailsAsync(dto.LoanId!.Value);
                if (loan == null)
                    throw new KeyNotFoundException($"Loan {dto.LoanId} not found.");

                if (loan.BorrowerId != reviewerId)
                    throw new UnauthorizedAccessException("Only the borrower of this loan can review the item.");

                var reviewableStatuses = new[] { LoanStatus.Returned, LoanStatus.Late };
                if (!reviewableStatuses.Contains(loan.Status))
                    throw new InvalidOperationException("You can only review an item after the loan is completed.");

                var existing = await _reviewRepository.GetItemReviewByLoanIdAsync(dto.LoanId!.Value);
                if (existing != null)
                    throw new InvalidOperationException("You have already reviewed this item for this loan.");
            }

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            int itemId = dto.ItemId;

            if (!isAdmin)
            {
                var loan2 = await _loanRepository.GetByIdWithDetailsAsync(dto.LoanId!.Value);
                if (loan2 == null)
                    throw new KeyNotFoundException($"Loan {dto.LoanId} not found.");
                itemId = loan2.ItemId;
            }

            var review = new ItemReview
            {
                ItemId = itemId,
                LoanId = dto.LoanId,
                ReviewerId = reviewerId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                IsAdminReview = isAdmin,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddItemReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();
            await _reviewRepository.LoadReviewerAsync(review);
            return MapToItemReviewDTO(review);
        }

        //Get item reviews (admin review FIRST)
        public async Task<List<ReviewDTO.ItemReviewResponseDTO>> GetItemReviewsAsync(int itemId)
        {
            var reviews = await _reviewRepository.GetItemReviewsByItemIdAsync(itemId);
            return reviews
                .OrderByDescending(r => r.IsAdminReview)
                .ThenByDescending(r => r.CreatedAt)
                .Select(MapToItemReviewDTO)
                .ToList();
        }

        //GET User reviews (admin review FIRST)
        public async Task<List<ReviewDTO.UserReviewResponseDTO>> GetUserReviewsAsync(string userId)
        {
            var reviews = await _reviewRepository.GetUserReviewsByReviewedUserIdAsync(userId);
            return reviews
                .OrderByDescending(r => r.IsAdminReview)
                .ThenByDescending(r => r.CreatedAt)
                .Select(MapToUserReviewDTO)
                .ToList();
        }

        //Create user reviews
        public async Task<ReviewDTO.UserReviewResponseDTO> CreateUserReviewAsync(string reviewerId, ReviewDTO.CreateUserReviewDTO dto, bool isAdmin = false)
        {
            if (!isAdmin && !dto.LoanId.HasValue)
                throw new ArgumentException("LoanId is required.");

            // Self-review check applies to everyone
            if (dto.ReviewedUserId == reviewerId)
                throw new ArgumentException("You cannot review yourself.");

            if (!isAdmin)
            {
                var loan = await _loanRepository.GetByIdWithDetailsAsync(dto.LoanId!.Value);
                if (loan == null)
                    throw new KeyNotFoundException($"Loan {dto.LoanId} not found.");

                var reviewerIsOwner = loan.Item.OwnerId == reviewerId;
                var reviewerIsBorrower = loan.BorrowerId == reviewerId;

                if (!reviewerIsOwner && !reviewerIsBorrower)
                    throw new UnauthorizedAccessException("You are not a party to this loan.");

                // reviewedUserId must be the OTHER party on this specific loan
                var reviewedIsOwner = loan.Item.OwnerId == dto.ReviewedUserId;
                var reviewedIsBorrower = loan.BorrowerId == dto.ReviewedUserId;

                if (!reviewedIsOwner && !reviewedIsBorrower)
                    throw new ArgumentException("The reviewed user is not a party to this loan.");

                // Make sure reviewer and reviewed are on opposite sides
                if (reviewerIsOwner && reviewedIsOwner)
                    throw new ArgumentException("You cannot review another owner on this loan.");

                if (reviewerIsBorrower && reviewedIsBorrower)
                    throw new ArgumentException("You cannot review another borrower on this loan.");

                var reviewableStatuses = new[] { LoanStatus.Returned, LoanStatus.Late };
                if (!reviewableStatuses.Contains(loan.Status))
                    throw new InvalidOperationException("You can only review a user after the loan is completed.");

                var existing = await _reviewRepository.GetUserReviewByLoanAndReviewerAsync(dto.LoanId!.Value, reviewerId);
                if (existing != null)
                    throw new InvalidOperationException("You have already left a review for this loan.");
            }

            // Verify reviewed user actually exists and is not deleted — applies to both user and admin
            var reviewedUser = await _userRepository.GetByIdAsync(dto.ReviewedUserId);
            if (reviewedUser == null)
                throw new KeyNotFoundException("The user you are trying to review does not exist.");

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            var review = new UserReview
            {
                LoanId = dto.LoanId,
                ReviewerId = reviewerId,
                ReviewedUserId = dto.ReviewedUserId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                IsAdminReview = isAdmin,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddUserReviewAsync(review);
            await _reviewRepository.SaveChangesAsync();

            if (review.LoanId.HasValue)
                await _reviewRepository.LoadUserReviewDetailsAsync(review);
            else
                await _reviewRepository.LoadUserReviewerAsync(review);

            return MapToUserReviewDTO(review);
        }

        //Edit item review - Admin only
        public async Task<ReviewDTO.ItemReviewResponseDTO> EditItemReviewAsync(int reviewId, ReviewDTO.EditReviewDTO dto)
        {
            var review = await _reviewRepository.GetItemReviewByIdAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review {reviewId} not found.");

            if (!review.IsAdminReview)
                throw new UnauthorizedAccessException("Only admin reviews can be edited.");

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            review.Rating = dto.Rating;
            review.Comment = dto.Comment?.Trim();
            review.IsEdited = true;
            review.EditedAt = DateTime.UtcNow;

            await _reviewRepository.SaveChangesAsync();
            return MapToItemReviewDTO(review);
        }

        //Admin edit user review
        public async Task<ReviewDTO.UserReviewResponseDTO> EditUserReviewAsync(int reviewId, ReviewDTO.EditReviewDTO dto)
        {
            var review = await _reviewRepository.GetUserReviewByIdAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review {reviewId} not found.");

            if (!review.IsAdminReview)
                throw new UnauthorizedAccessException("Only admin reviews can be edited.");

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            review.Rating = dto.Rating;
            review.Comment = dto.Comment?.Trim();
            review.IsEdited = true;
            review.EditedAt = DateTime.UtcNow;

            await _reviewRepository.SaveChangesAsync();
            return MapToUserReviewDTO(review);
        }

        //Admin deletes item review
        public async Task DeleteItemReviewAsync(int reviewId)
        {
            var review = await _reviewRepository.GetItemReviewByIdAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review {reviewId} not found.");

            _reviewRepository.DeleteItemReview(review);
            await _reviewRepository.SaveChangesAsync();
        }

        //Admin delete user review
        public async Task DeleteUserReviewAsync(int reviewId)
        {
            var review = await _reviewRepository.GetUserReviewByIdAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review {reviewId} not found.");

            _reviewRepository.DeleteUserReview(review);
            await _reviewRepository.SaveChangesAsync();
        }

        //Mappers
        private static ReviewDTO.ItemReviewResponseDTO MapToItemReviewDTO(ItemReview r)
        {
            return new ReviewDTO.ItemReviewResponseDTO
            {
                Id = r.Id,
                LoanId = r.LoanId,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewerName = r.Reviewer?.FullName ?? string.Empty,
                ReviewerAvatarUrl = r.Reviewer?.AvatarUrl,
                IsAdminReview = r.IsAdminReview,
                IsEdited = r.IsEdited,
                EditedAt = r.EditedAt,
                CreatedAt = r.CreatedAt
            };
        }

        private static ReviewDTO.UserReviewResponseDTO MapToUserReviewDTO(UserReview r)
        {
            return new ReviewDTO.UserReviewResponseDTO
            {
                Id = r.Id,
                LoanId = r.LoanId,
                ItemTitle = r.Loan?.Item?.Title ?? string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer?.FullName ?? string.Empty,
                ReviewerAvatarUrl = r.Reviewer?.AvatarUrl,
                IsAdminReview = r.IsAdminReview,
                IsEdited = r.IsEdited,
                EditedAt = r.EditedAt,
                CreatedAt = r.CreatedAt
            };
        }
    }
}