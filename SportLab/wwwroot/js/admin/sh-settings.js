(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        // --- Tab Switching ---
        const tabs = document.querySelectorAll('.sh-tab');
        const panels = document.querySelectorAll('[data-panel]');
        const forms = document.querySelectorAll('.sh-form[data-panel]');

        tabs.forEach(tab => {
            tab.addEventListener('click', function () {
                const target = this.dataset.tab;

                tabs.forEach(t => t.classList.toggle('is-active', t.dataset.tab === target));

                forms.forEach(f => {
                    if (f.tagName === 'FORM') f.hidden = f.dataset.panel !== target;
                });
                panels.forEach(p => {
                    if (p.tagName !== 'FORM') p.hidden = p.dataset.panel !== target;
                });
            });
        });

        // --- Helper Functions ---
        function showMsg(elId, text, isSuccess) {
            const el = document.getElementById(elId);
            if (!el) return;
            el.textContent = text;
            el.style.color = isSuccess ? '#AAFF00' : '#FF5252';
            el.classList.remove('hidden');
            setTimeout(() => el.classList.add('hidden'), 4000);
        }

        // --- Load Account Data ---
        async function loadAdminAccount() {
            try {
                const res = await fetch('/Profile/GetAccount');
                const data = await res.json();
                if (data.success) {
                    document.getElementById('admin-acc-name').value = data.name;
                    document.getElementById('admin-acc-email').value = data.email;
                }
            } catch (e) {
                console.error('Failed to load admin account', e);
            }
        }

        // --- Submit Account ---
        document.getElementById('admin-account-form')?.addEventListener('submit', async function (e) {
            e.preventDefault();
            const btn = document.getElementById('admin-btn-save-acc');
            btn.disabled = true;

            const payload = {
                name: document.getElementById('admin-acc-name').value.trim(),
                email: document.getElementById('admin-acc-email').value.trim()
            };

            try {
                const res = await fetch('/Profile/UpdateAccountSettings', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                    body: JSON.stringify(payload)
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    showMsg('admin-acc-msg', 'Account updated successfully.', true);
                } else {
                    showMsg('admin-acc-msg', data.message || 'Failed to update account.', false);
                }
            } catch (err) {
                showMsg('admin-acc-msg', 'Network error.', false);
            } finally {
                btn.disabled = false;
            }
        });

        // --- Submit Password ---
        document.getElementById('admin-security-form')?.addEventListener('submit', async function (e) {
            e.preventDefault();
            const btn = document.getElementById('admin-btn-save-pw');
            const newPw = document.getElementById('admin-pw-new').value;
            const confirmPw = document.getElementById('admin-pw-confirm').value;

            if (newPw !== confirmPw) {
                showMsg('admin-pw-msg', 'Passwords do not match.', false);
                return;
            }

            btn.disabled = true;
            const payload = {
                currentPassword: document.getElementById('admin-pw-current').value,
                newPassword: newPw,
                confirmPassword: confirmPw
            };

            try {
                const res = await fetch('/Profile/UpdatePassword', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                    body: JSON.stringify(payload)
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    showMsg('admin-pw-msg', 'Password updated successfully.', true);
                    document.getElementById('admin-security-form').reset();
                } else {
                    showMsg('admin-pw-msg', data.message || 'Failed to update password.', false);
                }
            } catch (err) {
                showMsg('admin-pw-msg', 'Network error.', false);
            } finally {
                btn.disabled = false;
            }
        });

        // Init
        loadAdminAccount();
    });
})();