(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const tbody = document.getElementById('orders-table-body');
        const searchInput = document.getElementById('order-search');
        const toast = document.getElementById('toast');
        const toastText = document.getElementById('toast-text');

        // Modal elements
        const modal = document.getElementById('order-modal');
        const modalBackdrop = document.getElementById('modal-backdrop');
        const modalClose = document.getElementById('modal-close');

        let ORDERS = [];
        let filteredOrders = [];

        function showToast(msg, isError = false) {
            toastText.textContent = msg;
            toastText.className = isError ? 'text-[#FF5252]' : 'text-neon';
            toast.classList.remove('translate-y-10', 'opacity-0');
            setTimeout(() => toast.classList.add('translate-y-10', 'opacity-0'), 3000);
        }

        function formatDate(dateStr) {
            return new Date(dateStr).toLocaleDateString('en-US', {
                year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
            });
        }

        function getStatusOptions(currentStatus) {
            const statuses = ['Pending', 'Processing', 'Shipped', 'Completed', 'Cancelled'];
            return statuses.map(s => `<option value="${s}" ${s === currentStatus ? 'selected' : ''}>${s}</option>`).join('');
        }

        async function loadOrders() {
            try {
                const res = await fetch('/Admin/GetOrdersList');
                ORDERS = await res.json();
                filteredOrders = [...ORDERS];
                renderTable();
            } catch (e) {
                console.error(e);
                tbody.innerHTML = '<tr><td colspan="6" class="p-8 text-center text-[#FF5252]">Failed to load orders.</td></tr>';
            }
        }

        function renderTable() {
            if (filteredOrders.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" class="p-8 text-center text-white/50">No orders found.</td></tr>';
                return;
            }

            tbody.innerHTML = filteredOrders.map(o => {
                let statusColor = 'bg-yellow-500/20 text-yellow-300';
                if (o.status === 'Completed' || o.status === 'Shipped') statusColor = 'bg-green-500/20 text-green-400';
                if (o.status === 'Cancelled') statusColor = 'bg-red-500/20 text-red-400';
                if (o.status === 'Processing') statusColor = 'bg-blue-500/20 text-blue-400';

                return `
                    <tr class="transition-colors hover:bg-white/5">
                        <td class="px-5 py-4 font-black text-white/80">#${o.id}</td>
                        <td class="px-5 py-4">
                            <div class="font-bold text-white">${o.customerName}</div>
                            <div class="text-xs text-white/50">${o.customerEmail}</div>
                        </td>
                        <td class="px-5 py-4 text-white/70 whitespace-nowrap">${formatDate(o.orderDate)}</td>
                        <td class="px-5 py-4 font-bold text-neon">₴${o.totalAmount.toFixed(2)} <span class="text-xs text-white/50 font-normal">(${o.itemsCount} items)</span></td>
                        <td class="px-5 py-4">
                            <select class="status-select rounded-md border border-white/10 bg-[#111] px-3 py-1.5 text-xs font-bold uppercase tracking-wider focus:outline-none ${statusColor}" data-id="${o.id}">
                                ${getStatusOptions(o.status)}
                            </select>
                        </td>
                        <td class="px-5 py-4 text-right">
                            <button type="button" class="btn-view rounded-md bg-white/10 px-3 py-1.5 text-xs font-bold uppercase tracking-wider text-white transition-colors hover:bg-white/20" data-id="${o.id}">
                                View
                            </button>
                        </td>
                    </tr>
                `;
            }).join('');
        }

        // Change Status Handler
        tbody.addEventListener('change', async function (e) {
            if (!e.target.classList.contains('status-select')) return;

            const select = e.target;
            const id = select.dataset.id;
            const newStatus = select.value;
            select.disabled = true;

            try {
                const formData = new URLSearchParams();
                formData.append('id', id);
                formData.append('status', newStatus);
                formData.append('__RequestVerificationToken', token);

                const res = await fetch('/Admin/UpdateOrderStatusApi', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: formData.toString()
                });

                const data = await res.json();
                if (data.success) {
                    showToast(`Order #${id} updated to ${newStatus}`);
                    // Update local data
                    const order = ORDERS.find(o => o.id == id);
                    if (order) order.status = newStatus;

                    // Re-apply colors
                    let statusColor = 'bg-yellow-500/20 text-yellow-300';
                    if (newStatus === 'Completed' || newStatus === 'Shipped') statusColor = 'bg-green-500/20 text-green-400';
                    if (newStatus === 'Cancelled') statusColor = 'bg-red-500/20 text-red-400';
                    if (newStatus === 'Processing') statusColor = 'bg-blue-500/20 text-blue-400';

                    // Reset classes
                    select.className = `status-select rounded-md border border-white/10 bg-[#111] px-3 py-1.5 text-xs font-bold uppercase tracking-wider focus:outline-none ${statusColor}`;
                } else {
                    showToast('Failed to update status.', true);
                    select.value = ORDERS.find(o => o.id == id).status; // revert
                }
            } catch (err) {
                showToast('Network error.', true);
            } finally {
                select.disabled = false;
            }
        });

        // View Details Handler
        tbody.addEventListener('click', function (e) {
            const btn = e.target.closest('.btn-view');
            if (!btn) return;

            const id = btn.dataset.id;
            const order = ORDERS.find(o => o.id == id);
            if (!order) return;

            document.getElementById('modal-client').textContent = order.customerName;
            document.getElementById('modal-email').textContent = order.customerEmail;
            document.getElementById('modal-date').textContent = formatDate(order.orderDate);
            document.getElementById('modal-total').textContent = `Total: ₴${order.totalAmount.toFixed(2)}`;

            const itemsHtml = order.items.map(item => `
                <div class="flex items-center gap-4 rounded-lg bg-[#222] p-3 border border-white/5">
                    <div class="h-12 w-12 shrink-0 overflow-hidden rounded-md bg-white/5">
                        ${item.photoUrl
                    ? `<img src="${item.photoUrl}" class="h-full w-full object-cover" alt="${item.productName}">`
                    : `<div class="flex h-full w-full items-center justify-center text-white/20"><svg class="h-6 w-6" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M21 8v8a2 2 0 0 1-1 1.7l-7 4a2 2 0 0 1-2 0l-7-4A2 2 0 0 1 3 16V8a2 2 0 0 1 1-1.7l7-4a2 2 0 0 1 2 0l7 4A2 2 0 0 1 21 8z"/></svg></div>`
                }
                    </div>
                    <div class="flex-1">
                        <p class="font-bold text-sm text-white">${item.productName}</p>
                        <p class="text-xs text-white/50">${item.quantity} × ₴${item.unitPrice.toFixed(2)}</p>
                    </div>
                    <div class="font-black text-neon">
                        ₴${(item.quantity * item.unitPrice).toFixed(2)}
                    </div>
                </div>
            `).join('');

            document.getElementById('modal-items').innerHTML = itemsHtml;

            modal.classList.remove('hidden');
            modal.classList.add('flex');
            document.body.style.overflow = 'hidden';
        });

        function closeModal() {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
            document.body.style.overflow = '';
        }

        modalClose.addEventListener('click', closeModal);
        modalBackdrop.addEventListener('click', closeModal);

        // Search
        searchInput.addEventListener('input', function () {
            const query = this.value.toLowerCase();
            filteredOrders = ORDERS.filter(o =>
                o.id.toString().includes(query) ||
                o.customerName.toLowerCase().includes(query) ||
                o.customerEmail.toLowerCase().includes(query)
            );
            renderTable();
        });

        loadOrders();
    });
})();