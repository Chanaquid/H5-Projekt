using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace backend.Hubs
{
    [Authorize]  //JWT required to connect — anonymous users cannot join

    public class ChatHub : Hub
    {
        private readonly ILoanMessageService _messageService;
        private readonly OnlineTracker _onlineTracker;

        public ChatHub(ILoanMessageService messageService, OnlineTracker onlineTracker)
        {
            _messageService = messageService;
            _onlineTracker = onlineTracker;

        }

        //join loan chat
        public async Task JoinLoanChat(int loanId)
        {
            try
            {
                var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Unauthorized.");
                    return;
                }

                var isParty = await _messageService.IsPartyToLoanAsync(loanId, userId);
                if (!isParty)
                {
                    await Clients.Caller.SendAsync("Error", "You are not a party to this loan.");
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"loan_{loanId}");

                //Track this user as online in this loan group
                _onlineTracker.Add(Context.ConnectionId, userId, loanId);

                //Also join their personal notification group for cross-page toasts
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                await Clients.Caller.SendAsync("JoinedChat", new { LoanId = loanId });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", $"Failed to join chat: {ex.Message}");
            }
        }

        //Leave loanchat
        public async Task LeaveLoanChat(int loanId)
        {
            try
            {
                var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"loan_{loanId}");
                _onlineTracker.Remove(Context.ConnectionId);

                await Clients.Caller.SendAsync("LeftChat", new { LoanId = loanId });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", $"Failed to leave chat: {ex.Message}");
            }
        }

        //Typing indicator
        public async Task Typing(int loanId)
        {
            try
            {
                var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return;

                await Clients.OthersInGroup($"loan_{loanId}")
                    .SendAsync("UserTyping", new { LoanId = loanId, UserId = userId });
            }
            catch
            {
                //Typing indicator failure is silent — not worth surfacing to user
            }
        }

        //Disconnect
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _onlineTracker.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }






    }
}
