using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodDeliveryyy.Models.Identity;

namespace FoodDeliveryyy.Models.Entities;

public class BranchApplication
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RestaurantId { get; set; }

    [Required]
    [StringLength(255)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string Zone { get; set; } = string.Empty;

    public decimal DeliveryFee { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsMain { get; set; }

    // Për Branch Manager
    public bool CreateBranchManager { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }

    // Statusi i aplikimit
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public string? AdminNotes { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Navigation properties
    [ForeignKey("RestaurantId")]
    public virtual Restaurant? Restaurant { get; set; }
}