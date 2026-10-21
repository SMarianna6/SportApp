document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    var TOAST_MS = 3000;
    var TRAINERS = ['Alex Rivera', 'Mia Chen', 'Jonas Weber', 'Sara Okafor', 'Leo Martins'];
    var SUBSCRIPTIONS = {
        valid: { active: true, left: 5, total: 12 },
        none: { active: false, left: 0, total: 0 }
    };
    var currentSub = SUBSCRIPTIONS.valid;

    var $ = function (id) { return document.getElementById(id); };

    var el = {
        fab: $('fab'), modal: $('qb-modal'), backdrop: $('qb-modal-backdrop'), close: $('qb-modal-close'),
        viewBooking: $('qb-view-booking'), viewNoSub: $('qb-view-nosub'),
        type: $('qb-type'), date: $('qb-date'), trainer: $('qb-trainer'),
        slotCard: $('qb-slot-card'), slotAccent: $('qb-slot-accent'), slotType: $('qb-slot-type'), slotDate: $('qb-slot-date'),
        slotChip: $('qb-slot-chip'), slotAvatar: $('qb-slot-avatar'), slotTrainer: $('qb-slot-trainer'),
        visitsLabel: $('qb-visits-label'), visitsBar: $('qb-visits-bar'), visitsHint: $('qb-visits-hint'),
        submit: $('qb-submit-btn'), buy: $('qb-buy-btn'), back: $('qb-back-btn'), toasts: $('toasts')
    };
    var lastFocus = null;

    // Захист: якщо елементів немає на сторінці - скрипт просто зупиняється без помилок
    if (!el.fab || !el.modal) return;

    /* ---------- Haptics ---------- */
    function vibrate(pattern) {
        if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
            try { navigator.vibrate(pattern); } catch (e) { /* unsupported */ }
        }
    }

    /* ---------- Toasts (Переписано на базові класи Tailwind) ---------- */
    var TOAST_STYLES = {
        success: { box: 'bg-neon text-ink', bar: 'bg-ink/30', haptic: [200], icon: '<path d="m5 12 5 5L20 7"/>' },
        error: { box: 'bg-red-600 text-white', bar: 'bg-black/30', haptic: [100, 50, 100], icon: '<path d="M6 6l12 12M18 6 6 18"/>' }
    };

    function showToast(type, title, message) {
        var s = TOAST_STYLES[type] || TOAST_STYLES.success;
        var toast = document.createElement('div');

        // Використовуємо базові транзиції замість кастомної анімації, щоб CDN точно спрацював
        toast.className = 'pointer-events-auto relative overflow-hidden rounded-lg shadow-2xl shadow-black/50 transition-all duration-500 ease-out translate-x-[120%] opacity-0 ' + s.box;
        toast.setAttribute('role', type === 'error' ? 'alert' : 'status');

        toast.innerHTML =
            '<div class="flex items-start gap-3 px-4 pb-5 pt-4">' +
            '<svg class="mt-0.5 h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round">' + s.icon + '</svg>' +
            '<div class="min-w-0 flex-1">' +
            '<p class="text-sm font-black uppercase tracking-wider">' + title + '</p>' +
            '<p class="mt-0.5 text-sm font-medium opacity-80">' + (message || '') + '</p>' +
            '</div>' +
            '<button type="button" class="toast-close -mr-1 -mt-1 rounded-md p-1.5 opacity-70 transition-opacity hover:opacity-100 focus:outline-none">' +
            '<svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round"><path d="M6 6l12 12M18 6 6 18"/></svg>' +
            '</button>' +
            '</div>' +
            '<div class="absolute inset-x-0 bottom-0 h-1.5 ' + s.bar + '">' +
            '<div class="toast-bar h-full w-full bg-current opacity-60" style="transform-origin:left;transition:transform ' + TOAST_MS + 'ms linear"></div>' +
            '</div>';

        el.toasts.appendChild(toast);

        // Запуск анімації появи (зсув і прозорість)
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                toast.classList.remove('translate-x-[120%]', 'opacity-0');
                toast.classList.add('translate-x-0', 'opacity-100');
                var bar = toast.querySelector('.toast-bar');
                if (bar) bar.style.transform = 'scaleX(0)';
            });
        });

        var removed = false;
        function dismiss() {
            if (removed) return;
            removed = true;
            // Анімація зникнення
            toast.classList.remove('translate-x-0', 'opacity-100');
            toast.classList.add('translate-x-[120%]', 'opacity-0');
            setTimeout(function () { toast.remove(); }, 500);
        }

        var timer = setTimeout(dismiss, TOAST_MS);
        toast.querySelector('.toast-close').addEventListener('click', function () { clearTimeout(timer); dismiss(); });
        vibrate(s.haptic);
    }

    /* ---------- Form data ---------- */
    function buildOptions() {
        var days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        var html = '<option value="">Select date</option>';
        for (var i = 0; i < 7; i++) {
            var d = new Date();
            d.setDate(d.getDate() + i);
            var label = (i === 0 ? 'Today' : i === 1 ? 'Tomorrow' : days[d.getDay()]) + ', ' + months[d.getMonth()] + ' ' + d.getDate();
            html += '<option value="' + label + '">' + label + '</option>';
        }
        if (el.date) el.date.innerHTML = html;
        if (el.trainer) el.trainer.innerHTML = '<option value="">Select trainer</option>' + TRAINERS.map(function (t) { return '<option>' + t + '</option>'; }).join('');
    }

    function initials(name) {
        return name.split(' ').map(function (p) { return p.charAt(0); }).join('').toUpperCase();
    }

    /* ---------- Slot preview ---------- */
    function updatePreview() {
        var type = el.type.value, date = el.date.value, trainer = el.trainer.value;
        var filled = [type, date, trainer].filter(Boolean).length;
        var complete = filled === 3;

        el.slotType.textContent = type || 'Your slot';
        el.slotType.className = 'text-2xl font-black uppercase leading-none tracking-tighter ' + (type ? 'text-white' : 'text-white/30');
        el.slotDate.textContent = date || 'Pick a date';
        el.slotDate.className = 'mt-1.5 text-sm font-semibold ' + (date ? 'text-neon' : 'text-mono/40');
        el.slotTrainer.textContent = trainer || 'Not selected';
        el.slotTrainer.className = 'text-sm font-bold ' + (trainer ? 'text-white' : 'text-mono/40');
        el.slotAvatar.textContent = trainer ? initials(trainer) : '?';
        el.slotAvatar.className = 'flex h-9 w-9 items-center justify-center rounded-md text-xs font-black ' + (trainer ? 'bg-violet text-white' : 'bg-white/10 text-mono/40');
        el.slotChip.textContent = complete ? 'Ready' : filled + '/3';
        el.slotChip.className = 'rounded-md px-2.5 py-1 text-[11px] font-extrabold uppercase tracking-widest ' + (complete ? 'bg-neon text-ink' : 'bg-white/10 text-mono/50');
        el.slotAccent.className = 'w-1.5 transition-colors duration-300 ' + (complete ? 'bg-neon' : filled ? 'bg-violet' : 'bg-white/10');
        el.slotCard.className = 'mt-6 overflow-hidden rounded-lg border bg-card/60 transition-all duration-300 ' + (complete ? 'border-neon/60 shadow-lg shadow-neon/10' : 'border-dashed border-white/20');
    }

    /* ---------- Subscription status ---------- */
    function visitsColor(ratio) {
        if (ratio > 0.5) return 'linear-gradient(90deg,#5FD400,#AAFF00)';
        if (ratio > 0.2) return 'linear-gradient(90deg,#F2A900,#FFD93D)';
        return 'linear-gradient(90deg,#C62828,#FF5252)';
    }

    function updateSubscription() {
        var s = currentSub;
        if (!s.active) {
            el.visitsLabel.innerHTML = '<span class="text-mono/50">No plan</span>';
            el.visitsBar.style.width = '0%';
            el.visitsHint.textContent = 'Buy a membership to start booking.';
            // Зменшена кнопка для No Sub
            el.submit.className = 'mt-5 w-full cursor-pointer rounded-md bg-card py-3.5 text-base font-black uppercase tracking-wider text-white/40 transition-all duration-300 hover:text-white/60 focus:outline-none focus-visible:ring-2 focus-visible:ring-violet';
            el.submit.setAttribute('aria-disabled', 'true');
            return;
        }
        var ratio = s.left / s.total;
        var color = visitsColor(ratio);
        var textColor = ratio > 0.5 ? '#AAFF00' : ratio > 0.2 ? '#FFD93D' : '#FF5252';

        el.visitsLabel.innerHTML = 'Visits left: <span style="color:' + textColor + '">' + s.left + '/' + s.total + '</span>';
        el.visitsBar.style.background = color;
        el.visitsBar.style.width = '0%';

        requestAnimationFrame(function () {
            requestAnimationFrame(function () { el.visitsBar.style.width = (ratio * 100) + '%'; });
        });

        el.visitsHint.textContent = ratio > 0.5 ? 'Plenty of visits left.' : ratio > 0.2 ? 'Running low. Consider renewing soon.' : 'Almost out of visits.';
        // Зменшена неонова кнопка
        el.submit.className = 'mt-5 w-full rounded-md bg-neon py-3.5 text-base font-black uppercase tracking-wider text-ink shadow-lg shadow-neon/30 transition-all duration-300 hover:-translate-y-0.5 hover:shadow-xl hover:shadow-neon/40 focus:outline-none focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-ink active:translate-y-0';
        el.submit.setAttribute('aria-disabled', 'false');
    }

    /* ---------- Modal ---------- */
    function showView(name) {
        el.viewBooking.classList.toggle('hidden', name !== 'booking');
        el.viewNoSub.classList.toggle('hidden', name !== 'nosub');
        if (name === 'nosub') el.buy.focus();
    }

    function openModal(sub, trigger) {
        currentSub = sub;
        lastFocus = trigger || document.activeElement;
        el.type.value = ''; el.date.value = ''; el.trainer.value = '';
        updatePreview();
        updateSubscription();
        showView('booking');
        el.modal.classList.remove('hidden');
        el.modal.classList.add('flex');
        document.body.style.overflow = 'hidden';
        el.type.focus();
    }

    function closeModal() {
        el.modal.classList.add('hidden');
        el.modal.classList.remove('flex');
        document.body.style.overflow = '';
        if (lastFocus && lastFocus.focus) lastFocus.focus();
    }

    function submitBooking() {
        if (!currentSub.active) { showView('nosub'); return; }
        if (!el.type.value || !el.date.value || !el.trainer.value) {
            showToast('error', 'Missing details', 'Select type, date and trainer to continue.');
            return;
        }
        var summary = el.type.value + ' with ' + el.trainer.value + ', ' + el.date.value;
        closeModal();
        showToast('success', 'Booking confirmed', summary);
    }

    /* ---------- Events ---------- */
    [el.type, el.date, el.trainer].forEach(function (s) { s.addEventListener('change', updatePreview); });
    el.fab.addEventListener('click', function () { openModal(SUBSCRIPTIONS.valid, el.fab); });
    el.close.addEventListener('click', closeModal);
    el.backdrop.addEventListener('click', closeModal);
    el.submit.addEventListener('click', submitBooking);
    el.back.addEventListener('click', function () { showView('booking'); el.submit.focus(); });

    el.buy.addEventListener('click', function () {
        closeModal();
        showToast('success', 'Redirecting', 'Taking you to membership plans.');
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !el.modal.classList.contains('hidden')) closeModal();
    });

    buildOptions();
});