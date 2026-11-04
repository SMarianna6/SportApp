/* Tariffs page. Demo data and in-memory add/edit/delete only (no backend).
   MVC integration point: replace the marked spots with calls to your own actions. */
(function () {
    'use strict';
    var SH = window.SH;

    var tariffs = [
        { id: 1, name: 'Basic Plan', description: 'Gym floor access during off-peak hours. No classes included.', price: 29, days: 30, type: 'Monthly' },
        { id: 2, name: 'Pro Plan', description: 'Unlimited gym access, group classes and one PT session.', price: 59, days: 30, type: 'Monthly' },
        { id: 3, name: 'Elite Plan', description: 'Everything in Pro plus sauna, nutrition coaching and guest passes.', price: 99, days: 30, type: 'Premium' },
        { id: 4, name: 'Annual Pass', description: 'Twelve months of Pro access at a discounted rate.', price: 590, days: 365, type: 'Annual' },
        { id: 5, name: 'Day Pass', description: 'Single visit access to all gym facilities.', price: 12, days: 1, type: 'Trial' }
    ];
    var editing = null;

    var grid = document.getElementById('sh-tariff-grid');
    var form = document.getElementById('sh-tariff-form');
    var title = document.getElementById('sh-tariff-modal-title');

    function render() {
        grid.innerHTML = tariffs.map(function (t) {
            return '<div class="sh-card sh-tariff" data-id="' + t.id + '">' +
                '<div class="sh-tariff-top"><h3 class="sh-tariff-name">' + SH.esc(t.name) + '</h3><span class="sh-tariff-type">' + SH.esc(t.type) + '</span></div>' +
                '<p class="sh-tariff-price">$' + SH.esc(t.price) + '</p>' +
                '<p class="sh-tariff-days">' + SH.esc(t.days) + ' ' + (t.days === 1 ? 'day' : 'days') + '</p>' +
                '<p class="sh-tariff-desc">' + SH.esc(t.description) + '</p>' +
                '<div class="sh-tariff-actions">' +
                '<button type="button" class="sh-tariff-btn sh-edit" data-act="edit">' + SH.icon.pencil('sh-ico-sm') + 'Edit</button>' +
                '<button type="button" class="sh-tariff-btn sh-del" data-act="delete">' + SH.icon.trash('sh-ico-sm') + 'Delete</button>' +
                '</div></div>';
        }).join('');
    }

    function openForm(t) {
        editing = t;
        title.textContent = t ? 'Edit Tariff' : 'Add Tariff';
        form.elements.name.value = t ? t.name : '';
        form.elements.description.value = t ? t.description : '';
        form.elements.price.value = t ? t.price : '';
        form.elements.days.value = t ? t.days : '30';
        form.elements.type.value = t ? t.type : 'Monthly';
        SH.openModal('sh-tariff-modal');
    }

    document.getElementById('sh-tariff-add').addEventListener('click', function () { openForm(null); });

    grid.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-act]');
        if (!btn) return;
        var id = Number(btn.closest('[data-id]').getAttribute('data-id'));
        if (btn.getAttribute('data-act') === 'edit') {
            openForm(tariffs.filter(function (t) { return t.id === id; })[0]);
        } else {
            // MVC integration point: delete via your own endpoint.
            tariffs = tariffs.filter(function (t) { return t.id !== id; });
            render();
        }
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var f = form.elements;
        var data = {
            name: f.name.value,
            description: f.description.value,
            price: parseFloat(f.price.value),
            days: parseInt(f.days.value, 10),
            type: f.type.value
        };
        // MVC integration point: send `data` to your own create/update action.
        if (editing) Object.assign(editing, data);
        else tariffs.push(Object.assign({ id: Date.now() }, data));
        SH.closeModal('sh-tariff-modal');
        render();
    });

    render();
})();
