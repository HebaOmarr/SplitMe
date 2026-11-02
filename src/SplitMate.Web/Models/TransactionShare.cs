namespace SplitMate.Web.Models;

public class TransactionShare
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidById { get; set; }
    public List<ShareStatusLog> Logs { get; set; } = [];
}
