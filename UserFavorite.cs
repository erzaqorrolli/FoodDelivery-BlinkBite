using FoodDeliveryyy.Models.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodDeliveryyy.Models.Entities;

public class UserFavorite
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }

    public int? RestaurantId { get; set; }  

    public int? MenuItemId { get; set; }    

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; }  

    [ForeignKey("RestaurantId")]
    public virtual Restaurant Restaurant { get; set; }

    [ForeignKey("MenuItemId")]
    public virtual MenuItems MenuItems { get; set; }  


}