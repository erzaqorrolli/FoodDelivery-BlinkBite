using System.ComponentModel.DataAnnotations;

namespace FoodDeliveryyy.Models.Entities;

public class RestaurantApplication
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RestaurantName { get; set; } = string.Empty;

    public string? RestaurantDescription { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }
}