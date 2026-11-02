using System.ComponentModel.DataAnnotations;

namespace SplitMate.Web.Models.ViewModels;

public class LoginVm
{
    [Required]
    [Display(Name = "Username")]
    public string UserName { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
