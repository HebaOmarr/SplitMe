using SplitMate.Web.Models;
using SplitMate.Web.Models.ViewModels;

namespace SplitMate.Web.Services;

public static class PersonStatsBuilder
{
    public static List<PersonStats> Build(IEnumerable<AppUser> members, IEnumerable<Transaction> transactions, DateOnly month)
    {
        var people = members.ToList();
        var names = people.ToDictionary(m => m.Id, m => m.DisplayName);
        var paid = transactions.Where(t => t.Kind != TransactionKind.FlatExpense).ToLookup(t => t.PayerId);

        return people
            .Select(m => Build(m, paid[m.Id].ToList(), names, month))
            .OrderByDescending(p => p.Paid)
            .ThenBy(p => p.Name)
            .ToList();
    }

    private static PersonStats Build(AppUser member, List<Transaction> paid, Dictionary<string, string> names, DateOnly month)
    {
        var inMonth = paid.Where(t => IsIn(t, month)).ToList();
        return new PersonStats(
            member.DisplayName,
            inMonth.Sum(t => t.Amount),
            Totals(inMonth.Select(t => (Reason(t), t.Amount))),
            Totals(inMonth.SelectMany(t => ForWhom(t, member.Id, names))),
            Enumerable.Range(-5, 6)
                .Select(month.AddMonths)
                .Select(d => new StatsItem(Ui.Date(d, "MMM"), paid.Where(t => IsIn(t, d)).Sum(t => t.Amount)))
                .ToList());
    }

    private static bool IsIn(Transaction t, DateOnly month) => t.Date >= month && t.Date < month.AddMonths(1);

    private static string Reason(Transaction t) => t.Kind switch
    {
        TransactionKind.Contribution => Ui.T("Into the flat"),
        TransactionKind.Settlement => Ui.T("Paid back"),
        _ => Ui.T(t.Reason ?? "Other")
    };

    private static IEnumerable<(string Name, decimal Amount)> ForWhom(Transaction t, string memberId, Dictionary<string, string> names)
    {
        if (t.Kind == TransactionKind.Contribution) return [(Ui.T("The flat"), t.Amount)];
        return t.Shares.Select(s => (s.UserId == memberId ? Ui.T("Own share") : names.GetValueOrDefault(s.UserId, "?"), s.Amount));
    }

    private static List<StatsItem> Totals(IEnumerable<(string Name, decimal Amount)> rows) =>
        rows.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new StatsItem(g.First().Name, g.Sum(r => r.Amount)))
            .OrderByDescending(i => i.Amount)
            .ToList();
}
