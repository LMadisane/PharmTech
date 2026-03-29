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

// Generic fetch wrapper for API calls
async function apiFetch(url, method = 'GET', body = null) {
    const options = {
        method,
        headers: { 'Content-Type': 'application/json' }
    };

    if (body) options.body = JSON.stringify(body);

    try {
        const response = await fetch(url, options);

        // Handle unauthorized or forbidden responses
        if (response.status === 401) {
            window.location.href = '/account/login';
            return null;
        }

        if (response.status === 403) {
            showToast('You do not have permission to perform this action.', 'error');
            return null;
        }

        return await response.json();
    } catch (err) {
        console.error('API error:', err);
        showToast('An unexpected error occurred.', 'error');
        return null;
    }
}

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