using System.ComponentModel.DataAnnotations;

namespace SplitMate.Web.Models.ViewModels;

public class ExpenseForm
{
    public const string FlatMoney = "flat";

    public int? Id { get; set; }

    [Required, Display(Name = "Paid by")]
    public string PayerId { get; set; } = "";

    [Range(typeof(decimal), "0.01", "10000000")]
    public decimal Amount { get; set; }

    [Display(Name = "For whom")]
    public List<string> BeneficiaryIds { get; set; } = [];

    public bool CustomSplit { get; set; }

    public Dictionary<string, decimal?> CustomAmounts { get; set; } = [];

    [StringLength(30)]
    public string? Reason { get; set; }

    [StringLength(100)]
    public string? Note { get; set; }

    public DateOnly Date { get; set; }

    public List<AppUser> Members { get; set; } = [];

    public List<string> ReasonSuggestions { get; set; } = [];
}
