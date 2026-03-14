using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static backend.DTOs.ChatDTO;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/messages/direct")]
    [Authorize]
    public class DirectMessageController : ControllerBase
    {
        private readonly IDirectMessageService _directMessageService;

        public DirectMessageController(IDirectMessageService directMessageService)
        {
            _directMessageService = directMessageService;
        }

        //GET — inbox list
        [HttpGet]
        public async Task<IActionResult> GetInbox()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var inbox = await _directMessageService.GetInboxAsync(userId);
            return Ok(inbox);
        }

        //GET — full thread/convo
        [HttpGet("{conversationId}")]
        public async Task<IActionResult> GetThread(int conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var thread = await _directMessageService.GetThreadAsync(conversationId, userId);
            return Ok(thread);
        }


        //POST- send message, finds or creates conversation automatically
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] DirectMessageDTO.SendDirectMessageDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var message = await _directMessageService.SendAsync(userId, dto);
            return Ok(message);
        }


        //POST — mark all unread as read
        [HttpPost("{conversationId}/read")]
        public async Task<IActionResult> MarkAsRead(int conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _directMessageService.MarkAsReadAsync(conversationId, userId);
            return NoContent();
        }


        //POST — hide/delete conversation for this user
        [HttpPost("{conversationId}/hide")]
        public async Task<IActionResult> Hide(int conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _directMessageService.HideConversationAsync(conversationId, userId);
            return NoContent();
        }


    }

}
