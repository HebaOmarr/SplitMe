namespace SplitMate.Web.Models.ViewModels;

public record StatsItem(string Name, decimal Amount);

public record StatsMonth(string Name, decimal Collected, decimal Spent);

public class StatsVm
{
    public DateOnly Month { get; set; }
    public decimal Total { get; set; }
    public decimal Collected { get; set; }
    public decimal SpentFromFlat { get; set; }
    public decimal StillOwed { get; set; }
    public List<Transaction> Contributions { get; set; } = [];
    public List<StatsItem> Payers { get; set; } = [];
    public List<Transaction> Items { get; set; } = [];
    public List<StatsItem> Categories { get; set; } = [];
    public List<StatsMonth> Months { get; set; } = [];
}
