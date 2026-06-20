using FoodDeliveryyy.Models.DTOs;

namespace FoodDeliveryyy.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendOrderStatusUpdateEmailAsync(string to, string customerName, int orderId, string oldStatus, string newStatus);
    Task SendMerchantCredentialsEmailAsync(string to, string restaurantName, string username, string password);
    Task SendBranchManagerCredentialsEmailAsync(string to, string branchAddress, string restaurantName, string username, string password);
    Task SendCourierCredentialsEmailAsync(string to, string fullName, string username, string password);
}