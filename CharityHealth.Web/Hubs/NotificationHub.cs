using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using CharityHealth.Application.Interfaces.Services;

namespace CharityHealth.Web.Hubs
{

    [Authorize]
    public class NotificationHub : Hub
    {
        /// <summary>Called when a user connects — join their personal group.</summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId is not null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            await base.OnConnectedAsync();
        }
    }

    // ─────────────────────────────────────────────────────
    // Notification Sender — inject this into handlers
    // ─────────────────────────────────────────────────────


    public class SignalRNotificationSender(IHubContext<NotificationHub> hubContext)
        : INotificationSender
    {
        public async Task SendToUserAsync(
            string userId, string eventName, object payload, CancellationToken ct = default)
            => await hubContext.Clients
                .Group($"user_{userId}")
                .SendAsync(eventName, payload, ct);

        public async Task SendToRoleGroupAsync(
            string role, string eventName, object payload, CancellationToken ct = default)
            => await hubContext.Clients
                .Group($"role_{role}")
                .SendAsync(eventName, payload, ct);
    }

}
