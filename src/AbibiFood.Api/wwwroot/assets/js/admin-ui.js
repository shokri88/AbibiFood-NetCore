// جست‌وجو و مرتب‌سازی فقط روی ردیف‌های همین صفحه انجام می‌شود؛ مجوزها در سرور کنترل می‌شوند.
(() => {
    const path = location.pathname.toLowerCase().replace(/\/$/, '') || '/admin/index';
    const current = path === '/admin/images' ? '/admin/foods' : path;
    document.querySelectorAll('.admin-nav a').forEach(link => {
        if (link.pathname.toLowerCase() === (current === '/admin' ? '/admin/index' : current)) link.setAttribute('aria-current', 'page');
    });
    const normalize = value => value.replace(/[۰-۹]/g, digit => '۰۱۲۳۴۵۶۷۸۹'.indexOf(digit)).replace(/[٠-٩]/g, digit => '٠١٢٣٤٥٦٧٨٩'.indexOf(digit)).replace(/ي/g, 'ی').replace(/ك/g, 'ک').trim().toLocaleLowerCase('fa');
    const collator = new Intl.Collator('fa', {numeric: true, sensitivity: 'base'});
    document.querySelectorAll('table.table').forEach((table, index) => {
        const body = table.tBodies[0];
        if (!body || !table.tHead) return;
        const rows = [...body.rows];
        const tools = document.createElement('div'); tools.className = 'grid-tools';
        const label = document.createElement('label'); label.className = 'form-label'; label.htmlFor = `grid-filter-${index}`; label.textContent = 'فیلتر جدول';
        const input = document.createElement('input'); input.id = label.htmlFor; input.type = 'search'; input.className = 'form-control mt-2'; input.placeholder = 'جست‌وجو در تمام ستون‌ها…'; label.append(input);
        const status = document.createElement('p'); status.className = 'grid-status'; status.setAttribute('role', 'status');
        tools.append(label, status); const wrapper = table.closest('.table-responsive') || table; wrapper.before(tools);
        const empty = document.createElement('div'); empty.className = 'grid-empty'; empty.textContent = 'رکوردی برای نمایش وجود ندارد.'; wrapper.after(empty);
        const filter = () => {
            const term = normalize(input.value); let visible = 0;
            rows.forEach(row => { row.hidden = !normalize(row.textContent).includes(term); if (!row.hidden) visible++; });
            status.textContent = `${visible.toLocaleString('fa')} از ${rows.length.toLocaleString('fa')} رکورد`;
            empty.hidden = visible !== 0;
        };
        input.addEventListener('input', filter); filter();
        [...table.tHead.rows[0].cells].forEach((header, column) => {
            if (header.hasAttribute('data-no-sort')) return;
            header.setAttribute('aria-sort', 'none');
            const button = document.createElement('button'); button.type = 'button'; button.className = 'table-sort'; button.textContent = header.textContent; header.replaceChildren(button);
            button.addEventListener('click', () => {
                const ascending = header.getAttribute('aria-sort') !== 'ascending';
                table.querySelectorAll('th[aria-sort]').forEach(th => th.setAttribute('aria-sort', 'none'));
                header.setAttribute('aria-sort', ascending ? 'ascending' : 'descending');
                rows.sort((a, b) => collator.compare(normalize(a.cells[column].dataset.sort || a.cells[column].textContent), normalize(b.cells[column].dataset.sort || b.cells[column].textContent)) * (ascending ? 1 : -1));
                rows.forEach(row => body.append(row));
            });
        });
    });
    document.querySelectorAll('input[type=file][data-image-upload]').forEach(input => {
        let url;
        const preview = document.createElement('img'); preview.className = 'image-preview'; preview.alt = 'پیش‌نمایش تصویر انتخاب‌شده'; preview.hidden = true; input.after(preview);
        input.addEventListener('change', () => {
            if (url) URL.revokeObjectURL(url);
            const file = input.files[0]; preview.hidden = true;
            if (file && ['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) { url = URL.createObjectURL(file); preview.src = url; preview.hidden = false; }
        });
    });
})();
