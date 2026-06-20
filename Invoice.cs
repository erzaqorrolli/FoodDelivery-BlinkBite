using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodDeliveryyy.Models.Entities;

public class Invoice
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public string InvoiceNumber { get; set; }= string.Empty;

    [Required]
    [Column(TypeName = "decimal(10.2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10.2)")]
    public decimal DeliveryFee { get; set; }

    [Column(TypeName = "decimal(10.2)")]
    public decimal Discount { get; set; }

    [Required]
    [Column(TypeName = "decimal(10.2)")]
    public decimal Total { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    [ForeignKey("OrderId")]
    public virtual Orders? Order { get; set; }




}