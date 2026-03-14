using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/favorites")]
    [Authorize]
    public class UserFavoriteController : ControllerBase
    {
        private readonly IUserFavoriteService _favoriteService;

        public UserFavoriteController(IUserFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        // GET /api/favorites
        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await _favoriteService.GetMyFavoritesAsync(userId);
            return Ok(result);
        }

        // POST /api/favorites/{itemId}
        [HttpPost("{itemId}")]
        public async Task<IActionResult> AddFavorite(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                await _favoriteService.AddAsync(userId, itemId);
                return Ok(new { message = "Item added to favorites." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // DELETE /api/favorites/{itemId}
        [HttpDelete("{itemId}")]
        public async Task<IActionResult> RemoveFavorite(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                await _favoriteService.RemoveAsync(userId, itemId);
                return Ok(new { message = "Item removed from favorites." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // PATCH /api/favorites/{itemId}/notify
        [HttpPatch("{itemId}/notify")]
        public async Task<IActionResult> ToggleNotify(int itemId, [FromBody] FavoriteDTO.ToggleNotifyDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _favoriteService.ToggleNotifyAsync(userId, itemId, dto.NotifyWhenAvailable);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}