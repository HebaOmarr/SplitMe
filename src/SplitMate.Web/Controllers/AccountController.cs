using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using SplitMate.Web.Models;
using SplitMate.Web.Models.ViewModels;

namespace SplitMate.Web.Controllers;

[AllowAnonymous]
public class AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) : Controller
{
    public IActionResult Login(string? returnUrl) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await signInManager.PasswordSignInAsync(vm.UserName, vm.Password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", Ui.T("Invalid username or password."));
            return View(vm);
        }

        return Url.IsLocalUrl(vm.ReturnUrl) ? Redirect(vm.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    public IActionResult Register(string? returnUrl) => View(new RegisterVm { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new AppUser { UserName = vm.UserName, DisplayName = vm.DisplayName };
        var result = await userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(vm);
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        return Url.IsLocalUrl(vm.ReturnUrl) ? Redirect(vm.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    public IActionResult Language(string? lang, string? returnUrl)
    {
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US", lang == "ar" ? "ar" : "en-US")),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
