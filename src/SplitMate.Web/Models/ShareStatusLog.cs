namespace SplitMate.Web.Models;

public class ShareStatusLog
{
    public int Id { get; set; }
    public int TransactionShareId { get; set; }
    public bool IsPaid { get; set; }
    public string ChangedById { get; set; } = "";
    public AppUser? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
}
