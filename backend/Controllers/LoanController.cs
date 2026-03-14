using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/loans")]
    [Authorize]
    public class LoanController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoanController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        // GET /api/loans/borrowed — loans where I am the borrower
        [HttpGet("borrowed")]
        public async Task<IActionResult> GetBorrowed()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _loanService.GetBorrowedLoansAsync(userId);
            return Ok(result);
        }

        // GET /api/loans/owned — loan requests on my items
        [HttpGet("owned")]
        public async Task<IActionResult> GetOwned()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _loanService.GetOwnedLoansAsync(userId);
            return Ok(result);
        }

        // GET /api/loans/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _loanService.GetByIdAsync(id, userId);
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

        // POST /api/loans — borrower requests a loan
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LoanDTO.CreateLoanDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _loanService.CreateAsync(userId, dto);
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

        // POST /api/loans/{id}/cancel — borrower cancels their own pending/approved loan
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] LoanDTO.CancelLoanDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _loanService.CancelAsync(id, userId, dto);
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

        // POST /api/loans/{id}/decide — owner approves or rejects a loan request
        [HttpPost("{id}/decide")]
        public async Task<IActionResult> Decide(int id, [FromBody] LoanDTO.LoanDecisionDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _loanService.DecideAsync(id, userId, dto);
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

        // POST /api/loans/{id}/request-extension — borrower requests extended end date
        [HttpPost("{id}/request-extension")]
        public async Task<IActionResult> RequestExtension(int id, [FromBody] LoanDTO.RequestExtensionDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _loanService.RequestExtensionAsync(id, userId, dto);
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST /api/loans/{id}/decide-extension — owner approves or rejects extension request
        [HttpPost("{id}/decide-extension")]
        public async Task<IActionResult> DecideExtension(int id, [FromBody] LoanDTO.ExtensionDecisionDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _loanService.DecideExtensionAsync(id, userId, dto);
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

        // ── Admin endpoints ─────────────────────────────────────────

        // GET /api/loans/admin/all
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _loanService.GetAllLoansAsync();
            return Ok(result);
        }

        // GET /api/loans/admin/pending — low-score users awaiting admin approval
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var result = await _loanService.GetPendingApprovalsAsync();
            return Ok(result);
        }

        // POST /api/loans/admin/{id}/decide — admin approves/rejects low-score loan
        [HttpPost("admin/{id}/decide")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDecide(int id, [FromBody] LoanDTO.LoanDecisionDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _loanService.AdminDecideAsync(id, adminId, dto);
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