using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        //POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthDTO.RegisterDTO dto)
        {
            try
            {
                var result = await _authService.RegisterAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiDTO.ApiResponse {  Message = ex.Message});
            }
        }


        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            var success = await _authService.ConfirmEmailAsync(userId, token);

            if (!success)
                return BadRequest(new ApiDTO.ApiResponse { Message = "Email confirmation failed. The link may have expired." });

            return Ok(new ApiDTO.ApiResponse{ Message = "Email confirmed. You can now log in." });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthDTO.LoginDTO dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Unauthorized(new ApiDTO.ApiResponse { Message = ex.Message });
            }
        }


        //refresh token
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] AuthDTO.RefreshTokenDTO dto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiDTO.ApiResponse { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(new ApiDTO.ApiResponse { Message = "Invalid token." });

            await _authService.LogoutAsync(userId);
            return Ok(new ApiDTO.ApiResponse { Message = "Logged out successfully." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] AuthDTO.ForgotPasswordDTO dto)
        {
            //Always returns 200 — so it doesnt reveal whether the email exists
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new ApiDTO.ApiResponse { Message = "If an account with that email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] AuthDTO.ResetPasswordDTO dto)
        {
            var success = await _authService.ResetPasswordAsync(dto);

            if (!success)
                return BadRequest(new ApiDTO.ApiResponse { Message = "Password reset failed. The link may have expired." });

            return Ok(new ApiDTO.ApiResponse { Message = "Password reset successfully. You can now log in." });
        }


        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] AuthDTO.ForgotPasswordDTO dto)
        {
            await _authService.ResendConfirmationEmailAsync(dto.Email);
            return Ok(new ApiDTO.ApiResponse { Message = "If your email is registered and unconfirmed, a new confirmation link has been sent." });
        }











    }
}
