using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitMate.Web.Data;
using SplitMate.Web.Models;

namespace SplitMate.Web.Controllers;

public class GroupController(AppDbContext db, UserManager<AppUser> userManager) : Controller
{
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<IActionResult> Index()
    {
        var me = await userManager.GetUserAsync(User);
        if (me?.GroupId is null) return RedirectToAction(nameof(Setup));

        var group = await db.Groups.Include(g => g.Members).FirstAsync(g => g.Id == me.GroupId);
        ViewBag.IsOwner = group.OwnerId == me.Id;
        return View(group);
    }

    public async Task<IActionResult> Setup(string? code)
    {
        var me = await userManager.GetUserAsync(User);
        if (me?.GroupId is not null) return RedirectToAction(nameof(Index));

        ViewBag.Code = code;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? name)
    {
        var me = await userManager.GetUserAsync(User);
        if (me is null) return RedirectToAction("Login", "Account");
        if (me.GroupId is not null) return RedirectToAction(nameof(Index));

        name = name?.Trim() ?? "";
        if (name.Length is 0 or > 50)
        {
            TempData["Error"] = Ui.T("Group name is required (max 50 characters).");
            return RedirectToAction(nameof(Setup));
        }

        me.Group = new Group { Name = name, JoinCode = await NewCodeAsync(), OwnerId = me.Id };
        await db.SaveChangesAsync();
        TempData["Message"] = Ui.T("Group created. Share the join code with your friends.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(string? code)
    {
        var me = await userManager.GetUserAsync(User);
        if (me is null) return RedirectToAction("Login", "Account");
        if (me.GroupId is not null) return RedirectToAction(nameof(Index));

        code = code?.Trim().ToUpperInvariant() ?? "";
        var group = await db.Groups.FirstOrDefaultAsync(g => g.JoinCode == code);
        if (group is null)
        {
            TempData["Error"] = Ui.T("No group found with that code.");
            return RedirectToAction(nameof(Setup), new { code });
        }

        me.GroupId = group.Id;
        await db.SaveChangesAsync();
        TempData["Message"] = Ui.T("You joined {0}.", group.Name);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateCode()
    {
        var me = await userManager.GetUserAsync(User);
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == me!.GroupId && g.OwnerId == me.Id);
        if (group is null) return RedirectToAction(nameof(Index));

        group.JoinCode = await NewCodeAsync();
        await db.SaveChangesAsync();
        TempData["Message"] = Ui.T("New join code created. The old one no longer works.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> NewCodeAsync()
    {
        string code;
        do code = RandomNumberGenerator.GetString(CodeChars, 8);
        while (await db.Groups.AnyAsync(g => g.JoinCode == code));
        return code;
    }
}
