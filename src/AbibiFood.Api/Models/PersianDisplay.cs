using System.Globalization;
using AbibiFood.Domain.Entities;
namespace AbibiFood.Api.Models;

public static class PersianDisplay
{
    public static string LatinDigits(string? value) => string.Concat((value ?? "").Select(c =>
        c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c))
        .Replace('٫', '.').Replace("٬", "");
    public static string Digits(string value) => string.Concat(value.Select(c => c is >= '0' and <= '9' ? (char)('۰' + c - '0') : c));
    public static string Date(DateOnly date)
    {
        var calendar = new PersianCalendar(); var day = date.ToDateTime(TimeOnly.MinValue);
        return Digits(string.Create(CultureInfo.InvariantCulture, $"{calendar.GetYear(day):0000}/{calendar.GetMonth(day):00}/{calendar.GetDayOfMonth(day):00}"));
    }
    public static string Timestamp(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(instant, "Asia/Tehran");
        return Date(DateOnly.FromDateTime(local.DateTime)) + " · " + Digits(local.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
    }
    public static bool TryDate(string? input, out DateOnly date)
    {
        date = default;
        var parts = LatinDigits(input).Trim().Split('/');
        if (parts.Length != 3 || parts[0].Length != 4 || !int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month) || !int.TryParse(parts[2], out var day)) return false;
        try { date = DateOnly.FromDateTime(new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0)); return true; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
    public static string Day(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "شنبه", DayOfWeek.Sunday => "یکشنبه", DayOfWeek.Monday => "دوشنبه",
        DayOfWeek.Tuesday => "سه‌شنبه", DayOfWeek.Wednesday => "چهارشنبه", DayOfWeek.Thursday => "پنجشنبه", _ => "جمعه"
    };
    public static string Hours(HoursKind kind) => kind == HoursKind.Ordering ? "سفارش‌گیری" : "مراجعه حضوری";
}
