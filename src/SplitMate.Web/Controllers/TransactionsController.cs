using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitMate.Web.Data;
using SplitMate.Web.Models;
using SplitMate.Web.Models.ViewModels;
using SplitMate.Web.Services;

namespace SplitMate.Web.Controllers;

public class TransactionsController(AppDbContext db, BalanceService balances) : AppController(db)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    public async Task<IActionResult> Details(int id)
    {
        var tx = await Db.Transactions.AsNoTracking()
            .Include(t => t.Payer)
            .Include(t => t.Shares).ThenInclude(s => s.User)
            .Include(t => t.Shares).ThenInclude(s => s.Logs).ThenInclude(l => l.ChangedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id && t.GroupId == GroupId);
        if (tx is null) return NotFound();

        ViewBag.CreatedBy = (await Db.Users.FindAsync(tx.CreatedById))?.DisplayName;
        return View(tx);
    }

    public async Task<IActionResult> Create(bool flat = false)
    {
        var form = new ExpenseForm { PayerId = flat ? ExpenseForm.FlatMoney : Me.Id, Date = Today };
        return View("Form", await FillAsync(form, await MembersAsync()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseForm form)
    {
        var members = await MembersAsync();
        var shares = BuildShares(form, members);
        if (shares is null) return View("Form", await FillAsync(form, members));

        var tx = new Transaction { GroupId = GroupId, Kind = TransactionKind.Expense, CreatedById = Me.Id, CreatedAt = DateTime.UtcNow };
        Apply(tx, form, shares);
        Db.Transactions.Add(tx);
        await Db.SaveChangesAsync();
        TempData["Message"] = Ui.T("Expense added.");
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tx = await FindOwnExpenseAsync(id);
        if (tx is null) return CannotChange(id);

        var ids = tx.Shares.OrderBy(s => s.Id).Select(s => s.UserId).ToList();
        var form = new ExpenseForm
        {
            Id = tx.Id,
            PayerId = tx.Kind == TransactionKind.FlatExpense ? ExpenseForm.FlatMoney : tx.PayerId,
            Amount = tx.Amount,
            BeneficiaryIds = ids,
            CustomSplit = ids.Count > 0 && tx.Shares.Any(s => balances.SplitEqually(tx.Amount, ids)[s.UserId] != s.Amount),
            CustomAmounts = tx.Shares.ToDictionary(s => s.UserId, s => (decimal?)s.Amount),
            Reason = tx.Reason,
            Note = tx.Note,
            Date = tx.Date
        };
        return View("Form", await FillAsync(form, await MembersAsync()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExpenseForm form)
    {
        var tx = await FindOwnExpenseAsync(id);
        if (tx is null) return CannotChange(id);

        var members = await MembersAsync();
        var shares = BuildShares(form, members);
        if (shares is null)
        {
            form.Id = id;
            return View("Form", await FillAsync(form, members));
        }

        Apply(tx, form, shares);
        await Db.SaveChangesAsync();
        TempData["Message"] = Ui.T("Expense updated.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tx = await Db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.GroupId == GroupId && t.CreatedById == Me.Id);
        if (tx is null) return CannotChange(id);

        Db.Transactions.Remove(tx);
        await Db.SaveChangesAsync();
        TempData["Message"] = Ui.T("Deleted.");
        return RedirectToAction("Index", "Timeline");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDone(int shareId)
    {
        var share = await Db.TransactionShares.Include(s => s.Transaction)
            .FirstOrDefaultAsync(s => s.Id == shareId && s.Transaction!.GroupId == GroupId);
        if (share is null) return NotFound();

        var tx = share.Transaction!;
        if (tx.Kind != TransactionKind.Expense || share.UserId == tx.PayerId || (Me.Id != tx.PayerId && Me.Id != share.UserId))
        {
            TempData["Error"] = Ui.T("Only the payer or that person can mark this as done.");
            return RedirectToAction(nameof(Details), new { id = tx.Id });
        }

        var now = DateTime.UtcNow;
        share.IsPaid = !share.IsPaid;
        share.PaidAt = share.IsPaid ? now : null;
        share.PaidById = share.IsPaid ? Me.Id : null;
        Db.ShareStatusLogs.Add(new ShareStatusLog { TransactionShareId = share.Id, IsPaid = share.IsPaid, ChangedById = Me.Id, ChangedAt = now });
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = tx.Id });
    }

    private Task<Transaction?> FindOwnExpenseAsync(int id) =>
        Db.Transactions.Include(t => t.Shares)
            .FirstOrDefaultAsync(t => t.Id == id && t.GroupId == GroupId && t.CreatedById == Me.Id && (t.Kind == TransactionKind.Expense || t.Kind == TransactionKind.FlatExpense));

    private IActionResult CannotChange(int id)
    {
        TempData["Error"] = Ui.T("Only the person who added this can change it.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<ExpenseForm> FillAsync(ExpenseForm form, List<AppUser> members)
    {
        form.Members = members;
        form.ReasonSuggestions = await ReasonsAsync();
        return form;
    }

    private Dictionary<string, decimal>? BuildShares(ExpenseForm form, List<AppUser> members)
    {
        form.Amount = Math.Round(form.Amount, 2);
        if (form.PayerId == ExpenseForm.FlatMoney) return ModelState.IsValid ? [] : null;

        var memberIds = members.Select(m => m.Id).ToHashSet();
        var ids = form.BeneficiaryIds.Distinct().Where(memberIds.Contains).ToList();
        if (!memberIds.Contains(form.PayerId)) ModelState.AddModelError(nameof(form.PayerId), Ui.T("Pick who paid."));
        if (ids.Count == 0) ModelState.AddModelError(nameof(form.BeneficiaryIds), Ui.T("Pick at least one person."));
        if (!ModelState.IsValid) return null;

        if (!form.CustomSplit) return balances.SplitEqually(form.Amount, ids);

        var shares = ids.ToDictionary(id => id, id => Math.Round(form.CustomAmounts.GetValueOrDefault(id) ?? 0, 2));
        var total = shares.Values.Sum();
        if (shares.Values.Any(a => a <= 0))
            ModelState.AddModelError(nameof(form.CustomAmounts), Ui.T("Each selected person needs an amount above 0."));
        else if (total != form.Amount)
            ModelState.AddModelError(nameof(form.CustomAmounts), Ui.T("Custom amounts add up to {0}, not {1}.", Ui.Money(total), Ui.Money(form.Amount)));
        return ModelState.IsValid ? shares : null;
    }

    private static void Apply(Transaction tx, ExpenseForm form, Dictionary<string, decimal> shares)
    {
        var fromFlat = form.PayerId == ExpenseForm.FlatMoney;
        tx.Kind = fromFlat ? TransactionKind.FlatExpense : TransactionKind.Expense;
        tx.PayerId = fromFlat ? tx.CreatedById : form.PayerId;
        tx.Amount = form.Amount;
        tx.Reason = Clean(form.Reason);
        tx.Note = Clean(form.Note);
        tx.Date = form.Date;

        tx.Shares.RemoveAll(s => !shares.ContainsKey(s.UserId));
        foreach (var (userId, amount) in shares)
        {
            var share = tx.Shares.FirstOrDefault(s => s.UserId == userId);
            if (share is null) tx.Shares.Add(new TransactionShare { UserId = userId, Amount = amount });
            else share.Amount = amount;
        }
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
