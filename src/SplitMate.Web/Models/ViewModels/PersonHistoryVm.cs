namespace SplitMate.Web.Models.ViewModels;

public record PersonHistoryItem(Transaction Transaction, decimal Amount, bool IsPaid);

public class PersonHistoryVm
{
    public AppUser Other { get; set; } = null!;
    public decimal Balance { get; set; }
    public List<PersonHistoryItem> Items { get; set; } = [];
}
