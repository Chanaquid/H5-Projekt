using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/appeals")]
    [Authorize]
    public class AppealController : ControllerBase
    {
        private readonly IAppealService _appealService;

        public AppealController(IAppealService appealService)
        {
            _appealService = appealService;
        }

        //GET user's appeal
        [HttpGet("my")]
        public async Task<IActionResult> GetMyAppeals()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var appeals = await _appealService.GetMyAppealsAsync(userId);
            return Ok(appeals);
        }


        //User submits a score appeal
        [HttpPost("score")]
        public async Task<IActionResult> CreateScoreAppeal([FromBody] AppealDTO.CreateScoreAppealDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var appeal = await _appealService.CreateScoreAppealAsync(userId, dto);
            return Ok(appeal);
        }

        //User submitts a fine appeal
        [HttpPost("fine")]
        public async Task<IActionResult> CreateFineAppeal([FromBody] AppealDTO.CreateFineAppealDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var appeal = await _appealService.CreateFineAppealAsync(userId, dto);
            return Ok(appeal);
        }

        //Get all pending appeals
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingAppeals()
        {
            var appeals = await _appealService.GetAllPendingAsync();
            return Ok(appeals);
        }

        //Admin decides score appeal
        [HttpPost("admin/{id}/decide/score")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DecideScoreAppeal(int id, [FromBody] AppealDTO.AdminScoreAppealDecisionDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var appeal = await _appealService.DecideScoreAppealAsync(id, adminId, dto);
            return Ok(appeal);
        }


        //Admin decides the fine appeal
        [HttpPost("admin/{id}/decide/fine")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DecideFineAppeal(int id, [FromBody] AppealDTO.AdminFineAppealDecisionDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var appeal = await _appealService.DecideFineAppealAsync(id, adminId, dto);
            return Ok(appeal);
        }



    }
}
