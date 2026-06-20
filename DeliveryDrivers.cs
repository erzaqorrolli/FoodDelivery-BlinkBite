using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodDeliveryyy.Models.Enums;
using FoodDeliveryyy.Models.Identity;

namespace FoodDeliveryyy.Models.Entities;

public class DeliveryDrivers
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Automjeti { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Targa { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Zona { get; set; } = string.Empty;

    [Required]
    public DriverStatus Statusi { get; set; } = DriverStatus.Available;

    [Range(0, 5)]
    public decimal Vlersimi { get; set; }

   
    [Required]
    public string UserId { get; set; } = string.Empty;

    
    [ForeignKey("UserId")]
    public virtual User? User {  get; set; }

    

}