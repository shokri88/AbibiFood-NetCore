using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
namespace AbibiFood.Api.Models;

// فقط فیلدهای عددی/ساعت فرم پنل نرمال می‌شوند؛ رمز، متن، شناسه‌ها و قرارداد JSON دست‌نخورده‌اند.
public sealed class PersianFormValueProvider(IFormCollection form) : FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture)
{
    private static readonly HashSet<string> NumericFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "BranchId", "FoodId", "BranchFoodId", "BranchGroupId", "ParentBranchFoodId", "AddonBranchFoodId",
        "Latitude", "Longitude", "SortOrder", "Price", "DiscountPrice", "MaxQuantity", "Total", "WarningThreshold", "Start", "End"
    };
    public override ValueProviderResult GetValue(string key)
    {
        if (!NumericFields.Contains(key)) return ValueProviderResult.None;
        var result = base.GetValue(key);
        return result == ValueProviderResult.None ? result : new ValueProviderResult(
            new StringValues(result.Values.Select(PersianDisplay.LatinDigits).ToArray()), CultureInfo.InvariantCulture);
    }
}
public sealed class PersianFormValueProviderFactory : IValueProviderFactory
{
    public async Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        var request = context.ActionContext.HttpContext.Request;
        if (context.ActionContext.RouteData.Values["controller"]?.ToString() != "Admin" || !request.HasFormContentType) return;
        context.ValueProviders.Add(new PersianFormValueProvider(await request.ReadFormAsync(request.HttpContext.RequestAborted)));
    }
}
