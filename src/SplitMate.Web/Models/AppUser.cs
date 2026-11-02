using Microsoft.AspNetCore.Identity;

namespace SplitMate.Web.Models;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
}
