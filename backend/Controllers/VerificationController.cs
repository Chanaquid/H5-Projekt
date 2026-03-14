using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/verification")]
    [Authorize]
    public class VerificationController : ControllerBase
    {

        private readonly IVerificationService _verificationService;

        public VerificationController(IVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        //GET - user gets their own verification req
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRequest()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var request = await _verificationService.GetUserRequestAsync(userId);
            return Ok(request);
        }

        //User submits verification request - BLOCK ADMIN FROM SENDING REQ
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] VerificationDTO.CreateVerificationRequestDTO dto)
        {
            if (User.IsInRole("Admin"))
                throw new UnauthorizedAccessException("Admins do not need verification.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var request = await _verificationService.SubmitRequestAsync(userId, dto);
            return Ok(request);
        }


        //GET - Admin gets all verification pending requests
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _verificationService.GetAllPendingAsync();
            return Ok(requests);
        }

        //Admin decides user's verification request
        [HttpPost("admin/{id}/decide")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Decide(int id, [FromBody] VerificationDTO.AdminVerificationDecisionDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var request = await _verificationService.DecideAsync(id, adminId, dto);
            return Ok(request);
        }



    }
}
