using System.ComponentModel.DataAnnotations;

namespace SplitMate.Web.Models.ViewModels;

public class ContributionForm
{
    [Required, Display(Name = "Paid by")]
    public string PayerId { get; set; } = "";

    [Range(typeof(decimal), "0.01", "10000000")]
    public decimal Amount { get; set; }

    [StringLength(100)]
    public string? Note { get; set; }

    public DateOnly Date { get; set; }

    public List<AppUser> Members { get; set; } = [];

    public decimal Collected { get; set; }

    public decimal Spent { get; set; }

    public List<StatsItem> Totals { get; set; } = [];

    public List<Transaction> Items { get; set; } = [];

    public List<StatsItem> Spending { get; set; } = [];

    public List<Transaction> Bills { get; set; } = [];
}
