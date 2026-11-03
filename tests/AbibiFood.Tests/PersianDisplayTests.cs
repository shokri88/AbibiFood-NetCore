using AbibiFood.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
namespace AbibiFood.Tests;

public sealed class PersianDisplayTests
{
    [Theory]
    [InlineData("۱۴۰۳/۰۱/۰۱", 2024, 3, 20)]
    [InlineData("1403/12/30", 2025, 3, 20)]
    [InlineData("١٤٠٤/٠١/٠١", 2025, 3, 21)]
    public void ParsesPersianDatesIncludingLeapYear(string value, int year, int month, int day)
    {
        Assert.True(PersianDisplay.TryDate(value, out var date));
        Assert.Equal(new DateOnly(year, month, day), date);
        Assert.Equal(PersianDisplay.LatinDigits(value), PersianDisplay.LatinDigits(PersianDisplay.Date(date)));
    }
    [Theory]
    [InlineData("۱۴۰۴/۱۲/۳۰")]
    [InlineData("۱۴۰۵/۱۳/۰۱")]
    [InlineData("۱۴۰۵/۰۸/۳۱")]
    [InlineData("not-a-date")]
    public void RejectsInvalidPersianDates(string value) => Assert.False(PersianDisplay.TryDate(value, out _));

    [Fact]
    public void TimestampUsesTehranDayAcrossMidnight()
    {
        Assert.Equal("۱۴۰۴/۰۱/۰۱ · ۰۰:۳۰:۰۰", PersianDisplay.Timestamp(new DateTimeOffset(2025, 3, 20, 21, 0, 0, TimeSpan.Zero)));
    }
    [Fact]
    public void MoneyUsesThousandsAndNoDecimalPlaces()
    {
        Assert.Equal("۱,۲۳۴,۵۶۷", PersianDisplay.Money(1234567m));
        var provider = new PersianFormValueProvider(new FormCollection(new Dictionary<string, StringValues> { ["Price"] = "۱,۲۳۴,۵۶۷", ["DiscountPrice"] = "۹۸۷٬۶۵۴" }));
        Assert.Equal("1234567", provider.GetValue("Price").FirstValue);
        Assert.Equal("987654", provider.GetValue("DiscountPrice").FirstValue);
    }
    [Fact]
    public void FormProviderNormalizesNumbersWithoutTakingOverPasswordsOrText()
    {
        var provider = new PersianFormValueProvider(new FormCollection(new Dictionary<string, StringValues>
        { ["Price"] = "۱۲۳٬۴۵۶٫۵", ["Start"] = "۱۸:۳۰", ["Password"] = "secret۱۲۳", ["Title"] = "غذای ۱۲" }));
        Assert.Equal("123456.5", provider.GetValue("Price").FirstValue);
        Assert.Equal("18:30", provider.GetValue("Start").FirstValue);
        Assert.Equal(ValueProviderResult.None, provider.GetValue("Password"));
        Assert.Equal(ValueProviderResult.None, provider.GetValue("Title"));
    }
}
