using Microsoft.AspNetCore.SignalR;

namespace BacklogApi.Infrastructure.Hubs;

public class BacklogHub : Hub
{
    public async Task NotifyItemsUpdated()
    {
        await Clients.All.SendAsync("ItemsUpdated");
    }
}
