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
