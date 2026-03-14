using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/recently-viewed")]
    [Authorize]
    public class UserRecentlyViewedController : ControllerBase
    {
        private readonly IUserRecentlyViewedService _recentlyViewedService;

        public UserRecentlyViewedController(IUserRecentlyViewedService recentlyViewedService)
        {
            _recentlyViewedService = recentlyViewedService;
        }

        //GET - Get all recently viewed (10)
        [HttpGet]
        public async Task<IActionResult> GetMyRecentlyViewed()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await _recentlyViewedService.GetMyRecentlyViewedAsync(userId);
            return Ok(new { userId, count = result.Count, result });
        }

        // POST - called automatically by when user views an item
        [HttpPost("{itemId}")]
        public async Task<IActionResult> TrackView(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                await _recentlyViewedService.TrackViewAsync(userId, itemId);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}