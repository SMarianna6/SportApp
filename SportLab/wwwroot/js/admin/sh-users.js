/* Users page. Add/edit/delete/toggle only change the in-memory demo array (no backend).
   MVC integration point: replace the marked spots with fetch()/form posts to your own actions. */
(function () {
    'use strict';
    var SH = window.SH;
    var L = window.SH_LOC || {};

    var users = [];

    function roleLabel(role) {
        var map = {
            Client: L.roleClient,
            Trainer: L.roleTrainer,
            Administrator: L.roleAdministrator,
            Admin: L.roleAdmin,
            Moderator: L.roleModerator
        };
        return map[role] || role;
    }
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
        count.textContent = (L.registeredAccounts || '{0} registered accounts').replace('{0}', users.length);
        body.innerHTML = users.map(function (u) {
            return '<tr data-id="' + u.id + '">' +
                '<td class="sh-strong">' + SH.esc(u.name) + '</td>' +
                '<td>' + SH.esc(u.email) + '</td>' +
                '<td>' + SH.esc(roleLabel(u.role)) + '</td>' +
                '<td><span class="sh-badge-violet">' + SH.esc(u.tariff) + '</span></td>' +
                '<td><button type="button" class="sh-status ' + (u.active ? 'is-active' : 'is-inactive') + '" data-act="toggle" title="' + SH.esc(L.toggleStatus || 'Toggle status') + '">' + (u.active ? SH.esc(L.active || 'Active') : SH.esc(L.inactive || 'Inactive')) + '</button></td>' +
                '<td><div class="sh-actions">' +
                '<button type="button" class="sh-icon-btn" data-act="edit" aria-label="' + SH.esc((L.editLabel || 'Edit: {0}').replace('{0}', u.name)) + '">' + SH.icon.pencil() + '</button>' +
                '<button type="button" class="sh-icon-btn sh-danger" data-act="delete" aria-label="' + SH.esc((L.deleteLabel || 'Delete: {0}').replace('{0}', u.name)) + '">' + SH.icon.trash() + '</button>' +
                '</div></td></tr>';
        }).join('');
    }

    if (typeof $ !== 'undefined' && form) {
        $(form)
            .off('.shLiveValidation')
            .on('input.shLiveValidation change.shLiveValidation', 'input, textarea, select', function () {
                $(this).valid();
            });
    }

    function openForm(user) {
        editing = user || null;

        if (form) {
            form.reset();

            var f = form.elements;

            if (f.Name) f.Name.value = user ? user.name : '';
            if (f.Email) f.Email.value = user ? user.email : '';

            if (f.Password) {
                f.Password.value = '';
                f.Password.placeholder = user ? (L.leaveBlankToKeep || 'Leave blank to keep') : '';
            }

            if (f.Role) {
                f.Role.value = user ? user.role : 'Client';
            }

            if (f.tariff) {
                f.tariff.value = (user && user.tariff && user.tariff !== 'No Plan')
                    ? user.tariff
                    : (tariffNames[0] || 'Basic Plan');
            }

            // CLEAR PREVIOUS VALIDATION ERRORS
            if (typeof $ !== 'undefined') {
                var validator = $(form).validate();

                validator.resetForm();

                // PASSWORD IS REQUIRED ONLY WHEN CREATING A USER
                if (user) {
                    $(f.Password).prop('required', false);
                    $(f.Password).rules('remove', 'required');
                } else {
                    $(f.Password).prop('required', true);
                    $(f.Password).rules('add', {
                        required: true,
                        messages: {
                            required: L.passwordRequired || 'Password is required'
                        }
                    });
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

        if (title) {
            title.textContent = user ? (L.editUser || 'Edit User') : (L.addUser || 'Add User');
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
            if (confirm(L.deleteUserConfirm || 'Are you sure you want to delete this user?')) {
                await fetch('/Admin/DeleteUserApi?id=' + id, { method: 'POST', headers: { 'RequestVerificationToken': SH.csrf() } });
                loadUsers();
            }
        }
        if (act === 'toggle') {
            await fetch('/Admin/ToggleUserApi?id=' + id, { method: 'POST', headers: { 'RequestVerificationToken': SH.csrf() } });
            loadUsers();
        }
    });

    if (form) {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            // JQUERY UNOBTRUSIVE VALIDATION
            if (typeof $ !== 'undefined' && $(form).valid && !$(form).valid()) {
                return;
            }

            var f = form.elements;

            var userData = {
                Id: editing ? editing.id : 0,
                Name: f.Name ? f.Name.value.trim() : '',
                Email: f.Email ? f.Email.value.trim() : '',
                Role: f.Role ? f.Role.value : 'Client',
                Password: f.Password ? f.Password.value : '',
                Tariff: f.tariff ? f.tariff.value : ''
            };

            await fetch('/Admin/SaveUserApi', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': SH.csrf() },
                body: JSON.stringify(userData)
            });

            SH.closeModal('sh-user-modal');
            loadUsers();
        });

        if (typeof $ !== 'undefined') {
            $(form).on('input change focusout', 'input, textarea, select', function () {
                $(this).valid();
            });
        }
    }

    loadUsers();

    var urlParams = new URLSearchParams(window.location.search);
    var openUserId = urlParams.get('openUser');

    if (openUserId) {
        var userToEdit = users.filter(function (u) { return u.id == openUserId; })[0];

        if (userToEdit) {

            window.history.replaceState({}, document.title, window.location.pathname);
        }
    }

    render();
})();
