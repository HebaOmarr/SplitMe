using SplitMate.Web.Models;
using SplitMate.Web.Models.ViewModels;
using SplitMate.Web.Services;

namespace SplitMate.Tests;

public class PersonStatsTests
{
    private static readonly DateOnly November = new(2026, 11, 1);

    private static readonly List<AppUser> Members =
    [
        new() { Id = "A", DisplayName = "Ann" },
        new() { Id = "B", DisplayName = "Bob" },
        new() { Id = "C", DisplayName = "Cid" }
    ];

    private static Transaction Expense(string payer, string date, string? reason, params (string User, decimal Amount)[] shares) => new()
    {
        Kind = TransactionKind.Expense,
        PayerId = payer,
        Reason = reason,
        Date = DateOnly.Parse(date),
        Amount = shares.Sum(s => s.Amount),
        Shares = shares.Select(s => new TransactionShare { UserId = s.User, Amount = s.Amount }).ToList()
    };

    private static Transaction Contribution(string payer, string date, decimal amount) => new()
    {
        Kind = TransactionKind.Contribution,
        PayerId = payer,
        Date = DateOnly.Parse(date),
        Amount = amount
    };

    private static PersonStats Stats(string name, params Transaction[] transactions) =>
        PersonStatsBuilder.Build(Members, transactions, November).Single(p => p.Name == name);

    [Fact]
    public void Paid_AddsBillsAndFlatPaymentsInTheMonthOnly()
    {
        var stats = Stats("Ann",
            Expense("A", "2026-11-02", "Rent", ("B", 300)),
            Contribution("A", "2026-11-30", 500),
            Expense("A", "2026-10-31", "Rent", ("B", 100)),
            Expense("A", "2026-12-01", "Rent", ("B", 100)),
            Expense("B", "2026-11-02", "Rent", ("A", 900)));

        Assert.Equal(800m, stats.Paid);
    }

    [Fact]
    public void Reasons_ShowWhereTheMoneyWent_LargestFirst()
    {
        var stats = Stats("Ann",
            Expense("A", "2026-11-02", "Rent", ("B", 300)),
            Expense("A", "2026-11-03", "Food", ("B", 60)),
            Expense("A", "2026-11-04", "food", ("B", 40)),
            Expense("A", "2026-11-05", null, ("B", 10)),
            Contribution("A", "2026-11-06", 500));

        Assert.Equal(
            [new StatsItem("Into the flat", 500), new StatsItem("Rent", 300), new StatsItem("Food", 100), new StatsItem("Other", 10)],
            stats.Reasons);
    }

    [Fact]
    public void ForWhom_SplitsBillsByShare_AndCountsTheFlat()
    {
        var stats = Stats("Ann",
            Expense("A", "2026-11-02", "Rent", ("A", 100), ("B", 150), ("C", 50)),
            Expense("A", "2026-11-03", "Food", ("B", 50)),
            Contribution("A", "2026-11-04", 500));

        Assert.Equal(
            [new StatsItem("The flat", 500), new StatsItem("Bob", 200), new StatsItem("Own share", 100), new StatsItem("Cid", 50)],
            stats.ForWhom);
    }

    [Fact]
    public void FlatMoneyBill_IsNotCountedForThePersonWhoAddedIt()
    {
        var bill = new Transaction { Kind = TransactionKind.FlatExpense, PayerId = "A", Date = new DateOnly(2026, 11, 2), Amount = 400, Reason = "Bills" };

        var stats = Stats("Ann", bill);

        Assert.Equal(0m, stats.Paid);
        Assert.Empty(stats.Reasons);
        Assert.Empty(stats.ForWhom);
        Assert.All(stats.Months, m => Assert.Equal(0m, m.Amount));
    }

    [Fact]
    public void Months_ListSixMonthsEndingAtTheSelectedMonth()
    {
        var stats = Stats("Ann",
            Expense("A", "2026-11-02", "Rent", ("B", 300)),
            Contribution("A", "2026-10-02", 100),
            Expense("A", "2026-06-30", "Food", ("B", 20)),
            Expense("A", "2026-05-31", "Food", ("B", 999)));

        Assert.Equal([20m, 0m, 0m, 0m, 100m, 300m], stats.Months.Select(m => m.Amount));
    }

    [Fact]
    public void Settlement_CountsAsPaidBackToThatPerson()
    {
        var settlement = new Transaction
        {
            Kind = TransactionKind.Settlement,
            PayerId = "A",
            Date = new DateOnly(2026, 11, 2),
            Amount = 70,
            Shares = [new TransactionShare { UserId = "B", Amount = 70 }]
        };

        var stats = Stats("Ann", settlement);

        Assert.Equal(70m, stats.Paid);
        Assert.Equal([new StatsItem("Paid back", 70)], stats.Reasons);
        Assert.Equal([new StatsItem("Bob", 70)], stats.ForWhom);
    }

    [Fact]
    public void People_ListEveryMember_HighestPaidFirst()
    {
        var people = PersonStatsBuilder.Build(Members, [Contribution("B", "2026-11-02", 50), Contribution("C", "2026-11-02", 90)], November);

        Assert.Equal(["Cid", "Bob", "Ann"], people.Select(p => p.Name));
        Assert.Equal([90m, 50m, 0m], people.Select(p => p.Paid));
    }
}
