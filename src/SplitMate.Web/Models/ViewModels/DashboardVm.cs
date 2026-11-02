namespace SplitMate.Web.Models.ViewModels;

public record PersonAmount(string UserId, string Name, decimal Amount);

public record TransferLine(string From, string To, decimal Amount);

public class DashboardVm
{
    public bool Simplify { get; set; }
    public List<PersonAmount> YouPayTo { get; set; } = [];
    public List<PersonAmount> PaysToYou { get; set; } = [];
    public decimal Net { get; set; }
    public List<TransferLine> GroupTransfers { get; set; } = [];
    public List<PersonAmount> Direct { get; set; } = [];
}
