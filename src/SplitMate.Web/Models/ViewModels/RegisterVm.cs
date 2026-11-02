using System.ComponentModel.DataAnnotations;

namespace SplitMate.Web.Models.ViewModels;

public class RegisterVm
{
    [Required, StringLength(30)]
    [Display(Name = "Username")]
    public string UserName { get; set; } = "";

    [Required, StringLength(50)]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
