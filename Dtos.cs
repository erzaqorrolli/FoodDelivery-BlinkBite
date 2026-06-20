using System.ComponentModel.DataAnnotations;

namespace FoodDeliveryyy.Models.DTOs;

public class RegisterDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? Role { get; set; }
}


public class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class OrderStatusUpdateDto
{
    [Required]
    public int OrderId { get; set; }
    [Required]
    public string? Comment { get; set; } = string.Empty;
}

public class SetUserRoleDto
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class AssignBranchManagerDto
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    public int RestaurantAddressId { get; set; }
}
public class BranchApplicationDto
{
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsMain { get; set; }
    public bool CreateBranchManager { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }
}