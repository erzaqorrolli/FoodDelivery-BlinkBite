using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodDeliveryyy.Models.Enums;
using FoodDeliveryyy.Models.Identity;

namespace FoodDeliveryyy.Models.Entities;

public class Orders : IValidatableObject
{

    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "UserId is required")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "RestaurantId is required")]
    public int RestaurantId { get; set; }

    public int? RestaurantAddressId { get; set; }

    [Required(ErrorMessage = "Delivery Address is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Address must be at least 5 characters long.")]
    [RegularExpression(@"^[a-zA-Z0-9\s,.-]+$", ErrorMessage = "Address contains invalid characters.")]
    public string AdresaDorezimit { get; set; } = string.Empty;

    [Required(ErrorMessage = "Total Amount is required")]
    [Range(0.01, 99999.99, ErrorMessage = "Total Amount must be between 0.01 and 99999.99")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ShumaTotale { get; set; } = decimal.Zero;

    [Range(0, 99.99, ErrorMessage = "Delivery Fee must be between 0 and 99.99")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TarifaDorezimit { get; set; } = decimal.Zero;

    [Range(0, double.MaxValue, ErrorMessage = "Discount must be between 0 and 99.99")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Zbritja { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [EnumDataType(typeof(OrderStatus), ErrorMessage = "The order status is invalid")]
    public OrderStatus Statusi { get; set; } = OrderStatus.Pending;

    [Required(ErrorMessage = "Payment method is required")]
    [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Payment method is invalid")]
    public PaymentMethod MetodaPageses { get; set; } = PaymentMethod.Cash;

    [DataType(DataType.DateTime)]
    public DateTime DataPorosis { get; set; } = DateTime.UtcNow;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Shenimet { get; set; } = string.Empty;

    // ========== KËTO DY RRRESHTA I SHTONI ==========
    public string? AssignedCourierId { get; set; }  // ID e courier-it që e dorëzoi porosinë
    public DateTime? AssignedAt { get; set; }       // Data kur courier-i e mori porosinë
    // ========== DERI KËTU ==========

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("RestaurantId")]
    public virtual Restaurant? Restaurant { get; set; }

    [ForeignKey("RestaurantAddressId")]
    public virtual RestaurantAddress? RestaurantAddress { get; set; }

    public virtual ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();

    public virtual Deliveries? Delivery { get; set; }

    public virtual Reviews? Review { get; set; }

    public DateTime? StatusiUpdatedAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Statusi == OrderStatus.Delivered && ShumaTotale <= 0)
        {
            yield return new ValidationResult(
                "Delivered orders must have a total greater than €0",
                new[] { nameof(ShumaTotale) }
            );
        }

        if (StatusiUpdatedAt.HasValue && StatusiUpdatedAt < DataPorosis)
        {
            yield return new ValidationResult(
                "The status update date cannot be before the order date",
                new[] { nameof(StatusiUpdatedAt) }
            );
        }
    }
}