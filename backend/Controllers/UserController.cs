using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        //GET - Get your own profile
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _userService.GetProfileAsync(userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


        //GET — public profile, any authenticated user
        [HttpGet("{id:guid}/profile")]
        public async Task<IActionResult> GetPublicProfile(string id)
        {
            try
            {
                var result = await _userService.GetPublicProfileAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


        //PUT - update own profile
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserDTO.UpdateProfileDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _userService.UpdateProfileAsync(userId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //DELETE - delete own account
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteAccount([FromBody] UserDTO.DeleteAccountDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                await _userService.DeleteAccountAsync(userId, dto);
                return Ok(new { message = "Your account has been deleted." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //GET - get own score history
        [HttpGet("me/score-history")]
        public async Task<IActionResult> GetScoreHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _userService.GetScoreHistoryAsync(userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("score-history/loan/{loanId}")]
        public async Task<IActionResult> GetScoreHistoryByLoan(int loanId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _userService.GetScoreHistoryByLoanIdAsync(loanId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        //Admin endpoints

        // GET - get all users
        [HttpGet]
        //[Authorize(Roles = "Admin")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        //GET - get user by id
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                var result = await _userService.GetUserByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        //POST - Adjust score for an user
        [HttpPost("{id:guid}/adjust-score")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdjustScore(string id, [FromBody] UserDTO.AdminScoreAdjustDTO dto)
        {
            try
            {
                await _userService.AdminAdjustScoreAsync(id, dto);
                return Ok(new { message = "Score adjusted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //PUT - Admin edits user
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminEditUser(string id, [FromBody] UserDTO.AdminEditUserDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _userService.AdminEditUserAsync(id, adminId, dto);
            return Ok(result);
        }

        //DELETE - admin can delete any user
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeleteUser(string id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminId == null) return Unauthorized();

            try
            {
                await _userService.AdminSoftDeleteUserAsync(id, adminId);
                return Ok(new { message = "User has been deleted." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}