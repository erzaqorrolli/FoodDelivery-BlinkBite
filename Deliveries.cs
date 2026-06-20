using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodDeliveryyy.Models.Enums;

namespace FoodDeliveryyy.Models.Entities;

public class Deliveries
{

    [Key]
    public int Id {  get; set; }

    [Required]
    [StringLength(100)]
    public DeliveryStatus Statusi {  get; set; }  = DeliveryStatus.Pending;

    
    public DateTime? DataMarrjes { get; set; }

    public DateTime? DataDorezimit { get; set; }

    public int? KohaVlersuar {  get; set; }

    [Required]
    public int OrderId {  get; set; }

    [Required]
    public int DriverId {  get; set; }

    [ForeignKey("OrderId")]
    public virtual Orders? Order { get; set; }

    [ForeignKey("DriverId")]
    public virtual DeliveryDrivers? Driver {  get; set; }



}
