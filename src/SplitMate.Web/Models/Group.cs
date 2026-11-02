namespace SplitMate.Web.Models;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string JoinCode { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public List<AppUser> Members { get; set; } = [];
}
