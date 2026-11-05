/* Weekly schedule page. Integrated with database. */
(function () {
    'use strict';
    var SH = window.SH;

    var URLS = { list: '/Admin/GetScheduleList', save: '/Admin/SaveScheduleApi', remove: '/Admin/DeleteScheduleApi' };

    var DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    var FIRST_HOUR = 8, LAST_HOUR = 20;
    var ROW = 64; // px per hour
    var HOURS = [];
    for (var h = FIRST_HOUR; h < LAST_HOUR; h++) HOURS.push(h);

    var TYPES = {
        HIIT: { cls: 'hiit', label: 'HIIT' },
        Pilates: { cls: 'pilates', label: 'Pilates' },
        Yoga: { cls: 'yoga', label: 'Yoga' },
        Strength: { cls: 'strength', label: 'Strength' },
        Cycling: { cls: 'cycling', label: 'Cycling' },
        Boxing: { cls: 'boxing', label: 'Boxing' }
    };

    var slots = [];
    var editing = null;
    var visible = {};
    Object.keys(TYPES).forEach(function (t) { visible[t] = true; });

    var cal = document.getElementById('sh-cal');
    var legend = document.getElementById('sh-schedule-legend');
    var form = document.getElementById('sh-schedule-form');
    var modalTitle = document.getElementById('sh-schedule-modal-title');
    var errorEl = document.getElementById('sh-schedule-error');
    var deleteBtn = document.getElementById('sh-schedule-delete');

    /* ---------- helpers ---------- */
    function pad(n) { return String(n).padStart(2, '0'); }
    function hhmm(v) { return pad(Math.floor(v)) + ':' + pad(Math.round((v % 1) * 60)); }
    function toHour(t) { var p = String(t).split(':').map(Number); return p[0] + (p[1] || 0) / 60; }

    function normDay(d) {
        var n = Number(d);
        if (n >= 1 && n <= 7) return n;
        var i = DAYS.map(function (x) { return x.toLowerCase(); }).indexOf(String(d).slice(0, 3).toLowerCase());
        return i + 1 || 1;
    }

    function normalize(s) {
        return {
            id: s.id, day: normDay(s.day), timeStart: String(s.timeStart).slice(0, 5), timeEnd: String(s.timeEnd).slice(0, 5),
            type: TYPES[s.type] ? s.type : 'Strength', coach: s.coach || '', booked: Number(s.booked) || 0, capacity: Number(s.capacity) || 10
        };
    }

    /* ---------- api ---------- */
    async function load() {
        try {
            const response = await fetch(URLS.list);
            const data = await response.json();
            slots = data.map(normalize);
            render();
        } catch (error) {
            console.error("Error loading schedule:", error);
        }
    }

    /* ---------- layout ---------- */
    function layoutDay(items) {
        var sorted = items.map(function (s) { return { s: s, start: toHour(s.timeStart), end: toHour(s.timeEnd) }; })
            .sort(function (a, b) { return a.start - b.start || a.end - b.end; });
        var cluster = [], lanes = [], clusterEnd = 0;
        function flush() {
            cluster.forEach(function (c) { c.laneCount = lanes.length; });
            cluster = []; lanes = []; clusterEnd = 0;
        }
        sorted.forEach(function (c) {
            if (cluster.length && c.start >= clusterEnd) flush();
            var lane = lanes.findIndex(function (end) { return end <= c.start; });
            if (lane === -1) { lane = lanes.length; lanes.push(0); }
            lanes[lane] = c.end;
            c.lane = lane;
            cluster.push(c);
            clusterEnd = Math.max(clusterEnd, c.end);
        });
        flush();
        return sorted;
    }

    function slotHtml(c) {
        var s = c.s;
        var top = (Math.max(c.start, FIRST_HOUR) - FIRST_HOUR) * ROW;
        var height = (Math.min(c.end, LAST_HOUR) - Math.max(c.start, FIRST_HOUR)) * ROW;
        var size = height < 56 ? 'sh-slot-sm' : height < 90 ? 'sh-slot-md' : 'sh-slot-lg';
        var full = s.booked >= s.capacity;
        var pct = Math.min(s.booked / s.capacity, 1) * 100;
        var width = 100 / c.laneCount;
        var tip = s.type + ' | ' + s.timeStart + ' - ' + s.timeEnd + ' | Coach: ' + s.coach + ' | ' + s.booked + '/' + s.capacity + ' booked';

        return '<button type="button" class="sh-slot sh-t-' + TYPES[s.type].cls + ' ' + size + (full ? ' is-full' : '') + '" data-id="' + s.id + '" title="' + SH.esc(tip) + '" ' +
            'style="top:' + (top + 1) + 'px;height:' + (height - 2) + 'px;left:calc(' + (c.lane * width) + '% + 2px);width:calc(' + width + '% - 4px)">' +
            '<span class="sh-slot-head"><span class="sh-slot-act">' + SH.esc(s.type) + '</span><span class="sh-slot-count">' + s.booked + '/' + s.capacity + '</span></span>' +
            '<span class="sh-slot-time">' + s.timeStart + ' - ' + s.timeEnd + '</span>' +
            '<span class="sh-slot-coach">' + SH.esc(s.coach) + '</span>' +
            '<span class="sh-slot-bar"><span class="sh-slot-fill" style="width:' + pct + '%"></span></span>' +
            '</button>';
    }

    /* ---------- render ---------- */
    function render() {
        if (!cal) return;
        var html = '<div class="sh-cal-head"><div></div>' +
            DAYS.map(function (d) { return '<div class="sh-cal-day">' + d + '</div>'; }).join('') + '</div>';

        html += '<div class="sh-cal-body"><div class="sh-cal-times">' +
            HOURS.map(function (h) { return '<div class="sh-cal-hour" style="height:' + ROW + 'px">' + hhmm(h) + '</div>'; }).join('') + '</div>';

        DAYS.forEach(function (name, i) {
            var day = i + 1;
            var items = slots.filter(function (s) { return s.day === day && visible[s.type]; });
            html += '<div class="sh-cal-col" data-day="' + day + '">' +
                HOURS.map(function (h) {
                    return '<div class="sh-cal-cell" data-day="' + day + '" data-hour="' + h + '" style="height:' + ROW + 'px" aria-label="Add class on ' + name + ' at ' + hhmm(h) + '"></div>';
                }).join('') +
                '<div class="sh-cal-layer">' + layoutDay(items).map(slotHtml).join('') + '</div></div>';
        });
        cal.innerHTML = html + '</div>';
    }

    function renderLegend() {
        if (!legend) return;
        legend.innerHTML = Object.keys(TYPES).map(function (t) {
            return '<label class="sh-legend-item sh-t-' + TYPES[t].cls + '">' +
                '<input type="checkbox" data-type="' + t + '"' + (visible[t] ? ' checked' : '') + ' />' +
                '<span class="sh-legend-swatch"></span><span>' + t + '</span></label>';
        }).join('');
    }

    if (legend) {
        legend.addEventListener('change', function (e) {
            var cb = e.target.closest('[data-type]');
            if (!cb) return;
            visible[cb.getAttribute('data-type')] = cb.checked;
            render();
        });
    }

    /* ---------- modal ---------- */
    var typeSelect = document.getElementById('sh-schedule-type');
    if (typeSelect) {
        typeSelect.innerHTML = Object.keys(TYPES).map(function (t) { return '<option>' + t + '</option>'; }).join('');
    }

    function showError(msg) {
        if (!errorEl) return;
        errorEl.textContent = msg || '';
        errorEl.hidden = !msg;
    }

    if (typeof $ !== 'undefined' && form) {
        $(form)
            .off('.shLiveValidation')
            .on('input.shLiveValidation change.shLiveValidation', 'input, textarea, select', function () {
                $(this).valid();
            });
    }

    function openForm(slot, day, hour) {
        editing = slot || null;
        showError('');

        if (form) {
            form.reset();

            var f = form.elements;

            if (f.ActivityName) f.ActivityName.value = slot ? slot.type : 'HIIT';
            if (f.day) f.day.value = String(slot ? slot.day : day);
            if (f.StartTime) f.StartTime.value = slot ? slot.timeStart : hhmm(hour);
            if (f.EndTime) f.EndTime.value = slot ? slot.timeEnd : hhmm(Math.min(hour + 1, LAST_HOUR));
            if (f.coach) f.coach.value = slot ? slot.coach : '';
            if (f.Capacity) f.Capacity.value = slot ? slot.capacity : '15';
            if (f.Booked) f.Booked.value = slot ? slot.booked : '0';

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
            modalTitle.textContent = slot ? 'Edit Class' : 'Add Class';
        }

        if (deleteBtn) {
            deleteBtn.hidden = !slot;
        }

        SH.openModal('sh-schedule-modal');
    }

    if (cal) {
        cal.addEventListener('click', function (e) {
            var slotEl = e.target.closest('.sh-slot');
            if (slotEl) {
                var id = Number(slotEl.getAttribute('data-id'));
                openForm(slots.filter(function (s) { return s.id === id; })[0]);
                return;
            }
            var cell = e.target.closest('.sh-cal-cell');
            if (cell) openForm(null, Number(cell.getAttribute('data-day')), Number(cell.getAttribute('data-hour')));
        });
    }

    if (form) {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            // Клієнтська валідація jQuery Unobtrusive
            if (typeof $ !== 'undefined' && $(form).valid && !$(form).valid()) {
                return; // Зупиняємо відправку, якщо є помилки
            }

            var f = form.elements;

            // Збираємо дані, читаючи їх з нових полів (з великої літери)
            var data = {
                type: f.ActivityName ? f.ActivityName.value : '',
                day: f.day ? Number(f.day.value) : 1,
                timeStart: f.StartTime ? f.StartTime.value : '',
                timeEnd: f.EndTime ? f.EndTime.value : '',
                coach: f.coach ? f.coach.value.trim() : '',
                capacity: f.Capacity ? parseInt(f.Capacity.value, 10) : 15,
                booked: f.Booked ? parseInt(f.Booked.value, 10) : 0
            };

            // Специфічна ручна валідація
            if (!data.timeStart || !data.timeEnd) return showError('Start and end time are required.');
            if (toHour(data.timeStart) < FIRST_HOUR || toHour(data.timeEnd) > LAST_HOUR) return showError('Classes must be between 08:00 and 20:00.');
            if (toHour(data.timeEnd) <= toHour(data.timeStart)) return showError('End time must be after start time.');
            if (!data.coach) return showError('Coach is required.');
            if (data.booked > data.capacity) return showError('Booked cannot exceed capacity.');

            var record;
            if (editing) {
                record = Object.assign(editing, data);
            } else {
                record = Object.assign({ id: -Date.now() }, data);
                slots.push(record);
            }

            SH.closeModal('sh-schedule-modal');
            render();

            const payload = Object.assign({}, data, { id: record.id < 0 ? 0 : record.id });
            const response = await fetch(URLS.save, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                const saved = await response.json();
                if (saved && saved.id && record.id < 0) {
                    record.id = saved.id;
                    render();
                }
            }
        });

        // Моментальна перевірка поля при втраті фокусу (blur)
        if (typeof $ !== 'undefined') {
            $(form).find('input, select').on('blur change', function () {
                $(this).valid();
            });
        }
    }

    if (deleteBtn) {
        deleteBtn.addEventListener('click', async function () {
            if (!editing) return;
            var id = editing.id;

            if (confirm('Delete this class?')) {
                slots = slots.filter(function (s) { return s.id !== id; });
                SH.closeModal('sh-schedule-modal');
                render();

                await fetch(URLS.remove, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: id })
                });
            }
        });
    }

    /* ---------- init ---------- */
    renderLegend();
    load();
})();