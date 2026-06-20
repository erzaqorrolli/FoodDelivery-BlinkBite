using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodDeliveryyy.Models.Entities;

public class MenuItemBranch
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MenuItemId { get; set; }

    [Required]
    public int RestaurantAddressId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Cmimi { get; set; }

    public bool? Disponueshme { get; set; }

    public string? Perberesit { get; set; }

    public string? RequestOptions { get; set; }

    public int? PromotionId { get; set; }

    [ForeignKey("MenuItemId")]
    public virtual MenuItems? MenuItem { get; set; }

    [ForeignKey("RestaurantAddressId")]
    public virtual RestaurantAddress? RestaurantAddress { get; set; }
}