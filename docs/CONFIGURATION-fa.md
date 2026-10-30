# تنظیمات بک‌اند

فایل `src/AbibiFood.Api/appsettings.json` فقط گزینه‌های غیرحساس و connection خالی دارد. secretها از User Secrets در توسعه یا متغیر محیطی در production می‌آیند:

- ConnectionStrings__DefaultConnection
- Jwt__Key (حداقل ۳۲ بایت؛ خارج Git)
- Jwt__Issuer / Jwt__Audience / Jwt__Minutes (پیش‌فرض ۳۰ دقیقه)
- AllowedOrigins__0 و موارد بعدی برای CORS
- Bootstrap__Email و Bootstrap__Password فقط هنگام ایجاد مدیر اولیه

appsettings.Development.json سطح log را دارد. Messages.fa.json منبع فعلی چند پیام عمومی با reloadOnChange است؛ تمام متن‌های DTO/Controller/View هنوز متمرکز نشده‌اند. تنظیمات مدیریتی فعلی همان داده‌های شعبه، منو، قیمت، ساعت، ظرفیت و دسترسی‌اند؛ جدول تنظیمات عمومی دلخواه هنوز اضافه نشده است.

Cookie فقط Secure/HttpOnly است؛ برای توسعه نیز HTTPS لازم است. JWT در فایل فرانت قرار نمی‌گیرد. ساعت ذخیره لاگ UTC و مبنای روز ظرفیت Asia/Tehran است؛ DateOnly روز را از زمان جدا می‌کند.

لاگ SQL هنگام سالم‌بودن دیتابیس ثبت می‌شود؛ اگر خود SQL قطع باشد، middleware فقط fallback لاگر میزبان دارد. تضمین بازیابی خطاهای دوره قطع SQL نیازمند طراحی صف/فایل پایدار و سیاست نگهداری است و هنوز انجام نشده است.

اعتبارسنجی سمت مرورگر فرم‌های Razor با JustValidate محلی انجام می‌شود؛ مرجع نهایی اعتبارسنجی همچنان DataAnnotations و CatalogService سمت سرور است.
