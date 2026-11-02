using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitMate.Web.Data;
using SplitMate.Web.Models;
using SplitMate.Web.Models.ViewModels;
using SplitMate.Web.Services;

namespace SplitMate.Web.Controllers;

public class HomeController(AppDbContext db, BalanceService balances) : AppController(db)
{
    public async Task<IActionResult> Index(bool simplify = false)
    {
        var names = (await MembersAsync()).ToDictionary(m => m.Id, m => m.DisplayName);
        var txs = await Db.Transactions.AsNoTracking().Include(t => t.Shares).Where(t => t.GroupId == GroupId).ToListAsync();
        var vm = new DashboardVm { Simplify = simplify };

        Dictionary<string, decimal> mine;
        if (simplify)
        {
            var transfers = balances.Simplify(balances.GetNetBalances(txs));
            vm.GroupTransfers = transfers.Select(t => new TransferLine(Name(t.FromUserId), Name(t.ToUserId), t.Amount)).ToList();
            mine = transfers
                .Where(t => t.FromUserId == Me.Id || t.ToUserId == Me.Id)
                .ToDictionary(t => t.FromUserId == Me.Id ? t.ToUserId : t.FromUserId, t => t.FromUserId == Me.Id ? -t.Amount : t.Amount);
            vm.Direct = balances.GetBalancesFor(Me.Id, txs).Select(kv => new PersonAmount(kv.Key, Name(kv.Key), kv.Value)).OrderBy(p => p.Amount).ToList();
        }
        else
        {
            mine = balances.GetBalancesFor(Me.Id, txs);
        }

        vm.YouPayTo = mine.Where(kv => kv.Value < 0).Select(kv => new PersonAmount(kv.Key, Name(kv.Key), -kv.Value)).OrderByDescending(p => p.Amount).ToList();
        vm.PaysToYou = mine.Where(kv => kv.Value > 0).Select(kv => new PersonAmount(kv.Key, Name(kv.Key), kv.Value)).OrderByDescending(p => p.Amount).ToList();
        vm.Net = mine.Values.Sum();
        return View(vm);

        string Name(string id) => names.GetValueOrDefault(id, "?");
    }

    public async Task<IActionResult> With(string id)
    {
        var other = await Db.Users.FirstOrDefaultAsync(u => u.Id == id && u.GroupId == GroupId);
        if (other is null || other.Id == Me.Id) return RedirectToAction(nameof(Index));

        var txs = await Db.Transactions.AsNoTracking()
            .Include(t => t.Payer)
            .Include(t => t.Shares).ThenInclude(s => s.User)
            .Where(t => t.GroupId == GroupId &&
                ((t.PayerId == Me.Id && t.Shares.Any(s => s.UserId == id)) ||
                 (t.PayerId == id && t.Shares.Any(s => s.UserId == Me.Id))))
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .ToListAsync();

        var vm = new PersonHistoryVm
        {
            Other = other,
            Balance = balances.GetBalancesFor(Me.Id, txs).GetValueOrDefault(id),
            Items = txs.Select(t =>
            {
                var iPaid = t.PayerId == Me.Id;
                var share = t.Shares.First(s => s.UserId == (iPaid ? id : Me.Id));
                return new PersonHistoryItem(t, iPaid ? share.Amount : -share.Amount, share.IsPaid);
            }).ToList()
        };
        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
