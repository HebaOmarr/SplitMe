namespace SplitMate.Web.Models.ViewModels;

public class TimelineVm
{
    public string? MemberId { get; set; }
    public string? Reason { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public List<AppUser> Members { get; set; } = [];
    public List<string> Reasons { get; set; } = [];
    public List<Transaction> Items { get; set; } = [];
}
