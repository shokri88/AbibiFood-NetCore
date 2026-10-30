# آبی‌بی — بک‌اند و مدیریت Razor

شروع فاز یک سامانه چندشعبه‌ای آبی‌بی. مخزن همراه: [shokri88/AbibiFood-PWA](https://github.com/shokri88/AbibiFood-PWA). مرجع قالب شخصی‌سازی‌شده: [AbibiFood-UI](https://github.com/shokri88/AbibiFood-UI) در `2af3cb18ef8c3de657fe2284db0209379fc2ee09`.

**شروع انتقال به انسان یا AI جدید:** [AI-HANDOVER-fa.md](docs/AI-HANDOVER-fa.md) و [AGENTS.md](AGENTS.md).

.NET 10.0.403، EF Core/Identity/JWT 10.0.14، SQL Server و پنل MVC/Razor. مدیریت شعب، مخزن، گروه، قیمت، افزودنی و ظرفیت روزانه در کد موجود است. برای اجرا ابتدا secretها، SQL و Migration را طبق راهنمای نصب تنظیم کنید؛ رمز پیش‌فرض وجود ندارد.

```sh
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-restore
```

Migration تولید شده است؛ SQL Server واقعی و جریان عملیاتی پنل هنوز در این محیط تست نشده‌اند. فروش و مشتری در این فاز فعال نیستند.

## اسناد

- [AI-HANDOVER-fa.md](docs/AI-HANDOVER-fa.md)
- [PROJECT-HISTORY-fa.md](docs/PROJECT-HISTORY-fa.md)
- [PROJECT-STATUS-fa.md](docs/PROJECT-STATUS-fa.md)
- [ARCHITECTURE-fa.md](docs/ARCHITECTURE-fa.md)
- [MODULES-fa.md](docs/MODULES-fa.md)
- [BUSINESS-RULES-fa.md](docs/BUSINESS-RULES-fa.md)
- [DATABASE-API-fa.md](docs/DATABASE-API-fa.md)
- [CONFIGURATION-fa.md](docs/CONFIGURATION-fa.md)
- [SETUP-DEPLOY-fa.md](docs/SETUP-DEPLOY-fa.md)
- [DECISIONS-fa.md](docs/DECISIONS-fa.md)
- [ROADMAP-fa.md](docs/ROADMAP-fa.md)
- [CHANGELOG-fa.md](docs/CHANGELOG-fa.md)
- [KNOWN-ISSUES-fa.md](docs/KNOWN-ISSUES-fa.md)

## وضعیت اعتبارسنجی

Build بدون خطا/هشدار و ۱۳ تست دامنه/سرویس InMemory موفق. صحت SQL و تراکنش واقعی نیازمند محیط عملیاتی است.
