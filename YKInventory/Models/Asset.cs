using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YKInventory.Models;

public class Asset
{
    [Key]
    public int Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchaseCost { get; set; }
    
    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string Specifications { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
