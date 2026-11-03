using AbibiFood.Domain.Entities;
namespace AbibiFood.Domain.Rules;

public static class CapacitySchedule
{
    public static DateOnly Today() => Today(DateTimeOffset.UtcNow);
    public static DateOnly Today(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(instant, "Asia/Tehran").DateTime);

    // رکورد تاریخ‌دار بر پیش‌فرض اولویت دارد؛ هیچ مصرفی از روز قبل منتقل نمی‌شود.
    public static DailyCapacity? Resolve(BranchFood food, DateOnly day, DateOnly today, DailyCapacity? recorded)
    {
        if (recorded is not null) return recorded;
        if (day < today || !food.DefaultDailyCapacity.HasValue) return null;
        return new DailyCapacity
        {
            BranchFoodId = food.Id, Date = day, Total = food.DefaultDailyCapacity.Value,
            WarningThreshold = food.DefaultCapacityWarningThreshold, IsActive = food.IsDailyCapacityActive
        };
    }
}
