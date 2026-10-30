# معماری بک‌اند

- Domain: موجودیت‌ها و CatalogRules؛ فاقد EF/HTTP.
- Application: DTOهای ورودی، IAppDbContext، IUserScope و CatalogService؛ قواعد مشترک API و پنل.
- Infrastructure: AppUser/Identity، AppDbContext، mapping SQL و Migrations.
- Api: برنامه میزبان API و MVC/Razor، CurrentUser، ErrorMiddleware، پیام‌ها و تنظیمات.

مسیر مدیریت: Razor Controller یا ManagementController → CatalogService → IAppDbContext → AppDbContext → SQL Server. منطق قیمت/افزودنی/ظرفیت در Controller کپی نمی‌شود.

IUserScope در CurrentUser از Claims و StaffBranches دیتابیس استفاده می‌کند. Manager دسترسی کامل دارد؛ Employee فقط ظرفیت شعبه‌های مجاز. endpoint مدیریت فقط Bearer می‌پذیرد؛ پنل Cookie همراه AntiForgery دارد. JWT مشتری و OTP هنوز اجرا نشده‌اند.

SaveWithAuditAsync تغییر اصلی را ذخیره می‌کند تا ID واقعی دریافت شود، سپس Audit را در همان transaction SQL می‌نویسد و commit می‌کند. ErrorMiddleware برای خطای ۵۰۰ DbContext مستقل می‌سازد تا entityهای ناموفق درخواست مجدداً ذخیره نشوند.

معماری یکپارچه و ساده است؛ Microservice، CQRS و Generic Repository اضافه نشده‌اند. پنل Razor و API فعلاً یک host و یک origin دارند.
