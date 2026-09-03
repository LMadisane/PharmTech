// dashboard.js - Chart.js initializations for the analytics dashboard
// Stock levels bar chart
function renderStockChart(canvasId, labels, data) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [{
                label: 'Stock Quantity',
                data,
                backgroundColor: 'rgba(59, 130, 246, 0.6)', // Tailwind blue-500
                borderColor: 'rgba(59, 130, 246, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: false },
                title: {
                    display: true,
                    text: 'Current Stock Levels'
                }
            },
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
}

// Prescription status doughnut chart
function renderPrescriptionChart(canvasId, dispensed, pending) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Dispensed', 'Pending'],
            datasets: [{
                data: [dispensed, pending],
                backgroundColor: [
                    'rgba(34, 197, 94, 0.7)',  // Tailwind green-500
                    'rgba(234, 179, 8, 0.7)'   // Tailwind yellow-500
                ],
                borderColor: [
                    'rgba(34, 197, 94, 1)',
                    'rgba(234, 179, 8, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: 'bottom' },
                title: {
                    display: true,
                    text: 'Prescription Status'
                }
            }
        }
    });
}

// Drug returns bar chart
function renderReturnsChart(canvasId, labels, data) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [{
                label: 'Total Returned',
                data,
                backgroundColor: 'rgba(239, 68, 68, 0.6)', // Tailwind red-500
                borderColor: 'rgba(239, 68, 68, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: false },
                title: {
                    display: true,
                    text: 'Drug Returns by Medicine'
                }
            },
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
}