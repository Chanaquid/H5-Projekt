using backend.DTOs;

namespace backend.Interfaces
{
    public interface IReviewService
    {
        //Item reviews
        Task<ReviewDTO.ItemReviewResponseDTO> CreateItemReviewAsync(string reviewerId, ReviewDTO.CreateItemReviewDTO dto, bool isAdmin = false);
        Task<List<ReviewDTO.ItemReviewResponseDTO>> GetItemReviewsAsync(int itemId);
        Task<ReviewDTO.ItemReviewResponseDTO> EditItemReviewAsync(int reviewId, ReviewDTO.EditReviewDTO dto);
        Task DeleteItemReviewAsync(int reviewId);

        //User reviews
        Task<ReviewDTO.UserReviewResponseDTO> CreateUserReviewAsync(string reviewerId, ReviewDTO.CreateUserReviewDTO dto, bool isAdmin = false);
        Task<List<ReviewDTO.UserReviewResponseDTO>> GetUserReviewsAsync(string userId);
        Task<ReviewDTO.UserReviewResponseDTO> EditUserReviewAsync(int reviewId, ReviewDTO.EditReviewDTO dto);
        Task DeleteUserReviewAsync(int reviewId);
    }
}
