using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoodDeliveryyy.Models.Identity;
namespace FoodDeliveryyy.Models.Entities
{
    public class RestaurantAddress
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int RestaurantId { get; set; }

        public string? MerchantUserId { get; set; }


        [Required]
        [StringLength(200)]
        public string Adresa { get; set; } = string.Empty;


        [Required]
        [StringLength(20)]
        public string Qyteti { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Zona { get; set; }

        public bool IsMain { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Added delivery fee property to match BranchModificationRequest.NewDeliveryFee
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TarifaDorezimit { get; set; }

        [ForeignKey("RestaurantId")]
        public virtual Restaurant? Restaurant { get; set; }

        [ForeignKey("MerchantUserId")]
        public virtual User? MerchantUser { get; set; }
    }
}
