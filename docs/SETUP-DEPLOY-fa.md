# نصب و اجرای بک‌اند

## پیش‌نیاز

SDK .NET 10.0.403، SQL Server و محیط میزبانی سازگار با net10.0. نسخه‌ها در global.json، csprojها و packages.lock.json هستند. برای IDE از نسخه سازگار با SDK انتخاب‌شده یا VS Code/CLI استفاده کنید؛ پروژه net10.0 را بدون بررسی سازگاری در محیط قدیمی بازهدف‌گذاری نکنید.

```sh
dotnet restore --locked-mode
dotnet tool restore
dotnet build --no-restore
dotnet test --no-restore
```

در این محیط محدود، Build با `-m:1 -p:BuildInParallel=false` اجرا شد؛ در حالت موازی MSBuild یک بار خروجی فاقد پیام خطای مفید داد. این تنظیم workaround محیط تست بود.

## تنظیم امن توسعه

```sh
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<رشته اتصال محیط خودتان>" --project src/AbibiFood.Api
dotnet user-secrets set "Jwt:Key" "<کلید تصادفی حداقل ۳۲ بایت>" --project src/AbibiFood.Api
dotnet dev-certs https --trust
dotnet ef database update --project src/AbibiFood.Infrastructure --startup-project src/AbibiFood.Api
dotnet user-secrets set "Bootstrap:Email" "<ایمیل مدیر>" --project src/AbibiFood.Api
dotnet user-secrets set "Bootstrap:Password" "<رمز قوی مدیر>" --project src/AbibiFood.Api
dotnet run --project src/AbibiFood.Api -- --bootstrap-manager
dotnet user-secrets remove "Bootstrap:Password" --project src/AbibiFood.Api
dotnet run --project src/AbibiFood.Api
```

Migration اولیه تولید شده ولی روی SQL واقعی در این جلسه اعمال نشده. DesignTimeFactory برای ساخت migration از connection محیط استفاده می‌کند و fallback فاقد رمز دارد؛ برای database update مقدار واقعی را تنظیم کنید. مدیر موجود به‌صورت ضمنی ارتقا داده نمی‌شود. ابتدا roleها ایجاد و بعد کارمند از پنل ساخته شود.

نشانی پنل https://localhost:7043 و Angular http://localhost:4200 . ورود مدیر در /Account/Login. تعریف اولیه: شعبه → مخزن و تصاویر → غذای شعبه → گروه/عضویت → افزودنی → ساعات → ظرفیت → کارمند و تخصیص شعبه.

## Production

`dotnet publish src/AbibiFood.Api -c Release -o publish`؛ نصب runtime/hosting bundle سازگار، secretهای محیطی، HTTPS معتبر، CORS دقیق، backup دیتابیس، اجرای migration در فرایند انتشار کنترل‌شده و محدودکردن دسترسی پنل. migration خودکار در startup فعال نیست. پشت reverse proxy فقط forwarded headers از proxy مورد اعتماد تنظیم شود؛ این پیکربندی هنوز برای هاست واقعی مالک انجام نشده است.

CI معمولی فقط restore/build/test انجام می‌دهد و SQL واقعی ندارد. اجرای اولیه بک‌اند پیش از شروع Job به‌دلیل Billing حساب GitHub مسدود شده است (run 41122471162). این وضعیت با نتیجه موفق محلی متفاوت است. هیچ استقرار production خودکار انجام نشده.


## پذیرش HTTP و SQL

`dotnet test --no-build --no-restore` در محیط فاقد SQL، ۱۷ تست موفق و یک Skipped داشت (۲۰۲۶-۱۰-۳۰). تست‌های HTTP، Identity و endpointهای واقعی را با دیتابیس InMemory میزبان تست اجرا می‌کنند؛ این روش تراکنش یا constraint SQL را اثبات نمی‌کند.

برای SQL از سرور آزمایشی جدا و حساب مجاز به ایجاد/حذف دیتابیس استفاده کنید. مقدار `ABIBI_TEST_SQL` را با secret store یا محیط امن سیستم تنظیم کنید؛ آن را در Git یا گفتگو قرار ندهید. سپس:

```sh
export ABIBI_REQUIRE_SQL=true
dotnet test --no-build --no-restore --filter FullyQualifiedName~SqlServerAcceptanceTests
```

با `ABIBI_REQUIRE_SQL=true` فقدان اتصال، شکست تست است؛ بدون آن و بدون اتصال، تست صریحاً Skipped می‌شود. تست نام دیتابیس را با `AbibiAcceptance_` و GUID جدید جایگزین می‌کند، migration را اجرا و نسخه SQL را در خروجی تست ثبت می‌کند. فقط دیتابیس تصادفی همین اجرا پس از migration موفق پاک می‌شود؛ اگر migration نیمه‌کاره شکست خورد، دیتابیس موقت ممکن است برای بررسی باقی بماند. هرگز SQL عملیاتی را برای تست معرفی نکنید.

گزینه جایگزین: پس از رفع Billing، workflow دستی `sql-integration-acceptance` در Actions اجرا شود. این workflow کانتینر SQL Server 2022 با رمز تصادفی همان اجرا می‌سازد، اتصال را فقط به loopback می‌بندد، تست SQL را اجباری می‌کند و TRX را artifact می‌گذارد؛ در پایان کانتینر پاک می‌شود. tag تصویر `2022-latest` است، نسخه دقیق CU ثابت نیست و تست ProductVersion را ثبت می‌کند. workflow این جلسه اجرا نشده است؛ هیچ credential عملیاتی نیاز ندارد. runner هر دو workflow بک‌اند `ubuntu-24.04` است. این تنظیم runner مشکل Billing را رفع نمی‌کند.


## بازیابی خطای 1753 در Migration اولیه (۲۰۲۶-۱۰-۳۱)

اجرای واقعی مالک روی SQL Server نسخه `16.0.1210.3` هنگام ساخت `StaffBranches` شکست خورد: `UserId` با `nvarchar(500)` به `AspNetUsers.Id` با `nvarchar(450)` ارجاع می‌داد. در مدل، Migration اولیه و metadata/snapshot، فقط طول همین FK به ۴۵۰ اصلاح شد. Migration جدیدِ بعدی این شکست را حل نمی‌کند، چون Migration اولیه هنوز قابل اجرا نبود. هیچ دیتابیس یا داده‌ای توسط این اصلاح حذف نمی‌شود.

قبل از تلاش مجدد روی دیتابیس تازه، در SSMS بررسی کنید:

```sql
USE [AbibiFood];
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory;
SELECT SCHEMA_NAME(schema_id) AS SchemaName, name
FROM sys.tables WHERE is_ms_shipped = 0 ORDER BY name;
```

اگر History خالی است و فقط جدول History مانده، پس از `git status` و `git pull --ff-only origin main`، اتصال محیط را مجدداً تنظیم و همان `dotnet ef database update --project src/AbibiFood.Infrastructure --startup-project src/AbibiFood.Api` اجرا شود. دیتابیس، فایل‌های mdf/ldf و جدول History را حذف نکنید. اگر History دارای رکورد است یا جدول‌های دیگری باقی مانده‌اند، خروجی بررسی شود؛ Migration را حذف یا به‌زور اجرا نکنید.

`IdentityForeignKeySchemaTests` نوع SQL ستون‌های FK و کلید مقصد را هم در مدل SQL provider و هم عملیات Migration بررسی می‌کند و اتصال به SQL نمی‌خواهد. موفقیت این تست جای اجرای واقعی Migration روی SQL را نمی‌گیرد. retry موفق مالک پس از اصلاح هنوز گزارش نشده است.

## استقرار بارگذاری تصاویر پنل

پس از Pull این تغییر، Build و راه‌اندازی مجدد کافی است؛ Migration تازه ندارد. پوشه `wwwroot/uploads/catalog` باید قابل نوشتن توسط حساب برنامه باشد و در انتشار بعدی حفظ شود. روی میزبان کانتینری آن را به volume پایدار متصل کنید. Backup فایل‌ها را همراه Backup SQL نگه دارید؛ SQL به‌تنهایی خود تصویر را ندارد. سقف درخواست reverse proxy/IIS باید حداقل سقف `ImageUploadStore.MaxRequestBytes` باشد؛ سقف پایین‌تر می‌تواند پیش از کنترل برنامه پاسخ 413 بدهد.

پذیرش دستی روی محیط SQL مالک: یک غذا بسازید، PNG معتبر بارگذاری کنید، نمایش و مسیر عمومی آن را بررسی کنید، ترتیب/وضعیت را بدون فایل تغییر دهید، فایل را جایگزین کنید و ثبت FoodImages/AuditLogs را ببینید. همین مسیر را برای تصویر گروه بررسی کنید. در مرورگر دسکتاپ و موبایل فرم‌ها، فیلتر و جهت‌های سورت جدول را بررسی کنید. تست‌های خودکار این مرحله جای این آزمون SQL/بصری را نمی‌گیرند.

## پذیرش اصلاح بازخورد پنل

Pull و راه‌اندازی مجدد کافی است؛ Migration ندارد. در مرورگر، کنار هر غذا «تصاویر» را باز کنید و افزودن/ویرایش/حذف تصویر را در همان صفحه مستقل بررسی کنید. در گروه غذایی، بدون انتخاب تصویر جدید ذخیره کنید و حفظ تصویر قبلی را ببینید. ثبت ظرفیت با تاریخ شمسی و اعداد فارسی، زمان گزارش تهران، سورت تاریخ و فوتر فرم را بررسی کنید. برای حذف، رکورد مستقل آزمایشی بسازید و تأیید/انصراف را امتحان کنید؛ حذف رکورد دارای وابستگی باید متوقف شود. سناریوی کارمند شعبه دیگر باید دسترسی نگیرد. این آزمون‌ها را روی داده آزمایشی انجام دهید.

## Migration ظرفیت روزانه تکرارشونده — ۲۰۲۶-۱۱-۰۳

پیش از اجرای نسخه جدید، پس از backup و با connection امن محیط خودتان:

```sh
dotnet ef database update --project src/AbibiFood.Infrastructure --startup-project src/AbibiFood.Api
```

Migration جدید `20261103115314_RecurringDailyCapacity` سه ستون تنظیم ظرفیت به BranchFoods اضافه می‌کند؛ ظرفیت‌های تاریخ‌دار قبلی و مصرفشان باقی می‌مانند. مقدار ظرفیت پیش‌فرض غذاهای قبلی null است؛ یک‌بار از صفحه ظرفیت روزانه به‌صورت گروهی تنظیم شود. اجرای این Migration روی دیتابیس آزمایشی مستقل SQL Server و تست rollback ثبت گروهی موفق بوده؛ روی دیتابیس مالک اجرا نشده است. Migration خودکار در startup فعال نشده است.
