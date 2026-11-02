using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitMate.Web.Data;
using SplitMate.Web.Models;
using SplitMate.Web.Models.ViewModels;
using SplitMate.Web.Services;

namespace SplitMate.Web.Controllers;

public class StatsController(AppDbContext db, BalanceService balances) : AppController(db)
{
    private static readonly string[] Categories = ["Food", "Rent", "Bills", "Shopping", "Transport", "Other"];

    public async Task<IActionResult> Index(string? month)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var selected = DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var m) ? m : new DateOnly(today.Year, today.Month, 1);
        var from = selected.AddMonths(-5);
        var to = selected.AddMonths(1);

        var txs = await Db.Transactions.AsNoTracking()
            .Include(t => t.Payer)
            .Include(t => t.Shares).ThenInclude(s => s.User)
            .Where(t => t.GroupId == GroupId)
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .ToListAsync();
        var expenses = txs.Where(t => t.Kind is TransactionKind.Expense or TransactionKind.FlatExpense).ToList();
        var inMonth = expenses.Where(e => e.Date >= selected && e.Date < to).ToList();

        var vm = new StatsVm
        {
            Month = selected,
            Total = inMonth.Sum(e => e.Amount),
            TotalExpenses = expenses.Sum(e => e.Amount),
            StillOwed = balances.GetNetBalances(txs).Values.Where(v => v > 0).Sum(),
            People = PersonStatsBuilder.Build(await MembersAsync(), txs, selected),
            Items = inMonth,
            Categories = Categories
                .Select(c => new StatsItem(c, inMonth.Where(e => Category(e.Reason) == c).Sum(e => e.Amount)))
                .Where(c => c.Amount > 0)
                .ToList(),
            Months = Enumerable.Range(0, 6)
                .Select(i => from.AddMonths(i))
                .Select(d => new StatsItem(Ui.Date(d, "MMM"), expenses.Where(e => e.Date >= d && e.Date < d.AddMonths(1)).Sum(e => e.Amount)))
                .ToList()
        };
        return View(vm);
    }

    private static string Category(string? reason) =>
        Categories.FirstOrDefault(c => string.Equals(c, reason, StringComparison.OrdinalIgnoreCase)) ?? "Other";
}
