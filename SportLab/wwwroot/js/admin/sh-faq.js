(function () {
    'use strict';
    var SH = window.SH;

    var URLS = {
        list: '/Admin/GetFaqList',
        save: '/Admin/SaveFaqApi',
        order: '/Admin/UpdateFaqOrderApi',
        remove: '/Admin/DeleteFaqApi'
    };

    var faqs = [];
    var expanded = new Set();
    var editing = null;

    var list = document.getElementById('sh-faq-list');
    var count = document.getElementById('sh-faq-count');
    var form = document.getElementById('sh-faq-form');
    var title = document.getElementById('sh-faq-modal-title');

    var chevron = '<svg class="sh-ico sh-ico-sm sh-accordion-chevron" viewBox="0 0 24 24" aria-hidden="true"><path d="m6 9 6 6 6-6"/></svg>';
    var arrowUp = '<svg class="sh-ico" viewBox="0 0 24 24" aria-hidden="true"><path d="m18 15-6-6-6 6"/></svg>';
    var arrowDown = '<svg class="sh-ico" viewBox="0 0 24 24" aria-hidden="true"><path d="m6 9 6 6 6-6"/></svg>';

    function sorted() {
        return faqs.slice().sort(function (a, b) { return a.sortOrder - b.sortOrder; });
    }

    function byId(id) {
        return faqs.filter(function (f) { return f.id === id; })[0];
    }

    async function loadFaqs() {
        try {
            const response = await fetch(URLS.list);
            faqs = await response.json();
            render();
        } catch (error) {
            console.error("Error loading FAQs:", error);
        }
    }

    function render() {
        if (!list) return;
        var items = sorted();
        count.textContent = items.length + (items.length === 1 ? ' question' : ' questions');

        if (!items.length) {
            list.innerHTML = '<div class="sh-card sh-accordion-empty">No FAQs yet. Click "Add FAQ" to create the first one.</div>';
            return;
        }

        list.innerHTML = items.map(function (f, i) {
            var open = expanded.has(f.id);
            var q = SH.esc(f.question);
            return '<div class="sh-card sh-accordion-item' + (open ? ' is-open' : '') + '" data-id="' + f.id + '">' +
                '<div class="sh-accordion-header" role="button" tabindex="0" aria-expanded="' + open + '">' +
                chevron +
                '<span class="sh-accordion-question">' + q + '</span>' +
                '<button type="button" class="sh-status ' + (f.isActive ? 'is-active' : 'is-inactive') + '" data-act="toggle" title="Toggle status">' + (f.isActive ? 'Active' : 'Inactive') + '</button>' +
                '<div class="sh-actions">' +
                '<button type="button" class="sh-icon-btn" data-act="up" aria-label="Move up: ' + q + '"' + (i === 0 ? ' disabled' : '') + '>' + arrowUp + '</button>' +
                '<button type="button" class="sh-icon-btn" data-act="down" aria-label="Move down: ' + q + '"' + (i === items.length - 1 ? ' disabled' : '') + '>' + arrowDown + '</button>' +
                '<button type="button" class="sh-icon-btn" data-act="edit" aria-label="Edit: ' + q + '">' + SH.icon.pencil() + '</button>' +
                '<button type="button" class="sh-icon-btn sh-danger" data-act="delete" aria-label="Delete: ' + q + '">' + SH.icon.trash() + '</button>' +
                '</div>' +
                '</div>' +
                '<div class="sh-accordion-body"' + (open ? '' : ' hidden') + '>' + SH.esc(f.answer) + '</div>' +
                '</div>';
        }).join('');
    }

    if (typeof $ !== 'undefined' && form) {
        $(form)
            .off('.shLiveValidation')
            .on('input.shLiveValidation change.shLiveValidation', 'input, textarea, select', function () {
                $(this).valid();
            });
    }

    function openForm(faq) {
        editing = faq;
        title.textContent = faq ? 'Edit FAQ' : 'Add FAQ';

        if (form.elements.Question) form.elements.Question.value = faq ? faq.question : '';
        if (form.elements.Answer) form.elements.Answer.value = faq ? faq.answer : '';
        if (form.elements.IsActive) form.elements.IsActive.checked = faq ? faq.isActive : true;

        if (typeof $ !== 'undefined' && $(form).data('validator')) {
            $(form).validate().resetForm();

            $(form).find('.input-validation-error').removeClass('input-validation-error');
            $(form).find('.field-validation-error')
                .removeClass('field-validation-error')
                .addClass('field-validation-valid')
                .text('');
        }

        SH.openModal('sh-faq-modal');
    }

    function toggleItem(item) {
        var id = Number(item.getAttribute('data-id'));
        if (expanded.has(id)) expanded.delete(id); else expanded.add(id);
        var open = expanded.has(id);
        item.classList.toggle('is-open', open);
        item.querySelector('.sh-accordion-header').setAttribute('aria-expanded', String(open));
        item.querySelector('.sh-accordion-body').hidden = !open;
    }

    async function move(id, direction) {
        var items = sorted();
        var index = items.findIndex(function (f) { return f.id === id; });
        var other = items[index + direction];

        if (index < 0 || !other) return;

        var current = items[index];
        var tmp = current.sortOrder;
        current.sortOrder = other.sortOrder;
        other.sortOrder = tmp;

        render();

        await fetch(URLS.order, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify([
                { Id: current.id, SortOrder: current.sortOrder },
                { Id: other.id, SortOrder: other.sortOrder }
            ])
        });

        loadFaqs();
    }

    if (list) {
        list.addEventListener('click', async function (e) {
            var item = e.target.closest('.sh-accordion-item');
            if (!item) return;
            var btn = e.target.closest('[data-act]');

            if (!btn) {
                if (e.target.closest('.sh-accordion-header')) toggleItem(item);
                return;
            }
            if (btn.disabled) return;

            var id = Number(item.getAttribute('data-id'));
            var faq = byId(id);
            var act = btn.getAttribute('data-act');

            if (act === 'up') move(id, -1);
            if (act === 'down') move(id, 1);
            if (act === 'edit') openForm(faq);

            if (act === 'toggle') {
                faq.isActive = !faq.isActive;
                render();
                await fetch(URLS.save, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ Id: faq.id, Question: faq.question, Answer: faq.answer, IsActive: faq.isActive, SortOrder: faq.sortOrder })
                });
                loadFaqs();
            }

            if (act === 'delete') {
                if (confirm('Are you sure you want to delete this FAQ?')) {
                    await fetch(URLS.remove + '?id=' + id, { method: 'POST' });
                    loadFaqs();
                }
            }
        });
    }

    var addBtn = document.getElementById('sh-faq-add');
    if (addBtn) {
        addBtn.addEventListener('click', function () { openForm(null); });
    }

    if (form) {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            if (typeof $ !== 'undefined' && $(form).valid && !$(form).valid()) {
                return;
            }

            var f = form.elements;
            var maxOrder = faqs.length > 0 ? Math.max(...faqs.map(item => item.sortOrder)) : 0;

            var data = {
                Id: editing ? editing.id : 0,
                Question: f.Question ? f.Question.value.trim() : '',
                Answer: f.Answer ? f.Answer.value.trim() : '',
                IsActive: f.IsActive ? f.IsActive.checked : true,
                SortOrder: editing ? editing.sortOrder : maxOrder + 1
            };

            await fetch(URLS.save, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            SH.closeModal('sh-faq-modal');
            loadFaqs();
        });

        if (typeof $ !== 'undefined') {
            $(form).find('input, textarea').on('blur', function () {
                $(this).valid();
            });
        }
    }

    loadFaqs();
})();