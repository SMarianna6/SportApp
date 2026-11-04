/* Shop / Inventory page. Demo data; stock changes and adds live in memory only.
   MVC integration point: persist stock changes and new products through your own endpoints. */
(function () {
    'use strict';
    var SH = window.SH;

    var products = [
        { id: 1, name: 'Whey Protein 2kg', category: 'Nutrition', price: 49.9, stock: 24 },
        { id: 2, name: 'Lifting Straps', category: 'Accessories', price: 14.5, stock: 3 },
        { id: 3, name: 'SportHUB Hoodie', category: 'Apparel', price: 54, stock: 12 },
        { id: 4, name: 'Adjustable Dumbbell', category: 'Equipment', price: 129, stock: 4 },
        { id: 5, name: 'Pre-Workout 300g', category: 'Nutrition', price: 32, stock: 18 },
        { id: 6, name: 'Resistance Bands', category: 'Equipment', price: 19, stock: 0 }
    ];

    var body = document.getElementById('sh-products-body');
    var form = document.getElementById('sh-product-form');

    function render() {
        body.innerHTML = products.map(function (p) {
            return '<tr data-id="' + p.id + '">' +
                '<td><div class="sh-photo">' + SH.icon.box('sh-ico-lg') + '</div></td>' +
                '<td class="sh-strong">' + SH.esc(p.name) + '</td>' +
                '<td><span class="sh-badge-violet">' + SH.esc(p.category) + '</span></td>' +
                '<td>$' + p.price.toFixed(2) + '</td>' +
                '<td><div class="sh-stock">' +
                '<button type="button" class="sh-stock-btn" data-delta="-1" aria-label="Decrease ' + SH.esc(p.name) + '">' + SH.icon.minus('sh-ico-sm') + '</button>' +
                '<span class="sh-stock-qty' + (p.stock < 5 ? ' is-low' : '') + '">' + p.stock + '</span>' +
                '<button type="button" class="sh-stock-btn" data-delta="1" aria-label="Increase ' + SH.esc(p.name) + '">' + SH.icon.plus('sh-ico-sm') + '</button>' +
                '</div></td></tr>';
        }).join('');
    }

    body.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-delta]');
        if (!btn) return;
        var id = Number(btn.closest('tr').getAttribute('data-id'));
        var delta = Number(btn.getAttribute('data-delta'));
        products.forEach(function (p) { if (p.id === id) p.stock = Math.max(0, p.stock + delta); });
        render();
    });

    document.getElementById('sh-product-add').addEventListener('click', function () {
        form.reset();
        form.elements.stock.value = '0';
        SH.openModal('sh-product-modal');
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var f = form.elements;
        products.push({
            id: Date.now(),
            name: f.name.value,
            category: f.category.value,
            price: parseFloat(f.price.value),
            stock: parseInt(f.stock.value, 10)
        });
        SH.closeModal('sh-product-modal');
        render();
    });

    render();
})();
