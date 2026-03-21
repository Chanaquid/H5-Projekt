using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/disputes")]
    [Authorize]
    public class DisputeController : ControllerBase
    {
        private readonly IDisputeService _disputeService;

        public DisputeController(IDisputeService disputeService)
        {
            _disputeService = disputeService;
        }

        // GET - all disputes
        [HttpGet("my")]
        public async Task<IActionResult> GetMyDisputes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _disputeService.GetDisputesByUserIdAsync(userId);
            return Ok(result);
        }

        // GET get dispute by id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _disputeService.GetByIdAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpGet("item/{itemId}")]
        public async Task<IActionResult> GetDisputeHistoryByItemId(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            try
            {
                var result = await _disputeService.GetDisputeHistoryByItemIdAsync(itemId, userId, isAdmin);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }


        // POST create a dispute
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DisputeDTO.CreateDisputeDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _disputeService.CreateAsync(userId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //POST respond to dispute
        [HttpPost("{id}/respond")]
        public async Task<IActionResult> Respond(int id, [FromBody] DisputeDTO.DisputeResponseDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _disputeService.SubmitResponseAsync(id, userId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //POST - add pic to the dispute
        [HttpPost("{id}/photos")]
        public async Task<IActionResult> AddPhoto(int id, [FromBody] DisputeDTO.AddDisputePhotoDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _disputeService.AddPhotoAsync(id, userId, dto.PhotoUrl, dto.Caption);
                return Ok(new { message = "Photo added." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET - get all opem disputes
        [HttpGet("admin/open")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOpen()
        {
            var result = await _disputeService.GetAllOpenAsync();
            return Ok(result);
        }

        // POST - issue verdict
        [HttpPost("admin/{id}/verdict")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> IssueVerdict(int id, [FromBody] DisputeDTO.AdminVerdictDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _disputeService.IssueVerdictAsync(id, adminId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }
    }
}