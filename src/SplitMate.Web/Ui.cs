using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using SplitMate.Web.Models;

namespace SplitMate.Web;

public static class Ui
{
    private static readonly string[] Colors = ["#6366f1", "#ec4899", "#f59e0b", "#10b981", "#06b6d4", "#8b5cf6", "#ef4444", "#84cc16"];
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-EG");

    public static bool Ar => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    public static string T(string text) => Ar ? Arabic.Texts.GetValueOrDefault(text, text) : text;

    public static string T(string text, params object?[] args) => string.Format(T(text), args);

    public static string Date(DateOnly date, string format) => date.ToString(format, Ar ? ArabicCulture : CultureInfo.CurrentCulture);

    public static string Money(decimal amount) => amount.ToString("#,0.00");

    public static IHtmlContent Avatar(string? name, string size = "")
    {
        var initial = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[..1].ToUpperInvariant();
        var color = Colors[(name ?? "").Trim().ToLowerInvariant().Aggregate(7, (h, c) => (h * 31 + c) % 1000003) % Colors.Length];
        return new HtmlString($"<span class=\"avatar {size}\" style=\"background:{color}\">{HtmlEncoder.Default.Encode(initial)}</span>");
    }

    public static string Icon(Transaction t) => t.Kind == TransactionKind.Contribution ? "💰" : t.Kind == TransactionKind.Settlement ? "🤝" : t.Reason?.ToLowerInvariant() switch
    {
        "food" => "🍕",
        "transport" => "🚕",
        "rent" => "🏠",
        "shopping" => "🛍️",
        "bills" => "💡",
        _ => "🧾"
    };

    public static string Tone(Transaction t) => t.Kind is TransactionKind.Settlement or TransactionKind.Contribution ? "tone-settle" : Tone(t.Reason);

    public static string Tone(string? reason) => reason?.ToLowerInvariant() switch
    {
        "food" => "tone-food",
        "transport" => "tone-transport",
        "rent" => "tone-rent",
        "shopping" => "tone-shopping",
        "bills" => "tone-bills",
        _ => "tone-other"
    };

    public static string Day(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (date == today) return T("Today");
        if (date == today.AddDays(-1)) return T("Yesterday");
        return Date(date, date.Year == today.Year ? "ddd, MMM d" : "MMM d, yyyy");
    }
}
