using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitMate.Web.Data;
using SplitMate.Web.Models;
using SplitMate.Web.Models.ViewModels;

namespace SplitMate.Web.Controllers;

public class ContributionsController(AppDbContext db) : AppController(db)
{
    public async Task<IActionResult> Index()
    {
        var form = new ContributionForm { PayerId = Me.Id, Date = DateOnly.FromDateTime(DateTime.Now) };
        return View(await FillAsync(form));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContributionForm form)
    {
        var members = await MembersAsync();
        if (members.All(m => m.Id != form.PayerId)) ModelState.AddModelError(nameof(form.PayerId), Ui.T("Pick who paid."));
        if (!ModelState.IsValid) return View(await FillAsync(form));

        Db.Transactions.Add(new Transaction
        {
            GroupId = GroupId,
            Kind = TransactionKind.Contribution,
            PayerId = form.PayerId,
            Amount = Math.Round(form.Amount, 2),
            Note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim(),
            Date = form.Date,
            CreatedById = Me.Id,
            CreatedAt = DateTime.UtcNow
        });
        await Db.SaveChangesAsync();
        TempData["Message"] = Ui.T("Flat payment added.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<ContributionForm> FillAsync(ContributionForm form)
    {
        form.Members = await MembersAsync();
        var txs = await Db.Transactions.AsNoTracking()
            .Include(t => t.Payer)
            .Where(t => t.GroupId == GroupId && (t.Kind == TransactionKind.Contribution || t.Kind == TransactionKind.FlatExpense))
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .ToListAsync();
        form.Items = txs.Where(t => t.Kind == TransactionKind.Contribution).ToList();
        form.Bills = txs.Where(t => t.Kind == TransactionKind.FlatExpense).ToList();
        form.Collected = form.Items.Sum(t => t.Amount);
        form.Spent = form.Bills.Sum(t => t.Amount);
        form.Spending = form.Bills
            .GroupBy(t => Ui.T(t.Reason ?? "Other"), StringComparer.OrdinalIgnoreCase)
            .Select(g => new StatsItem(g.Key, g.Sum(t => t.Amount)))
            .OrderByDescending(i => i.Amount)
            .ToList();
        form.Totals = form.Members
            .Select(m => new StatsItem(m.DisplayName, form.Items.Where(t => t.PayerId == m.Id).Sum(t => t.Amount)))
            .OrderByDescending(p => p.Amount)
            .ToList();
        return form;
    }
}
