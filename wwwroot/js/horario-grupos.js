/* ==========================================================================
   Horario semanal de clases por grupo de materia.
   Pages/Catalogs/Subjects/Horario.cshtml

   - Chips de grupos ("Grupo A · docente · N estudiantes"), color fijo por
     posición con paleta suave de 8 colores; se elige uno y las celdas se
     marcan/desmarcan para ese grupo.
   - Clic en una celda marca o desmarca ese periodo; arrastrar con el mouse
     presionado pinta/despinta varias celdas seguidas.
   - Una celda con varios grupos muestra mini-etiquetas de cada uno.
   - Las celdas donde el grupo seleccionado tiene clase de OTRA materia
     (SiblingClass) quedan rayadas en rojo suave con title informativo y no
     se pueden marcar para ese grupo.
   - Resumen en vivo por grupo ("Lunes periodos 1–3 · Miércoles periodo 4")
     e indicador "Cambios sin guardar".
   - Guardar: arma el JSON {groupId: [{weekday, periodOrder}, ...]}, pide
     confirmación (SweetAlert2 v7) y envía el form POST con antiforgery.
   ES5, IIFE. Sin dependencias.
   ========================================================================== */
(function () {
    'use strict';

    var COLOR_COUNT = 8;
    var state = null;

    /* Todo texto de usuario se inserta con textContent o en propiedades DOM
       (title), nunca via innerHTML: no se necesita escapado HTML. */

    function cellKey(weekday, order) { return weekday + ':' + order; }

    function currentGroup() { return state.config.groups[state.selected]; }

    function slotsOf(groupId) {
        if (!state.slots[groupId]) state.slots[groupId] = {};
        return state.slots[groupId];
    }

    function dayName(weekday) {
        var names = state.config.dayNames || [];
        return names[weekday - 1] || '';
    }

    /** Mapa celda -> choque con otra materia para el grupo indicado. */
    function siblingMap(group) {
        var map = {};
        (group.siblings || []).forEach(function (s) {
            map[cellKey(Number(s.weekday), Number(s.periodOrder))] = s;
        });
        return map;
    }

    /* ------------------------------------------------------------ Resumen */
    /** "Lunes periodos 1–3 · Miércoles periodo 4" (mismo formato que el servicio). */
    function summarize(slotsMap) {
        var byDay = {};
        var k, parts, weekday, order;
        for (k in slotsMap) {
            if (!Object.prototype.hasOwnProperty.call(slotsMap, k)) continue;
            parts = k.split(':');
            weekday = Number(parts[0]);
            order = Number(parts[1]);
            if (!byDay[weekday]) byDay[weekday] = [];
            byDay[weekday].push(order);
        }
        var days = [];
        for (weekday in byDay) {
            if (Object.prototype.hasOwnProperty.call(byDay, weekday)) days.push(Number(weekday));
        }
        days.sort(function (a, b) { return a - b; });
        if (days.length === 0) return 'Sin horario';

        var chunks = [];
        days.forEach(function (day) {
            var orders = byDay[day].slice().sort(function (a, b) { return a - b; });
            var runs = [];
            var start = orders[0];
            var prev = orders[0];
            var i;
            for (i = 1; i < orders.length; i++) {
                if (orders[i] === prev + 1) {
                    prev = orders[i];
                } else {
                    runs.push([start, prev]);
                    start = prev = orders[i];
                }
            }
            runs.push([start, prev]);
            var text = runs.map(function (r) {
                return r[0] === r[1] ? 'periodo ' + r[0] : 'periodos ' + r[0] + '\u2013' + r[1];
            }).join(', ');
            chunks.push(dayName(day) + ' ' + text);
        });
        return chunks.join(' \u00B7 ');
    }

    /* ------------------------------------------------------------ Payload */
    /** {"3":[{"weekday":1,"periodOrder":2},...]}: incluye todos los grupos (vacío = sin clases). */
    function payloadJson() {
        var payload = {};
        state.config.groups.forEach(function (g) {
            var arr = [];
            var map = slotsOf(g.id);
            var k, parts;
            for (k in map) {
                if (!Object.prototype.hasOwnProperty.call(map, k)) continue;
                parts = k.split(':');
                arr.push({ weekday: Number(parts[0]), periodOrder: Number(parts[1]) });
            }
            arr.sort(function (a, b) {
                return a.weekday - b.weekday || a.periodOrder - b.periodOrder;
            });
            payload[String(g.id)] = arr;
        });
        return JSON.stringify(payload);
    }

    function refreshIndicator() {
        if (!state.unsaved) return;
        state.unsaved.hidden = payloadJson() === state.initialJson;
    }

    /* ------------------------------------------------------------ Pintado */
    function paintCell(cell, key) {
        var group = currentGroup();
        var occupying = [];
        state.config.groups.forEach(function (g, i) {
            if (slotsOf(g.id)[key]) occupying.push({ group: g, index: i });
        });

        var classes = [cell.getAttribute('data-base-class')];
        if (occupying.length === 1) {
            classes.push('hg-cell--marked', 'hg-cell--c' + (occupying[0].index % COLOR_COUNT));
        } else if (occupying.length > 1) {
            classes.push('hg-cell--marked', 'hg-cell--multi');
        }
        var sibling = state.currentSiblings[key];
        if (sibling) classes.push('hg-cell--sibling');
        cell.className = classes.join(' ');
        cell.setAttribute('aria-pressed', occupying.some(function (o) { return o.group === group; }) ? 'true' : 'false');

        /* Mini-etiquetas de los grupos que ocupan la celda (siempre vía textContent). */
        var tags = cell.querySelector('.hg-cell__tags');
        if (!tags) {
            tags = document.createElement('span');
            tags.className = 'hg-cell__tags';
            tags.setAttribute('aria-hidden', 'true');
            cell.appendChild(tags);
        }
        tags.textContent = '';
        occupying.forEach(function (o) {
            var tag = document.createElement('span');
            tag.className = 'hg-cell__tag hg-tint--c' + (o.index % COLOR_COUNT);
            tag.textContent = o.group.name;
            tags.appendChild(tag);
        });

        if (sibling) {
            /* title es una asignación de propiedad (texto plano): no requiere escapado HTML. */
            cell.title = 'Grupo ' + group.name + ' tiene clase de ' + (sibling.subjectName || 'otra materia');
            cell.setAttribute('aria-disabled', 'true');
        } else {
            cell.removeAttribute('aria-disabled');
            cell.title = occupying.length
                ? 'Clase de: ' + occupying.map(function (o) { return 'Grupo ' + o.group.name; }).join(', ')
                : '';
        }
    }

    function paintAll() {
        var key;
        for (key in state.cells) {
            if (Object.prototype.hasOwnProperty.call(state.cells, key)) paintCell(state.cells[key], key);
        }
    }

    /* ------------------------------------------------------------ Chips */
    function renderChips() {
        state.chips.textContent = '';
        state.config.groups.forEach(function (g, i) {
            var chip = document.createElement('button');
            chip.type = 'button';
            chip.className = 'hg-chip hg-chip--c' + (i % COLOR_COUNT) + (i === state.selected ? ' hg-chip--active' : '');
            chip.setAttribute('data-group-index', String(i));
            chip.setAttribute('aria-pressed', i === state.selected ? 'true' : 'false');

            var dot = document.createElement('span');
            dot.className = 'hg-dot hg-dot--c' + (i % COLOR_COUNT);
            dot.setAttribute('aria-hidden', 'true');
            chip.appendChild(dot);

            var label = document.createElement('span');
            label.className = 'hg-chip__label';
            label.textContent = g.label || ('Grupo ' + g.name);
            chip.appendChild(label);

            var meta = [];
            if (g.teacher) meta.push(g.teacher);
            if (g.students !== null && g.students !== undefined) {
                meta.push(g.students + (g.students === 1 ? ' estudiante' : ' estudiantes'));
            }
            if (meta.length) {
                var m = document.createElement('span');
                m.className = 'hg-chip__meta';
                m.textContent = ' \u00B7 ' + meta.join(' \u00B7 ');
                chip.appendChild(m);
            }
            state.chips.appendChild(chip);
        });
    }

    function renderSummaries() {
        if (!state.summaries) return;
        state.summaries.textContent = '';
        state.config.groups.forEach(function (g, i) {
            var row = document.createElement('div');
            row.className = 'hg-summary';

            var dot = document.createElement('span');
            dot.className = 'hg-dot hg-dot--c' + (i % COLOR_COUNT);
            dot.setAttribute('aria-hidden', 'true');
            row.appendChild(dot);

            var name = document.createElement('strong');
            name.className = 'hg-summary__name';
            name.textContent = (g.label || ('Grupo ' + g.name)) + ':';
            row.appendChild(name);

            var text = document.createElement('span');
            text.className = 'hg-summary__text' + (Object.keys(slotsOf(g.id)).length === 0 ? ' hg-summary__text--empty' : '');
            text.textContent = ' ' + summarize(slotsOf(g.id));
            row.appendChild(text);

            state.summaries.appendChild(row);
        });
    }

    /* ------------------------------------------------------------ Edición */
    function applyCell(cell, value) {
        var group = currentGroup();
        var key = cellKey(Number(cell.getAttribute('data-weekday')), Number(cell.getAttribute('data-order')));
        if (state.currentSiblings[key]) return; /* choque con otra materia: no se puede marcar */
        var map = slotsOf(group.id);
        if (value) map[key] = true; else delete map[key];
        paintAll();
        renderSummaries();
        refreshIndicator();
    }

    function bind() {
        /* Selección de grupo (permitida también en solo lectura para revisar choques). */
        state.chips.addEventListener('click', function (e) {
            var chip = e.target.closest ? e.target.closest('.hg-chip') : null;
            if (!chip) return;
            var index = Number(chip.getAttribute('data-group-index'));
            if (index === state.selected) return;
            state.selected = index;
            state.currentSiblings = siblingMap(currentGroup());
            renderChips();
            paintAll();
        });

        if (state.config.readOnly === true) return; /* solo lectura: sin edición ni guardado */

        state.grid.addEventListener('mousedown', function (e) {
            if (e.button !== 0) return; /* solo el botón izquierdo pinta */
            var cell = e.target.closest ? e.target.closest('button.hg-cell') : null;
            if (!cell || cell.disabled) return;
            var key = cellKey(Number(cell.getAttribute('data-weekday')), Number(cell.getAttribute('data-order')));
            if (state.currentSiblings[key]) return; /* choque: no inicia pintado ni arrastre */
            e.preventDefault(); /* evita selección de texto al arrastrar */
            state.dragging = true;
            state.dragValue = !slotsOf(currentGroup().id)[key];
            applyCell(cell, state.dragValue);
        });

        state.grid.addEventListener('mouseover', function (e) {
            if (!state.dragging) return;
            var cell = e.target.closest ? e.target.closest('button.hg-cell') : null;
            if (!cell || cell.disabled) return;
            applyCell(cell, state.dragValue);
        });

        document.addEventListener('mouseup', function () {
            state.dragging = false;
        });

        /* Teclado (Enter/Espacio): el clic sintético llega con detail 0; el clic
           del mouse ya fue atendido por mousedown (que además permite arrastrar). */
        state.grid.addEventListener('click', function (e) {
            if (e.detail !== 0) return;
            var cell = e.target.closest ? e.target.closest('button.hg-cell') : null;
            if (!cell || cell.disabled) return;
            applyCell(cell, !slotsOf(currentGroup().id)[cellKey(
                Number(cell.getAttribute('data-weekday')),
                Number(cell.getAttribute('data-order')))]);
        });

        /* Guardar: JSON + confirmación SweetAlert2 v7 + submit nativo del form. */
        if (state.saveBtn) {
            state.saveBtn.addEventListener('click', function () {
                state.slotsInput.value = payloadJson();
                var submit = function () {
                    HTMLFormElement.prototype.submit.call(state.form);
                };
                if (typeof Swal === 'undefined') { submit(); return; }
                Swal.fire({
                    title: '\u00BFGuardar el horario?',
                    text: 'Se reemplazar\u00E1 el horario de los grupos de ' + (state.config.subjectName || 'la materia') + '.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'S\u00ED, guardar',
                    cancelButtonText: 'Cancelar',
                    confirmButtonColor: '#7460ee',
                    cancelButtonColor: '#6c757d'
                }).then(function (result) {
                    if (result && (result.isConfirmed === true || result.value === true)) submit();
                });
            });
        }
    }

    /* ------------------------------------------------------------ Init */
    function init() {
        var configEl = document.getElementById('hg-config');
        var chips = document.getElementById('hg-chips');
        var grid = document.getElementById('hg-grid');
        if (!configEl || !chips || !grid) return;

        var config;
        try { config = JSON.parse(configEl.textContent); } catch (err) { return; }
        if (!config || !config.groups || !config.groups.length) return;

        state = {
            config: config,
            chips: chips,
            grid: grid,
            summaries: document.getElementById('hg-summaries'),
            unsaved: document.getElementById('hg-unsaved'),
            form: document.getElementById('hg-form'),
            saveBtn: document.getElementById('hg-save'),
            slotsInput: document.getElementById('SlotsJson'),
            selected: 0,
            slots: {},
            cells: {},
            dragging: false,
            dragValue: false,
            currentSiblings: {},
            initialJson: '{}'
        };

        Array.prototype.forEach.call(grid.querySelectorAll('.hg-cell'), function (btn) {
            btn.setAttribute('data-base-class', btn.className);
            state.cells[cellKey(Number(btn.getAttribute('data-weekday')), Number(btn.getAttribute('data-order')))] = btn;
        });

        config.groups.forEach(function (g) {
            var map = {};
            (g.slots || []).forEach(function (s) {
                map[cellKey(Number(s.weekday), Number(s.periodOrder))] = true;
            });
            state.slots[g.id] = map;
        });
        state.initialJson = payloadJson();
        state.currentSiblings = siblingMap(currentGroup());

        renderChips();
        renderSummaries();
        paintAll();
        refreshIndicator();
        bind();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
