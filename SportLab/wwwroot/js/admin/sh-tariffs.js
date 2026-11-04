(function () {
    'use strict';
    var SH = window.SH;

    var URLS = {
        list: '/Admin/GetTariffsList',
        save: '/Admin/SaveTariffApi',
        remove: '/Admin/DeleteTariffApi'
    };

    var tariffs = [];
    var editing = null;

    var grid = document.getElementById('sh-tariff-grid');
    var form = document.getElementById('sh-tariff-form');
    var title = document.getElementById('sh-tariff-modal-title');

    async function loadTariffs() {
        try {
            const response = await fetch(URLS.list);
            tariffs = await response.json();
            render();
        } catch (error) {
            console.error("Error loading tariffs:", error);
        }
    }

    function render() {
        if (!grid) return;

        if (!tariffs.length) {
            grid.innerHTML = '<div class="sh-card sh-tariff-empty">No tariffs available. Click "Add Tariff" to create one.</div>';
            return;
        }

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
        if (title) title.textContent = t ? 'Edit Tariff' : 'Add Tariff';

        if (form.elements.name) form.elements.name.value = t ? t.name : '';
        if (form.elements.description) form.elements.description.value = t ? t.description : '';
        if (form.elements.price) form.elements.price.value = t ? t.price : '';
        if (form.elements.days) form.elements.days.value = t ? t.days : '30';
        if (form.elements.type) form.elements.type.value = t ? t.type : 'Monthly';

        SH.openModal('sh-tariff-modal');
    }

    var addBtn = document.getElementById('sh-tariff-add');
    if (addBtn) {
        addBtn.addEventListener('click', function () { openForm(null); });
    }

    if (grid) {
        grid.addEventListener('click', async function (e) {
            var btn = e.target.closest('[data-act]');
            if (!btn) return;

            var card = btn.closest('[data-id]');
            if (!card) return;

            var id = Number(card.getAttribute('data-id'));
            var tariff = tariffs.filter(function (t) { return t.id === id; })[0];
            var act = btn.getAttribute('data-act');

            if (act === 'edit') {
                openForm(tariff);
            }

            if (act === 'delete') {
                if (confirm('Are you sure you want to delete this tariff?')) {
                    await fetch(URLS.remove + '?id=' + id, { method: 'POST' });
                    loadTariffs();
                }
            }
        });
    }

    if (form) {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            var f = form.elements;

            var data = {
                Id: editing ? editing.id : 0,
                Name: f.name ? f.name.value.trim() : '',
                Description: f.description ? f.description.value.trim() : '',
                Price: f.price ? parseFloat(f.price.value) : 0,
                Days: f.days ? parseInt(f.days.value, 10) : 30,
                Type: f.type ? f.type.value.trim() : 'Monthly'
            };

            await fetch(URLS.save, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            SH.closeModal('sh-tariff-modal');
            loadTariffs();
        });
    }

    loadTariffs();
})();