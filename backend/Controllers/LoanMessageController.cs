using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/messages/loan")]
    [Authorize]
    public class LoanMessageController : ControllerBase
    {

        private readonly ILoanMessageService _loanMessageService;

        public LoanMessageController(ILoanMessageService loanMessageService)
        {
            _loanMessageService = loanMessageService;
        }

        //GET - load full thread 
        [HttpGet("{loanId}")]
        public async Task<IActionResult> GetThread(int loanId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var thread = await _loanMessageService.GetThreadAsync(loanId, userId);
            return Ok(thread);
        }

        //POST - Send message to a loan chat
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ChatDTO.LoanMessageDTO.SendLoanMessageDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var message = await _loanMessageService.SendAsync(userId, dto);
            return Ok(message);
        }

        //POST - mark all unread messages as read 
        [HttpPost("{loanId}/read")]
        public async Task<IActionResult> MarkAsRead(int loanId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _loanMessageService.MarkThreadAsReadAsync(loanId, userId);
            return NoContent();
        }


    }
}
