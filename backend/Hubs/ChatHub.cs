using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace backend.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILoanMessageService _messageService;
        private readonly IOnlineTracker _onlineTracker;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILoanMessageService messageService, IOnlineTracker onlineTracker, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _onlineTracker = onlineTracker;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                _logger.LogInformation("[ChatHub] Client connected: {ConnectionId}, User: {UserId}",
                    Context.ConnectionId,
                    Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown");
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChatHub] OnConnectedAsync failed");
                throw;
            }
        }

        public async Task JoinLoanChat(int loanId)
        {
            try
            {
                var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation("[ChatHub] JoinLoanChat called. LoanId: {LoanId}, UserId: {UserId}", loanId, userId);

                if (userId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Unauthorized.");
                    return;
                }

                var isAdmin = Context.User?.IsInRole("Admin") ?? false;
                var isParty = await _messageService.IsPartyToLoanAsync(loanId, userId);

                _logger.LogInformation("[ChatHub] isAdmin: {IsAdmin}, isParty: {IsParty}", isAdmin, isParty);

                if (!isParty && !isAdmin)
                {
                    await Clients.Caller.SendAsync("Error", "You are not a party to this loan.");
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"loan_{loanId}");

                if (isParty)
                    _onlineTracker.Add(Context.ConnectionId, userId, loanId);

                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                await Clients.Caller.SendAsync("JoinedChat", new { LoanId = loanId });

                _logger.LogInformation("[ChatHub] Successfully joined loan_{LoanId}", loanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChatHub] JoinLoanChat failed for LoanId: {LoanId}", loanId);
                await Clients.Caller.SendAsync("Error", $"Failed to join chat: {ex.Message}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("[ChatHub] Client disconnected: {ConnectionId}, Error: {Error}",
                Context.ConnectionId,
                exception?.Message ?? "none");
            _onlineTracker.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinUserGroup()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("[ChatHub] User {UserId} joined personal notification group", userId);
        }

        public async Task LeaveUserGroup()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("[ChatHub] User {UserId} left personal notification group", userId);
        }


    }

}
