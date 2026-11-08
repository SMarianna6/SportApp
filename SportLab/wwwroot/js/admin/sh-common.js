/* Shared helpers for the SportHUB admin pages. Load before the page script. */
(function () {
    'use strict';
    var SH = (window.SH = window.SH || {});

    SH.esc = function (value) {
        return String(value).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    };

    function svg(paths, cls) {
        return '<svg class="sh-ico ' + (cls || '') + '" viewBox="0 0 24 24" aria-hidden="true">' + paths + '</svg>';
    }
    SH.icon = {
        pencil: function (c) { return svg('<path d="M21.2 5.6 18.4 2.8a2 2 0 0 0-2.8 0L3 15.4V21h5.6L21.2 8.4a2 2 0 0 0 0-2.8z"/><path d="m15 5 4 4"/>', c); },
        trash: function (c) { return svg('<path d="M3 6h18"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>', c); },
        plus: function (c) { return svg('<path d="M5 12h14"/><path d="M12 5v14"/>', c); },
        minus: function (c) { return svg('<path d="M5 12h14"/>', c); },
        box: function (c) { return svg('<path d="M21 8v8a2 2 0 0 1-1 1.7l-7 4a2 2 0 0 1-2 0l-7-4A2 2 0 0 1 3 16V8a2 2 0 0 1 1-1.7l7-4a2 2 0 0 1 2 0l7 4A2 2 0 0 1 21 8z"/><path d="m3.3 7 8.7 5 8.7-5"/><path d="M12 22V12"/>', c); },
        trend: function (c) { return svg('<path d="m22 7-8.5 8.5-5-5L2 17"/><path d="M16 7h6v6"/>', c); },
        chevronDown: function (c) { return svg('<path d="m6 9 6 6 6-6"/>', c); }
    };

    SH.openModal = function (id) {
        var el = document.getElementById(id);
        if (!el) return;
        el.hidden = false;
        var first = el.querySelector('input:not([type=hidden]), select, textarea');
        if (first) first.focus();
    };
    SH.closeModal = function (target) {
        var el = typeof target === 'string' ? document.getElementById(target) : target;
        if (el) el.hidden = true;
    };

    document.addEventListener('click', function (e) {
        var closer = e.target.closest('[data-sh-close]');
        if (closer) {
            SH.closeModal(closer.closest('.sh-modal-backdrop'));
            return;
        }
        if (e.target.classList && e.target.classList.contains('sh-modal-backdrop')) SH.closeModal(e.target);
    });
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        document.querySelectorAll('.sh-modal-backdrop:not([hidden])').forEach(function (m) { SH.closeModal(m); });
    });
})();
