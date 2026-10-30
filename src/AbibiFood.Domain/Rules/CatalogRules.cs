namespace AbibiFood.Domain.Rules;

public static class CatalogRules
{
    public static void Price(decimal price, decimal? discount)
    {
        if (price < 0 || (discount.HasValue && (discount < 0 || discount >= price)))
            throw new ArgumentException("قیمت نامعتبر است؛ قیمت تخفیفی باید از قیمت اصلی کمتر باشد.");
    }
    // رزرو و فروش بعداً اضافه می‌شوند؛ ظرفیت نباید از تعهدهای ثبت‌شده کمتر شود.
    public static void Capacity(int total, int reserved, int sold, int warning)
    {
        if (total < 0 || reserved < 0 || sold < 0 || warning < 0 || total < reserved + sold)
            throw new ArgumentException("ظرفیت نمی‌تواند منفی یا کمتر از مجموع فروش و رزرو باشد.");
    }
    public static void Addon(int parentId, int addonId, int parentBranch, int addonBranch, int max)
    {
        if (parentId == addonId || parentBranch != addonBranch || max < 1 || max > 20)
            throw new ArgumentException("افزودنی باید قلم دیگری از همان شعبه با تعداد مجاز ۱ تا ۲۰ باشد.");
    }
}
