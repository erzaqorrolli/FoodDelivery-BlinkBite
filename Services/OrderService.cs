using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using FoodDeliveryyy.Hubs;
using FoodDeliveryyy.Models.Identity;


namespace FoodDeliveryyy.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly IEmailService _emailService;

    public OrderService(AppDbContext context, ILogger<OrderService> logger, IHubContext<OrderHub> hubContext, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
        _emailService = emailService;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string userId, string role, string? comment = null)
    {
        var order = await _context.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.RestaurantAddress)
            .Include(o => o.User)
            .Include(o => o.Delivery) 
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return false;

        if (order.Statusi == newStatus) return false;

        if (order.Statusi == OrderStatus.Delivered || order.Statusi == OrderStatus.Cancelled)
        {
            _logger.LogWarning("Attempted to change status of finished order {OrderId} from {OldStatus} to {NewStatus}",
                orderId, order.Statusi, newStatus);
            return false;
        }

        var normalizedRole = AppRoles.Normalize(role);

        if (normalizedRole == AppRoles.Merchant && order.Restaurant.UserId != userId)
            return false;

        if (normalizedRole == AppRoles.BranchManager)
        {
            if (!order.RestaurantAddressId.HasValue || order.RestaurantAddress?.MerchantUserId != userId)
                return false;
        }

        bool isValidTransition = (order.Statusi, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Accepted) => normalizedRole == AppRoles.Merchant || normalizedRole == AppRoles.BranchManager,
            (OrderStatus.Accepted, OrderStatus.Preparing) => normalizedRole == AppRoles.Merchant || normalizedRole == AppRoles.BranchManager,
            (OrderStatus.Preparing, OrderStatus.Ready) => normalizedRole == AppRoles.Merchant || normalizedRole == AppRoles.BranchManager,
            (OrderStatus.Ready, OrderStatus.Delivered) => normalizedRole == AppRoles.Courier,
            (_, OrderStatus.Cancelled) => normalizedRole == AppRoles.Customer || normalizedRole == AppRoles.Merchant || normalizedRole == AppRoles.BranchManager,
            _ => false
        };

        if (!isValidTransition)

        {
            _logger.LogWarning("Invalid transition {OldStatus} -> {NewStatus} by {Role}",

                order.Statusi, newStatus, role
                );
            return false;
        }
        
        var oldStatus = order.Statusi;

        order.Statusi = newStatus;
        order.StatusiUpdatedAt = DateTime.UtcNow;

        var history =new OrderStatusHistory
        {
            OrderId = orderId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangeBy = $"{role} ({userId})",
            ChangedAt = DateTime.UtcNow,
            Comments = comment ?? string.Empty
        };


        _context.OrderStatusHistories.Add(history);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group($"order-{orderId}").SendAsync("OrderStatusUpdated", new
        {
            OrderId = orderId,
            OldStatus = oldStatus.ToString(),
            NewStatus = newStatus.ToString(),
            UpdatedAt = DateTime.UtcNow,
            Comment = comment ?? string.Empty,
            ChangedBy = role
        });
        if (newStatus == OrderStatus.Ready)
        {
            await _hubContext.Clients.Group($"order-{orderId}")
                .SendAsync("DriverAssigned", new
                {
                    OrderId = orderId,
                    Message = "Your driver has been assigned and is on the way!"
                });
        }


        try
        {
            if (order.User != null && !string.IsNullOrEmpty(order.User.Email))
            {
                await _emailService.SendOrderStatusUpdateEmailAsync(
                    order.User.Email,
                    order.User.UserName ?? "Customer",
                    orderId,
                    oldStatus.ToString(),
                    newStatus.ToString()
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification for order {OrderId}", orderId);
        }
        _logger.LogInformation("Order {OrderId} status changed from {OldStatus} to {NewStatus} by {Role} ({UserId})",

            orderId, order.Statusi, newStatus, role, userId

            );

        return true;
    }

    public async Task<List<OrderStatusHistory>> GetOrderHistoryAsync(int orderId)
    {
        return await _context.OrderStatusHistories
            .Where(h => h.OrderId == orderId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }

    public async Task<OrderStatusHistory?> GetLastStatusChangeAsync(int orderId)
    {
        return await _context.OrderStatusHistories
            .Where(h => h.OrderId == orderId)
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> OrderExistsAsync(int orderId)
    {
        return await _context.Orders.AnyAsync(x => x.Id == orderId);
    }

    
}

