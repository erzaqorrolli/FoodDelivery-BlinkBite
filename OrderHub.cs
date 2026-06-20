using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodDeliveryyy.Hubs;


[Authorize]
public class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }


public async Task JoinOrderGroup(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
        _logger.LogInformation("Connection {ConnectionId} joined group order-{OrderId}",

            Context.ConnectionId, orderId
            );
    }

    public async Task LeaveOrderGroup(int orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,$"order-{orderId}");

        _logger.LogInformation("Connection {ConnectionId} left the group order-{OrderId}",
            Context.ConnectionId, orderId
            );
    }


    public async Task RequestCurrentStatus (int orderId)
    {
        await Clients.Caller.SendAsync("StatusReceived", new { OrderId = orderId });
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Connection {ConnectionId} connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync (Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
                await base.OnDisconnectedAsync(exception);
    }
}