using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitMate.Web.Data;
using SplitMate.Web.Models.ViewModels;

namespace SplitMate.Web.Controllers;

public class TimelineController(AppDbContext db) : AppController(db)
{
    public async Task<IActionResult> Index(string? memberId, string? reason, DateOnly? from, DateOnly? to)
    {
        var query = Db.Transactions.AsNoTracking()
            .Include(t => t.Payer)
            .Include(t => t.Shares).ThenInclude(s => s.User)
            .Where(t => t.GroupId == GroupId);

        if (!string.IsNullOrEmpty(memberId)) query = query.Where(t => t.PayerId == memberId || t.Shares.Any(s => s.UserId == memberId));
        if (!string.IsNullOrEmpty(reason)) query = query.Where(t => t.Reason != null && t.Reason.ToLower() == reason.ToLower());
        if (from is not null) query = query.Where(t => t.Date >= from);
        if (to is not null) query = query.Where(t => t.Date <= to);

        var vm = new TimelineVm
        {
            MemberId = memberId,
            Reason = reason,
            From = from,
            To = to,
            Members = await MembersAsync(),
            Reasons = await ReasonsAsync(),
            Items = await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync()
        };
        return View(vm);
    }
}
