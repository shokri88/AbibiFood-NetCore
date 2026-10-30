# تغییرات این شروع

- ایجاد solution چهارپروژه‌ای net10.0 با manifest نسخه‌ها و lockfileها.
- مدل مخزن مشترک، منوی شعبه، افزودنی مشترک و ظرفیت روزانه؛ Migration اولیه Identity/SQL.
- CatalogService مشترک، کنترل شعبه، rowversion و ثبت Audit تراکنشی.
- API عمومی و مدیریتی؛ ورود کارکنان با Identity، Cookie/JWT، lockout، rate limit و AntiForgery پنل.
- صفحات مستقل Razor و assets منظم؛ فونت Hasti از قالب مالک.
- ErrorLog مستقل SQL با correlation ID و fallback logger.
- ۱۳ تست دامنه/سرویس موفق و Build بدون هشدار/خطا؛ تست SQL واقعی انجام نشده.
- مستندات فارسی انتقال و دستور پایان جلسه در AGENTS.

این شروع با پروژه کیوسک ارتباط ندارد. SHAهای انتشار از Git واقعی خوانده شوند؛ در این فایل حدس زده نشده‌اند.


## ۲۰۲۶-۱۰-۳۰ — ادامه پذیرش فاز اول

- مبنا: `c36d134e`؛ فرانت‌اند و مرجع طراحی بدون تغییر.
- انتقال کنترل فعال‌بودن JWT به OnTokenValidated و خواندن نقش فعلی از Identity؛ اصلاح ترتیب احراز هویت Bearer بدون تغییر قرارداد API.
- چهار تست HTTP برای مجوز شعبه/نقش، غیرفعال‌شدن حساب، حذف نقش و Cookie/AntiForgery؛ پکیج Microsoft.AspNetCore.Mvc.Testing نسخه 10.0.14 با lockfile.
- آماده‌سازی تست پذیرش واقعی SQL و workflow دستی با دیتابیس/رمز موقت؛ این تست هنوز اجرا نشده است.
- نتیجه محلی: Build بدون هشدار/خطا، ۱۷ موفق و یک Skipped. CI اولیه به‌دلیل Billing پیش از شروع Job متوقف بوده است.
