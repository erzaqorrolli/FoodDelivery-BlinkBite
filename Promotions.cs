using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodDeliveryyy.Models.Enums;

namespace FoodDeliveryyy.Models.Entities;


public class Promotions
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RestaurantId { get; set; }

    public int? RestaurantAddressId { get; set; }

    public string Kodi { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
    public decimal ZbritjaPerqind { get; set; } = decimal.Zero;

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Maximum discount must be a positive number")]
    public decimal ZbritjaMax { get; set; } = decimal.Zero;

    [Required]
    public DateTime DataFillimit { get; set; } = DateTime.Now;

    [Required]
    public DateTime DataPerfundimit { get; set; }

    [Required]
    [StringLength(20)]
    public PromotionStatus Statusi { get; set; } = PromotionStatus.Active;

    [ForeignKey("RestaurantId")]
    public virtual Restaurant? Restaurant { get; set; }

    [ForeignKey("RestaurantAddressId")]
    public virtual RestaurantAddress? RestaurantAddress { get; set; }

    [Required]
    [StringLength(20)]
    public PromotionStatus Status { get; set; } = PromotionStatus.Active;
}
