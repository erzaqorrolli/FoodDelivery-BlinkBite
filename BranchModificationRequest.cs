using FoodDeliveryyy.Models.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace FoodDeliveryyy.Models.Entities
{
    public class BranchModificationRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        [StringLength(100)]
        public string RequestType { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } ="Pending";

        public string? NewAddress { get; set; }
        public string ? Address { get; set; }
        public string? NewCity { get; set; }
        public string? NewZone { get; set; }
        public bool? NewIsActive  { get; set; }
        public decimal? NewDeliveryFee { get; set; }

        public string? Reason { get; set; }

        [Required]
        public string RequestedBy { get; set; } = string.Empty;

        public string? ApprovedBy { get; set; }


        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        [ForeignKey("BranchId")]
        public virtual RestaurantAddress ? Branch { get; set; }

        [ForeignKey("RequestedBy")]
        public virtual User? Requester { get; set; }    



    }
}
