/* Dashboard page. Data below is DEMO DATA from the original prototype.
   MVC integration point: replace with values rendered from your view model / an API call. */
(function () {
    'use strict';
    var SH = window.SH;

    var stats = [
        { label: 'Revenue', value: '$128,430', trend: '+5.2%' },
        { label: 'Active Members', value: '1,284', trend: '+3.1%' },
        { label: 'Class Bookings', value: '3,902', trend: '+8.7%' },
        { label: 'Shop Sales', value: '$9,640', trend: '+2.4%' }
    ];
    var events = [
        { text: 'Ivan bought a Pro Plan', time: '2 min ago' },
        { text: 'Anna cancelled a class', time: '14 min ago' },
        { text: 'New member registered: Daniel C.', time: '41 min ago' },
        { text: 'Order #1042 paid: Whey Protein 2kg', time: '1 h ago' },
        { text: 'Lifting Straps stock is running low', time: '3 h ago' }
    ];
    var revenue = [32, 38, 35, 46, 52, 49, 61, 66, 63, 78, 84, 92];
    var newUsers = [18, 22, 30, 26, 38, 42, 40, 55, 52, 61, 58, 72];
    var MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    var W = 700, H = 220;

    document.getElementById('sh-stats').innerHTML = stats.map(function (s) {
        return '<div class="sh-card sh-stat"><p class="sh-stat-label">' + SH.esc(s.label) + '</p>' +
            '<div class="sh-stat-row"><span class="sh-stat-value">' + SH.esc(s.value) + '</span>' +
            '<span class="sh-trend">' + SH.icon.trend('sh-ico-sm') + SH.esc(s.trend) + '</span></div></div>';
    }).join('');

    document.getElementById('sh-events').innerHTML = events.map(function (e) {
        return '<li><button type="button" class="sh-event"><span class="sh-event-dot"></span><span>' +
            '<span class="sh-event-text">' + SH.esc(e.text) + '</span><span class="sh-event-time">' + SH.esc(e.time) + '</span></span></button></li>';
    }).join('');

    function linePath(data) {
        return data.map(function (v, i) {
            return (i === 0 ? 'M' : 'L') + (i / (data.length - 1)) * W + ',' + (H - (v / 100) * H);
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
    html += '<path d="' + linePath(newUsers) + ' L' + W + ',' + H + ' L0,' + H + 'Z" fill="url(#shGV)"/>';
    html += '<path d="' + linePath(revenue) + ' L' + W + ',' + H + ' L0,' + H + 'Z" fill="url(#shGN)"/>';
    html += '<path d="' + linePath(newUsers) + '" fill="none" stroke="#7D51FE" stroke-width="2.5"/>';
    html += '<path d="' + linePath(revenue) + '" fill="none" stroke="#AAFF00" stroke-width="2.5"/>';
    MONTHS.forEach(function (m, i) {
        var anchor = i === 0 ? 'start' : i === 11 ? 'end' : 'middle';
        html += '<text x="' + x(i) + '" y="' + (H + 18) + '" fill="#fff" fill-opacity=".4" font-size="11" text-anchor="' + anchor + '">' + m + '</text>';
        html += '<rect data-i="' + i + '" x="' + (x(i) - W / 22) + '" y="0" width="' + W / 11 + '" height="' + H + '" fill="transparent"/>';
    });
    html += '<g id="sh-hover"></g>';
    svg.innerHTML = html;

    var hoverLayer = document.getElementById('sh-hover');

    function show(i) {
        hoverLayer.innerHTML =
            '<line x1="' + x(i) + '" x2="' + x(i) + '" y1="0" y2="' + H + '" stroke="#fff" stroke-opacity=".25"/>' +
            '<circle cx="' + x(i) + '" cy="' + (H - revenue[i] * H / 100) + '" r="5" fill="#AAFF00"/>' +
            '<circle cx="' + x(i) + '" cy="' + (H - newUsers[i] * H / 100) + '" r="5" fill="#7D51FE"/>';
        tooltip.innerHTML = '<p style="font-weight:700;text-transform:uppercase;color:#f2f2f4">' + MONTHS[i] + '</p>' +
            '<p style="color:#aaff00">Revenue: $' + revenue[i] + 'k</p>' +
            '<p style="color:#7d51fe">New users: ' + newUsers[i] * 4 + '</p>';
        tooltip.hidden = false;
    }
    function hide() {
        hoverLayer.innerHTML = '';
        tooltip.hidden = true;
    }

    svg.addEventListener('mouseover', function (e) {
        var rect = e.target.closest('rect[data-i]');
        if (rect) show(Number(rect.getAttribute('data-i')));
    });
    svg.addEventListener('mouseleave', hide);
})();
