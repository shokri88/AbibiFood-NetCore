# دیتابیس و API

## موجودیت‌ها

علاوه بر AspNet* متعلق به Identity: Branches، Foods، FoodImages، BranchGroups، BranchFoods، GroupFoods، AllowedAddons، DailyCapacities، BranchHours، StaffBranches، AuditLogs و ErrorLogs.

- BranchFoods: unique(BranchId,FoodId)، قیمت decimal(18,2)، DiscountPrice nullable و RowVersion.
- DailyCapacities: unique(BranchFoodId,Date)، RowVersion، Total/Reserved/Sold/WarningThreshold. Remaining ستون SQL نیست؛ property محاسباتی است.
- GroupFoods: unique(BranchGroupId,BranchFoodId). کنترل هم‌شعبه‌بودن در سرویس.
- AllowedAddons: unique(ParentBranchFoodId,AddonBranchFoodId). خودارجاعی و شعبه متفاوت در سرویس رد می‌شوند.
- StaffBranches: unique(UserId,BranchId). IsActive دسترسی شعبه را کنترل می‌کند.
- حذف‌های relational وابسته Restrict هستند؛ endpoint حذف فیزیکی نداریم.

طرح واقعی و migration در `src/AbibiFood.Infrastructure/Persistence/Migrations` است. هیچ جدول Order/Payment/Invoice/Reservation ساخته نشده است.

## Endpointها

| مسیر | دسترسی/شرح |
|---|---|
| GET /health/live | زنده‌بودن host؛ صحت اتصال SQL را اثبات نمی‌کند |
| GET /api/catalog/branches | عمومی؛ شعب فعال |
| GET /api/catalog/branches/{id}/menu?date=yyyy-MM-dd | عمومی؛ گروه، عضویت، اقلام، افزودنی، ساعات و باقی‌مانده؛ orderingEnabled=false |
| POST /api/auth/token | ایمیل/رمز کارکنان؛ محدودیت نرخ و lockout؛ بدون OTP |
| POST /api/manage/branches | Manager |
| POST /api/manage/foods و images | Manager |
| POST /api/manage/groups و branch-foods و group-foods | Manager |
| POST /api/manage/addons و hours | Manager |
| POST /api/manage/capacities | Manager یا Employee شعبه مجاز |
| GET /api/manage/branches/{id}/capacities?from=...&to=... | Manager یا Employee شعبه مجاز |

قرارداد ورودی دقیق: `Application/Models/CatalogInputs.cs`. برای ویرایش BranchFood/Capacity، Version با Base64 از RowVersion لازم است؛ در فرم Razor hidden field است. ظرفیت با BranchFoodId+Date upsert می‌شود. شناسه گروه و غذا نباید بین شعبه‌ها مخلوط شوند. POSTها فعلاً 200 با id برمی‌گردانند؛ API versioning هنوز اضافه نشده است.

Razor: /Account/Login، /Admin/Index، Branches، Foods، Images، Groups، Menu، Memberships، Addons، Capacities، Hours، Employees و Logs. پنل AntiForgery می‌خواهد؛ bearer API از AntiForgery معاف است و cookie پنل برای ManagementApi معتبر نیست.
