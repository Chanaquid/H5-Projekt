using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static backend.DTOs.ChatDTO;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/support")]
    [Authorize]
    public class SupportChatController : ControllerBase
    {
        private readonly ISupportChatService _supportChatService;

        public SupportChatController(ISupportChatService supportChatService)
        {
            _supportChatService = supportChatService;
        }

        // GET /api/support/my — user gets their own threads
        [HttpGet("my")]
        public async Task<IActionResult> GetMyThreads()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _supportChatService.GetMyThreadsAsync(userId);
            return Ok(result);
        }

        // GET /api/support/{id} — get full thread with messages
        [HttpGet("{id}")]
        public async Task<IActionResult> GetThread(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            try
            {
                var result = await _supportChatService.GetThreadAsync(id, userId, isAdmin);
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

        // POST /api/support — user opens a new thread
        [HttpPost]
        public async Task<IActionResult> CreateThread([FromBody] SupportChatDTO.CreateSupportThreadDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _supportChatService.CreateThreadAsync(userId, dto);
            return Ok(result);
        }

        // POST /api/support/messages — send a message in a thread
        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SupportChatDTO.SendSupportMessageDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _supportChatService.SendMessageAsync(userId, dto);
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

        // POST /api/support/{id}/read — mark messages as read
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _supportChatService.MarkReadAsync(id, userId);
                return Ok();
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

        // ── Admin endpoints ──────────────────────────────────────────

        // GET /api/support/admin/open — all open/claimed threads
        [HttpGet("admin/open")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOpen()
        {
            var result = await _supportChatService.GetAllOpenThreadsAsync();
            return Ok(result);
        }

        // POST /api/support/admin/{id}/claim — admin claims a thread
        [HttpPost("admin/{id}/claim")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClaimThread(int id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _supportChatService.ClaimThreadAsync(id, adminId);
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
        }

        // POST /api/support/admin/{id}/close — admin closes a thread
        [HttpPost("admin/{id}/close")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CloseThread(int id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _supportChatService.CloseThreadAsync(id, adminId);
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
        }

        // POST /api/support/admin/{id}/reopen — admin reopens a closed thread
        [HttpPost("admin/{id}/reopen")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReopenThread(int id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _supportChatService.ReopenThreadAsync(id, adminId);
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
        }
    }
}