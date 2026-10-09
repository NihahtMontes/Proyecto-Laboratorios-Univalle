/* ==========================================================================
   Grilla por laboratorio (Pages/LaboratoryOperations/Grilla.cshtml)
   1) Menú contextual "⋮" de los bloques (Abrir / Reprogramar).
   2) Modo reprogramación (?reprogramar={id}, solo SuperAdmin): arrastrar y soltar HTML5 nativo, fantasma del
      destino con su tamaño real, previsualización con /Api/SlotValidation y confirmación (POST con antiforgery).
   El servidor manda siempre: la confirmación vuelve a validar todo en ReschedulingService.
   ========================================================================== */
(function () {
    'use strict';

    function esc(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    // ---------------------------------------------------------------- Menú contextual
    var menu = document.getElementById('gr-block-menu');
    var menuOwner = null;
    if (menu && menu.parentNode !== document.body) document.body.appendChild(menu);

    function closeMenu() {
        if (!menu || !menuOwner) return;
        menu.classList.remove('show');
        menu.setAttribute('aria-hidden', 'true');
        menu.innerHTML = '';
        menuOwner.setAttribute('aria-expanded', 'false');
        menuOwner = null;
    }

    function addMenuItem(href, icon, text) {
        var a = document.createElement('a');
        a.className = 'dropdown-item';
        a.href = href;
        a.setAttribute('role', 'menuitem');
        var i = document.createElement('i');
        i.className = icon + ' mr-1';
        i.setAttribute('aria-hidden', 'true');
        a.appendChild(i);
        a.appendChild(document.createTextNode(' ' + text));
        menu.appendChild(a);
    }

    function openMenu(button) {
        var block = button.closest('.gr-block');
        if (!block || !menu) return;
        if (menuOwner === button) { closeMenu(); return; }
        closeMenu();

        var header = document.createElement('h6');
        header.className = 'dropdown-header';
        header.textContent = block.getAttribute('data-code') || '';
        menu.appendChild(header);
        addMenuItem(block.getAttribute('data-next-url'), 'fas fa-folder-open', 'Abrir');
        var rescheduleUrl = block.getAttribute('data-reschedule-url');
        if (rescheduleUrl) addMenuItem(rescheduleUrl, 'mdi mdi-calendar-clock', 'Reprogramar');

        var rect = button.getBoundingClientRect();
        menu.style.top = (rect.bottom + window.pageYOffset + 2) + 'px';
        menu.style.left = Math.max(8, rect.right + window.pageXOffset - 170) + 'px';
        menu.classList.add('show');
        menu.setAttribute('aria-hidden', 'false');
        button.setAttribute('aria-expanded', 'true');
        menuOwner = button;
        var first = menu.querySelector('.dropdown-item');
        if (first) first.focus();
    }

    document.addEventListener('click', function (e) {
        var button = e.target.closest ? e.target.closest('.gr-block__menu-btn') : null;
        if (button) {
            e.preventDefault();
            e.stopPropagation();
            openMenu(button);
            return;
        }
        if (menu && !menu.contains(e.target)) closeMenu();
    });
    document.addEventListener('keydown', function (e) {
        if ((e.key === 'Escape' || e.key === 'Esc') && menuOwner) {
            var owner = menuOwner;
            closeMenu();
            owner.focus();
        }
    });
    window.addEventListener('resize', closeMenu);

    // Destino recién creado: llevarlo a la vista.
    var highlighted = document.querySelector('.gr-block--highlight');
    if (highlighted && highlighted.scrollIntoView) highlighted.scrollIntoView({ block: 'center' });

    // ---------------------------------------------------------------- Modo reprogramación
    var configElement = document.getElementById('gr-rs-config');
    if (!configElement) return;
    var cfg;
    try { cfg = JSON.parse(configElement.textContent); } catch (err) { return; }

    var form = document.getElementById('gr-rs-form');
    var dateInput = document.getElementById('gr-rs-date');
    var periodSelect = document.getElementById('gr-rs-period');
    var labSelect = document.getElementById('gr-rs-lab');
    var teacherSelect = document.getElementById('gr-rs-teacher');
    var previewButton = document.getElementById('gr-rs-preview-btn');
    var previewBox = document.getElementById('gr-rs-preview');
    var confirmBox = document.getElementById('gr-rs-confirm');
    var acceptWrap = document.getElementById('gr-rs-accept-wrap');
    var acceptBox = document.getElementById('gr-rs-accept');
    var acceptedHolder = document.getElementById('gr-rs-accepted');
    var reasonInput = document.getElementById('gr-rs-reason');
    if (!form || !previewBox || !teacherSelect) return;

    var periodsByOrder = {};
    var periodsById = {};
    (cfg.periods || []).forEach(function (p) { periodsByOrder[p.order] = p; periodsById[p.id] = p; });
    var labsById = {};
    (cfg.labs || []).forEach(function (l) { labsById[l.id] = l; });

    var LEVELS = {
        valid: { icon: 'mdi mdi-check-circle', text: 'Disponible' },
        warning: { icon: 'mdi mdi-alert', text: 'Con advertencias' },
        invalid: { icon: 'mdi mdi-close-circle', text: 'No disponible' },
        pending: { icon: 'fas fa-spinner fa-spin', text: 'Validando…' }
    };
    var SEVERITIES = {
        block: { icon: 'mdi mdi-close-circle', text: 'Bloquea' },
        warning: { icon: 'mdi mdi-alert', text: 'Advierte' },
        info: { icon: 'mdi mdi-information', text: 'Info' }
    };

    var cache = {};          // clave fecha|periodo|ambiente|docente → respuesta del servidor
    var inflight = null;     // { key, controller, promise }
    var debounceTimer = null;
    var dragging = false;
    var dropped = false;
    var hover = null;        // destino bajo el cursor durante el arrastre
    var fixed = null;        // { dest, result } destino fijado (sin bloqueos)
    var chooseSeq = 0;
    var ghost = null;

    // ------------------------------------------------ utilidades
    function currentTeacherId() { return parseInt(teacherSelect.value, 10) || 0; }

    function currentTeacherName() {
        var option = teacherSelect.options[teacherSelect.selectedIndex];
        return option ? option.text : '—';
    }

    function keyOf(d) { return [d.date, d.periodId, d.labId, d.teacherId].join('|'); }

    function lastOrderOf(order) { return order + cfg.periodCount - 1; }

    function fits(order) { return order >= 1 && lastOrderOf(order) <= cfg.maxOrder; }

    function periodsText(order) {
        var first = periodsByOrder[order];
        if (!first) return 'P' + order;
        var lastOrder = lastOrderOf(order);
        var last = periodsByOrder[lastOrder];
        var range = cfg.periodCount === 1 ? 'P' + order : 'P' + order + '–P' + lastOrder;
        if (!last) return range + ' (no cabe en la jornada)';
        var start = String(first.label).split('–')[0].trim();
        var end = String(last.label).split('–').pop().trim();
        return range + ' · ' + start + '–' + end;
    }

    function formatDate(iso) {
        var parts = String(iso).split('-');
        if (parts.length !== 3) return iso;
        var date = new Date(+parts[0], +parts[1] - 1, +parts[2]);
        var text = date.toLocaleDateString('es-BO', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' });
        return text.charAt(0).toUpperCase() + text.slice(1);
    }

    function formatQty(value) {
        if (value === null || value === undefined) return '—';
        return Number(value).toLocaleString('es-BO', { maximumFractionDigits: 2 });
    }

    function makeDest(date, periodId, labId) {
        var period = periodsById[periodId];
        return { date: date, periodId: periodId, order: period ? period.order : 0, labId: labId, teacherId: currentTeacherId() };
    }

    function destFromCell(cell) {
        var order = parseInt(cell.getAttribute('data-order'), 10);
        var period = periodsByOrder[order];
        return {
            date: cell.getAttribute('data-date'),
            order: order,
            periodId: period ? period.id : 0,
            labId: parseInt(cell.getAttribute('data-laboratory-id'), 10),
            teacherId: currentTeacherId()
        };
    }

    function cellFor(d) {
        return document.querySelector('.gr-cell[data-date="' + d.date + '"][data-order="' + d.order + '"][data-laboratory-id="' + d.labId + '"]');
    }

    function cellAt(x, y) {
        var elements = document.elementsFromPoint ? document.elementsFromPoint(x, y) : [document.elementFromPoint(x, y)];
        for (var i = 0; i < elements.length; i++) {
            var el = elements[i];
            if (el && el.classList && el.classList.contains('gr-cell')) return el;
        }
        return null;
    }

    /** Rojo inmediato (sin esperar al servidor) para lo evidente: no cabe, día suspendido o fuera de la gestión. */
    function localProblem(d) {
        if (!fits(d.order)) return 'El bloque de ' + cfg.periodCount + ' periodo(s) no cabe desde P' + d.order + '.';
        var cell = cellFor(d);
        if (cell && cell.getAttribute('data-suspended') === 'true') return 'El día está suspendido.';
        if (cell && cell.getAttribute('data-out') === 'true') return 'El día está fuera de la gestión.';
        return null;
    }

    function isSameAsOrigin(d) {
        return d.date === cfg.originDate && d.order === cfg.originStartOrder
            && d.labId === cfg.originLabId && d.teacherId === cfg.originTeacherId;
    }

    // ------------------------------------------------ fantasma
    function removeGhost() {
        if (ghost && ghost.parentNode) ghost.parentNode.removeChild(ghost);
        ghost = null;
    }

    function drawGhost(d, level, isFixed) {
        removeGhost();
        var cell = cellFor(d);
        if (!cell) return;   // el destino no está en la vista actual
        var gridElement = cell.closest('.gr-grid');
        if (!gridElement) return;
        var span = Math.max(1, Math.min(cfg.periodCount, cfg.maxOrder - d.order + 1));
        var info = LEVELS[level] || LEVELS.pending;
        ghost = document.createElement('div');
        ghost.className = 'gr-ghost gr-ghost--' + level + (isFixed ? ' gr-ghost--fixed' : '');
        ghost.style.gridColumn = cell.getAttribute('data-col');
        ghost.style.gridRow = (d.order + 1) + ' / span ' + span;
        ghost.setAttribute('aria-hidden', 'true');
        ghost.innerHTML = '<i class="' + info.icon + '"></i><span>' + esc(cfg.code) + '</span><span>' + esc(periodsText(d.order)) + '</span>'
            + '<span>' + esc(fits(d.order) ? info.text : 'No cabe') + '</span>';
        gridElement.appendChild(ghost);
    }

    // ------------------------------------------------ panel de previsualización
    function renderEmpty(message) {
        previewBox.innerHTML = '<div class="gr-rs-preview__empty"><i class="mdi mdi-hand-pointing-right" aria-hidden="true"></i> '
            + (message ? esc(message) : 'Arrastre el bloque a una celda o elija el destino y pulse <strong>Previsualizar</strong>.') + '</div>';
    }

    function levelBadge(level) {
        var info = LEVELS[level] || LEVELS.pending;
        return '<span class="gr-rs-level gr-rs-level--' + esc(level) + '"><i class="' + info.icon + '" aria-hidden="true"></i> ' + esc(info.text) + '</span>';
    }

    function destinationHtml(d, isFixed) {
        var lab = labsById[d.labId];
        return '<dl class="gr-rs-dest' + (isFixed ? ' gr-rs-dest--fixed' : '') + '">'
            + '<dt>Fecha</dt><dd>' + esc(formatDate(d.date)) + '</dd>'
            + '<dt>Periodos</dt><dd>' + esc(periodsText(d.order)) + '</dd>'
            + '<dt>Ambiente</dt><dd>' + esc(lab ? lab.label : '—') + '</dd>'
            + '<dt>Docente</dt><dd>' + esc(currentTeacherName()) + '</dd>'
            + '</dl>';
    }

    function issuesHtml(result) {
        var issues = result.issues || [];
        if (issues.length === 0) {
            return '<ul class="gr-rs-issues"><li class="gr-rs-issue gr-rs-issue--info"><i class="mdi mdi-check-circle" aria-hidden="true"></i>'
                + '<span>Sin observaciones: el ambiente y el docente están libres.</span></li></ul>';
        }
        var order = { block: 0, warning: 1, info: 2 };
        var sorted = issues.slice().sort(function (a, b) { return (order[a.severity] || 0) - (order[b.severity] || 0); });
        var html = '<ul class="gr-rs-issues">';
        sorted.forEach(function (issue) {
            var sev = SEVERITIES[issue.severity] || SEVERITIES.info;
            html += '<li class="gr-rs-issue gr-rs-issue--' + esc(issue.severity) + '"><i class="' + sev.icon + '" aria-hidden="true"></i><div>'
                + '<span class="gr-rs-issue__severity">' + esc(sev.text) + ':</span> ' + esc(issue.message);
            if (issue.details && issue.details.length) {
                html += '<ul>' + issue.details.map(function (d) { return '<li>' + esc(d) + '</li>'; }).join('') + '</ul>';
            }
            html += '</div></li>';
        });
        return html + '</ul>';
    }

    function shortagesHtml(result) {
        var shortages = result.shortages || [];
        if (shortages.length === 0) return '';
        var html = '<div class="gr-rs-preview__title mb-1">Faltantes de material</div>'
            + '<div class="table-responsive"><table class="gr-rs-shortages"><thead><tr>'
            + '<th>Artículo</th><th class="text-right">Solicitado</th><th class="text-right">Disponible</th><th class="text-right">Faltante</th>'
            + '</tr></thead><tbody>';
        shortages.forEach(function (s) {
            html += '<tr><td>' + esc(s.articleCode) + ' · ' + esc(s.articleName) + '</td>'
                + '<td class="text-right">' + esc(formatQty(s.requested)) + '</td>'
                + '<td class="text-right">' + esc(formatQty(s.available)) + '</td>'
                + '<td class="text-right text-danger font-weight-bold">' + esc(formatQty(s.shortage)) + '</td></tr>';
        });
        return html + '</tbody></table></div>';
    }

    /** opts: { hover, fixed, local, notice } */
    function renderPreview(d, result, opts) {
        opts = opts || {};
        var level = result ? result.level : (opts.local ? 'invalid' : 'pending');
        var title = opts.fixed ? 'Destino elegido' : opts.hover ? 'Destino bajo el cursor' : 'Destino';
        var html = '<div class="gr-rs-preview__head"><span class="gr-rs-preview__title">' + esc(title) + '</span>' + levelBadge(level) + '</div>';
        html += destinationHtml(d, opts.fixed);
        if (opts.notice) html += '<div class="small text-danger font-weight-bold mb-2"><i class="mdi mdi-close-circle" aria-hidden="true"></i> ' + esc(opts.notice) + '</div>';
        if (!result) {
            if (opts.local) html += '<div class="small text-danger mb-2">' + esc(opts.local) + '</div>';
            html += '<div class="small text-muted"><i class="fas fa-spinner fa-spin mr-1" aria-hidden="true"></i> Validando en el servidor…</div>';
        } else {
            html += issuesHtml(result) + shortagesHtml(result);
        }
        previewBox.innerHTML = html;
    }

    function renderError(d, message) {
        var html = '<div class="gr-rs-preview__head"><span class="gr-rs-preview__title">Destino</span>' + levelBadge('invalid') + '</div>';
        html += destinationHtml(d, false);
        html += '<div class="gr-rs-issue gr-rs-issue--block"><i class="mdi mdi-close-circle" aria-hidden="true"></i><span>' + esc(message) + '</span></div>';
        previewBox.innerHTML = html;
    }

    // ------------------------------------------------ servidor
    function requestValidation(d) {
        var key = keyOf(d);
        if (cache[key]) return Promise.resolve(cache[key]);
        if (inflight && inflight.key === key) return inflight.promise;
        if (inflight && inflight.controller) inflight.controller.abort();   // petición obsoleta

        var controller = window.AbortController ? new AbortController() : null;
        var params = new URLSearchParams({
            planningId: cfg.planningId,
            date: d.date,
            startPeriodId: d.periodId,
            laboratoryId: d.labId,
            teacherPersonId: d.teacherId
        });
        var promise = fetch(cfg.validateUrl + '?' + params.toString(), {
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' },
            signal: controller ? controller.signal : undefined
        }).then(function (response) {
            var type = response.headers.get('content-type') || '';
            if (response.status === 401 || response.status === 403 || (response.ok && type.indexOf('json') < 0)) {
                throw new Error('No se pudo validar: la sesión expiró o no tiene permiso de SuperAdmin. Vuelva a iniciar sesión.');
            }
            if (!response.ok) {
                return response.json().catch(function () { return {}; }).then(function (body) {
                    throw new Error(body.message || ('No se pudo validar el destino (error ' + response.status + ').'));
                });
            }
            return response.json();
        }).then(function (result) {
            cache[key] = result;
            return result;
        });

        var entry = { key: key, controller: controller, promise: promise };
        inflight = entry;
        var clear = function () { if (inflight === entry) inflight = null; };
        promise.then(clear, clear);
        return promise;
    }

    // ------------------------------------------------ fijar destino
    function setFields(d) {
        if (dateInput) dateInput.value = d.date;
        if (periodSelect) periodSelect.value = String(d.periodId);
        if (labSelect && labsById[d.labId]) labSelect.value = String(d.labId);
    }

    function unfix() {
        fixed = null;
        if (confirmBox) confirmBox.hidden = true;
    }

    function fix(d, result) {
        fixed = { dest: d, result: result };
        drawGhost(d, result.level, true);
        renderPreview(d, result, { fixed: true });
        if (confirmBox) confirmBox.hidden = false;
        if (acceptWrap) acceptWrap.hidden = !result.hasWarnings;
        if (acceptBox) acceptBox.checked = false;
        if (reasonInput && !reasonInput.value) reasonInput.focus();
    }

    function restoreFixed() {
        removeGhost();
        if (fixed) {
            drawGhost(fixed.dest, fixed.result.level, true);
            renderPreview(fixed.dest, fixed.result, { fixed: true });
        } else {
            renderEmpty();
        }
    }

    function choose(d) {
        clearTimeout(debounceTimer);
        hover = null;
        var seq = ++chooseSeq;
        setFields(d);

        if (!d.periodId || !d.date) { unfix(); renderEmpty('Elija la fecha y el periodo inicial del destino.'); return; }
        if (isSameAsOrigin(d)) {
            unfix();
            removeGhost();
            renderPreview(d, null, { notice: 'Es la ubicación actual de la planificación: elija otro destino u otro docente.', local: ' ' });
            previewBox.querySelector('.fa-spinner') && previewBox.querySelector('.fa-spinner').parentNode.remove();
            return;
        }

        var cached = cache[keyOf(d)];
        var local = localProblem(d);
        drawGhost(d, cached ? cached.level : (local ? 'invalid' : 'pending'), true);
        renderPreview(d, cached || null, { local: local });

        requestValidation(d).then(function (result) {
            if (seq !== chooseSeq) return;
            if (result.hasBlocking) {
                unfix();
                drawGhost(d, 'invalid', false);
                renderPreview(d, result, { notice: 'Ese destino está bloqueado: elija otro.' });
                return;
            }
            fix(d, result);
        }, function (error) {
            if (error && error.name === 'AbortError') return;
            if (seq !== chooseSeq) return;
            unfix();
            drawGhost(d, 'invalid', false);
            renderError(d, error.message);
        });
    }

    // ------------------------------------------------ arrastre
    function onHover(d) {
        var key = keyOf(d);
        if (hover && keyOf(hover) === key) return;
        hover = d;
        clearTimeout(debounceTimer);

        var cached = cache[key];
        var local = localProblem(d);
        drawGhost(d, cached ? cached.level : (local ? 'invalid' : 'pending'), false);
        renderPreview(d, cached || null, { hover: true, local: local });
        if (cached) return;

        debounceTimer = setTimeout(function () {
            requestValidation(d).then(function (result) {
                if (!dragging || !hover || keyOf(hover) !== key) return;
                drawGhost(d, result.level, false);
                renderPreview(d, result, { hover: true });
            }, function (error) {
                if (error && error.name === 'AbortError') return;
                if (!dragging || !hover || keyOf(hover) !== key) return;
                drawGhost(d, 'invalid', false);
                renderError(d, error.message);
            });
        }, 250);
    }

    function bindDragSource(source) {
        if (!source || source.getAttribute('draggable') !== 'true') return;
        source.addEventListener('dragstart', function (e) {
            dragging = true;
            dropped = false;
            hover = null;
            closeMenu();
            try { e.dataTransfer.setData('text/plain', cfg.code); } catch (ignored) { /* IE */ }
            e.dataTransfer.effectAllowed = 'move';
            // Cambiar el elemento arrastrado en el mismo dragstart cancela el arrastre en algunos navegadores.
            setTimeout(function () { source.classList.add('gr-block--dragging'); }, 0);
        });
        source.addEventListener('dragend', function () {
            dragging = false;
            clearTimeout(debounceTimer);
            source.classList.remove('gr-block--dragging');
            if (!dropped) {
                hover = null;
                restoreFixed();
            }
        });
    }

    bindDragSource(document.querySelector('.gr-block--origin'));
    bindDragSource(document.getElementById('gr-rs-origin-card'));

    Array.prototype.forEach.call(document.querySelectorAll('.gr-grid'), function (gridElement) {
        gridElement.addEventListener('dragover', function (e) {
            if (!dragging) return;
            var cell = cellAt(e.clientX, e.clientY);
            if (!cell) return;
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            onHover(destFromCell(cell));
        });
        gridElement.addEventListener('drop', function (e) {
            if (!dragging) return;
            var cell = cellAt(e.clientX, e.clientY);
            if (!cell) return;
            e.preventDefault();
            dropped = true;
            choose(destFromCell(cell));
        });
    });

    // ------------------------------------------------ alternativa sin arrastre
    if (previewButton) {
        previewButton.addEventListener('click', function () {
            if (!dateInput || !dateInput.value) {
                unfix();
                removeGhost();
                renderEmpty('Elija la fecha de destino.');
                if (dateInput) dateInput.focus();
                return;
            }
            choose(makeDest(dateInput.value, parseInt(periodSelect.value, 10), parseInt(labSelect.value, 10)));
        });
    }

    // Cambiar fecha, periodo o ambiente a mano invalida el destino fijado hasta volver a previsualizar.
    [dateInput, periodSelect, labSelect].forEach(function (field) {
        if (!field) return;
        field.addEventListener('change', function () {
            if (!fixed) return;
            unfix();
            removeGhost();
            renderEmpty('El destino cambió: pulse Previsualizar para validarlo.');
        });
    });

    // El docente se elige con Select2; al cambiarlo se revalida el destino elegido.
    function onTeacherChange() {
        var base = fixed ? fixed.dest : null;
        if (!base && dateInput && dateInput.value) {
            base = makeDest(dateInput.value, parseInt(periodSelect.value, 10), parseInt(labSelect.value, 10));
        }
        if (!base) return;
        choose({ date: base.date, periodId: base.periodId, order: base.order, labId: base.labId, teacherId: currentTeacherId() });
    }
    if (window.jQuery && jQuery.fn.select2) {
        jQuery(teacherSelect).select2({
            width: '100%',
            language: { noResults: function () { return 'Sin coincidencias'; } }
        }).on('change', onTeacherChange);
    } else {
        teacherSelect.addEventListener('change', onTeacherChange);
    }

    // ------------------------------------------------ confirmación
    function alertBox(title, text) {
        if (typeof Swal === 'undefined') { return; }
        Swal.fire({ title: title, text: text, type: 'warning', confirmButtonText: 'Entendido', confirmButtonColor: '#6f42c1' });
    }

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        if (!fixed || !fixed.result || fixed.result.hasBlocking) {
            alertBox('Elija un destino', 'Arrastre el bloque a un destino válido o use Previsualizar antes de confirmar.');
            return;
        }
        if (fixed.dest.teacherId !== currentTeacherId()) {
            onTeacherChange();
            return;
        }
        var reason = reasonInput ? reasonInput.value.trim() : '';
        if (!reason) {
            alertBox('Falta el motivo', 'Indique el motivo de la reprogramación.');
            if (reasonInput) reasonInput.focus();
            return;
        }
        var result = fixed.result;
        if (result.hasWarnings && !(acceptBox && acceptBox.checked)) {
            alertBox('Advertencias sin aceptar', 'Revise las advertencias y marque "Acepto las advertencias" para continuar.');
            return;
        }

        setFields(fixed.dest);
        acceptedHolder.innerHTML = '';
        var codes = {};
        (result.issues || []).forEach(function (issue) {
            if (issue.severity !== 'warning' || codes[issue.code]) return;
            codes[issue.code] = true;
            var input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'Move.AcceptedWarnings';
            input.value = issue.code;
            acceptedHolder.appendChild(input);
        });

        if (typeof Swal === 'undefined') {
            HTMLFormElement.prototype.submit.call(form);
            return;
        }
        var completed = (result.issues || []).some(function (issue) { return issue.code === 'COMPLETED'; });
        var lab = labsById[fixed.dest.labId];
        var options = completed
            ? {
                 title: 'Planificación completada',
                 text: cfg.recovery ? 'La planificación original estaba completada; la reposición se ejecutará otra vez. ¿Continuar?' : cfg.completedText,
                type: 'warning',
                showCancelButton: true,
                 confirmButtonText: cfg.recovery ? 'Sí, reponer' : 'Sí, reprogramar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#6f42c1',
                cancelButtonColor: '#6c757d'
            }
            : {
                 title: cfg.recovery ? '¿Confirmar la reposición?' : '¿Confirmar la reprogramación?',
                text: cfg.code + ' pasará a ' + formatDate(fixed.dest.date) + ', ' + periodsText(fixed.dest.order)
                     + (lab ? ', ' + lab.label : '') + (cfg.recovery ? '. El origen quedará como Repuesta.' : '. El origen quedará como Reprogramada.'),
                type: 'question',
                showCancelButton: true,
                 confirmButtonText: cfg.recovery ? 'Sí, reponer' : 'Sí, reprogramar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#6f42c1',
                cancelButtonColor: '#6c757d'
            };
        Swal.fire(options).then(function (r) {
            if (r && (r.isConfirmed === true || r.value === true)) {
                HTMLFormElement.prototype.submit.call(form);
            }
        });
    });

    // Tras un intento fallido se vuelve a previsualizar el destino intentado.
    if (cfg.initial && cfg.initial.date && cfg.initial.periodId) {
        if (dateInput) dateInput.value = cfg.initial.date;
        if (periodSelect) periodSelect.value = String(cfg.initial.periodId);
        choose(makeDest(cfg.initial.date, cfg.initial.periodId, parseInt(labSelect.value, 10)));
    }
})();
