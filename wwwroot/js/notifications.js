// notifications.js - Alpine.js stores for notifications and toasts

// Notification bell store - shows in-app notifications
function notificationStore() {
    return {
        showNotifications: false,
        notifications: [],
        unreadCount: 0,

        // Toggle notification dropdown
        toggleNotifications() {
            this.showNotifications = !this.showNotifications;
            if (this.showNotifications) this.markAllRead();
        },

        // Add a new notification
        addNotification(message) {
            this.notifications.unshift({
                id: Date.now(),
                message,
                read: false
            });
            this.unreadCount++;
        },

        // Mark all notifications as read
        markAllRead() {
            this.notifications.forEach(n => n.read = true);
            this.unreadCount = 0;
        },

        // Load notifications on init - checks for low stock warnings
        async init() {
            try {
                const data = await apiFetch('/api/inventory/lowstock');
                if (data && data.length > 0) {
                    data.forEach(item => {
                        this.addNotification(`⚠️ Low stock: ${item.medicine} (${item.quantity} remaining)`);
                    });
                }
            } catch (err) {
                console.error('Failed to load notifications:', err);
            }
        }
    };
}

// Toast store - shows temporary popup messages
function toastStore() {
    return {
        toasts: [],

        // Listen for global show-toast events triggered by showToast() in site.js
        init() {
            window.addEventListener('show-toast', (e) => {
                this.addToast(e.detail.message, e.detail.type);
            });
        },

        // Add a toast and auto-remove after 4 seconds
        addToast(message, type = 'info') {
            const id = Date.now();
            this.toasts.push({ id, message, type });
            setTimeout(() => this.removeToast(id), 4000);
        },

        // Manually remove a toast
        removeToast(id) {
            this.toasts = this.toasts.filter(t => t.id !== id);
        }
    };
}