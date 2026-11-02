namespace SplitMate.Web.Models;

public class Transaction
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public TransactionKind Kind { get; set; }
    public string PayerId { get; set; } = "";
    public AppUser? Payer { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public DateOnly Date { get; set; }
    public string CreatedById { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<TransactionShare> Shares { get; set; } = [];
}
