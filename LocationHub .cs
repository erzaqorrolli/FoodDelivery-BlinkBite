using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace FoodDeliveryyy.Hubs;

[Authorize]
public class LocationHub : Hub {

    private static readonly Dictionary<int, DriverLocation> _driverLocations = new();
    private readonly ILogger<LocationHub> _logger;

    public LocationHub(ILogger<LocationHub> logger)
    {
        _logger = logger;
    }

    public async Task UpdateDriverLocation(int orderId, double latitude, double longitude) {
        var driverId = Context.UserIdentifier;

        _driverLocations[orderId] = new DriverLocation
        {

            OrderId = orderId,
            DriverId = driverId,
            Latitude = latitude,
            Longitude = longitude,
            LastUpdate = DateTime.UtcNow,
        };

        await Clients.Group($"order-{orderId}").SendAsync("DriverLocationUpdated", new
        {
            OrderId = orderId,
            Latitude=latitude,
            Longitude=longitude,
            LastUpdate=DateTime.UtcNow,

        });

        _logger.LogInformation("Driver {DriverId} updated location for order{OrderId}", driverId, orderId);
    }
    public async Task JoinOrderTracking(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");

        if (_driverLocations.TryGetValue(orderId, out var lastLocation)) {

            await Clients.Caller.SendAsync("DriverLocationUpdated", new
            {
            OrderId=orderId,
            Latitude=lastLocation.Latitude,
            Longitude=lastLocation.Longitude,
            LastUpdate=lastLocation.LastUpdate
            });
        }
    }

    public async Task LeaveOrderTracking(int orderId) {

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }
}
public class DriverLocation
{
    public int OrderId { get; set; }
    public string? DriverId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime LastUpdate { get; set; }
}