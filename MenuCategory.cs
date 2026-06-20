using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodDeliveryyy.Models.Entities;

public class MenuCategory {

    [Key]
    public int Id {  get; set; }

    [Required]
    [StringLength(100)]
    public string Emertimi { get; set; }= string.Empty;

    [StringLength(500)]
    public string Pershkrimi { get; set; } = string.Empty;

    public int Renditja { get; set; }

    [Required]
    public int RestaurantId { get; set; }
    [ForeignKey("RestaurantId")]
    public virtual Restaurant? Restaurant { get; set; }

    public virtual ICollection<MenuItems> MenuItems { get; set; } = new List<MenuItems>();
}