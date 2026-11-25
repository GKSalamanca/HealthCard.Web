'use strict';
document.addEventListener('DOMContentLoaded', () => {
    const category = document.querySelector('[data-parameters-url]');
    if (category) {
        let controller;
        category.addEventListener('change', async () => {
            controller?.abort(); controller = new AbortController();
            const target = document.getElementById('parameters-container');
            const save = document.getElementById('save-record');
            target.replaceChildren();
            if (!category.value) { save.disabled = false; return; }
            save.disabled = true; target.textContent = 'Загружаем показатели…';
            try {
                const response = await fetch(`${category.dataset.parametersUrl}?categoryId=${encodeURIComponent(category.value)}`, { signal: controller.signal });
                if (!response.ok) throw new Error('Unavailable category');
                target.innerHTML = await response.text();
                if (window.jQuery?.validator?.unobtrusive) {
                    const form = window.jQuery('#record-form'); form.removeData('validator').removeData('unobtrusiveValidation');
                    window.jQuery.validator.unobtrusive.parse(form);
                }
                save.disabled = false;
            } catch (error) { if (error.name !== 'AbortError') target.textContent = 'Не удалось загрузить показатели. Выберите категорию повторно.'; }
        });
    }
    const categoryForm = document.getElementById('category-form');
    if (categoryForm) {
        const rows = document.getElementById('parameter-rows');
        const reindex = () => rows.querySelectorAll('[data-parameter-row]').forEach((row, index) => {
            row.querySelectorAll('[data-field]').forEach(input => { input.name = `Parameters[${index}].${input.dataset.field}`; input.id = `Parameters_${index}__${input.dataset.field}`; });
        });
        document.getElementById('add-parameter').addEventListener('click', () => {
            if (rows.children.length >= 100) return;
            rows.appendChild(document.getElementById('parameter-template').content.cloneNode(true)); reindex();
            rows.lastElementChild.querySelector('[data-field="Name"]').focus();
        });
        rows.addEventListener('click', event => { if (event.target.closest('[data-remove-row]')) { event.target.closest('tr').remove(); reindex(); } });
        document.getElementById('category-type')?.addEventListener('change', event => { document.getElementById('category-parameters').hidden = event.target.value !== 'true'; });
        categoryForm.addEventListener('submit', reindex); reindex();
    }
    document.getElementById('analytics-category')?.addEventListener('change', () => {
        const parameter = document.getElementById('analytics-parameter'); parameter.disabled = true;
        document.getElementById('analytics-filter').requestSubmit();
    });
    const canvas = document.getElementById('trend-chart');
    if (!canvas) return;
    const series = JSON.parse(canvas.dataset.series);
    const ctx = canvas.getContext('2d');
    let points = [];
    function draw() {
        const width = canvas.parentElement.clientWidth, height = 280, ratio = window.devicePixelRatio || 1;
        canvas.width = width * ratio; canvas.height = height * ratio; canvas.style.width = width + 'px'; canvas.style.height = height + 'px'; ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
        const left = 62, right = width - 18, top = 20, bottom = height - 40;
        const values = series.map(p => p.value), low = Math.min(...values, series[0].min), high = Math.max(...values, series[0].max);
        const padding = Math.max((high - low) * .12, .1), min = Math.max(0, low - padding), max = high + padding;
        const y = value => bottom - (value - min) / (max - min) * (bottom - top);
        const x = index => series.length === 1 ? (left + right) / 2 : left + index / (series.length - 1) * (right - left);
        ctx.fillStyle = '#edf7ef'; ctx.fillRect(left, y(series[0].max), right - left, y(series[0].min) - y(series[0].max));
        ctx.font = '12px system-ui'; ctx.textAlign = 'right'; ctx.fillStyle = '#64748b';
        for (let i = 0; i <= 4; i++) { const value = min + (max - min) * i / 4, pos = y(value); ctx.strokeStyle = '#e2e8f0'; ctx.beginPath(); ctx.moveTo(left, pos); ctx.lineTo(right, pos); ctx.stroke(); ctx.fillText(Number(value.toFixed(4)).toLocaleString('ru-RU', { maximumFractionDigits: 4 }), left - 8, pos + 4); }
        ctx.strokeStyle = '#60a477'; ctx.setLineDash([5, 4]);
        for (const value of [series[0].min, series[0].max]) { ctx.beginPath(); ctx.moveTo(left, y(value)); ctx.lineTo(right, y(value)); ctx.stroke(); }
        ctx.setLineDash([]); ctx.strokeStyle = '#2563eb'; ctx.lineWidth = 2; ctx.beginPath();
        points = series.map((p, index) => ({ x: x(index), y: y(p.value), ...p }));
        points.forEach((p, i) => i ? ctx.lineTo(p.x, p.y) : ctx.moveTo(p.x, p.y)); ctx.stroke();
        points.forEach(p => { ctx.beginPath(); ctx.fillStyle = p.value < p.min || p.value > p.max ? '#dc3545' : '#2563eb'; ctx.arc(p.x, p.y, 4, 0, Math.PI * 2); ctx.fill(); });
        ctx.fillStyle = '#64748b'; ctx.textAlign = 'left'; ctx.fillText(series[0].date, left, height - 12);
        if (series.length > 1) { ctx.textAlign = 'right'; ctx.fillText(series[series.length - 1].date, right, height - 12); }
    }
    canvas.addEventListener('pointermove', event => {
        const rect = canvas.getBoundingClientRect(), localX = event.clientX - rect.left;
        const nearest = points.reduce((a, b) => Math.abs(a.x - localX) < Math.abs(b.x - localX) ? a : b);
        document.getElementById('chart-point').textContent = `${nearest.date} · запись №${nearest.recordId}: ${nearest.value} · ${nearest.status}`;
    });
    new ResizeObserver(draw).observe(canvas.parentElement); draw();
});
