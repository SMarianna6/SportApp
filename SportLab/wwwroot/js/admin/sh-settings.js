/* Settings page: tab switching and notification toggles (frontend only). */
(function () {
    'use strict';
    var tabs = document.querySelectorAll('.sh-page[data-sh-page="settings"] [data-tab]');
    var panels = document.querySelectorAll('.sh-page[data-sh-page="settings"] [data-panel]');

    tabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            var name = tab.getAttribute('data-tab');
            tabs.forEach(function (t) { t.classList.toggle('is-active', t === tab); t.setAttribute('aria-selected', String(t === tab)); });
            panels.forEach(function (p) { p.hidden = p.getAttribute('data-panel') !== name; });
        });
    });

    document.querySelectorAll('.sh-switch').forEach(function (sw) {
        sw.addEventListener('click', function () {
            sw.setAttribute('aria-checked', String(sw.getAttribute('aria-checked') !== 'true'));
        });
    });

    // The prototype's forms never submitted anywhere; keep that until you wire a real action.
    document.getElementById('sh-settings-form').addEventListener('submit', function (e) { e.preventDefault(); });
})();
