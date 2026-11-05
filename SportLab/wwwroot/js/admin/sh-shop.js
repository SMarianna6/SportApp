/* Shop page.
   Requires sh-common.js (SH.esc, SH.icon, SH.openModal, SH.closeModal).

   Endpoints (adjust to your controller):
     GET  /Admin/GetProductList          -> [{ id, name, description, category, price, stock, photo }]
     POST /Admin/SaveProductApi          -> multipart FormData: id (0 = new), name, description, category, price, stock, photo (file, optional)
     POST /Admin/UpdateProductStockApi   -> JSON { id, stock }
     POST /Admin/DeleteProductApi        -> JSON { id }
*/
(function () {
    'use strict';
    var SH = window.SH;

    var URLS = {
        list: '/Admin/GetProductList',
        save: '/Admin/SaveProductApi',
        stock: '/Admin/UpdateProductStockApi',
        remove: '/Admin/DeleteProductApi'
    };

    var MOCK_PRODUCTS = [
        { id: 1, name: 'Whey Protein 2kg', description: 'Fast-absorbing whey isolate with 24 g of protein per serving. Mixes easily, low in sugar and available in chocolate and vanilla. Ideal after training or as a high-protein snack.', category: 'Nutrition', price: 49.9, stock: 24, photo: null },
        { id: 2, name: 'Lifting Straps', description: 'Padded cotton straps for heavy pulls.', category: 'Accessories', price: 14.5, stock: 3, photo: null }
    ];

    var products = [];
    var editing = null;
    var pendingPhoto = null;

    var body = document.getElementById('sh-products-body');
    var form = document.getElementById('sh-product-form');
    var modalTitle = document.getElementById('sh-product-modal-title');
    var fileInput = document.getElementById('sh-product-photo');
    var previewImg = document.getElementById('sh-photo-preview-img');
    var previewEmpty = document.getElementById('sh-photo-preview-empty');
    var lightboxImg = document.getElementById('sh-lightbox-img');

    /* ---------- api ---------- */
    function csrfToken() {
        if (!form) return null;
        var el = form.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : null;
    }

    function send(url, options) {
        return fetch(url, options)
            .then(function (res) {
                if (!res.ok) throw new Error(url + ' responded ' + res.status);
                return res.status === 204 ? null : res.json().catch(function () { return null; });
            })
            .catch(function (err) {
                console.warn('[sh-shop] request failed, continuing with local data:', err.message);
                return null;
            });
    }

    function postJson(url, payload) {
        var headers = { 'Content-Type': 'application/json' };
        if (csrfToken()) headers['RequestVerificationToken'] = csrfToken();
        return send(url, { method: 'POST', headers: headers, body: JSON.stringify(payload) });
    }

    function postForm(url, formData) {
        var headers = {};
        if (csrfToken()) headers['RequestVerificationToken'] = csrfToken();
        return send(url, { method: 'POST', headers: headers, body: formData });
    }

    function loadProducts() {
        return fetch(URLS.list, { headers: { Accept: 'application/json' } })
            .then(function (res) {
                if (!res.ok) throw new Error('GetProductList responded ' + res.status);
                return res.json();
            })
            .catch(function (err) {
                console.warn('[sh-shop] using mock products:', err.message);
                return MOCK_PRODUCTS.map(function (p) { return Object.assign({}, p); });
            });
    }

    /* ---------- render ---------- */
    function photoCell(p) {
        var inner = p.photo
            ? '<img src="' + SH.esc(p.photo) + '" alt="' + SH.esc(p.name) + '" />'
            : SH.icon.box('sh-ico-lg');
        var attrs = p.photo
            ? ' data-act="lightbox" aria-label="View photo of ' + SH.esc(p.name) + '"'
            : ' data-act="lightbox" disabled aria-label="No photo for ' + SH.esc(p.name) + '"';
        return '<button type="button" class="sh-photo sh-photo-btn"' + attrs + '>' + inner + '</button>';
    }

    function render() {
        if (!body) return;
        body.innerHTML = products.map(function (p) {
            var desc = p.description
                ? '<div class="sh-desc-wrap"><span class="sh-desc">' + SH.esc(p.description) + '</span>' +
                '<span class="sh-desc-toggle" role="button" tabindex="0" data-act="desc" hidden>Read More</span></div>'
                : '';
            return '<tr data-id="' + p.id + '">' +
                '<td>' + photoCell(p) + '</td>' +
                '<td class="sh-info"><div class="sh-info-name">' + SH.esc(p.name) + '</div>' + desc + '</td>' +
                '<td><span class="sh-badge-violet">' + SH.esc(p.category) + '</span></td>' +
                '<td>$' + Number(p.price).toFixed(2) + '</td>' +
                '<td><div class="sh-stock">' +
                '<button type="button" class="sh-stock-btn" data-delta="-1" aria-label="Decrease ' + SH.esc(p.name) + '">' + SH.icon.minus('sh-ico-sm') + '</button>' +
                '<span class="sh-stock-qty' + (p.stock < 5 ? ' is-low' : '') + '">' + p.stock + '</span>' +
                '<button type="button" class="sh-stock-btn" data-delta="1" aria-label="Increase ' + SH.esc(p.name) + '">' + SH.icon.plus('sh-ico-sm') + '</button>' +
                '</div></td>' +
                '<td><div class="sh-actions">' +
                '<button type="button" class="sh-icon-btn" data-act="edit" aria-label="Edit ' + SH.esc(p.name) + '">' + SH.icon.pencil() + '</button>' +
                '<button type="button" class="sh-icon-btn sh-danger" data-act="delete" aria-label="Delete ' + SH.esc(p.name) + '">' + SH.icon.trash() + '</button>' +
                '</div></td></tr>';
        }).join('');
        markTruncated();
    }

    function markTruncated() {
        if (!body) return;
        body.querySelectorAll('.sh-desc').forEach(function (desc) {
            var toggle = desc.parentNode.querySelector('.sh-desc-toggle');
            if (toggle) toggle.hidden = desc.scrollHeight <= desc.clientHeight + 1;
        });
    }

    /* ---------- photo preview ---------- */
    function showPreview(src) {
        if (previewImg) previewImg.hidden = !src;
        if (previewEmpty) previewEmpty.hidden = !!src;
        if (previewImg) {
            if (src) previewImg.src = src; else previewImg.removeAttribute('src');
        }
    }

    if (fileInput) {
        fileInput.addEventListener('change', function () {
            var file = fileInput.files && fileInput.files[0];
            var fileNameSpan = document.getElementById('sh-file-chosen');

            if (!file) {
                pendingPhoto = null;
                showPreview(editing ? editing.photo : null);

                if (fileNameSpan) {
                    fileNameSpan.textContent = 'Choose file';
                    fileNameSpan.style.color = '';
                }
                return;
            }

            if (fileNameSpan) {
                fileNameSpan.textContent = file.name;
                fileNameSpan.style.color = '#fff';
            }

            var reader = new FileReader();
            reader.onload = function () {
                pendingPhoto = reader.result;
                showPreview(pendingPhoto);
            };
            reader.readAsDataURL(file);
        });
    }

    /* ---------- modal ---------- */

    if (typeof $ !== 'undefined' && form) {
        $(form)
            .off('.shLiveValidation')
            .on('input.shLiveValidation change.shLiveValidation', 'input, textarea, select', function () {
                $(this).valid();
            });
    }

    function openForm(product) {
        editing = product;
        pendingPhoto = null;

        if (form) {
            form.reset();

            var f = form.elements;

            if (f.Name) f.Name.value = product ? product.name : '';
            if (f.Description) f.Description.value = product ? product.description || '' : '';
            if (f.Category) f.Category.value = product ? product.category : 'Nutrition';
            if (f.Price) f.Price.value = product ? product.price : '';
            if (f.StockQuantity) f.StockQuantity.value = product ? product.stock : '0';

            // CLEAR PREVIOUS VALIDATION ERRORS
            if (typeof $ !== 'undefined') {
                var validator = $(form).data('validator');

                if (validator) {
                    validator.resetForm();
                }

                $(form).find('[data-valmsg-for]')
                    .removeClass('field-validation-error')
                    .addClass('field-validation-valid')
                    .empty();

                $(form).find('[data-valmsg-summary="true"]')
                    .removeClass('validation-summary-errors')
                    .addClass('validation-summary-valid')
                    .empty();

                $(form).find('.input-validation-error')
                    .removeClass('input-validation-error');
            }
        }

        if (modalTitle) {
            modalTitle.textContent = product ? 'Edit Product' : 'Add Product';
        }

        showPreview(product ? product.photo : null);

        var fileNameSpan = document.getElementById('sh-file-chosen');
        if (fileNameSpan) {
            fileNameSpan.textContent = 'Choose file';
            fileNameSpan.style.color = '';
        }

        SH.openModal('sh-product-modal');
    }

    var addBtn = document.getElementById('sh-product-add');
    if (addBtn) addBtn.addEventListener('click', function () { openForm(null); });

    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            // Клієнтська валідація jQuery Unobtrusive
            if (typeof $ !== 'undefined' && $(form).valid && !$(form).valid()) {
                return; // Якщо є помилки, зупиняємо відправку
            }

            var f = form.elements;

            // Збираємо дані, читаючи їх з нових полів (з великої літери)
            var data = {
                name: f.Name ? f.Name.value.trim() : '',
                description: f.Description ? f.Description.value.trim() : '',
                category: f.Category ? f.Category.value : 'Nutrition',
                price: f.Price ? parseFloat(f.Price.value) : 0,
                stock: f.StockQuantity ? parseInt(f.StockQuantity.value, 10) : 0
            };

            var formData = new FormData();
            formData.append('id', editing ? editing.id : 0);
            Object.keys(data).forEach(function (k) { formData.append(k, data[k]); });

            if (fileInput && fileInput.files && fileInput.files[0]) {
                formData.append('photo', fileInput.files[0]);
            }

            var record;
            if (editing) {
                record = Object.assign(editing, data);
                if (pendingPhoto) record.photo = pendingPhoto;
            } else {
                record = Object.assign({ id: -Date.now(), photo: pendingPhoto }, data);
                products.push(record);
            }
            var tempId = record.id;

            SH.closeModal('sh-product-modal');
            render();

            postForm(URLS.save, formData).then(function (saved) {
                if (saved && tempId < 0) {
                    if (saved.id) record.id = saved.id;
                    if (saved.photo) record.photo = saved.photo;
                    render();
                }
            });
        });

        // Моментальна перевірка поля при втраті фокусу (blur)
        if (typeof $ !== 'undefined') {
            $(form).find('input, select, textarea').on('blur change', function () {
                $(this).valid();
            });
        }
    }

    /* ---------- table events ---------- */
    function toggleDescription(toggle) {
        var desc = toggle.parentNode.querySelector('.sh-desc');
        var expanded = desc.classList.toggle('is-expanded');
        toggle.textContent = expanded ? 'Show Less' : 'Read More';
    }

    if (body) {
        body.addEventListener('click', function (e) {
            var row = e.target.closest('tr');
            if (!row) return;
            var id = Number(row.getAttribute('data-id'));
            var product = products.filter(function (p) { return p.id === id; })[0];
            if (!product) return;

            var stockBtn = e.target.closest('[data-delta]');
            if (stockBtn) {
                product.stock = Math.max(0, product.stock + Number(stockBtn.getAttribute('data-delta')));
                render();
                postJson(URLS.stock, { id: product.id, stock: product.stock });
                return;
            }

            var actBtn = e.target.closest('[data-act]');
            if (!actBtn) return;
            switch (actBtn.getAttribute('data-act')) {
                case 'lightbox':
                    if (!product.photo || !lightboxImg) return;
                    lightboxImg.src = product.photo;
                    lightboxImg.alt = product.name;
                    SH.openModal('sh-lightbox-modal');
                    break;
                case 'desc': toggleDescription(actBtn); break;
                case 'edit': openForm(product); break;
                case 'delete':
                    products = products.filter(function (p) { return p.id !== id; });
                    render();
                    postJson(URLS.remove, { id: id });
                    break;
            }
        });
    }

    /* ---------- init ---------- */
    loadProducts().then(function (data) {
        products = data;
        render();
    });
})();