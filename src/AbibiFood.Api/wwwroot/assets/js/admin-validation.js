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
        if (field.required || field.dataset.valRequired) rules.push({rule: 'required', errorMessage: 'این فیلد الزامی است.'});
        if (field.type === 'email') rules.push({rule: 'email', errorMessage: 'ایمیل معتبر وارد کنید.'});
        const max = Number(field.getAttribute('maxlength') || field.dataset.valLengthMax || field.dataset.valMaxlengthMax);
        if (max > 0) rules.push({rule: 'maxLength', value: max, errorMessage: `حداکثر ${max} نویسه مجاز است.`});
        if (rules.length) validation.addField(field, rules);
    });
    validation.onSuccess(() => HTMLFormElement.prototype.submit.call(form));
});
