using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Models;
using AbibiFood.Domain.Entities;
using AbibiFood.Domain.Rules;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Application.Services;

public sealed class CatalogService(IAppDbContext db, IUserScope user)
{
    public async Task<List<Branch>> BranchesAsync(CancellationToken ct) =>
        await db.Branches.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
    private void Manager()
    {
        if (!user.IsManager) throw new UnauthorizedAccessException("این عملیات فقط برای مدیر مجاز است.");
    }
    private Task AuditAsync(string action, object entity, int? branch, CancellationToken ct) =>
        db.SaveWithAuditAsync(new AuditLog
        {
            ActorId = user.UserId,
            BranchId = branch,
            Action = action,
            EntityType = entity.GetType().Name
        }, entity, ct);
    private static void ImagePath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && (!path.StartsWith("/assets/") && !path.StartsWith("/uploads/")))
            throw new ArgumentException("مسیر تصویر باید زیر /assets/ یا /uploads/ باشد.");
        if (path.Contains("..") || path.Contains('\\')) throw new ArgumentException("مسیر تصویر نامعتبر است.");
    }
    public async Task<int> SaveBranchAsync(BranchInput input, CancellationToken ct)
    {
        Manager(); var entity = input.Id == 0 ? new Branch() : await db.Branches.SingleAsync(x => x.Id == input.Id, ct);
        entity.Name = input.Name.Trim(); entity.Address = input.Address.Trim(); entity.Description = input.Description;
        entity.Latitude = input.Latitude; entity.Longitude = input.Longitude; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.Branches.Add(entity);
        await AuditAsync("SaveBranch", entity, input.Id == 0 ? null : input.Id, ct); return entity.Id;
    }
    public async Task<int> SaveFoodAsync(FoodInput input, CancellationToken ct)
    {
        Manager(); var entity = input.Id == 0 ? new Food() : await db.Foods.SingleAsync(x => x.Id == input.Id, ct);
        entity.Name = input.Name.Trim(); entity.Title = entity.Name; entity.Description = input.Description; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.Foods.Add(entity);
        await AuditAsync("SaveFood", entity, null, ct); return entity.Id;
    }
    public async Task<int> SaveImageAsync(ImageInput input, CancellationToken ct)
    {
        Manager(); ImagePath(input.Path);
        if (!await db.Foods.AnyAsync(x => x.Id == input.FoodId, ct)) throw new ArgumentException("غذا پیدا نشد.");
        var entity = input.Id == 0 ? new FoodImage() : await db.FoodImages.SingleAsync(x => x.Id == input.Id, ct);
        entity.FoodId = input.FoodId; entity.Path = input.Path; entity.SortOrder = input.SortOrder; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.FoodImages.Add(entity);
        await AuditAsync("SaveImage", entity, null, ct); return entity.Id;
    }
    public async Task<int> SaveGroupAsync(GroupInput input, CancellationToken ct)
    {
        Manager(); await user.EnsureBranchAsync(input.BranchId, ct); ImagePath(input.ImagePath);
        var entity = input.Id == 0 ? new BranchGroup() : await db.BranchGroups.SingleAsync(x => x.Id == input.Id, ct);
        if (input.Id != 0 && entity.BranchId != input.BranchId) throw new ArgumentException("شعبه رکورد قابل تغییر نیست.");
        entity.BranchId = input.BranchId; entity.Title = input.Title.Trim(); entity.ImagePath = input.ImagePath;
        entity.SortOrder = input.SortOrder; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.BranchGroups.Add(entity);
        await AuditAsync("SaveGroup", entity, input.BranchId, ct); return entity.Id;
    }
    public async Task<int> SaveBranchFoodAsync(BranchFoodInput input, CancellationToken ct)
    {
        Manager(); await user.EnsureBranchAsync(input.BranchId, ct); CatalogRules.Price(input.Price, input.DiscountPrice);
        var entity = input.Id == 0 ? new BranchFood() : await db.BranchFoods.SingleAsync(x => x.Id == input.Id, ct);
        if (input.Id != 0 && (entity.BranchId != input.BranchId || entity.FoodId != input.FoodId))
            throw new ArgumentException("هویت غذای شعبه قابل تغییر نیست.");
        if (!await db.Foods.AnyAsync(x => x.Id == input.FoodId, ct)) throw new ArgumentException("غذا پیدا نشد.");
        if (input.Id != 0) SetVersion(entity, input.Version);
        entity.BranchId = input.BranchId; entity.FoodId = input.FoodId; entity.Price = input.Price;
        entity.DiscountPrice = input.DiscountPrice; entity.CanSellStandalone = input.CanSellStandalone; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.BranchFoods.Add(entity);
        await AuditAsync("SaveBranchFood", entity, input.BranchId, ct); return entity.Id;
    }
    private static byte[] VersionBytes(string value)
    {
        try { return Convert.FromBase64String(value); }
        catch (FormatException) { throw new ArgumentException("نسخه رکورد نامعتبر است."); }
    }
    private static void SetVersion(BranchFood entity, string? version)
    {
        if (version is null || !entity.RowVersion.SequenceEqual(VersionBytes(version)))
            throw new AbibiFood.Domain.Rules.ConflictException("اطلاعات تغییر کرده است؛ صفحه را دوباره بارگذاری کنید.");
    }
    public async Task<int> SaveGroupFoodAsync(GroupFoodInput input, CancellationToken ct)
    {
        Manager(); var group = await db.BranchGroups.SingleAsync(x => x.Id == input.BranchGroupId, ct);
        var food = await db.BranchFoods.SingleAsync(x => x.Id == input.BranchFoodId, ct);
        await user.EnsureBranchAsync(group.BranchId, ct);
        if (group.BranchId != food.BranchId) throw new ArgumentException("گروه و غذا باید متعلق به یک شعبه باشند.");
        var entity = input.Id == 0 ? new GroupFood() : await db.GroupFoods.SingleAsync(x => x.Id == input.Id, ct);
        entity.BranchGroupId = input.BranchGroupId; entity.BranchFoodId = input.BranchFoodId;
        entity.SortOrder = input.SortOrder; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.GroupFoods.Add(entity);
        await AuditAsync("SaveGroupFood", entity, group.BranchId, ct); return entity.Id;
    }
    public async Task<int> SaveAddonAsync(AddonInput input, CancellationToken ct)
    {
        Manager(); var parent = await db.BranchFoods.SingleAsync(x => x.Id == input.ParentBranchFoodId, ct);
        var addon = await db.BranchFoods.SingleAsync(x => x.Id == input.AddonBranchFoodId, ct);
        await user.EnsureBranchAsync(parent.BranchId, ct);
        CatalogRules.Addon(parent.Id, addon.Id, parent.BranchId, addon.BranchId, input.MaxQuantity);
        var entity = input.Id == 0 ? new AllowedAddon() : await db.AllowedAddons.SingleAsync(x => x.Id == input.Id, ct);
        entity.ParentBranchFoodId = parent.Id; entity.AddonBranchFoodId = addon.Id;
        entity.MaxQuantity = input.MaxQuantity; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.AllowedAddons.Add(entity);
        await AuditAsync("SaveAddon", entity, parent.BranchId, ct); return entity.Id;
    }
    public async Task SaveCapacityDefaultsAsync(CapacityDefaultsInput input, CancellationToken ct)
    {
        await user.EnsureBranchAsync(input.BranchId, ct);
        var selected = input.Foods.Where(f => f.Selected).ToList();
        var ids = selected.Select(f => f.BranchFoodId).Distinct().ToArray();
        if (ids.Length == 0 || ids.Length > 500 || ids.Length != selected.Count)
            throw new ArgumentException("بین یک تا ۵۰۰ غذای متفاوت انتخاب کنید.");
        CatalogRules.Capacity(input.Total, 0, 0, input.WarningThreshold);
        var foods = await db.BranchFoods.Where(f => ids.Contains(f.Id) && f.BranchId == input.BranchId).ToListAsync(ct);
        if (foods.Count != ids.Length) throw new ArgumentException("همه غذاها باید متعلق به شعبه انتخاب‌شده باشند.");
        // قبل از تغییر هر رکورد، تمام انتخاب‌ها و نسخه‌ها بررسی می‌شوند.
        foreach (var food in foods) SetVersion(food, selected.Single(f => f.BranchFoodId == food.Id).Version);
        foreach (var food in foods)
        {
            food.DefaultDailyCapacity = input.Total;
            food.DefaultCapacityWarningThreshold = input.WarningThreshold;
            food.IsDailyCapacityActive = input.IsActive;
        }
        await db.SaveWithAuditAsync(new AuditLog
        {
            ActorId = user.UserId, BranchId = input.BranchId, Action = "SaveCapacityDefaults", EntityType = nameof(BranchFood),
            Details = System.Text.Json.JsonSerializer.Serialize(new { BranchFoodIds = ids, input.Total, input.WarningThreshold, input.IsActive })
        }, foods[0], ct);
    }
    private async Task ValidateHomeContentAsync(SpecialInput input, CancellationToken ct)
    {
        Manager(); await user.EnsureBranchAsync(input.BranchId, ct);
        if (input.SortOrder < 0 || !await db.BranchFoods.AnyAsync(f => f.Id == input.BranchFoodId && f.BranchId == input.BranchId, ct))
            throw new ArgumentException("غذا باید از همان شعبه انتخاب شود و ترتیب منفی نباشد.");
    }
    public async Task<int> SaveSpecialAsync(SpecialInput input, CancellationToken ct)
    {
        await ValidateHomeContentAsync(input, ct);
        var entity = input.Id == 0 ? new BranchSpecial { BranchId = input.BranchId } : await db.BranchSpecials.SingleAsync(x => x.Id == input.Id, ct);
        if (entity.BranchId != input.BranchId) throw new ArgumentException("شعبه رکورد قابل تغییر نیست.");
        if (await db.BranchSpecials.AnyAsync(x => x.BranchId == input.BranchId && x.BranchFoodId == input.BranchFoodId && x.Id != input.Id, ct))
            throw new ArgumentException("این غذا قبلاً در پیشنهادهای ویژه ثبت شده است.");
        entity.BranchFoodId = input.BranchFoodId; entity.SortOrder = input.SortOrder; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.BranchSpecials.Add(entity);
        await AuditAsync("SaveSpecial", entity, input.BranchId, ct); return entity.Id;
    }
    public async Task<int> SaveBannerAsync(BannerInput input, CancellationToken ct)
    {
        await ValidateHomeContentAsync(input, ct);
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 200 || string.IsNullOrWhiteSpace(input.Text) || input.Text.Trim().Length > 2000)
            throw new ArgumentException("عنوان و متن معتبر برای بنر وارد کنید.");
        ImagePath(input.ImagePath);
        if (string.IsNullOrWhiteSpace(input.ImagePath)) throw new ArgumentException("تصویر اختصاصی بنر را انتخاب کنید.");
        var entity = input.Id == 0 ? new BranchBanner { BranchId = input.BranchId } : await db.BranchBanners.SingleAsync(x => x.Id == input.Id, ct);
        if (entity.BranchId != input.BranchId) throw new ArgumentException("شعبه رکورد قابل تغییر نیست.");
        entity.BranchFoodId = input.BranchFoodId; entity.SortOrder = input.SortOrder; entity.IsActive = input.IsActive;
        entity.Title = input.Title.Trim(); entity.Text = input.Text.Trim(); entity.ImagePath = input.ImagePath;
        if (input.Id == 0) db.BranchBanners.Add(entity);
        await AuditAsync("SaveBanner", entity, input.BranchId, ct); return entity.Id;
    }
    public async Task<int> SaveCapacityAsync(CapacityInput input, CancellationToken ct)
    {
        var food = await db.BranchFoods.SingleAsync(x => x.Id == input.BranchFoodId, ct);
        await user.EnsureBranchAsync(food.BranchId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Tehran").DateTime);
        if (input.Date < today || input.Date > today.AddDays(366)) throw new ArgumentException("تاریخ ظرفیت باید امروز تا یک سال آینده باشد.");
        var entity = await db.DailyCapacities.SingleOrDefaultAsync(x => x.BranchFoodId == input.BranchFoodId && x.Date == input.Date, ct);
        if (entity is null) { entity = new DailyCapacity { BranchFoodId = food.Id, Date = input.Date }; db.DailyCapacities.Add(entity); }
        else if (input.Version is null || !entity.RowVersion.SequenceEqual(VersionBytes(input.Version)))
            throw new AbibiFood.Domain.Rules.ConflictException("ظرفیت تغییر کرده؛ صفحه را دوباره بارگذاری کنید.");
        CatalogRules.Capacity(input.Total, entity.Reserved, entity.Sold, input.WarningThreshold);
        entity.Total = input.Total; entity.WarningThreshold = input.WarningThreshold; entity.IsActive = input.IsActive;
        await AuditAsync("SaveCapacity", entity, food.BranchId, ct); return entity.Id;
    }
    public async Task<int> SaveHoursAsync(HoursInput input, CancellationToken ct)
    {
        Manager(); await user.EnsureBranchAsync(input.BranchId, ct);
        if (!Enum.IsDefined(input.Kind) || !Enum.IsDefined(input.Day) || input.Start == input.End)
            throw new ArgumentException("نوع ساعت، روز یا بازه زمانی نامعتبر است.");
        var entity = input.Id == 0 ? new BranchHours() : await db.BranchHours.SingleAsync(x => x.Id == input.Id, ct);
        if (input.Id != 0 && entity.BranchId != input.BranchId) throw new ArgumentException("شعبه رکورد قابل تغییر نیست.");
        entity.BranchId = input.BranchId; entity.Kind = input.Kind; entity.Day = input.Day;
        entity.Start = input.Start; entity.End = input.End; entity.IsActive = input.IsActive;
        if (input.Id == 0) db.BranchHours.Add(entity);
        await AuditAsync("SaveHours", entity, input.BranchId, ct); return entity.Id;
    }
    // حذف آبشاری نداریم؛ ابتدا وابستگی‌ها بررسی و حذف همراه Audit در تراکنش ثبت می‌شود.
    public async Task DeleteAsync(string kind, int id, CancellationToken ct)
    {
        if (kind != nameof(DailyCapacity)) Manager();
        object entity;
        int? branchId = null;
        bool hasDependencies;
        switch (kind)
        {
            case nameof(Branch):
                entity = await db.Branches.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                branchId = id;
                hasDependencies = await db.BranchFoods.AnyAsync(x => x.BranchId == id, ct) ||
                    await db.BranchGroups.AnyAsync(x => x.BranchId == id, ct) || await db.BranchHours.AnyAsync(x => x.BranchId == id, ct) ||
                    await db.StaffBranches.AnyAsync(x => x.BranchId == id, ct) || await db.BranchSpecials.AnyAsync(x => x.BranchId == id, ct) || await db.BranchBanners.AnyAsync(x => x.BranchId == id, ct);
                if (hasDependencies) throw new ArgumentException("شعبه دارای غذا، گروه، ساعت یا دسترسی کارمند است؛ ابتدا وابستگی‌ها را حذف کنید یا شعبه را غیرفعال کنید.");
                db.Branches.Remove((Branch)entity); break;
            case nameof(Food):
                entity = await db.Foods.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                if (await db.BranchFoods.AnyAsync(x => x.FoodId == id, ct) || await db.FoodImages.AnyAsync(x => x.FoodId == id, ct))
                    throw new ArgumentException("این غذا تصویر یا عرضه در شعبه دارد؛ ابتدا وابستگی‌ها را حذف کنید یا غذا را غیرفعال کنید.");
                db.Foods.Remove((Food)entity); break;
            case nameof(FoodImage):
                entity = await db.FoodImages.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                db.FoodImages.Remove((FoodImage)entity); break;
            case nameof(BranchGroup):
                var group = await db.BranchGroups.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                entity = group; branchId = group.BranchId;
                if (await db.GroupFoods.AnyAsync(x => x.BranchGroupId == id, ct)) throw new ArgumentException("ابتدا عضویت غذاهای این گروه را حذف کنید.");
                db.BranchGroups.Remove(group); break;
            case nameof(BranchFood):
                var food = await db.BranchFoods.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                entity = food; branchId = food.BranchId;
                hasDependencies = await db.GroupFoods.AnyAsync(x => x.BranchFoodId == id, ct) ||
                    await db.AllowedAddons.AnyAsync(x => x.ParentBranchFoodId == id || x.AddonBranchFoodId == id, ct) ||
                    await db.DailyCapacities.AnyAsync(x => x.BranchFoodId == id, ct) || await db.BranchSpecials.AnyAsync(x => x.BranchFoodId == id, ct) || await db.BranchBanners.AnyAsync(x => x.BranchFoodId == id, ct);
                if (hasDependencies) throw new ArgumentException("این غذای شعبه دارای گروه، افزودنی، ظرفیت، پیشنهاد ویژه یا بنر است؛ ابتدا وابستگی‌ها را حذف کنید یا آن را غیرفعال کنید.");
                db.BranchFoods.Remove(food); break;
            case nameof(GroupFood):
                var membership = await db.GroupFoods.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                entity = membership;
                branchId = await db.BranchGroups.Where(x => x.Id == membership.BranchGroupId).Select(x => x.BranchId).SingleAsync(ct);
                db.GroupFoods.Remove(membership); break;
            case nameof(AllowedAddon):
                var addon = await db.AllowedAddons.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                entity = addon;
                branchId = await db.BranchFoods.Where(x => x.Id == addon.ParentBranchFoodId).Select(x => x.BranchId).SingleAsync(ct);
                db.AllowedAddons.Remove(addon); break;
            case nameof(BranchHours):
                var hours = await db.BranchHours.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                entity = hours; branchId = hours.BranchId;
                db.BranchHours.Remove(hours); break;
            case nameof(DailyCapacity):
                var capacity = await db.DailyCapacities.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                entity = capacity;
                branchId = await db.BranchFoods.Where(x => x.Id == capacity.BranchFoodId).Select(x => x.BranchId).SingleAsync(ct);
                await user.EnsureBranchAsync(branchId.Value, ct);
                if (capacity.Reserved != 0 || capacity.Sold != 0) throw new ArgumentException("ظرفیت دارای رزرو یا فروش قابل حذف نیست؛ در صورت نیاز آن را غیرفعال کنید.");
                db.DailyCapacities.Remove(capacity); break;
            case nameof(BranchSpecial):
                var special = await db.BranchSpecials.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                await user.EnsureBranchAsync(special.BranchId, ct);
                entity = special; branchId = special.BranchId; db.BranchSpecials.Remove(special); break;
            case nameof(BranchBanner):
                var banner = await db.BranchBanners.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                await user.EnsureBranchAsync(banner.BranchId, ct);
                entity = banner; branchId = banner.BranchId; db.BranchBanners.Remove(banner); break;
            case nameof(StaffBranch):
                var staff = await db.StaffBranches.FindAsync([id], ct) ?? throw new ArgumentException("رکورد پیدا نشد.");
                entity = staff; branchId = staff.BranchId;
                db.StaffBranches.Remove(staff); break;
            default: throw new ArgumentException("نوع رکورد برای حذف مجاز نیست.");
        }
        await AuditAsync("Delete" + kind, entity, branchId, ct);
    }
}
