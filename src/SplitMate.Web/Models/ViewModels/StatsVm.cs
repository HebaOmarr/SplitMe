namespace SplitMate.Web.Models.ViewModels;

public record StatsItem(string Name, decimal Amount);

public record PersonStats(string Name, decimal Paid, List<StatsItem> Reasons, List<StatsItem> ForWhom, List<StatsItem> Months);

public class StatsVm
{
    public DateOnly Month { get; set; }
    public decimal Total { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal StillOwed { get; set; }
    public List<PersonStats> People { get; set; } = [];
    public List<Transaction> Items { get; set; } = [];
    public List<StatsItem> Categories { get; set; } = [];
    public List<StatsItem> Months { get; set; } = [];
}
