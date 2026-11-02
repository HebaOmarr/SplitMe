using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SplitMate.Web.Data;
using SplitMate.Web.Models;

namespace SplitMate.Web.Controllers;

public abstract class AppController(AppDbContext db) : Controller
{
    private static readonly string[] PresetReasons = ["Food", "Transport", "Rent", "Shopping", "Bills", "Other"];

    protected AppDbContext Db { get; } = db;
    protected AppUser Me { get; private set; } = null!;
    protected int GroupId => Me.GroupId!.Value;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            await next();
            return;
        }

        var me = await Db.Users.FindAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (me is null)
        {
            context.Result = RedirectToAction("Login", "Account");
            return;
        }
        if (me.GroupId is null)
        {
            context.Result = RedirectToAction("Setup", "Group");
            return;
        }

        Me = me;
        ViewBag.MeId = me.Id;
        ViewBag.MeName = me.DisplayName;
        await next();
    }

    protected Task<List<AppUser>> MembersAsync() =>
        Db.Users.Where(u => u.GroupId == GroupId).OrderBy(u => u.DisplayName).ToListAsync();

    protected async Task<List<string>> ReasonsAsync()
    {
        var used = await Db.Transactions
            .Where(t => t.GroupId == GroupId && t.Reason != null)
            .Select(t => t.Reason!)
            .Distinct()
            .ToListAsync();
        return PresetReasons.Concat(used.Order()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
