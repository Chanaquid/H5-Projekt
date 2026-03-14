using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static backend.DTOs.ChatDTO;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/blocks")]
    [Authorize]
    public class UserBlockController : ControllerBase
    {
        private readonly IUserBlockService _userBlockService;

        public UserBlockController(IUserBlockService userBlockService)
        {
            _userBlockService = userBlockService;
        }

        //GET — get my blocked users list
        [HttpGet]
        public async Task<IActionResult> GetBlockedUsers()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var blocks = await _userBlockService.GetBlockedUsersAsync(userId);
            return Ok(blocks);
        }

        //POST — block a user
        [HttpPost]
        public async Task<IActionResult> Block([FromBody] UserBlockDTO.BlockUserDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var block = await _userBlockService.BlockAsync(userId, dto.BlockedUserId);
            return Ok(block);
        }

        //DELETE — unblock a user
        [HttpDelete("{blockedUserId}")]
        public async Task<IActionResult> Unblock(string blockedUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _userBlockService.UnblockAsync(userId, blockedUserId);
            return NoContent();
        }



    }
}
