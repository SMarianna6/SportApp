/* Dashboard page. Fetches real data and handles interactive controls. */
(function () {
    'use strict';
    var SH = window.SH;

    var MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    var W = 700, H = 220;

    var revenue = [];
    var newUsers = [];

    var showRevenue = true;
    var showUsers = true;
    var currentYear = new Date().getFullYear();

    fetch('/Admin/GetDashboardData')
        .then(function (res) { return res.json(); })
        .then(function (data) {
            renderStats(data.stats);
            renderEvents(data.events);

            revenue = data.chart.revenue;
            newUsers = data.chart.newUsers;

            drawChart();
            bindControls();
        })
        .catch(function (err) {
            console.error("Failed to load dashboard data", err);
            document.getElementById('sh-stats').innerHTML = '<p style="color:red">Failed to load data.</p>';
        });

    function renderStats(stats) {
        var html = `
            <div class="sh-card sh-stat">
                <p class="sh-stat-label">Revenue</p>
                <div class="sh-stat-row">
                    <span class="sh-stat-value">₴${stats.revenue.toLocaleString()}</span>
                    <span class="sh-trend">${SH.icon.trend('sh-ico-sm')} +5.2%</span>
                </div>
            </div>
            <div class="sh-card sh-stat">
                <p class="sh-stat-label">Active Members</p>
                <div class="sh-stat-row">
                    <span class="sh-stat-value">${stats.activeMembers.toLocaleString()}</span>
                    <span class="sh-trend">${SH.icon.trend('sh-ico-sm')} +3.1%</span>
                </div>
            </div>
            <div class="sh-card sh-stat">
                <p class="sh-stat-label">Class Bookings</p>
                <div class="sh-stat-row">
                    <span class="sh-stat-value">${stats.classBookings.toLocaleString()}</span>
                    <span class="sh-trend">${SH.icon.trend('sh-ico-sm')} +8.7%</span>
                </div>
            </div>
            <div class="sh-card sh-stat">
                <p class="sh-stat-label">Shop Sales</p>
                <div class="sh-stat-row">
                    <span class="sh-stat-value">₴${stats.shopSales.toLocaleString()}</span>
                    <span class="sh-trend">${SH.icon.trend('sh-ico-sm')} +2.4%</span>
                </div>
            </div>
        `;
        document.getElementById('sh-stats').innerHTML = html;
    }

    function renderEvents(events) {
        var html = events.map(function (e) {
            return '<li><button type="button" class="sh-event"><span class="sh-event-dot"></span><span>' +
                '<span class="sh-event-text">' + SH.esc(e.text) + '</span><span class="sh-event-time">' + SH.esc(e.time) + '</span></span></button></li>';
        }).join('');
        document.getElementById('sh-events').innerHTML = html;
    }

    function drawChart() {
        if (revenue.length === 0) return;

        function linePath(data) {
            var max = Math.max.apply(null, data) || 1;
            return data.map(function (v, i) {
                return (i === 0 ? 'M' : 'L') + (i / (data.length - 1)) * W + ',' + (H - (v / max) * H);
            }).join(' ');
        }
        function x(i) { return (i / 11) * W; }

        var svg = document.getElementById('sh-chart');
        var tooltip = document.getElementById('sh-tooltip');
        svg.setAttribute('viewBox', '0 0 ' + W + ' ' + (H + 24));

        var html = '<defs>' +
            '<linearGradient id="shGN" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stop-color="#AAFF00" stop-opacity=".3"/><stop offset="100%" stop-color="#AAFF00" stop-opacity="0"/></linearGradient>' +
            '<linearGradient id="shGV" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stop-color="#7D51FE" stop-opacity=".35"/><stop offset="100%" stop-color="#7D51FE" stop-opacity="0"/></linearGradient></defs>';

        [0, 55, 110, 165, 220].forEach(function (y) {
            html += '<line x1="0" x2="' + W + '" y1="' + y + '" y2="' + y + '" stroke="#fff" stroke-opacity=".07"/>';
        });

        html += '<g id="layer-revenue" style="display: ' + (showRevenue ? 'block' : 'none') + '">';
        html += '<path d="' + linePath(revenue) + ' L' + W + ',' + H + ' L0,' + H + 'Z" fill="url(#shGN)"/>';
        html += '<path d="' + linePath(revenue) + '" fill="none" stroke="#AAFF00" stroke-width="2.5"/>';
        html += '</g>';

        html += '<g id="layer-users" style="display: ' + (showUsers ? 'block' : 'none') + '">';
        html += '<path d="' + linePath(newUsers) + ' L' + W + ',' + H + ' L0,' + H + 'Z" fill="url(#shGV)"/>';
        html += '<path d="' + linePath(newUsers) + '" fill="none" stroke="#7D51FE" stroke-width="2.5"/>';
        html += '</g>';

        MONTHS.forEach(function (m, i) {
            var anchor = i === 0 ? 'start' : i === 11 ? 'end' : 'middle';
            html += '<text x="' + x(i) + '" y="' + (H + 18) + '" fill="#fff" fill-opacity=".4" font-size="11" text-anchor="' + anchor + '">' + m + '</text>';
            html += '<rect data-i="' + i + '" x="' + (x(i) - W / 22) + '" y="0" width="' + W / 11 + '" height="' + H + '" fill="transparent" style="cursor: crosshair;"/>';
        });

        html += '<g id="sh-hover"></g>';
        svg.innerHTML = html;

        var hoverLayer = document.getElementById('sh-hover');

        function show(i) {
            var maxRev = Math.max.apply(null, revenue) || 1;
            var maxUsr = Math.max.apply(null, newUsers) || 1;

            var hoverHtml = '<line x1="' + x(i) + '" x2="' + x(i) + '" y1="0" y2="' + H + '" stroke="#fff" stroke-opacity=".25"/>';
            var tooltipHtml = '<p style="font-weight:700;text-transform:uppercase;color:#f2f2f4">' + MONTHS[i] + '</p>';

            if (showRevenue) {
                hoverHtml += '<circle cx="' + x(i) + '" cy="' + (H - revenue[i] * H / maxRev) + '" r="5" fill="#AAFF00"/>';
                tooltipHtml += '<p style="color:#aaff00">Revenue: ₴' + revenue[i].toLocaleString() + '</p>';
            }
            if (showUsers) {
                hoverHtml += '<circle cx="' + x(i) + '" cy="' + (H - newUsers[i] * H / maxUsr) + '" r="5" fill="#7D51FE"/>';
                tooltipHtml += '<p style="color:#7d51fe">New users: ' + newUsers[i] + '</p>';
            }

            hoverLayer.innerHTML = hoverHtml;
            tooltip.innerHTML = tooltipHtml;
            tooltip.hidden = false;
        }
        function hide() {
            hoverLayer.innerHTML = '';
            tooltip.hidden = true;
        }

        var newSvg = svg.cloneNode(true);
        svg.parentNode.replaceChild(newSvg, svg);
        svg = newSvg;
        var newHoverLayer = svg.querySelector('#sh-hover');

        svg.addEventListener('mouseover', function (e) {
            var rect = e.target.closest('rect[data-i]');
            if (rect) show(Number(rect.getAttribute('data-i')));
        });
        svg.addEventListener('mouseleave', hide);
    }

    function bindControls() {
        var btnRevenue = document.querySelector('.sh-select-btn.sh-n');
        var btnUsers = document.querySelector('.sh-select-btn.sh-v');

        btnRevenue.addEventListener('click', function () {
            showRevenue = !showRevenue;

            this.style.opacity = showRevenue ? '1' : '0.5';
            this.querySelector('.sh-dot-n').style.opacity = showRevenue ? '1' : '0.3';

            var layer = document.getElementById('layer-revenue');
            if (layer) layer.style.display = showRevenue ? 'block' : 'none';
        });

        btnUsers.addEventListener('click', function () {
            showUsers = !showUsers;

            this.style.opacity = showUsers ? '1' : '0.5';
            this.querySelector('.sh-dot-v').style.opacity = showUsers ? '1' : '0.3';

            var layer = document.getElementById('layer-users');
            if (layer) layer.style.display = showUsers ? 'block' : 'none';
        });

        var btnYear = document.querySelector('.sh-select-btn.sh-dim');
        btnYear.addEventListener('click', function () {
        });
    }
})();