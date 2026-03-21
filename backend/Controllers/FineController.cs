using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/fines")]
    [Authorize]
    public class FineController : ControllerBase
    {
        private readonly IFineService _fineService;

        public FineController(IFineService fineService)
        {
            _fineService = fineService;
        }

        //GET - user views their own fines
        [HttpGet("my")]
        public async Task<IActionResult> GetMyFines()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var fines = await _fineService.GetUserFinesAsync(userId);
            return Ok(fines);
        }

        //GET - all pending fine verifications
        [HttpGet("admin/pending-verification")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingVerification()
        {
            var fines = await _fineService.GetPendingVerificationAsync();
            return Ok(fines);
        }


        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var fines = await _fineService.GetAllFinesAsync();
            return Ok(fines);
        }


        //POST - User marks fine as paid for themselves
        [HttpPost("pay")]
        public async Task<IActionResult> MarkAsPaid([FromBody] FineDTO.PayFineDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var fine = await _fineService.MarkAsPaidAsync(userId, dto);
            return Ok(fine);
        }


        //GET - Admin views all unpaid fines 
        [HttpGet("admin/unpaid")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUnpaid()
        {
            var fines = await _fineService.GetAllUnpaidAsync();
            return Ok(fines);
        }


        //GET - all fines by userId
        [HttpGet("admin/user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetFinesByUser(string userId)
        {
            var fines = await _fineService.GetUserFinesAsync(userId);
            return Ok(fines);
        }

        //Get fine by disputeId
        [HttpGet("dispute/{disputeId}")]
        public async Task<IActionResult> GetByDisputeId(int disputeId)
        {
            var result = await _fineService.GetByDisputeIdAsync(disputeId);
            return Ok(result);
        }

        //POST - Admin issues customFine
        [HttpPost("admin/issue")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIssueFine([FromBody] FineDTO.AdminIssueFineDTO dto)
        {
            var fine = await _fineService.AdminIssueFineAsync(dto);
            return Ok(fine);
        }

        //Admin confirms payment and marks as resolved
        [HttpPost("admin/confirm-payment")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmPayment([FromBody] FineDTO.AdminFineVerificationDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var fine = await _fineService.AdminConfirmPaymentAsync(adminId, dto);
            return Ok(fine);
        }

        //Admin updates fine
        [HttpPut("admin/{fineId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdateFine(int fineId, [FromBody] FineDTO.AdminUpdateFineDTO dto)
        {
            var fine = await _fineService.AdminUpdateFineAsync(fineId, dto);
            return Ok(fine);
        }


    }
}
