using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Enums;

namespace FoodDeliveryyy.Services;

public interface IOrderService
{
    Task<bool> UpdateOrderStatusAsync(int orderid, OrderStatus newStatus, string userId, string role, string? comment = null);

    Task<List<OrderStatusHistory>> GetOrderHistoryAsync(int orderId);
    Task<OrderStatusHistory> GetLastStatusChangeAsync(int orderId);
    Task<bool> OrderExistsAsync(int orderId);

}


