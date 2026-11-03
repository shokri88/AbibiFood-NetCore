using AbibiFood.Application.Abstractions;
using AbibiFood.Domain.Entities;
using AbibiFood.Domain.Rules;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Application.Services;

public static class CapacityReader
{
    public static async Task<List<DailyCapacity>> ReadAsync(IAppDbContext db, int branchId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from || to.DayNumber - from.DayNumber > 366) throw new ArgumentException("بازه ظرفیت نامعتبر است.");
        var foods = await db.BranchFoods.AsNoTracking().Where(f => f.BranchId == branchId).ToListAsync(ct);
        var ids = foods.Select(f => f.Id).ToArray();
        var records = (await db.DailyCapacities.AsNoTracking().Where(c => ids.Contains(c.BranchFoodId) && c.Date >= from && c.Date <= to).ToListAsync(ct))
            .ToDictionary(c => (c.BranchFoodId, c.Date));
        var today = CapacitySchedule.Today();
        var result = new List<DailyCapacity>();
        for (var offset = 0; offset <= to.DayNumber - from.DayNumber; offset++)
            foreach (var food in foods)
            {
                var day = from.AddDays(offset);
                records.TryGetValue((food.Id, day), out var recorded);
                var capacity = CapacitySchedule.Resolve(food, day, today, recorded);
                if (capacity is not null) result.Add(capacity);
            }
        return result;
    }
}
