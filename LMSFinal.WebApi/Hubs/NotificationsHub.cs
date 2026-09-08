using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LMSFinal.WebApi.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    // Группа = userId: PublishAsync шлёт по userId, а не по ConnectionId,
    // потому что у одного пользователя может быть несколько вкладок/устройств.
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }
}
