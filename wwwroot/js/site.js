// site.js - Global utilities shared across all pages
// Format a date string into a readable format
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-ZA', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

// Format a datetime string into a readable format
function formatDateTime(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleString('en-ZA', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

/*  Global fetch override to automatically include the 
    Anti-forgery token for POST, PUT, DELETE, PATCH requests.
    This ensures CSRF protection works with AJAX calls.*/
(function () {
    const originalFetch = window.fetch;

    window.fetch = function (url, options = {}) {
        const method = (options.method || 'GET').toUpperCase();

        // Only add token for state-changing methods
        if (['POST', 'PUT', 'DELETE', 'PATCH'].includes(method)) {
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            const token = tokenInput ? tokenInput.value : null;

            if (token) {
                options.headers = options.headers || {};
                // Headers could be a Headers object or plain object
                if (options.headers instanceof Headers) {
                    options.headers.set('RequestVerificationToken', token);
                } else {
                    options.headers['RequestVerificationToken'] = token;
                }
            }
        }

        return originalFetch(url, options);
    };
})();

// Global toast trigger (works with Alpine.js toastStore)
function showToast(message, type = 'info') {
    window.dispatchEvent(new CustomEvent('show-toast', {
        detail: { message, type }
    }));
}

// Get a status badge color class based on status string
function statusColor(status) {
    switch (status?.toLowerCase()) {
        case 'pending': return 'bg-yellow-100 text-yellow-800';
        case 'approved': return 'bg-green-100 text-green-800';
        case 'rejected': return 'bg-red-100 text-red-800';
        case 'dispensed': return 'bg-blue-100 text-blue-800';
        case 'fulfilled': return 'bg-purple-100 text-purple-800';
        default: return 'bg-gray-100 text-gray-800';
    }
}