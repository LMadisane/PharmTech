// auth.js - Handles session timeout warnings and role-based UI visibility
// Session timeout warning, warns user 2 minutes before session expires

function sessionTimer() {
    return {
        warningVisible: false,
        // Session is set to 1 hour in Program.cs, warn at 58 minutes
        timeoutDuration: 58 * 60 * 1000,
        timer: null,

        init() {
            this.resetTimer();

            // Reset timer on any user activity
            ['mousemove', 'keydown', 'click', 'scroll'].forEach(event => {
                document.addEventListener(event, () => this.resetTimer());
            });
        },

        resetTimer() {
            clearTimeout(this.timer);
            this.warningVisible = false;
            this.timer = setTimeout(() => {
                // Show warning, session about to expire
                this.warningVisible = true;
            }, this.timeoutDuration);
        },

        // Extend session by making a lightweight API call
        async extendSession() {
            await apiFetch('/account/ping');
            this.resetTimer();
        },

        // Logout immediately
        logout() {
            window.location.href = '/account/logout';
        }
    };
}

// Hide or show elements based on user role
document.addEventListener('DOMContentLoaded', () => {
    const userRole = document.body.dataset.userRole;

    document.querySelectorAll('[data-role]').forEach(el => {
        const allowedRoles = el.dataset.role.split(',').map(r => r.trim());
        if (!allowedRoles.includes(userRole)) {
            el.style.display = 'none';
        }
    });
});