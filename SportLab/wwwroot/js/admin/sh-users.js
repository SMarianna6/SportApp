/* Users page. Add/edit/delete/toggle only change the in-memory demo array (no backend).
   MVC integration point: replace the marked spots with fetch()/form posts to your own actions. */
(function () {
    'use strict';
    var SH = window.SH;

    var users = [];
    var tariffDataEl = document.getElementById('sh-tariffs-data');
    var tariffNames = tariffDataEl ? JSON.parse(tariffDataEl.textContent) : ['Basic', 'Pro', 'Elite'];
    var editing = null;

    var body = document.getElementById('sh-users-body');
    var count = document.getElementById('sh-users-count');
    var form = document.getElementById('sh-user-form');
    var title = document.getElementById('sh-user-modal-title');
    var tariffSelect = document.getElementById('sh-user-tariff');

    tariffSelect.innerHTML = tariffNames.concat(['Staff']).map(function (t) {
        return '<option>' + SH.esc(t) + '</option>';
    }).join('');

    async function loadUsers() {
        try {
            const response = await fetch('/Admin/GetUsersList');
            users = await response.json();
            render();
        } catch (error) {
            console.error("Error loading users:", error);
        }
    }

    function render() {
        count.textContent = users.length + ' registered accounts';
        body.innerHTML = users.map(function (u) {
            return '<tr data-id="' + u.id + '">' +
                '<td class="sh-strong">' + SH.esc(u.name) + '</td>' +
                '<td>' + SH.esc(u.email) + '</td>' +
                '<td>' + SH.esc(u.role) + '</td>' +
                '<td><span class="sh-badge-violet">' + SH.esc(u.tariff) + '</span></td>' +
                '<td><button type="button" class="sh-status ' + (u.active ? 'is-active' : 'is-inactive') + '" data-act="toggle" title="Toggle status">' + (u.active ? 'Active' : 'Inactive') + '</button></td>' +
                '<td><div class="sh-actions">' +
                '<button type="button" class="sh-icon-btn" data-act="edit" aria-label="Edit ' + SH.esc(u.name) + '">' + SH.icon.pencil() + '</button>' +
                '<button type="button" class="sh-icon-btn sh-danger" data-act="delete" aria-label="Delete ' + SH.esc(u.name) + '">' + SH.icon.trash() + '</button>' +
                '</div></td></tr>';
        }).join('');
    }

    function openForm(user) {
        editing = user;
        title.textContent = user ? 'Edit User' : 'Add User';
        if (form.elements.name) form.elements.name.value = user ? user.name : '';
        if (form.elements.email) form.elements.email.value = user ? user.email : '';
        if (form.elements.password) {
            form.elements.password.value = '';
            form.elements.password.required = !user;
            form.elements.password.placeholder = user ? 'Leave blank to keep' : '';
        }

        if (form.elements.role) {
            form.elements.role.value = user ? user.role : 'Member';
        }

        if (form.elements.tariff) {
            form.elements.tariff.value = (user && user.tariff && user.tariff !== 'No Plan') ? user.tariff : (tariffNames[0] || 'Basic Plan');
        }
        SH.openModal('sh-user-modal');
    }

    document.getElementById('sh-user-add').addEventListener('click', function () { openForm(null); });

    body.addEventListener('click', async function (e) {
        var btn = e.target.closest('[data-act]');
        if (!btn) return;
        var id = Number(btn.closest('tr').getAttribute('data-id'));
        var user = users.filter(function (u) { return u.id === id; })[0];
        var act = btn.getAttribute('data-act');
        if (act === 'edit') openForm(user);
        if (act === 'delete') {
            if (confirm('Are you sure you want to delete this user?')) {
                await fetch('/Admin/DeleteUserApi?id=' + id, { method: 'POST' });
                loadUsers();
            }
        }
        if (act === 'toggle') {
            await fetch('/Admin/ToggleUserApi?id=' + id, { method: 'POST' });
            loadUsers();
        }
    });

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        var f = form.elements;
        var userData = {
            Id: editing ? editing.id : 0,
            Name: f.name ? f.name.value : '',
            Email: f.email ? f.email.value : '',
            Role: f.role ? f.role.value : 'Member',
            Password: f.password ? f.password.value : '',
            Tariff: f.tariff ? f.tariff.value : ''
        };

        await fetch('/Admin/SaveUserApi', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(userData)
        });

        SH.closeModal('sh-user-modal');
        loadUsers();
    });

    loadUsers();

    var urlParams = new URLSearchParams(window.location.search);
    var openUserId = urlParams.get('openUser');

    if (openUserId) {
        var userToEdit = users.filter(function (u) { return u.id == openUserId; })[0];

        if (userToEdit) {
            openForm(userToEdit);

            window.history.replaceState({}, document.title, window.location.pathname);
        }
    }

    render();
})();
