/* Shop page.
   Requires sh-common.js (SH.esc, SH.icon, SH.openModal, SH.closeModal).

   Endpoints (adjust to your controller):
     GET  /Admin/GetProductList          -> [{ id, name, description, category, price, stock, photo }]
     POST /Admin/SaveProductApi          -> multipart FormData: id (0 = new), name, description, category, price, stock, photo (file, optional)
     POST /Admin/UpdateProductStockApi   -> JSON { id, stock }
     POST /Admin/DeleteProductApi        -> JSON { id }

   If an endpoint is unreachable the UI keeps working on the local array (demo mode)
   and logs a console warning. Actual file storage is handled by the backend later. */
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
        { id: 2, name: 'Lifting Straps', description: 'Padded cotton straps for heavy pulls.', category: 'Accessories', price: 14.5, stock: 3, photo: null },
        { id: 3, name: 'SportHUB Hoodie', description: 'Heavyweight fleece hoodie with embroidered logo, a relaxed fit and a double-lined hood. Machine washable and available in sizes S to XXL.', category: 'Apparel', price: 54, stock: 12, photo: null },
        { id: 4, name: 'Adjustable Dumbbell', description: 'Quick-change dial dumbbell from 2 to 24 kg. Replaces twelve pairs of fixed weights and takes up almost no floor space.', category: 'Equipment', price: 129, stock: 4, photo: null },
        { id: 5, name: 'Pre-Workout 300g', description: 'Caffeine, citrulline and beta-alanine blend for focus and endurance.', category: 'Nutrition', price: 32, stock: 18, photo: null },
        { id: 6, name: 'Resistance Bands', description: '', category: 'Equipment', price: 19, stock: 0, photo: null }
    ];

    var products = [];
    var editing = null;       // product being edited, or null when adding
    var pendingPhoto = null;  // Data URL of a newly selected file (demo display only)

    var body = document.getElementById('sh-products-body');
    var form = document.getElementById('sh-product-form');
    var modalTitle = document.getElementById('sh-product-modal-title');
    var fileInput = document.getElementById('sh-product-photo');
    var previewImg = document.getElementById('sh-photo-preview-img');
    var previewEmpty = document.getElementById('sh-photo-preview-empty');
    var lightboxImg = document.getElementById('sh-lightbox-img');

    /* ---------- api ---------- */
    function csrfToken() {
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
        var headers = {}; // the browser sets the multipart boundary itself
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

    // Show the "Read More" toggle only for descriptions that are actually clamped.
    function markTruncated() {
        body.querySelectorAll('.sh-desc').forEach(function (desc) {
            var toggle = desc.parentNode.querySelector('.sh-desc-toggle');
            toggle.hidden = desc.scrollHeight <= desc.clientHeight + 1;
        });
    }

    /* ---------- photo preview ---------- */
    function showPreview(src) {
        previewImg.hidden = !src;
        previewEmpty.hidden = !!src;
        if (src) previewImg.src = src; else previewImg.removeAttribute('src');
    }

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

    /* ---------- modal ---------- */
    function openForm(product) {
        editing = product;
        pendingPhoto = null;
        form.reset();
        modalTitle.textContent = product ? 'Edit Product' : 'Add Product';
        var f = form.elements;
        f.name.value = product ? product.name : '';
        f.description.value = product ? product.description || '' : '';
        f.category.value = product ? product.category : 'Nutrition';
        f.price.value = product ? product.price : '';
        f.stock.value = product ? product.stock : '0';
        showPreview(product ? product.photo : null);
        var fileNameSpan = document.getElementById('sh-file-chosen');
        if (fileNameSpan) {
            fileNameSpan.textContent = 'Choose file';
            fileNameSpan.style.color = '';
        }
        SH.openModal('sh-product-modal');
    }

    document.getElementById('sh-product-add').addEventListener('click', function () { openForm(null); });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var f = form.elements;
        var data = {
            name: f.name.value.trim(),
            description: f.description.value.trim(),
            category: f.category.value,
            price: parseFloat(f.price.value),
            stock: parseInt(f.stock.value, 10)
        };

        var formData = new FormData();
        formData.append('id', editing ? editing.id : 0);
        Object.keys(data).forEach(function (k) { formData.append(k, data[k]); });
        if (fileInput.files && fileInput.files[0]) formData.append('photo', fileInput.files[0]);

        var record;
        if (editing) {
            record = Object.assign(editing, data);
            if (pendingPhoto) record.photo = pendingPhoto;
        } else {
            record = Object.assign({ id: -Date.now(), photo: pendingPhoto }, data); // temporary client id
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

    /* ---------- table events ---------- */
    function toggleDescription(toggle) {
        var desc = toggle.parentNode.querySelector('.sh-desc');
        var expanded = desc.classList.toggle('is-expanded');
        toggle.textContent = expanded ? 'Show Less' : 'Read More';
    }

    body.addEventListener('click', function (e) {
        var row = e.target.closest('tr');
        if (!row) return;
        var id = Number(row.getAttribute('data-id'));
        var product = products.filter(function (p) { return p.id === id; })[0];
        if (!product) return;

        var stockBtn = e.target.closest('[data-delta]');
        if (stockBtn) {
            // optimistic UI: update immediately, then tell the server
            product.stock = Math.max(0, product.stock + Number(stockBtn.getAttribute('data-delta')));
            render();
            postJson(URLS.stock, { id: product.id, stock: product.stock });
            return;
        }

        var actBtn = e.target.closest('[data-act]');
        if (!actBtn) return;
        switch (actBtn.getAttribute('data-act')) {
            case 'lightbox':
                if (!product.photo) return;
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

    body.addEventListener('keydown', function (e) {
        var toggle = e.target.closest('.sh-desc-toggle');
        if (toggle && (e.key === 'Enter' || e.key === ' ')) {
            e.preventDefault();
            toggleDescription(toggle);
        }
    });

    window.addEventListener('resize', function () {
        body.querySelectorAll('.sh-desc:not(.is-expanded)').forEach(function (desc) {
            desc.parentNode.querySelector('.sh-desc-toggle').hidden = desc.scrollHeight <= desc.clientHeight + 1;
        });
    });

    /* ---------- init ---------- */
    loadProducts().then(function (data) {
        products = data;
        render();
    });
})();
