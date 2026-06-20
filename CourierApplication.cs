using System.ComponentModel.DataAnnotations;

namespace FoodDeliveryyy.Models.Entities;

public class CourierApplication
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string VehicleType { get; set; } = string.Empty;

    public string? LicensePlate { get; set; }

    [Required]
    public string WorkingArea { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }
}