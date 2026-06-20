using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using FoodDeliveryyy.Models.Converters;

namespace FoodDeliveryyy.Models.Entities;

public class MenuItems
{

    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Emertimi { get; set; } = string.Empty;

    [StringLength(500)]
    public string Pershkrimi { get; set; } = string.Empty;

    [Required]
    //10 shifra gjithsej, 2 pas presjes dhjetore
    [Column(TypeName = "decimal(10,2)")]
    public decimal Cmimi { get; set; }

    public string? Foto { get; set; }

    public bool Disponueshme { get; set; } = true;

    [StringLength(200)]
    public string Alergjene { get; set; } = string.Empty;

    public int? Kalori { get; set; }

    [JsonConverter(typeof(StringOrArrayToCsvJsonConverter))]
    public string Perberesit { get; set; } = string.Empty;

    [JsonConverter(typeof(StringOrArrayToCsvJsonConverter))]
    public string RequestOptions { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    public int? RestaurantAddressId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual MenuCategory? Category { get; set; }

    [ForeignKey("RestaurantAddressId")]
    public virtual RestaurantAddress? RestaurantAddress { get; set; }

    // Lidhje me branch customizations
    public virtual ICollection<MenuItemBranch> BranchDetails { get; set; } = new List<MenuItemBranch>();
}