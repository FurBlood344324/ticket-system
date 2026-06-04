/**
 * Dashboard Charts – TicketSupport
 * Reads window.dashboardData and renders three Chart.js charts.
 */
(function () {
    'use strict';

    const data = window.dashboardData;
    if (!data) return;

    const fontFamily = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

    // Shared defaults
    Chart.defaults.font.family = fontFamily;
    Chart.defaults.font.size = 13;

    // ── Color Palettes ──
    const categoryColors = ['#3b82f6', '#8b5cf6', '#06b6d4', '#f59e0b', '#10b981', '#ef4444'];
    const priorityColorMap = {
        'Low': '#10b981',
        'Medium': '#3b82f6',
        'High': '#f59e0b',
        'Critical': '#ef4444'
    };

    // ── Helpers ──
    function getColors(labels, colorMap, fallbackPalette) {
        return labels.map(function (label, i) {
            return colorMap[label] || fallbackPalette[i % fallbackPalette.length];
        });
    }

    // ── 1. Category Bar Chart (Horizontal) ──
    (function () {
        var canvas = document.getElementById('categoryChart');
        if (!canvas) return;

        var ctx = canvas.getContext('2d');
        var colors = data.categoryLabels.map(function (_, i) {
            return categoryColors[i % categoryColors.length];
        });

        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.categoryLabels,
                datasets: [{
                    data: data.categoryCounts,
                    backgroundColor: colors,
                    borderRadius: 8,
                    borderSkipped: false,
                    maxBarThickness: 36
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    easing: 'easeOutQuart',
                    duration: 1000
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#1e293b',
                        titleFont: { weight: '600' },
                        cornerRadius: 8,
                        padding: 10
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        grid: { color: 'rgba(0,0,0,0.05)', drawBorder: false },
                        ticks: { precision: 0 }
                    },
                    y: {
                        grid: { display: false },
                        ticks: { font: { weight: '500' } }
                    }
                }
            }
        });
    })();

    // ── 2. Priority Doughnut Chart ──
    (function () {
        var canvas = document.getElementById('priorityChart');
        if (!canvas) return;

        var ctx = canvas.getContext('2d');
        var colors = getColors(data.priorityLabels, priorityColorMap, categoryColors);
        var total = data.priorityCounts.reduce(function (a, b) { return a + b; }, 0);

        // Center text plugin
        var centerTextPlugin = {
            id: 'centerText',
            afterDraw: function (chart) {
                var ctx2 = chart.ctx;
                var meta = chart.getDatasetMeta(0);
                if (!meta || !meta.data || meta.data.length === 0) return;

                var centerX = (chart.chartArea.left + chart.chartArea.right) / 2;
                var centerY = (chart.chartArea.top + chart.chartArea.bottom) / 2;

                ctx2.save();
                ctx2.textAlign = 'center';
                ctx2.textBaseline = 'middle';

                // Total number
                ctx2.font = "800 1.5rem " + fontFamily;
                ctx2.fillStyle = '#0f172a';
                ctx2.fillText(total, centerX, centerY - 10);

                // Label
                ctx2.font = "500 0.75rem " + fontFamily;
                ctx2.fillStyle = '#94a3b8';
                ctx2.fillText('Toplam', centerX, centerY + 14);

                ctx2.restore();
            }
        };

        var labelMap = {
            'Low': 'Düşük',
            'Medium': 'Orta',
            'High': 'Yüksek',
            'Critical': 'Kritik'
        };

        var displayLabels = data.priorityLabels.map(function (l) {
            return labelMap[l] || l;
        });

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: displayLabels,
                datasets: [{
                    data: data.priorityCounts,
                    backgroundColor: colors,
                    borderWidth: 0,
                    spacing: 4,
                    borderRadius: 6,
                    hoverOffset: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '65%',
                animation: {
                    easing: 'easeOutBounce',
                    duration: 1200
                },
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 16,
                            usePointStyle: true,
                            pointStyle: 'circle',
                            font: { weight: '500' }
                        }
                    },
                    tooltip: {
                        backgroundColor: '#1e293b',
                        cornerRadius: 8,
                        padding: 10
                    }
                }
            },
            plugins: [centerTextPlugin]
        });
    })();

    // ── 3. Trend Line Chart ──
    (function () {
        var canvas = document.getElementById('trendChart');
        if (!canvas) return;

        var ctx = canvas.getContext('2d');

        // Create gradient fill
        var gradient = ctx.createLinearGradient(0, 0, 0, 220);
        gradient.addColorStop(0, 'rgba(59, 130, 246, 0.25)');
        gradient.addColorStop(1, 'rgba(59, 130, 246, 0.01)');

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.trendLabels,
                datasets: [{
                    label: 'Talepler',
                    data: data.trendCounts,
                    borderColor: '#3b82f6',
                    backgroundColor: gradient,
                    borderWidth: 2.5,
                    tension: 0.4,
                    fill: true,
                    pointBackgroundColor: '#fff',
                    pointBorderColor: '#3b82f6',
                    pointBorderWidth: 2,
                    pointRadius: 5,
                    pointHoverRadius: 8,
                    pointHoverBackgroundColor: '#3b82f6',
                    pointHoverBorderColor: '#fff',
                    pointHoverBorderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    easing: 'easeOutQuart',
                    duration: 1000
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#1e293b',
                        cornerRadius: 8,
                        padding: 10,
                        callbacks: {
                            title: function (items) {
                                return items[0].label;
                            },
                            label: function (item) {
                                return ' ' + item.parsed.y + ' talep';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { font: { weight: '500' } }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: 'rgba(0,0,0,0.05)', drawBorder: false },
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    })();
})();
