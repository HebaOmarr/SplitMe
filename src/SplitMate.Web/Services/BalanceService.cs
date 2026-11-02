using SplitMate.Web.Models;

namespace SplitMate.Web.Services;

public record Transfer(string FromUserId, string ToUserId, decimal Amount);

public class BalanceService
{
    public Dictionary<string, decimal> SplitEqually(decimal amount, IList<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        var baseShare = Math.Floor(amount / ids.Count * 100) / 100;
        var leftover = amount - baseShare * ids.Count;
        var result = new Dictionary<string, decimal>();
        foreach (var id in ids)
        {
            var share = baseShare;
            if (leftover > 0)
            {
                share += 0.01m;
                leftover -= 0.01m;
            }
            result[id] = share;
        }
        return result;
    }

    public Dictionary<string, decimal> GetBalancesFor(string userId, IEnumerable<Transaction> transactions)
    {
        var result = new Dictionary<string, decimal>();
        foreach (var (debtor, creditor, amount) in GetDebts(transactions))
        {
            if (creditor == userId) Add(result, debtor, amount);
            else if (debtor == userId) Add(result, creditor, -amount);
        }
        return result.Where(kv => kv.Value != 0).ToDictionary();
    }

    public Dictionary<string, decimal> GetNetBalances(IEnumerable<Transaction> transactions)
    {
        var result = new Dictionary<string, decimal>();
        foreach (var (debtor, creditor, amount) in GetDebts(transactions))
        {
            Add(result, creditor, amount);
            Add(result, debtor, -amount);
        }
        return result.Where(kv => kv.Value != 0).ToDictionary();
    }

    public List<Transfer> Simplify(Dictionary<string, decimal> netBalances)
    {
        var debtors = netBalances.Where(kv => kv.Value < 0).Select(kv => (Id: kv.Key, Left: -kv.Value)).OrderByDescending(x => x.Left).ToList();
        var creditors = netBalances.Where(kv => kv.Value > 0).Select(kv => (Id: kv.Key, Left: kv.Value)).OrderByDescending(x => x.Left).ToList();
        var transfers = new List<Transfer>();
        int d = 0, c = 0;
        while (d < debtors.Count && c < creditors.Count)
        {
            var amount = Math.Min(debtors[d].Left, creditors[c].Left);
            transfers.Add(new Transfer(debtors[d].Id, creditors[c].Id, amount));
            debtors[d] = (debtors[d].Id, debtors[d].Left - amount);
            creditors[c] = (creditors[c].Id, creditors[c].Left - amount);
            if (debtors[d].Left == 0) d++;
            if (creditors[c].Left == 0) c++;
        }
        return transfers;
    }

    private static IEnumerable<(string Debtor, string Creditor, decimal Amount)> GetDebts(IEnumerable<Transaction> transactions) =>
        transactions.SelectMany(t => t.Shares
            .Where(s => s.UserId != t.PayerId && !s.IsPaid)
            .Select(s => (s.UserId, t.PayerId, s.Amount)));

    private static void Add(Dictionary<string, decimal> map, string key, decimal amount) =>
        map[key] = map.GetValueOrDefault(key) + amount;
}
