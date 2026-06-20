using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodDeliveryyy.Models.Enums;
using FoodDeliveryyy.Models.Identity;

namespace FoodDeliveryyy.Models.Entities;

public class Restaurant {
    [Key]
    public int Id {  get; set; }


    [Required]
    [StringLength(100)]
    public string Emertimi { get; set; } = string.Empty;

    [StringLength(500)]
    public string Pershkrimi {  get; set; } = string.Empty;

    [Required]
    public string Telefoni { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email {  get; set; } = string.Empty;

    public string Logo { get; set; } = string.Empty;
    
    public TimeOnly OrariHapjes {  get; set; }

    public TimeOnly OrariMbylljes { get; set; }

    [Range (0,5)]

    public decimal Rating { get; set; }

    [StringLength(20)]
    public RestaurantStatus Statusi {  get; set; } = RestaurantStatus.Pending;

    [Required]
    public string UserId {  get; set; } = string.Empty;

    [StringLength(50)]
    public string Kategori { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }

    [ForeignKey("UserId")]
    //virtual -> mundeson lazyloading (te dhenat i marrim veq kur na duhen)
    public virtual User? User { get; set; }
    public virtual ICollection<RestaurantAddress> Adresat { get; set; } = new List<RestaurantAddress>();
    public virtual ICollection<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();

    public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();

    public virtual ICollection<Promotions> Promotions { get; set; } = new List<Promotions>();

    public virtual ICollection<Reviews> Reviews { get; set; } = new List<Reviews>();

    [NotMapped]

    public bool Opened
    {
        get {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            return now >=OrariHapjes && now<=OrariMbylljes;
                }
    }

}


