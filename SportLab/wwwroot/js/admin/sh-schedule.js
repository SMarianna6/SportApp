/* Schedule page. Demo data; changes live in memory only.
   MVC integration points: loading slots, saving slots, and the "notify clients" step on
   cancellation must be wired to your own backend - nothing is sent anywhere here. */
(function () {
    'use strict';
    var SH = window.SH;

    var TRAINERS = ['Max Berg', 'Sara Lindqvist', 'Daniel Cho', 'Olga Smirnova'];
    var DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    var FIRST_HOUR = 8, LAST_HOUR = 20, ROW = 56;
    var HOURS = [];
    for (var h = FIRST_HOUR; h < LAST_HOUR; h++) HOURS.push(h);

    var slots = [
        { id: 1, activity: 'Yoga', trainer: 'Sara Lindqvist', day: 0, start: 9, end: 10, status: 'Open', capacity: 15, booked: 11 },
        { id: 2, activity: 'HIIT', trainer: 'Max Berg', day: 1, start: 18, end: 19, status: 'Open', capacity: 20, booked: 19 },
        { id: 3, activity: 'Spinning', trainer: 'Daniel Cho', day: 2, start: 12, end: 13, status: 'Open', capacity: 12, booked: 6 },
        { id: 4, activity: 'Boxing', trainer: 'Max Berg', day: 3, start: 17, end: 18.5, status: 'Open', capacity: 16, booked: 9 },
        { id: 5, activity: 'Pilates', trainer: 'Olga Smirnova', day: 4, start: 10, end: 11, status: 'Open', capacity: 14, booked: 14 },
        { id: 6, activity: 'CrossFit', trainer: 'Daniel Cho', day: 5, start: 11, end: 12.5, status: 'Open', capacity: 18, booked: 7 }
    ];
    var editing = null;
    var cancelling = false;

    function pad(n) { return String(n).padStart(2, '0'); }
    function hhmm(v) { return pad(Math.floor(v)) + ':' + (v % 1 ? '30' : '00'); }
    function toHour(t) { var p = t.split(':').map(Number); return p[0] + (p[1] || 0) / 60; }

    var root = document.getElementById('sh-sched');
    var form = document.getElementById('sh-slot-form');
    var title = document.getElementById('sh-slot-modal-title');
    var submitBtn = document.getElementById('sh-slot-submit');
    var cancelBtn = document.getElementById('sh-slot-cancel-class');
    var confirmBtn = document.getElementById('sh-slot-confirm-cancel');
    var reasonWrap = document.getElementById('sh-slot-reason-wrap');
    var reason = document.getElementById('sh-slot-reason');

    document.getElementById('sh-slot-trainer').innerHTML = TRAINERS.map(function (t) { return '<option>' + SH.esc(t) + '</option>'; }).join('');
    document.getElementById('sh-slot-day').innerHTML = DAYS.map(function (d, i) { return '<option value="' + i + '">' + d + '</option>'; }).join('');

    function slotHtml(s) {
        var pct = Math.min(s.booked / s.capacity, 1) * 100;
        return '<button type="button" class="sh-slot' + (s.cancelled ? ' is-cancelled' : '') + '" data-slot="' + s.id + '" ' +
            'style="top:' + ((s.start - FIRST_HOUR) * ROW + 2) + 'px;height:' + ((s.end - s.start) * ROW - 4) + 'px">' +
            '<span><span class="sh-slot-act">' + SH.esc(s.activity) + '</span>' +
            '<span class="sh-slot-trainer">' + SH.esc(s.trainer) + '</span>' +
            '<span class="sh-slot-time">' + hhmm(s.start) + ' - ' + hhmm(s.end) + '</span></span>' +
            '<span><span class="sh-slot-bar"><span class="sh-slot-fill' + (s.booked / s.capacity >= 1 ? ' is-full' : '') + '" style="width:' + pct + '%"></span></span>' +
            '<span class="sh-slot-count">' + s.booked + '/' + s.capacity + '</span></span></button>';
    }

    function render() {
        var html = '<div class="sh-sched-row sh-sched-days"><div></div>' +
            DAYS.map(function (d) { return '<div class="sh-sched-day">' + d + '</div>'; }).join('') + '</div>';
        html += '<div class="sh-sched-row"><div>' +
            HOURS.map(function (h) { return '<div class="sh-sched-hour" style="height:' + ROW + 'px">' + hhmm(h) + '</div>'; }).join('') + '</div>';
        DAYS.forEach(function (d, di) {
            html += '<div class="sh-sched-col" data-day="' + di + '" style="height:' + ROW * HOURS.length + 'px">' +
                HOURS.map(function () { return '<div class="sh-sched-cell" style="height:' + ROW + 'px"></div>'; }).join('') +
                slots.filter(function (s) { return s.day === di; }).map(slotHtml).join('') + '</div>';
        });
        root.innerHTML = html + '</div>';
    }

    function syncCancelUi() {
        reasonWrap.hidden = !(editing && cancelling);
        cancelBtn.hidden = !(editing && !cancelling);
        confirmBtn.hidden = !(editing && cancelling);
        confirmBtn.disabled = !reason.value.trim();
        reason.required = !!(editing && cancelling);
    }

    function openModal(slot, day, hour) {
        editing = slot || null;
        cancelling = false;
        reason.value = '';
        var f = form.elements;
        title.textContent = slot ? 'Edit / Cancel Class' : 'Add Slot';
        submitBtn.textContent = slot ? 'Save' : 'Add Slot';
        f.activity.value = slot ? slot.activity : '';
        f.trainer.value = slot ? slot.trainer : TRAINERS[0];
        f.day.value = String(slot ? slot.day : day);
        f.start.value = hhmm(slot ? slot.start : hour);
        f.end.value = hhmm(slot ? slot.end : Math.min(hour + 1, LAST_HOUR));
        f.status.value = slot ? slot.status : 'Open';
        f.capacity.value = slot ? slot.capacity : '15';
        syncCancelUi();
        SH.openModal('sh-slot-modal');
    }

    root.addEventListener('click', function (e) {
        var slotEl = e.target.closest('[data-slot]');
        if (slotEl) {
            var id = Number(slotEl.getAttribute('data-slot'));
            openModal(slots.filter(function (s) { return s.id === id; })[0]);
            return;
        }
        var col = e.target.closest('.sh-sched-col');
        if (col) {
            var y = e.clientY - col.getBoundingClientRect().top;
            openModal(null, Number(col.getAttribute('data-day')), FIRST_HOUR + Math.floor(y / ROW));
        }
    });

    cancelBtn.addEventListener('click', function () { cancelling = true; syncCancelUi(); reason.focus(); });
    reason.addEventListener('input', function () { confirmBtn.disabled = !reason.value.trim(); });

    confirmBtn.addEventListener('click', function () {
        if (!editing || !reason.value.trim()) return;
        editing.cancelled = reason.value;
        editing.status = 'Cancelled';
        SH.closeModal('sh-slot-modal');
        render();
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        if (cancelling) return;
        var f = form.elements;
        var data = {
            activity: f.activity.value, trainer: f.trainer.value, day: Number(f.day.value),
            start: toHour(f.start.value), end: toHour(f.end.value), status: f.status.value, capacity: parseInt(f.capacity.value, 10)
        };
        if (data.end <= data.start) return;
        // MVC integration point: send `data` to your own create/update action.
        if (editing) Object.assign(editing, data);
        else slots.push(Object.assign({ id: Date.now(), booked: 0 }, data));
        SH.closeModal('sh-slot-modal');
        render();
    });

    render();
})();
