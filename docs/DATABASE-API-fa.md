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

## مسیرهای پنل تصاویر

- `GET /Admin/Images?foodId={id}`: فرم تصویر جدید در محدوده یک غذا؛ بدون شناسه به مخزن غذا می‌رود.
- `GET /Admin/Images?foodId={id}&id={imageId}`: ویرایش تصویر همان غذا؛ رابطه نامعتبر 404.
- `POST /Admin/SaveImage`: multipart با Id، FoodId، SortOrder، IsActive و imageFile؛ برای رکورد جدید فایل لازم است، در ویرایش اختیاری. Path ارسالی کاربر نادیده گرفته می‌شود.
- `POST /Admin/SaveGroup`: فرم قبلی به‌اضافه imageFile اختیاری؛ فایل جدید بر ImagePath اولویت دارد.

همه این عملیات Manager و Cookie/AntiForgery لازم دارند. endpointهای API کاتالوگ و schema SQL تغییر نکردند. آپلود از پنل است؛ endpoint بارگذاری برای مشتری ایجاد نشده است.

## تغییر مسیرهای پنل در اصلاح بازخورد

`POST /Admin/Delete` با kind، id و returnUrl محلی و AntiForgery اضافه شد. kindهای مجاز: Branch، Food، FoodImage، BranchGroup، BranchFood، GroupFood، AllowedAddon، BranchHours، DailyCapacity، StaffBranch و Employee. مجوزها و وابستگی‌ها در BUSINESS-RULES آمده‌اند. GET هیچ حذف انجام نمی‌دهد؛ endpoint JSON حذف عمومی ایجاد نشده است.

`POST /Admin/SaveCapacity` در فرم پنل اکنون persianDate با قالب شمسی `۱۴۰۵/۰۸/۰۹` می‌پذیرد؛ تاریخ query و API JSON همچنان ISO است. مقادیر ذخیره‌شده DateOnly/UTC تغییر نمی‌کنند. `SaveGroup` دیگر ImagePath فرم را مبنا نمی‌گیرد؛ مسیر موجود از رکورد سرور خوانده و تنها فایل جدید آن را جایگزین می‌کند. پس از SaveImage کاربر به صفحه تصاویر همان غذا برمی‌گردد.
