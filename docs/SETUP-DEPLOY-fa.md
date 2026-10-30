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

CI فقط restore/build/test انجام می‌دهد و SQL/secret واقعی ندارد. نتیجه اجرای GitHub Actions هنوز مشاهده نشده است. هیچ استقرار production خودکار انجام نشده.
