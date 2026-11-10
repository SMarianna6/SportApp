(function () {
    'use strict';

    var SH = window.SH;
    var CONFIG = window.PROFILE_SCHEDULE;

    if (!SH || !CONFIG) {
        console.error('Profile schedule dependencies are missing.');
        return;
    }

    var IS_TRAINER = CONFIG.isTrainer;
    var IS_CLIENT = CONFIG.isClient;

    var URLS = {
        list: '/Profile/GetScheduleList',
        save: '/Profile/SaveScheduleApi',
        cancelSlot: '/Profile/CancelScheduleApi',
        deleteSlot: '/Profile/DeleteScheduleApi',
        cancelBooking: '/Profile/CancelScheduleBooking'
    };

    var DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

    var WINDOW_START = 8;
    var WINDOW_END = 20;
    var FIRST_HOUR = WINDOW_START;
    var LAST_HOUR = WINDOW_END;
    var ROW = 64;
    var HOURS = [];

    var TYPES = {
        HIIT: { cls: 'hiit' },
        Pilates: { cls: 'pilates' },
        Yoga: { cls: 'yoga' },
        Strength: { cls: 'strength' },
        Cycling: { cls: 'cycling' },
        Boxing: { cls: 'boxing' }
    };

    var slots = [];
    var editing = null;
    var selectedSlot = null;

    var visible = {};
    Object.keys(TYPES).forEach(function (type) {
        visible[type] = true;
    });

    var page = document.querySelector('[data-sh-page="profile-schedule"]');
    var calendar = document.getElementById('sh-cal');
    var legend = document.getElementById('sh-schedule-legend');

    var modal = document.getElementById('sh-schedule-modal');
    var modalTitle = document.getElementById('sh-schedule-modal-title');

    var form = document.getElementById('sh-schedule-form');
    var details = document.getElementById('sh-schedule-details');

    var addButton = document.getElementById('sh-schedule-add');
    var editButton = document.getElementById('sh-schedule-edit');
    var deleteButton = document.getElementById('sh-schedule-delete');

    var cancelSlotButton = document.getElementById('sh-schedule-cancel-slot');
    var cancelBookingButton = document.getElementById('sh-schedule-cancel-booking');

    var formError = document.getElementById('sh-schedule-error');
    var detailError = document.getElementById('sh-detail-error');

    var typeInput = document.getElementById('sh-schedule-type');
    var dateInput = document.getElementById('sh-schedule-date');
    var startInput = document.getElementById('sh-schedule-start');
    var endInput = document.getElementById('sh-schedule-end');
    var capacityInput = document.getElementById('sh-schedule-capacity');

    var currentWeekStart = getMonday(new Date());

    // Add class to easily hide the '+' icon in CSS for clients
    if (IS_CLIENT && page) {
        page.classList.add('is-client-view');
    }

    /* ---------- DATE HELPERS ---------- */

    function pad(value) {
        return String(value).padStart(2, '0');
    }

    function formatDate(date) {
        return date.getFullYear() + '-' +
            pad(date.getMonth() + 1) + '-' +
            pad(date.getDate());
    }

    function parseDate(value) {
        var parts = String(value).split('-').map(Number);
        return new Date(parts[0], parts[1] - 1, parts[2]);
    }

    function getMonday(date) {
        var result = new Date(date);
        result.setHours(0, 0, 0, 0);
        var day = result.getDay();
        var diff = day === 0 ? -6 : 1 - day;
        result.setDate(result.getDate() + diff);
        return result;
    }

    function getWeekDate(day) {
        var date = new Date(currentWeekStart);
        date.setDate(date.getDate() + day - 1);
        return date;
    }

    function formatDisplayDate(value) {
        var d = parseDate(value);
        var months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
        return pad(d.getDate()) + ' ' + months[d.getMonth()] + ' ' + d.getFullYear();
    }

    function toHour(value) {
        var parts = String(value).split(':').map(Number);
        return parts[0] + (parts[1] || 0) / 60;
    }

    function normalize(slot) {
        return {
            id: Number(slot.id),
            date: slot.date,
            day: Number(slot.day),
            timeStart: String(slot.timeStart).slice(0, 5),
            timeEnd: String(slot.timeEnd).slice(0, 5),
            type: TYPES[slot.type] ? slot.type : 'Strength',
            coach: slot.coach || 'TBA',
            booked: Number(slot.booked) || 0,
            capacity: Number(slot.capacity) || 1,
            status: slot.status || 'Available',
            clients: Array.isArray(slot.clients)
                ? slot.clients.map(function (client) {
                    return typeof client === 'string' ? client : client.name;
                })
                : []
        };
    }

    /* ---------- API ---------- */

    function getToken() {
        var token = document.querySelector('#sh-schedule-form input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    async function post(url, values) {
        var body = new URLSearchParams();
        body.append('__RequestVerificationToken', getToken());

        Object.keys(values || {}).forEach(function (key) {
            body.append(key, values[key] == null ? '' : String(values[key]));
        });

        var response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
            body: body.toString()
        });

        if (!response.ok) {
            var message = await response.text();
            throw new Error(message || 'The request could not be completed.');
        }

        var contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            return await response.json();
        }
        return null;
    }

    async function load() {
        try {
            var response = await fetch(URLS.list);
            if (!response.ok) throw new Error('Failed to load schedule.');
            var data = await response.json();
            slots = data.map(normalize);
            render();
        } catch (error) {
            console.error('Schedule loading error:', error);
            showPageError(error.message);
        }
    }

    function showPageError(message) {
        if (!calendar) return;
        calendar.innerHTML = '<p class="sh-form-error">' + SH.esc(message) + '</p>';
    }

    /* ---------- CALENDAR LAYOUT ---------- */

    function layoutDay(items) {
        var sorted = items.map(function (slot) {
            return {
                slot: slot,
                start: toHour(slot.timeStart),
                end: toHour(slot.timeEnd)
            };
        }).sort(function (a, b) {
            return a.start - b.start || a.end - b.end;
        });

        var cluster = [];
        var lanes = [];
        var clusterEnd = 0;

        function flush() {
            cluster.forEach(function (item) {
                item.laneCount = lanes.length;
            });
            cluster = [];
            lanes = [];
            clusterEnd = 0;
        }

        sorted.forEach(function (item) {
            if (cluster.length && item.start >= clusterEnd) flush();
            var lane = lanes.findIndex(function (end) { return end <= item.start; });
            if (lane === -1) {
                lane = lanes.length;
                lanes.push(0);
            }
            lanes[lane] = item.end;
            item.lane = lane;
            cluster.push(item);
            clusterEnd = Math.max(clusterEnd, item.end);
        });

        flush();
        return sorted;
    }

    function slotHtml(item) {
        var slot = item.slot;
        var top = (Math.max(item.start, FIRST_HOUR) - FIRST_HOUR) * ROW;
        var height = (Math.min(item.end, LAST_HOUR) - Math.max(item.start, FIRST_HOUR)) * ROW;

        var size = height < 56 ? 'sh-slot-sm' : height < 90 ? 'sh-slot-md' : 'sh-slot-lg';
        var full = slot.booked >= slot.capacity;
        var cancelled = slot.status === 'Cancelled';
        var completed = slot.status === 'Completed';
        var percentage = Math.min(slot.booked / slot.capacity, 1) * 100;
        var width = 100 / item.laneCount;

        var statusText = cancelled ? 'Cancelled' : completed ? 'Completed' : full ? 'Fully booked' : 'Available';
        var tooltip = slot.type + ' | ' + slot.timeStart + ' - ' + slot.timeEnd + ' | ' + slot.booked + '/' + slot.capacity + ' | ' + statusText;

        return '<button type="button" ' +
            'class="sh-slot sh-t-' + TYPES[slot.type].cls +
            (full ? ' is-full' : '') + (cancelled ? ' is-cancelled' : '') +
            '" data-id="' + slot.id + '" title="' + SH.esc(tooltip) + '" ' +
            'style="top:' + (top + 1) + 'px;height:' + Math.max(height - 2, 20) +
            'px;left:calc(' + (item.lane * width) + '% + 2px);width:calc(' + width + '% - 4px)">' +
            '<span class="sh-slot-head">' +
            '<span class="sh-slot-act">' + SH.esc(slot.type) + '</span>' +
            '<span class="sh-slot-count">' + slot.booked + '/' + slot.capacity + '</span>' +
            '</span>' +
            '<span class="sh-slot-time">' + slot.timeStart + ' - ' + slot.timeEnd + '</span>' +
            '<span class="sh-slot-coach">' + SH.esc(IS_TRAINER ? slot.booked + ' clients' : slot.coach) + '</span>' +
            '<span class="sh-slot-bar"><span class="sh-slot-fill" style="width:' + percentage + '%"></span></span>' +
            '</button>';
    }

    function rebuildHours() {
        var start = WINDOW_START;
        var end = WINDOW_END;

        if (IS_CLIENT) {
            var active = slots.filter(function (slot) {
                return slot.status !== 'Cancelled';
            });

            if (!active.length) {
                // Empty state for clients: Minimum 3 rows (9:00 to 12:00)
                start = 9;
                end = 12;
            } else {
                start = WINDOW_END;
                end = WINDOW_START;

                active.forEach(function (slot) {
                    var slotStart = Math.floor(toHour(slot.timeStart));
                    var slotEnd = Math.ceil(toHour(slot.timeEnd));
                    if (slotStart < start) start = slotStart;
                    if (slotEnd > end) end = slotEnd;
                });

                // Ensure at least 3 rows of height even if only 1 slot exists
                if (end - start < 3) {
                    end = start + 3;
                }

                start = Math.max(WINDOW_START, start);
                end = Math.min(WINDOW_END, Math.max(end, start + 1));
            }
        }

        FIRST_HOUR = start;
        LAST_HOUR = end;
        HOURS = [];

        for (var hour = FIRST_HOUR; hour < LAST_HOUR; hour++) {
            HOURS.push(hour);
        }
    }

    function render() {
        if (!calendar) return;
        rebuildHours();

        var html = '<div class="sh-cal-head"><div></div>' +
            DAYS.map(function (day, index) {
                var date = getWeekDate(index + 1);
                return '<div class="sh-cal-day">' + day + '<br><span>' + pad(date.getDate()) + '.' + pad(date.getMonth() + 1) + '</span></div>';
            }).join('') + '</div>';

        html += '<div class="sh-cal-body">';
        html += '<div class="sh-cal-times">' +
            HOURS.map(function (hour) {
                return '<div class="sh-cal-hour" style="height:' + ROW + 'px">' + pad(hour) + ':00</div>';
            }).join('') + '</div>';

        DAYS.forEach(function (name, index) {
            var day = index + 1;
            var items = slots.filter(function (slot) {
                return slot.day === day && visible[slot.type] && slot.status !== 'Cancelled';
            });

            var cells = HOURS.map(function (hour) {
                return '<div class="sh-cal-cell" ' +
                    'data-day="' + day + '" ' +
                    'data-hour="' + hour + '" ' +
                    'style="height:' + ROW + 'px" ' +
                    'aria-label="' + name + ' at ' + pad(hour) + ':00"></div>';
            }).join('');

            html += '<div class="sh-cal-col" data-day="' + day + '">' +
                cells + '<div class="sh-cal-layer">' + layoutDay(items).map(slotHtml).join('') + '</div></div>';
        });

        html += '</div>';
        calendar.innerHTML = html;
    }

    /* ---------- FILTERS ---------- */

    function renderLegend() {
        if (!legend) return;
        legend.innerHTML = Object.keys(TYPES).map(function (type) {
            return '<label class="sh-legend-item sh-t-' + TYPES[type].cls + '">' +
                '<input type="checkbox" data-type="' + type + '"' + (visible[type] ? ' checked' : '') + ' />' +
                '<span class="sh-legend-swatch"></span><span>' + type + '</span></label>';
        }).join('');
    }

    if (legend) {
        legend.addEventListener('change', function (event) {
            var checkbox = event.target.closest('[data-type]');
            if (!checkbox) return;
            visible[checkbox.dataset.type] = checkbox.checked;
            render();
        });
    }

    /* ---------- MODAL HELPERS ---------- */

    function showFormError(message) {
        if (!formError) return;
        formError.textContent = message || '';
        formError.hidden = !message;
    }

    function showDetailError(message) {
        if (!detailError) return;
        detailError.textContent = message || '';
        detailError.hidden = !message;
    }

    function showForm() {
        if (form) form.hidden = false;
        if (details) details.hidden = true;
    }

    function showDetails() {
        if (form) form.hidden = true;
        if (details) details.hidden = false;
    }

    function openCreateForm(day, hour) {
        if (!IS_TRAINER || !form) return;

        editing = null;
        selectedSlot = null;
        form.reset();
        showFormError('');

        var date = getWeekDate(day);
        document.querySelector('#sh-schedule-form [name="Id"]').value = '0';

        dateInput.value = formatDate(date);
        startInput.value = pad(hour) + ':00';
        endInput.value = pad(Math.min(hour + 1, LAST_HOUR)) + ':00';
        capacityInput.value = '15';

        if (modalTitle) modalTitle.textContent = 'Create workout';
        if (deleteButton) deleteButton.hidden = true;

        showForm();
        SH.openModal('sh-schedule-modal');
    }

    function openEditForm(slot) {
        if (!IS_TRAINER || !slot) return;

        if (slot.booked > 0) {
            alert('A workout with existing bookings cannot be edited.');
            return;
        }

        editing = slot;
        selectedSlot = slot;

        form.reset();
        showFormError('');
        document.querySelector('#sh-schedule-form [name="Id"]').value = String(slot.id);

        typeInput.value = slot.type;
        dateInput.value = slot.date;
        startInput.value = slot.timeStart;
        endInput.value = slot.timeEnd;
        capacityInput.value = String(slot.capacity);

        if (modalTitle) modalTitle.textContent = 'Edit workout';
        if (deleteButton) deleteButton.hidden = false;

        showForm();
        SH.openModal('sh-schedule-modal');
    }

    function openDetails(slot) {
        if (!slot) return;

        selectedSlot = slot;
        editing = null;
        showDetailError('');

        document.getElementById('sh-detail-type').textContent = slot.type;
        document.getElementById('sh-detail-date').textContent = formatDisplayDate(slot.date);
        document.getElementById('sh-detail-time').textContent = slot.timeStart + ' - ' + slot.timeEnd;
        document.getElementById('sh-detail-coach').textContent = slot.coach;
        document.getElementById('sh-detail-capacity').textContent = slot.booked + ' / ' + slot.capacity;
        document.getElementById('sh-detail-status').textContent = slot.status;

        if (IS_TRAINER) {
            var clients = document.getElementById('sh-detail-clients');
            if (clients) {
                clients.innerHTML = slot.clients.length
                    ? '<ul>' + slot.clients.map(function (name) { return '<li>' + SH.esc(name) + '</li>'; }).join('') + '</ul>'
                    : '<p>No clients have booked this workout yet.</p>';
            }
            if (editButton) editButton.hidden = slot.booked > 0 || slot.status === 'Cancelled' || slot.status === 'Completed';
            if (cancelSlotButton) cancelSlotButton.hidden = slot.status === 'Cancelled' || slot.status === 'Completed';
        }

        // Client Cancel logic
        if (IS_CLIENT && cancelBookingButton) {
            cancelBookingButton.hidden =
                slot.status === 'Cancelled' ||
                slot.status === 'Completed' ||
                new Date(slot.date + 'T' + slot.timeStart) <= new Date();
        }

        if (modalTitle) modalTitle.textContent = 'Workout details';

        showDetails();
        SH.openModal('sh-schedule-modal');
    }

    /* ---------- CALENDAR EVENTS ---------- */

    if (calendar) {
        calendar.addEventListener('click', function (event) {
            var slotElement = event.target.closest('.sh-slot');
            if (slotElement) {
                var id = Number(slotElement.dataset.id);
                var slot = slots.find(function (item) { return item.id === id; });
                if (!slot) return;

                openDetails(slot);
                return;
            }

            var cell = event.target.closest('.sh-cal-cell');
            // Only trainers can click empty slots to create
            if (cell && IS_TRAINER) {
                openCreateForm(Number(cell.dataset.day), Number(cell.dataset.hour));
            }
        });
    }

    if (addButton) {
        addButton.addEventListener('click', function () {
            var now = new Date();
            var day = now.getDay() === 0 ? 7 : now.getDay();
            var hour = Math.max(FIRST_HOUR, Math.min(now.getHours() + 1, LAST_HOUR - 1));
            openCreateForm(day, hour);
        });
    }

    /* ---------- ACTIONS ---------- */

    if (form) {
        form.addEventListener('submit', async function (event) {
            event.preventDefault();
            if (!IS_TRAINER) return;
            showFormError('');

            var data = {
                Id: document.querySelector('#sh-schedule-form [name="Id"]').value,
                Type: typeInput.value,
                Date: dateInput.value,
                TimeStart: startInput.value,
                TimeEnd: endInput.value,
                Capacity: capacityInput.value
            };

            if (!data.Date || !data.TimeStart || !data.TimeEnd) {
                return showFormError('Please complete all date and time fields.');
            }
            if (toHour(data.TimeStart) < WINDOW_START || toHour(data.TimeEnd) > WINDOW_END || toHour(data.TimeEnd) <= toHour(data.TimeStart)) {
                return showFormError('The workout must be between 08:00 and 20:00, and the end time must be later than the start time.');
            }
            if (!Number.isInteger(Number(data.Capacity)) || Number(data.Capacity) < 1 || Number(data.Capacity) > 1000) {
                return showFormError('Capacity must be between 1 and 1000.');
            }

            try {
                await post(URLS.save, data);
                SH.closeModal(modal);
                await load();
            } catch (error) {
                showFormError(error.message);
            }
        });
    }

    if (editButton) {
        editButton.addEventListener('click', function () {
            if (selectedSlot) openEditForm(selectedSlot);
        });
    }

    if (cancelSlotButton) {
        cancelSlotButton.addEventListener('click', async function () {
            if (!selectedSlot || !confirm('Cancel this workout? All registered clients will be notified.')) return;
            try {
                await post(URLS.cancelSlot, { id: selectedSlot.id });
                SH.closeModal(modal);
                await load();
            } catch (error) {
                showDetailError(error.message);
            }
        });
    }

    if (deleteButton) {
        deleteButton.addEventListener('click', async function () {
            if (!editing || !confirm('Delete this workout?')) return;
            try {
                await post(URLS.deleteSlot, { id: editing.id });
                SH.closeModal(modal);
                await load();
            } catch (error) {
                showFormError(error.message);
            }
        });
    }

    if (cancelBookingButton) {
        cancelBookingButton.addEventListener('click', async function () {
            if (!selectedSlot || !confirm('Cancel your booking for this workout?')) return;
            try {
                await post(URLS.cancelBooking, { id: selectedSlot.id });
                SH.closeModal(modal);
                await load();
            } catch (error) {
                showDetailError(error.message);
            }
        });
    }

    /* ---------- INIT ---------- */
    slots = (CONFIG.initialSlots || []).map(normalize);
    renderLegend();
    render();
    load();

})();