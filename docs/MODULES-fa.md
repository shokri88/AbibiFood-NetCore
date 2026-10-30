# نقشه ماژول‌ها و متدهای مهم

| محل | وظیفه |
|---|---|
| Domain/Entities/Branch,Food,FoodImage | شعبه و مخزن مشترک چندتصویری |
| BranchFood,BranchGroup,GroupFood | قیمت شعبه و عضویت گروه بدون تکثیر موجودی |
| AllowedAddon | ارتباط دو BranchFood هم‌شعبه |
| DailyCapacity | روز، کل، رزرو، فروش، آستانه و Remaining محاسباتی |
| BranchHours/HoursKind | ساعت هفتگی Ordering و InPerson |
| StaffBranch | نگاشت دسترسی فعال کارمند به شعبه |
| AuditLog/ErrorLog | عملیات کاربر و خطای فنی جدا |
| Domain/Rules/CatalogRules | Price، Capacity و Addon؛ تست‌پذیر بدون دیتابیس |
| Application/Services/CatalogService | SaveBranch/Food/Image/Group/BranchFood/GroupFood/Addon/Capacity/HoursAsync |
| CatalogService.SaveCapacityAsync | تاریخ تهران، جلوگیری از ظرفیت گذشته، scope شعبه و نسخه رکورد |
| Infrastructure/Persistence/AppDbContext | DbSetها، FK/unique/precision/rowversion و SaveWithAuditAsync |
| Infrastructure/Identity/AppUser | IdentityUser با DisplayName و IsActive |
| Api/Services/CurrentUser | UserId/IsManager و EnsureBranchAsync |
| Api/Controllers/AccountController | ورود Cookie، Logout و JWT کارکنان با lockout |
| PublicCatalogController | خواندن منوی عمومی فعال و ظرفیت روز |
| ManagementController | write DTOهای مدیریت و GET ظرفیت محدوده تاریخ |
| AdminController | صفحات مستقل Razor و کارکنان/داشبورد/لاگ |
| Middleware/ErrorMiddleware | ProblemDetails، شناسه پیگیری و SQL ErrorLog |
| Api/Persistence/DesignTimeFactory | تولید migration بدون secret یا اتصال واقعی |
| Api/Views/Admin و Views/Shared | صفحه‌های مستقل، Layout، BranchFilter و Rows مشترک |
| tests/AbibiFood.Tests | تست قواعد و سرویس با InMemory |

در Audit فعلی نوع عملیات، کاربر، شعبه و ID موجود است؛ ثبت مقدار قبل/بعد هنوز کامل نشده. ظاهر پنل جدید مدیریتی است و هویت/فونت از قالب مالک گرفته شده، نه این‌که پنل مشتری به پنل مدیر تغییر داده شده باشد.
