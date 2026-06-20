using System.ComponentModel.DataAnnotations;

namespace FoodDeliveryyy.Models.Entities;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public virtual ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
}
