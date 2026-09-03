// pharmacy.js - Shared UI logic for pharmacy operations
// Generic table filter by search input
function tableFilter() {
    return {
        search: '',

        // Filter table rows based on search input
        filterRows() {
            const rows = document.querySelectorAll('[data-table-row]');
            rows.forEach(row => {
                const text = row.textContent.toLowerCase();
                row.style.display = text.includes(this.search.toLowerCase()) ? '' : 'none';
            });
        }
    };
}

// Status badge renderer - returns Tailwind classes based on status
function statusBadge(status) {
    const colors = {
        'Pending': 'bg-yellow-100 text-yellow-800',
        'Approved': 'bg-green-100 text-green-800',
        'Rejected': 'bg-red-100 text-red-800',
        'Dispensed': 'bg-blue-100 text-blue-800',
        'Fulfilled': 'bg-purple-100 text-purple-800'
    };
    return colors[status] || 'bg-gray-100 text-gray-800';
}

// Confirm dialog before destructive actions
function confirmAction(message, callback) {
    if (window.confirm(message)) callback();
}

// Alpine.js data store for any form with loading state
function formStore() {
    return {
        loading: false,
        error: null,
        success: null,

        // Submit a form to the API
        async submit(url, method, body) {
            this.loading = true;
            this.error = null;
            this.success = null;

            const result = await apiFetch(url, method, body);

            this.loading = false;

            if (result) {
                this.success = 'Operation completed successfully.';
                showToast(this.success, 'success');
            } else {
                this.error = 'Something went wrong. Please try again.';
                showToast(this.error, 'error');
            }

            return result;
        }
    };
}