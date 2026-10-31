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

در Audit فعلی نوع عملیات، کاربر، شعبه و ID موجود است؛ ثبت مقدار قبل/بعد هنوز کامل نشده. ظاهر پنل مدیریتی مستقل است و هویت بصری آن از قالب مالک گرفته شده است. طبق درخواست ۲۰۲۶-۱۰-۳۱ فونت بک‌اند وزیر است؛ Hasti برای فرانت‌اند مشتری باقی می‌ماند. CSS مرکزی بک‌اند در wwwroot/assets/css/vazir.css است.

## تست‌های پذیرش و اعتبار JWT

`Program.cs` در `JwtBearerEvents.OnTokenValidated`، موجودبودن/فعال‌بودن حساب و نقش فعلی Identity را قبل از مجوزدهی ManagementApi بررسی می‌کند. کنترل middleware پس از UseAuthentication برای Cookie باقی می‌ماند؛ Bearer ممکن است هنگام policy احراز هویت شود. این تفاوت علت اصلاح این بخش است.

`PhaseOneFactory` میزبان واقعی ASP.NET با جایگزینی SQL توسط InMemory و رازهای تصادفی موقت تست می‌سازد؛ `PhaseOneHttpTests` ورود API/Cookie و مرز مجوزها را بررسی می‌کند. `SqlServerAcceptanceTests` مسیر مستقل و اختیاری SQL را دارد؛ نبود SQL پیش‌فرض Skipped و در حالت الزامی شکست است. این کلاس‌ها کد تولید نیستند.

## اجزای بهبود پنل

- `Api/Services/ImageUploadStore.cs`: دریافت فایل محدود، کنترل MIME و امضای PNG/JPEG/WebP، تولید نام تصادفی، نوشتن در `wwwroot/uploads/catalog` و حذف فایل جدید در شکست ذخیره رکورد. وابسته به IWebHostEnvironment و Messages؛ در Program به‌صورت Scoped ثبت شده است.
- `AdminController.Images/SaveImage`: مسیر فرزند غذا؛ کنترل وجود غذا و مالکیت تصویر، حفظ مسیر قبلی هنگام ویرایش بدون فایل. SaveImageAsync سرویس کاتالوگ همچنان مسئول ثبت رکورد و Audit است.
- `AdminController.SaveGroup`: بارگذاری اختیاری تصویر گروه؛ مسیر فایل جدید به SaveGroupAsync داده می‌شود؛ قواعد شعبه حفظ شده‌اند.
- `Views/Admin/Foods.cshtml` و `Images.cshtml`: جدول تصاویر غذا و فرم multipart با Browse/پیش‌نمایش، ترتیب و فعال‌بودن.
- `Views/Shared/_Rows.cshtml`: کارت مستقل جدول؛ Layout منوی گروه‌بندی‌شده و استایل‌های مشترک را بارگذاری می‌کند.
- `wwwroot/assets/js/admin-ui.js`: فیلتر، مرتب‌سازی با Intl.Collator فارسی، حالت خالی و تعداد نتایج، منوی فعال و پیش‌نمایش تصویر. وابستگی خارجی جدید ندارد.
- `wwwroot/assets/css/admin.css`: رنگ‌ها و اجزای ظاهری پنل؛ تنظیم فونت همچنان در vazir.css مستقل است.
