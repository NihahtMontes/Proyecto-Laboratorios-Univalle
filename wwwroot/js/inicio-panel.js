/* ════════════════════════════════════════════════════════════════════
   Inicio · Panel de control (InicioPanel)
   - Bandeja de pendientes: posponer (calculados) y marcar leídos (persistidos),
     con contadores sincronizados (pestaña, KPI y tarjeta del Resumen).
   - Tabla de la semana: plegar días pasados.
   - Gráficos Chart.js 3.9.1: se crean todos al abrir por primera vez la pestaña
     «graficos» (PanelTabs.whenShown) con un único fetch del handler Indicadores.
   ════════════════════════════════════════════════════════════════════ */
$(function () {
    'use strict';

    var $panel = $('.ip-panel');
    if ($panel.length === 0) return;

    // ─── Datos embebidos por el servidor (System.Text.Json) ───
    var data = {};
    try {
        data = JSON.parse($('#ip-data').text() || '{}');
    } catch (err) {
        console.error('Inicio: datos del panel ilegibles:', err);
    }

    var REVIEW_GROUP = 'revisar-datos';
    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function escapeHtml(value) {
        return $('<div>').text(value == null ? '' : String(value)).html();
    }

    function fadeOut($el, done) {
        if (reduceMotion || !$.fn.fadeOut) { $el.remove(); if (done) done(); return; }
        $el.fadeOut(180, function () { $el.remove(); if (done) done(); });
    }

    // ════════════════ 1. Bandeja de pendientes ════════════════

    // El layout define window.LabNotifications DESPUÉS de esta sección: se espera un ciclo
    // (mismo criterio que Pages/Notifications/Index.cshtml).
    setTimeout(function hideSnoozed() {
        if (!window.LabNotifications) return;
        $('.ip-feed__item[data-synthetic-key]').each(function () {
            var key = $(this).attr('data-synthetic-key');
            if (key && window.LabNotifications.isSnoozed(key)) {
                $(this).hide(); // Oculto hoy: los contadores siguen coincidiendo con la campana.
            }
        });
    }, 0);

    function decrement($badge) {
        var value = parseInt($badge.first().text(), 10);
        if (!isNaN(value) && value > 0) {
            $badge.first().text(String(value - 1));
        }
    }

    function currentValue($badge) {
        var value = parseInt($badge.first().text(), 10);
        return isNaN(value) ? 0 : value;
    }

    // Quita el aviso de la vista previa del Resumen y, si ya no queda nada, muestra «Todo al día».
    function syncResumenPeek(feedKey, actionableLeft) {
        var $peek = $('#ip-resumen-peek');
        if ($peek.length) {
            $peek.find('li').filter(function () { return $(this).attr('data-feed-key') === feedKey; }).remove();
            if ($peek.find('li').length === 0) {
                if (actionableLeft <= 0) {
                    $peek.attr('hidden', 'hidden');
                    $('#ip-resumen-peek-empty').removeAttr('hidden');
                } else {
                    $peek.attr('hidden', 'hidden');
                }
            }
        }
    }

    function removeFeedItem($item, group) {
        var $group = $item.closest('[data-ip-group]');
        var feedKey = $item.attr('data-feed-key');
        fadeOut($item, function () {
            if ($group.length && $group.find('.ip-feed__item').length === 0) {
                $group.remove();
            }
            decrement($('[data-ip-count="' + group + '"]'));
            decrement($('#ip-feed-total'));
            if (group !== REVIEW_GROUP) {
                // Contadores accionables: KPI, pestaña y tarjeta del Resumen.
                decrement($('#ip-kpi-pendientes'));
                decrement($('#ip-tab-bandeja-count'));
                decrement($('#ip-resumen-bandeja-count'));
                decrement($('#ip-resumen-bandeja-total'));
            }
            syncResumenPeek(feedKey, group === REVIEW_GROUP ? 1 : currentValue($('#ip-resumen-bandeja-count')));
            if ($('#ip-feed .ip-feed__item').length === 0) {
                $('#ip-feed-empty').removeAttr('hidden');
            }
        });
    }

    $('#bandeja').on('click', '.js-ip-snooze', function () {
        var $item = $(this).closest('.ip-feed__item');
        var key = $item.attr('data-synthetic-key');
        if (window.LabNotifications && key) {
            window.LabNotifications.snooze(key);
        }
        removeFeedItem($item, $item.closest('[data-ip-group]').attr('data-ip-group'));
    });

    $('#bandeja').on('click', '.js-ip-done', function () {
        var $item = $(this).closest('.ip-feed__item');
        var id = parseInt($item.attr('data-notification-id'), 10) || 0;
        var group = $item.closest('[data-ip-group]').attr('data-ip-group');
        var finish = function () { removeFeedItem($item, group); };
        if (window.LabNotifications && id > 0) {
            window.LabNotifications.markAsRead(id).always(finish);
        } else {
            finish();
        }
    });

    // ════════════════ 2. Tabla de la semana: plegar días ════════════════

    $('.ip-day__toggle').on('click', function () {
        var expanded = $(this).attr('aria-expanded') !== 'false';
        $(this).attr('aria-expanded', expanded ? 'false' : 'true');
        var dayId = $(this).attr('aria-controls');
        if (dayId) {
            $('#' + dayId + ' tr.ip-row').attr('hidden', expanded ? null : 'hidden');
        }
    });

    // ════════════════ 3. Gráficos (pestaña «graficos») ════════════════

    if (typeof Chart === 'undefined') {
        $('[data-chart-fallback]').each(function () {
            $(this).removeClass('d-none');
        });
        return;
    }

    var donutPalette = ['#1e88e5', '#16a34a', '#f59e0b', '#0ea5e9', '#64748b', '#e5484d'];
    var statusPalette = ['#16a34a', '#1e88e5', '#f59e0b', '#e5484d', '#0ea5e9', '#64748b'];
    var barColors = {
        equipment: '#1e88e5',
        utensil: '#16a34a',
        other: '#f59e0b',
        condition: '#0ea5e9',
        environment: '#64748b'
    };

    // Valor sobre cada barra de la serie semanal.
    var weeklyValuePlugin = {
        id: 'ipWeeklyValues',
        afterDatasetsDraw: function (chart) {
            var ctx = chart.ctx;
            ctx.save();
            ctx.font = '700 11px "Segoe UI", Arial, sans-serif';
            ctx.fillStyle = '#475569';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'bottom';
            chart.getDatasetMeta(0).data.forEach(function (element, index) {
                var value = chart.data.datasets[0].data[index];
                if (value !== null && value !== undefined) {
                    ctx.fillText(String(value), element.x, element.y - 6);
                }
            });
            ctx.restore();
        }
    };

    // Total al centro de las donas.
    var centerTotalPlugin = {
        id: 'ipCenterTotal',
        afterDraw: function (chart) {
            var opts = chart.options.plugins.ipCenterTotal;
            if (!opts || !opts.display) return;
            var arc = chart.getDatasetMeta(0).data[0];
            if (!arc) return;
            var ctx = chart.ctx;
            ctx.save();
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.font = '700 22px "Segoe UI", Arial, sans-serif';
            ctx.fillStyle = '#1f2a3c';
            ctx.fillText(opts.total, arc.x, arc.y - 9);
            ctx.font = '500 11px "Segoe UI", Arial, sans-serif';
            ctx.fillStyle = '#64748b';
            ctx.fillText(opts.label, arc.x, arc.y + 12);
            ctx.restore();
        }
    };

    // Etiqueta de valor al final de cada barra horizontal (indicadores).
    var barValuePlugin = {
        id: 'ipBarValues',
        afterDatasetsDraw: function (chart) {
            var ctx = chart.ctx;
            ctx.save();
            ctx.font = '600 11px "Segoe UI", Arial, sans-serif';
            ctx.fillStyle = '#475569';
            ctx.textAlign = 'left';
            ctx.textBaseline = 'middle';
            chart.getDatasetMeta(0).data.forEach(function (element, index) {
                var value = chart.data.datasets[0].data[index];
                if (value !== null && value !== undefined) {
                    ctx.fillText(String(value), element.x + 6, element.y);
                }
            });
            ctx.restore();
        }
    };

    // ─── 3a. Barras: prácticas por semana (semana actual resaltada) ───

    function renderWeekly() {
        var canvas = document.getElementById('ip-chart-weekly');
        var weekly = data.weekly || [];
        if (!canvas || weekly.length === 0) return;

        var labels = weekly.map(function (p) { return p.label; });
        var values = weekly.map(function (p) { return p.count; });
        var colors = weekly.map(function (p) { return p.isCurrent ? '#1e88e5' : '#c9d4e0'; });

        new Chart(canvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    data: values,
                    backgroundColor: colors,
                    borderRadius: 6,
                    maxBarThickness: 42
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                layout: { padding: { top: 22 } },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                var n = context.parsed.y;
                                return n + (n === 1 ? ' práctica' : ' prácticas') + (weekly[context.dataIndex] && weekly[context.dataIndex].isCurrent ? ' · semana actual' : '');
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#64748b', font: { size: 11 } }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0, color: '#93a1b4', font: { size: 11 } },
                        grid: { color: '#eef2f7' }
                    }
                }
            },
            plugins: [weeklyValuePlugin]
        });
    }

    // ─── 3b. Handler Indicadores: un solo fetch para todos los gráficos ───

    var indicadoresUrl = data.indicadoresUrl || '/AssetView/Index?handler=Indicadores';
    var indicadoresPromise = null;
    var indicadoresRendered = false;

    function fetchIndicadores() {
        if (!indicadoresPromise) {
            indicadoresPromise = fetch(indicadoresUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (response) {
                    if (!response.ok) throw new Error('HTTP ' + response.status);
                    return response.json();
                })
                .catch(function (err) {
                    indicadoresPromise = null; // Permite reintentar.
                    throw err;
                });
        }
        return indicadoresPromise;
    }

    function renderLegend($legend, items, colors, total) {
        if (!$legend || $legend.length === 0) return;
        $legend.html(items.map(function (item, index) {
            var pct = total > 0 ? Math.round(item.value * 100 / total) : 0;
            return '<li><span class="ip-legend__dot" style="background:' + colors[index] + ';"></span>' +
                escapeHtml(item.label) + ' · ' + item.value + ' (' + pct + ' %)</li>';
        }).join(''));
    }

    // Estado de las unidades: el color dice algo (verde = operativo, ámbar = por confirmar, rojo = con falla).
    function statusColor(item) {
        var label = String(item.label || '').toLowerCase();
        if (label.indexOf('operativo') !== -1) return '#16a34a';
        if (label.indexOf('por confirmar') !== -1) return '#f59e0b';
        if (label.indexOf('mantenimiento') !== -1 || label.indexOf('reparaci') !== -1) return '#1e88e5';
        if (label.indexOf('préstamo') !== -1 || label.indexOf('prestamo') !== -1) return '#0ea5e9';
        if (label.indexOf('fuera de servicio') !== -1 || label.indexOf('averiado') !== -1) return '#e5484d';
        return '#94a3b8';
    }

    function renderDonut(chartId, chart, palette) {
        var canvas = document.getElementById(chartId);
        var $legend = $('[data-ip-legend="' + chart.key + '"]');
        if (!canvas || !chart.items || chart.items.length === 0) {
            showChartEmpty(chart.key, 'fas fa-chart-pie');
            if ($legend.length) $legend.empty();
            return;
        }

        var labels = chart.items.map(function (item) { return item.label; });
        var values = chart.items.map(function (item) { return item.value; });
        var total = chart.total || values.reduce(function (sum, value) { return sum + value; }, 0);
        var colors = chart.key === 'status' ? chart.items.map(statusColor) : palette.slice(0, values.length);

        new Chart(canvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{ data: values, backgroundColor: colors, borderWidth: 2, borderColor: '#ffffff' }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '66%',
                plugins: {
                    legend: { display: false },
                    ipCenterTotal: { display: true, total: String(total), label: 'unidades' },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                var value = context.parsed;
                                var pct = total > 0 ? Math.round(value * 100 / total) : 0;
                                return ' ' + value + ' unidades (' + pct + ' %)';
                            }
                        }
                    }
                }
            },
            plugins: [centerTotalPlugin]
        });

        renderLegend($legend, chart.items, colors, total);
    }

    function renderBar(chart) {
        var canvas = document.getElementById('indicadores-chart-' + chart.key);
        if (!canvas || !chart.items || chart.items.length === 0) {
            showChartEmpty(chart.key, 'fas fa-chart-bar');
            return;
        }

        var labels = chart.items.map(function (item) { return item.label; });
        var values = chart.items.map(function (item) { return item.value; });
        var maxValue = Math.max.apply(null, values);
        var box = $('[data-ip-box="' + chart.key + '"]');
        box.css('min-height', Math.max(220, chart.items.length * 36 + 30) + 'px');

        new Chart(canvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{ data: values, backgroundColor: barColors[chart.key] || '#1e88e5', borderRadius: 4, barThickness: 18 }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                layout: { padding: { right: 56 } },
                plugins: {
                    legend: { display: false },
                    tooltip: { callbacks: { label: function (context) { return context.parsed.x + ' unidades'; } } }
                },
                scales: {
                    x: { display: false, beginAtZero: true, suggestedMax: maxValue * 1.12 },
                    y: { grid: { display: false }, ticks: { color: '#475569', font: { size: 11 }, autoSkip: false } }
                }
            },
            plugins: [barValuePlugin]
        });
    }

    function showChartEmpty(key, icon) {
        var $box = $('[data-ip-box="' + key + '"]');
        $box.css('min-height', '200px').html(
            '<div class="ip-loading"><div>' +
            '<i class="' + icon + ' d-block mb-2" aria-hidden="true"></i>' +
            '<strong class="d-block">Sin datos todavía</strong>' +
            '<div class="small">Cuando existan registros, el gráfico aparecerá aquí.</div>' +
            '</div></div>');
    }

    function chartByKey(payload, key) {
        var charts = payload && payload.charts ? payload.charts : [];
        for (var i = 0; i < charts.length; i++) {
            if (charts[i].key === key) return charts[i];
        }
        return null;
    }

    function showCategoryState(state) {
        $('[data-ip-chart="category"]').find('[data-ip-state]').each(function () {
            $(this).toggleClass('d-none', $(this).attr('data-ip-state') !== state);
        });
    }

    function renderIndicadores(payload) {
        if (indicadoresRendered) return;
        indicadoresRendered = true; // Antes de dibujar: evita dobles gráficos.

        // Dona del inventario (columna 4).
        var category = chartByKey(payload, 'category');
        if (category) {
            renderDonut('ip-chart-category', category, donutPalette);
        }
        var $meta = $('#ip-category-meta');
        if ($meta.length && payload.totalUnits !== undefined) {
            $meta.html('<i class="fas fa-database mr-1"></i> Unidades vigentes: <strong>' +
                escapeHtml(payload.totalUnits) + '</strong> · actualizado a las ' + escapeHtml(payload.generatedAt));
        }
        showCategoryState('content');

        // Resto de los gráficos en dos columnas.
        var keys = ['status', 'equipment', 'utensil', 'other', 'condition', 'environment'];
        keys.forEach(function (key) {
            var chart = chartByKey(payload, key);
            if (!chart) return;
            $('[data-ip-title="' + key + '"]').text(chart.title);
            $('[data-ip-conclusion="' + key + '"]').text(chart.conclusion);
            if (chart.type === 'donut') {
                renderDonut('indicadores-chart-' + chart.key, chart, statusPalette);
            } else {
                renderBar(chart);
            }
        });
    }

    var indicadoresBusy = false;

    function ensureIndicadores() {
        if (indicadoresRendered || indicadoresBusy) return;
        indicadoresBusy = true;

        var $loading = $('#indicadores-loading');
        var $error = $('#indicadores-error');
        var $content = $('#indicadores-content');
        $loading.removeClass('d-none');
        $error.addClass('d-none');
        $content.addClass('d-none');
        showCategoryState('loading');

        fetchIndicadores()
            .then(function (payload) {
                indicadoresBusy = false;
                renderIndicadores(payload);
                $loading.addClass('d-none');
                $content.removeClass('d-none');
            })
            .catch(function (err) {
                // Se reporta en consola y se ofrece reintento: no es un error silencioso.
                console.error('No se pudieron cargar los indicadores:', err);
                indicadoresBusy = false;
                $loading.addClass('d-none');
                $error.removeClass('d-none');
                showCategoryState('error');
            });
    }

    // Todos los gráficos se crean la primera vez que se ve la pestaña «graficos»
    // (en un panel oculto los canvas miden 0). Un solo fetch para toda la pestaña.
    function initCharts() {
        renderWeekly();
        ensureIndicadores();
    }

    if (window.PanelTabs && typeof window.PanelTabs.whenShown === 'function') {
        window.PanelTabs.whenShown('graficos', initCharts);
    } else {
        initCharts();
    }

    $(document).on('click', '[data-ip-retry], #indicadores-retry', ensureIndicadores);
});
