using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodDeliveryyy.Models.Enums;

namespace FoodDeliveryyy.Models.Entities;


public class OrderStatusHistory
{

    [Key]
    public int Id { get; set; }

    [Required]

    public int OrderId { get; set; }

    [Required]
    public OrderStatus OldStatus { get; set; }

    [Required]
    public OrderStatus NewStatus { get; set; }

    [Required]
    [StringLength (500)]
    public string ChangeBy { get; set; }= string.Empty;

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string Comments { get; set; } =string.Empty;

    [ForeignKey("OrderId")]
    public virtual Orders? Order { get; set; }
}