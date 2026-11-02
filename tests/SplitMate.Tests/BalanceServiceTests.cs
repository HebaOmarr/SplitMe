using SplitMate.Web.Models;
using SplitMate.Web.Services;

namespace SplitMate.Tests;

public class BalanceServiceTests
{
    private readonly BalanceService _service = new();

    private static Transaction Expense(string payer, params (string User, decimal Amount)[] shares) => new()
    {
        Kind = TransactionKind.Expense,
        PayerId = payer,
        Amount = shares.Sum(s => s.Amount),
        Shares = shares.Select(s => new TransactionShare { UserId = s.User, Amount = s.Amount }).ToList()
    };

    private static Transaction Settlement(string from, string to, decimal amount) => new()
    {
        Kind = TransactionKind.Settlement,
        PayerId = from,
        Amount = amount,
        Shares = [new TransactionShare { UserId = to, Amount = amount }]
    };

    private Transaction EqualExpense(string payer, decimal amount, params string[] users) =>
        Expense(payer, _service.SplitEqually(amount, users).Select(kv => (kv.Key, kv.Value)).ToArray());

    [Fact]
    public void SplitEqually_DividesEvenly()
    {
        var shares = _service.SplitEqually(300, ["A", "B", "C"]);

        Assert.All(shares.Values, v => Assert.Equal(100m, v));
    }

    [Fact]
    public void SplitEqually_GivesLeftoverCentsToFirstPeople()
    {
        var shares = _service.SplitEqually(100, ["A", "B", "C"]);

        Assert.Equal(33.34m, shares["A"]);
        Assert.Equal(33.33m, shares["B"]);
        Assert.Equal(33.33m, shares["C"]);
        Assert.Equal(100m, shares.Values.Sum());
    }

    [Fact]
    public void EqualSplit_PayerIncluded_OthersOweTheirShare()
    {
        var txs = new[] { EqualExpense("A", 300, "A", "B", "C") };

        var forA = _service.GetBalancesFor("A", txs);
        var forB = _service.GetBalancesFor("B", txs);

        Assert.Equal(100m, forA["B"]);
        Assert.Equal(100m, forA["C"]);
        Assert.Equal(-100m, forB["A"]);
        Assert.False(forB.ContainsKey("C"));
    }

    [Fact]
    public void CustomSplit_UsesEachPersonsAmount()
    {
        var txs = new[] { Expense("A", ("B", 70), ("C", 30)) };

        var forA = _service.GetBalancesFor("A", txs);

        Assert.Equal(70m, forA["B"]);
        Assert.Equal(30m, forA["C"]);
    }

    [Fact]
    public void DebtsBothWays_AreNetted()
    {
        var txs = new[] { Expense("A", ("B", 100)), Expense("B", ("A", 40)) };

        Assert.Equal(60m, _service.GetBalancesFor("A", txs)["B"]);
        Assert.Equal(-60m, _service.GetBalancesFor("B", txs)["A"]);
    }

    [Fact]
    public void Settlement_ReducesDebt()
    {
        var txs = new[] { Expense("A", ("B", 100)), Settlement("B", "A", 60) };

        Assert.Equal(40m, _service.GetBalancesFor("A", txs)["B"]);
    }

    [Theory]
    [InlineData(TransactionKind.Contribution)]
    [InlineData(TransactionKind.FlatExpense)]
    public void FlatMoney_DoesNotChangeBalances(TransactionKind kind)
    {
        var flat = new Transaction { Kind = kind, PayerId = "B", Amount = 500 };
        var txs = new[] { Expense("A", ("B", 100)), flat };

        Assert.Equal(100m, _service.GetBalancesFor("A", txs)["B"]);
        Assert.Equal(-100m, _service.GetNetBalances(txs)["B"]);
    }

    [Fact]
    public void FullSettlement_ClearsDebt()
    {
        var txs = new[] { Expense("A", ("B", 100)), Settlement("B", "A", 100) };

        Assert.Empty(_service.GetBalancesFor("A", txs));
        Assert.Empty(_service.GetNetBalances(txs));
    }

    [Fact]
    public void DoneShare_IsNotCounted()
    {
        var tx = Expense("A", ("B", 100), ("C", 50));
        tx.Shares[0].IsPaid = true;

        var forA = _service.GetBalancesFor("A", [tx]);

        Assert.False(forA.ContainsKey("B"));
        Assert.Equal(50m, forA["C"]);
    }

    [Fact]
    public void NetBalances_SumToZero()
    {
        var txs = new[] { EqualExpense("A", 300, "A", "B", "C"), Expense("B", ("C", 50)) };

        var net = _service.GetNetBalances(txs);

        Assert.Equal(200m, net["A"]);
        Assert.Equal(-50m, net["B"]);
        Assert.Equal(-150m, net["C"]);
        Assert.Equal(0m, net.Values.Sum());
    }

    [Fact]
    public void Simplify_CollapsesChain()
    {
        var txs = new[] { Expense("B", ("A", 100)), Expense("C", ("B", 100)) };

        var transfers = _service.Simplify(_service.GetNetBalances(txs));

        var transfer = Assert.Single(transfers);
        Assert.Equal(new Transfer("A", "C", 100m), transfer);
    }

    [Fact]
    public void Simplify_KeepsEveryonesNetTotal()
    {
        var txs = new[]
        {
            EqualExpense("A", 300, "A", "B", "C", "D"),
            Expense("B", ("C", 40), ("D", 60)),
            Expense("D", ("A", 25))
        };
        var net = _service.GetNetBalances(txs);

        var transfers = _service.Simplify(net);

        foreach (var (user, amount) in net)
        {
            var received = transfers.Where(t => t.ToUserId == user).Sum(t => t.Amount);
            var paid = transfers.Where(t => t.FromUserId == user).Sum(t => t.Amount);
            Assert.Equal(amount, received - paid);
        }
        Assert.True(transfers.Count < net.Count);
    }

    [Fact]
    public void Simplify_NothingOwed_ReturnsEmpty()
    {
        Assert.Empty(_service.Simplify([]));
    }
}
