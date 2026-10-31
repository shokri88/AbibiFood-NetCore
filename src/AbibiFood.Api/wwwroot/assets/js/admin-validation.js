// قواعد اولیه از metadata استاندارد Razor خوانده می‌شوند؛ اعتبارسنجی سرور مستقل باقی می‌ماند.
document.querySelectorAll('form[method="post"]').forEach(form => {
    const fields = [...form.querySelectorAll('input:not([type=hidden]):not([type=checkbox]), textarea, select')];
    if (!fields.length || !window.JustValidate) return;
    const validation = new JustValidate(form, {
        errorFieldCssClass: 'is-invalid', errorLabelCssClass: 'text-danger small',
        errorFieldStyle: {}, errorLabelStyle: {}, focusInvalidField: true
    });
    fields.forEach(field => {
        const rules = [];
        const latin = value => String(value).replace(/[۰-۹]/g, digit => '۰۱۲۳۴۵۶۷۸۹'.indexOf(digit)).replace(/[٠-٩]/g, digit => '٠١٢٣٤٥٦٧٨٩'.indexOf(digit)).replace(/٫/g, '.').replace(/٬/g, '');
        if (field.type === 'number') rules.push({ validator: value => !value || /^-?\d+(\.\d+)?$/.test(latin(value)), errorMessage: 'عدد معتبر وارد کنید.' });
        if (field.type === 'time') rules.push({ validator: value => !value || /^([01]?\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/.test(latin(value)), errorMessage: 'ساعت معتبر مانند ۱۸:۳۰ وارد کنید.' });
        if (field.hasAttribute('data-persian-date')) rules.push({ validator: value => /^\d{4}\/\d{1,2}\/\d{1,2}$/.test(latin(value)), errorMessage: 'تاریخ شمسی مانند ۱۴۰۵/۰۸/۰۹ وارد کنید.' });
        if (field.required || field.dataset.valRequired) rules.push({rule: 'required', errorMessage: 'این فیلد الزامی است.'});
        if (field.type === 'email') rules.push({rule: 'email', errorMessage: 'ایمیل معتبر وارد کنید.'});
        const max = Number(field.getAttribute('maxlength') || field.dataset.valLengthMax || field.dataset.valMaxlengthMax);
        if (max > 0) rules.push({rule: 'maxLength', value: max, errorMessage: `حداکثر ${max} نویسه مجاز است.`});
        if (rules.length) validation.addField(field, rules);
    });
    validation.onSuccess(() => HTMLFormElement.prototype.submit.call(form));
});
