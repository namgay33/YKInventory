using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YKInventory.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }
    
    [ForeignKey("Asset")]
    public int AssetId { get; set; }
    
    [ForeignKey("Employee")]
    public int EmployeeId { get; set; }
    
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = "CheckOut";
    public int Quantity { get; set; } = 1;
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public string ConditionOnReturn { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Approved";
    
    // Navigation properties
    public Asset Asset { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
