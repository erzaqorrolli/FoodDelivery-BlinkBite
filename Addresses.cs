using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodDeliveryyy.Models.Entities;


public class Addresses
{

    [Key]
    public int Id {  get; set; }

    [Required]
    public string UserId {  get; set; }

    [Required]
    [StringLength(100)]
    public string Emertimi {  get; set; }

    [Required]
    [StringLength(600)]
    public string Adresa { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Qyteti { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9,6)")]
    public decimal Latitude { get; set; }
    [Column(TypeName = "decimal(9,6)")]
    public decimal Longitude { get; set; }

    public bool EshteKryesore {  get; set; }=false;

    [ForeignKey("UserId")]
    public virtual Identity.User? User { get; set; }




}