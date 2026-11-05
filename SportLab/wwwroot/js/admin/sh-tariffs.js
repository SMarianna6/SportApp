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

    if (typeof $ !== 'undefined' && form) {
        $(form)
            .off('.shLiveValidation')
            .on('input.shLiveValidation change.shLiveValidation', 'input, textarea, select', function () {
                $(this).valid();
            });
    }

    function openForm(t) {
        editing = t || null;

        if (form) {
            form.reset();

            var f = form.elements;

            if (f.Name) f.Name.value = t ? t.name : '';
            if (f.Description) f.Description.value = t ? t.description : '';
            if (f.Price) f.Price.value = t ? t.price : '';
            if (f.Duration) f.Duration.value = t ? t.days : '30';
            if (f.Type) f.Type.value = t ? t.type : 'Monthly';

            // CLEAR PREVIOUS VALIDATION ERRORS
            if (typeof $ !== 'undefined') {
                var validator = $(form).data('validator');

                if (validator) {
                    validator.resetForm();
                }

                // CLEAR FIELD VALIDATION MESSAGES
                $(form).find('[data-valmsg-for]')
                    .removeClass('field-validation-error')
                    .addClass('field-validation-valid')
                    .empty();

                // CLEAR VALIDATION SUMMARY
                $(form).find('[data-valmsg-summary="true"]')
                    .removeClass('validation-summary-errors')
                    .addClass('validation-summary-valid')
                    .empty();

                // REMOVE ERROR STYLING FROM INPUTS
                $(form).find('.input-validation-error')
                    .removeClass('input-validation-error');
            }
        }

        if (title) {
            title.textContent = t ? 'Edit Tariff' : 'Add Tariff';
        }

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

            // JQUERY UNOBTRUSIVE VALIDATION
            if (typeof $ !== 'undefined' && $(form).valid && !$(form).valid()) {
                return;
            }

            var f = form.elements;

            var data = {
                Id: editing ? editing.id : 0,
                Name: f.Name ? f.Name.value.trim() : '',
                Description: f.Description ? f.Description.value.trim() : '',
                Price: f.Price ? parseFloat(f.Price.value) : 0,
                Days: f.Duration ? parseInt(f.Duration.value, 10) : 30,
                Type: f.Type ? f.Type.value.trim() : ''
            };

            await fetch(URLS.save, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            SH.closeModal('sh-tariff-modal');
            loadTariffs();
        });

        // VALIDATE FIELDS ON BLUR AND CHANGE
        if (typeof $ !== 'undefined') {
            $(form).find('input, textarea, select').on('blur change', function () {
                $(this).valid();
            });
        }
    }

    loadTariffs();
})();