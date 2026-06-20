using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodDeliveryyy.Models.Entities;

public class OrderItems {

    [Key]
    public int Id {  get; set; }

    [Required]
    public int Sasia { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Cmimi { get; set; } = decimal.Zero;

    [StringLength(300)]
    public string Shenimet { get; set; } = string.Empty;

    [Required]
    public int OrderId {  get; set; }

    [Required]
    public int MenuItemId {  get; set; }

    [ForeignKey("OrderId")]
    public virtual Orders? Order { get; set; }

    [ForeignKey("MenuItemId")]
    public virtual MenuItems? MenuItem { get; set; }





}
