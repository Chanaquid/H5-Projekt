using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        //POST - create item review
        [HttpPost("items")]
        [Authorize]
        public async Task<IActionResult> CreateItemReview([FromBody] ReviewDTO.CreateItemReviewDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var review = await _reviewService.CreateItemReviewAsync(userId, dto, isAdmin);
            return Ok(review); //return Ok instead of CreatedAtAction
        }

        //GET - Get item review 
        [HttpGet("items/{itemId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetItemReviews(int itemId)
        {
            var reviews = await _reviewService.GetItemReviewsAsync(itemId);
            return Ok(reviews);
        }

        //PUT  — admin only
        [HttpPut("items/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditItemReview(int id, [FromBody] ReviewDTO.EditReviewDTO dto)
        {
            var review = await _reviewService.EditItemReviewAsync(id, dto);
            return Ok(review);
        }

        //DELETE  — admin only
        [HttpDelete("items/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteItemReview(int id)
        {
            await _reviewService.DeleteItemReviewAsync(id);
            return NoContent();
        }

        //POST - create user review 
        [HttpPost("users")]
        [Authorize]
        public async Task<IActionResult> CreateUserReview([FromBody] ReviewDTO.CreateUserReviewDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var review = await _reviewService.CreateUserReviewAsync(userId, dto, isAdmin);
            return Ok(review); //just return Ok instead of CreatedAtAction
        }

        //GET - Get user review by id
        [HttpGet("users/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserReviews(string userId)
        {
            var reviews = await _reviewService.GetUserReviewsAsync(userId);
            return Ok(reviews);
        }

        //PUT  — admin only
        [HttpPut("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUserReview(int id, [FromBody] ReviewDTO.EditReviewDTO dto)
        {
            var review = await _reviewService.EditUserReviewAsync(id, dto);
            return Ok(review);
        }

        //DELETE — admin only
        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserReview(int id)
        {
            await _reviewService.DeleteUserReviewAsync(id);
            return NoContent();
        }
    }
}