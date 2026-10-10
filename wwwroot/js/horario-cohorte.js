/* ==========================================================================
   Horario semanal de clases por COHORTE (semestre-grupo).
   Pages/Catalogs/CourseGroups/Horario.cshtml

   - Una sola grilla para todas las materias del cohorte (gestión + carrera
     + semestre + nombre de grupo): cada materia se pinta como bloque de
     color con su nombre (abreviado si es largo) y docente en tooltip.
   - Agregar: clic en celda libre → modal Bootstrap 4 con materia del
     cohorte y cantidad de periodos consecutivos libres hacia abajo.
   - Editar: clic sobre un bloque → cambiar materia, cambiar duración,
     Mover (modo reprogramar: resalta el bloque y las celdas destino
     válidas; clic en destino mueve el bloque completo; Esc cancela) y
     Quitar (confirmación SweetAlert2 v7).
   - Las celdas donde el docente de la materia seleccionada ya da clase en
     otro grupo fuera del cohorte (TeacherBusy) se marcan en rojo suave:
     es un aviso, el JS no bloquea (el servidor sí).
   - Guardar: arma el JSON {courseGroupId: [{weekday, periodOrder}, ...]},
     pide confirmación (SweetAlert2 v7) y envía el form POST nativo.
     Aviso "cambios sin guardar" con beforeunload.
   ES5, IIFE. Sin dependencias nuevas (jQuery/Bootstrap 4 del layout).
   ========================================================================== */
(function () {
    'use strict';

    var COLOR_COUNT = 8;
    var SHORT_NAME_MAX = 26;
    var state = null;

    /* Todo texto de usuario se inserta con textContent o en propiedades DOM
       (title), nunca via innerHTML: no se necesita escapado HTML. */

    function cellKey(weekday, order) { return weekday + ':' + order; }

    function dayName(weekday) {
        var names = state.config.dayNames || [];
        return names[weekday - 1] || '';
    }

    function maxOrder() {
        var max = 0, k, parts;
        for (k in state.cells) {
            if (!Object.prototype.hasOwnProperty.call(state.cells, k)) continue;
            parts = k.split(':');
            if (Number(parts[1]) > max) max = Number(parts[1]);
        }
        return max;
    }

    function subjectOf(groupId) {
        var i;
        for (i = 0; i < state.config.subjects.length; i++) {
            if (Number(state.config.subjects[i].courseGroupId) === Number(groupId)) return state.config.subjects[i];
        }
        return null;
    }

    function slotsOf(groupId) {
        if (!state.slots[groupId]) state.slots[groupId] = {};
        return state.slots[groupId];
    }

    /** Materias que ocupan la celda (además de los grupos de excludeIds, si se indica). */
    function occupantsOf(key, excludeIds) {
        var found = [], i, subject, map, excluded;
        for (i = 0; i < state.config.subjects.length; i++) {
            subject = state.config.subjects[i];
            excluded = false;
            (excludeIds || []).forEach(function (id) {
                if (Number(subject.courseGroupId) === Number(id)) excluded = true;
            });
            if (excluded) continue;
            map = slotsOf(subject.courseGroupId);
            if (map[key]) found.push(subject);
        }
        return found;
    }

    function shorten(text) {
        text = String(text || '');
        return text.length > SHORT_NAME_MAX ? text.slice(0, SHORT_NAME_MAX - 1) + '\u2026' : text;
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
    /** {"601":[{"weekday":1,"periodOrder":2},...]}: incluye todas las materias (vacío = sin clases). */
    function payloadJson() {
        var payload = {};
        state.config.subjects.forEach(function (s) {
            var arr = [];
            var map = slotsOf(s.courseGroupId);
            var k, parts;
            for (k in map) {
                if (!Object.prototype.hasOwnProperty.call(map, k)) continue;
                parts = k.split(':');
                arr.push({ weekday: Number(parts[0]), periodOrder: Number(parts[1]) });
            }
            arr.sort(function (a, b) {
                return a.weekday - b.weekday || a.periodOrder - b.periodOrder;
            });
            payload[String(s.courseGroupId)] = arr;
        });
        return JSON.stringify(payload);
    }

    function refreshIndicator() {
        if (!state.unsaved) return;
        state.unsaved.hidden = payloadJson() === state.initialJson;
    }

    /* ------------------------------------------------------------ Pintado */
    function clearBusy() {
        var k, cell;
        for (k in state.busyCells) {
            if (!Object.prototype.hasOwnProperty.call(state.busyCells, k)) continue;
            cell = state.cells[k];
            if (cell) {
                cell.classList.remove('hc-cell--busy');
                cell.removeAttribute('data-busy-subject');
            }
        }
        state.busyCells = {};
    }

    /** Marca en rojo suave las celdas donde el docente de la materia ya da clase fuera del cohorte (aviso). */
    function markBusy(groupId) {
        clearBusy();
        state.selected = groupId === undefined ? null : groupId;
        var subject = groupId == null ? null : subjectOf(groupId);
        if (!subject) { renderChips(); return; }
        (subject.busy || []).forEach(function (b) {
            var key = cellKey(Number(b.weekday), Number(b.periodOrder));
            var cell = state.cells[key];
            if (!cell || state.busyCells[key]) return;
            cell.classList.add('hc-cell--busy');
            cell.setAttribute('data-busy-subject', '1');
            cell.title = 'Aviso: el docente de ' + subject.name + ' ya da clase de ' +
                (b.otherSubject || 'otra materia') + ' en ese horario (otro grupo). El servidor bloquea el guardado.';
            state.busyCells[key] = true;
        });
        renderChips();
    }

    function paintBlocks() {
        Array.prototype.forEach.call(state.grid.querySelectorAll('.hc-block'), function (b) {
            b.parentNode.removeChild(b);
        });

        state.config.subjects.forEach(function (s) {
            var map = slotsOf(s.courseGroupId);
            var byDay = {};
            var k, parts, weekday, order;
            for (k in map) {
                if (!Object.prototype.hasOwnProperty.call(map, k)) continue;
                parts = k.split(':');
                weekday = Number(parts[0]);
                order = Number(parts[1]);
                if (!byDay[weekday]) byDay[weekday] = [];
                byDay[weekday].push(order);
            }
            Object.keys(byDay).forEach(function (dayKey) {
                var orders = byDay[dayKey].slice().sort(function (a, b) { return a - b; });
                var start = orders[0];
                var prev = orders[0];
                var flush = function (from, to) { drawBlock(s, Number(dayKey), from, to - from + 1); };
                var i;
                for (i = 1; i < orders.length; i++) {
                    if (orders[i] !== prev + 1) { flush(start, prev); start = orders[i]; }
                    prev = orders[i];
                }
                flush(start, prev);
            });
        });

        /* Bloque "en mano" durante el modo mover: queda resaltado en su lugar de origen. */
        if (state.moving) {
            var selector = '.hc-block[data-course-group-id="' + state.moving.courseGroupId + '"]' +
                '[data-weekday="' + state.moving.weekday + '"][data-start="' + state.moving.start + '"]';
            var movingBlock = state.grid.querySelector(selector);
            if (movingBlock) movingBlock.classList.add('hc-block--moving');
        }
    }

    function drawBlock(subject, weekday, start, count) {
        var color = ((subject.color % COLOR_COUNT) + COLOR_COUNT) % COLOR_COUNT;
        var block = document.createElement('div');
        block.className = 'hc-block hc-block--c' + color;
        block.style.gridColumn = String(weekday + 1);
        block.style.gridRow = (start + 1) + ' / span ' + count;
        block.setAttribute('data-course-group-id', String(subject.courseGroupId));
        block.setAttribute('data-weekday', String(weekday));
        block.setAttribute('data-start', String(start));
        block.setAttribute('data-count', String(count));
        block.setAttribute('role', 'button');
        block.setAttribute('tabindex', '0');
        block.title = subject.name +
            (subject.code ? ' (' + subject.code + ')' : '') +
            ' \u00B7 Grupo ' + subject.group + ' \u00B7 ' + (subject.teacher || 'Sin docente') +
            ' \u00B7 ' + dayName(weekday) + ' periodo' + (count === 1 ? ' ' : 's ') + start +
            (count === 1 ? '' : '\u2013' + (start + count - 1));

        var name = document.createElement('span');
        name.className = 'hc-block__name';
        name.textContent = shorten(subject.name);
        block.appendChild(name);

        var sub = document.createElement('span');
        sub.className = 'hc-block__sub';
        sub.textContent = '(' + subject.group + ')';
        block.appendChild(sub);

        state.grid.appendChild(block);
    }

    function clearTargets() {
        Array.prototype.forEach.call(state.grid.querySelectorAll('.hc-cell--target'), function (cell) {
            cell.classList.remove('hc-cell--target');
            if (cell.getAttribute('data-target') === '1') cell.removeAttribute('title');
            cell.removeAttribute('data-target');
        });
    }

    /** Celdas destino válidas para mover: rango libre de otras materias y dentro de la jornada. */
    function markTargets(moving) {
        clearTargets();
        var top = maxOrder();
        var weekday, start, order, ok, key, cell;
        for (weekday = 1; weekday <= 6; weekday++) {
            for (start = 1; start + moving.count - 1 <= top; start++) {
                ok = true;
                for (order = start; order <= start + moving.count - 1; order++) {
                    key = cellKey(weekday, order);
                    if (occupantsOf(key, [moving.courseGroupId]).length > 0) { ok = false; break; }
                }
                if (!ok) continue;
                cell = state.cells[cellKey(weekday, start)];
                if (!cell || cell.disabled) continue;
                cell.classList.add('hc-cell--target');
                cell.setAttribute('data-target', '1');
                cell.title = 'Mover ' + moving.count + ' periodo(s) aquí';
            }
        }
    }

    function cancelMove() {
        if (!state.moving) return;
        state.moving = null;
        clearTargets();
        paintBlocks();
    }

    function paintAll() {
        paintBlocks();
        renderSummaries();
        refreshIndicator();
    }

    /* ------------------------------------------------------------ Chips */
    function renderChips() {
        state.chips.textContent = '';
        state.config.subjects.forEach(function (s) {
            var color = ((s.color % COLOR_COUNT) + COLOR_COUNT) % COLOR_COUNT;
            var selected = state.selected === Number(s.courseGroupId);
            var chip = document.createElement('button');
            chip.type = 'button';
            chip.className = 'hg-chip hg-chip--c' + color + (selected ? ' hg-chip--active' : '');
            chip.setAttribute('data-course-group-id', String(s.courseGroupId));
            chip.setAttribute('aria-pressed', selected ? 'true' : 'false');
            chip.title = s.name + (s.code ? ' (' + s.code + ')' : '') + ' \u00B7 ' + (s.teacher || 'Sin docente');

            var dot = document.createElement('span');
            dot.className = 'hg-dot hg-dot--c' + color;
            dot.setAttribute('aria-hidden', 'true');
            chip.appendChild(dot);

            var label = document.createElement('span');
            label.className = 'hg-chip__label';
            label.textContent = shorten(s.name);
            chip.appendChild(label);

            var meta = document.createElement('span');
            meta.className = 'hg-chip__meta';
            meta.textContent = ' (' + s.group + ')';
            chip.appendChild(meta);

            state.chips.appendChild(chip);
        });
    }

    function renderSummaries() {
        if (!state.summaries) return;
        state.summaries.textContent = '';
        state.config.subjects.forEach(function (s) {
            var color = ((s.color % COLOR_COUNT) + COLOR_COUNT) % COLOR_COUNT;
            var row = document.createElement('div');
            row.className = 'hg-summary';

            var dot = document.createElement('span');
            dot.className = 'hg-dot hg-dot--c' + color;
            dot.setAttribute('aria-hidden', 'true');
            row.appendChild(dot);

            var name = document.createElement('strong');
            name.className = 'hg-summary__name';
            name.textContent = s.name + ' (Grupo ' + s.group + '):';
            row.appendChild(name);

            var text = document.createElement('span');
            text.className = 'hg-summary__text' + (Object.keys(slotsOf(s.courseGroupId)).length === 0 ? ' hg-summary__text--empty' : '');
            text.textContent = ' ' + summarize(slotsOf(s.courseGroupId));
            row.appendChild(text);

            state.summaries.appendChild(row);
        });
    }

    /* ------------------------------------------------------------ Modal */
    function showModal() {
        if (window.jQuery) window.jQuery('#hc-modal').modal('show');
    }

    function hideModal() {
        if (window.jQuery) window.jQuery('#hc-modal').modal('hide');
    }

    function fillSubjectSelect(selectedGroupId) {
        var select = state.modalSubject;
        select.textContent = '';
        state.config.subjects.forEach(function (s) {
            var option = document.createElement('option');
            option.value = String(s.courseGroupId);
            option.textContent = s.name + ' (Grupo ' + s.group + ')';
            if (Number(selectedGroupId) === Number(s.courseGroupId)) option.selected = true;
            select.appendChild(option);
        });
    }

    function fillCountSelect(from, maxCount, selected) {
        var select = state.modalCount;
        select.textContent = '';
        if (maxCount < 1) maxCount = 1;
        for (var n = 1; n <= maxCount; n++) {
            var option = document.createElement('option');
            option.value = String(n);
            option.textContent = n + ' periodo' + (n === 1 ? '' : 's') +
                ' (' + dayName(state.modalWeekday) + ' ' + from + (n === 1 ? '' : '\u2013' + (from + n - 1)) + ')';
            if (n === selected) option.selected = true;
            select.appendChild(option);
        }
    }

    /** Cantidad máxima de periodos consecutivos hacia abajo sin pisar OTRAS materias. */
    function maxRunDown(weekday, start, excludeIds) {
        var top = maxOrder();
        var order = start;
        while (order + 1 <= top &&
            occupantsOf(cellKey(weekday, order + 1), excludeIds).length === 0) {
            order++;
        }
        return order - start + 1;
    }

    /** Aviso (no bloqueo) si el rango elegido toca celdas donde el docente ya da clase fuera del cohorte. */
    function busyWarning(subject, weekday, from, count) {
        var hits = {};
        (subject.busy || []).forEach(function (b) {
            var order = Number(b.periodOrder);
            if (Number(b.weekday) === weekday && order >= from && order < from + count) {
                hits[b.otherSubject || 'otra materia'] = true;
            }
        });
        var names = Object.keys(hits);
        if (names.length === 0) return null;
        return 'Aviso: el docente ya da clase de ' + names.join(', ') +
            ' en ese horario (otro grupo). El servidor rechazar\u00E1 el guardado.';
    }

    function setModalWarning(text) {
        var box = state.modalWarning;
        if (!box) return;
        if (text) {
            box.textContent = text;
            box.hidden = false;
        } else {
            box.textContent = '';
            box.hidden = true;
        }
    }

    function refreshModalWarning() {
        if (!state.modalContext) return;
        var groupId = Number(state.modalSubject.value);
        var subject = subjectOf(groupId);
        var from = state.modalStart;
        var count = Number(state.modalCount.value) || 1;
        setModalWarning(subject ? busyWarning(subject, state.modalWeekday, from, count) : null);
    }

    /** Abre el modal en modo agregar (celda libre) o editar (bloque existente). */
    function openModal(mode, weekday, start, subject, count) {
        state.modalContext = { mode: mode, weekday: weekday, start: start, groupId: subject ? subject.courseGroupId : null };
        state.modalWeekday = weekday;
        state.modalStart = start;

        fillSubjectSelect(subject ? subject.courseGroupId : null);

        var maxCount = mode === 'add'
            ? maxRunDown(weekday, start, [])
            : maxRunDown(weekday, start, [subject.courseGroupId]);
        fillCountSelect(start, maxCount, count || 1);

        state.modalTitle.textContent = mode === 'add' ? 'Agregar clase \u00B7 ' + dayName(weekday) + ' periodo ' + start
            : 'Editar clase \u00B7 ' + dayName(weekday) + ' periodos ' + start + '\u2013' + (start + (count || 1) - 1);
        state.modalAccept.textContent = mode === 'add' ? 'Agregar' : 'Guardar cambios';
        state.modalMove.hidden = mode !== 'edit';
        state.modalRemove.hidden = mode !== 'edit';

        markBusy(subject ? subject.courseGroupId : undefined);
        refreshModalWarning();
        showModal();
    }

    /** Aplica el cambio del modal si el rango está libre de otras materias; false si choca. */
    function applyModalChange() {
        var ctx = state.modalContext;
        if (!ctx) return;
        var newGroupId = Number(state.modalSubject.value);
        var subject = subjectOf(newGroupId);
        var count = Number(state.modalCount.value) || 1;
        if (!subject) return;

        var exclude = ctx.mode === 'edit' ? [ctx.groupId] : [];
        var keys = [];
        var blocked = null;
        for (var i = 0; i < count; i++) {
            var key = cellKey(state.modalWeekday, ctx.start + i);
            var others = occupantsOf(key, exclude);
            if (others.length > 0 && Number(others[0].courseGroupId) !== Number(newGroupId)) {
                blocked = others[0];
                break;
            }
            keys.push(key);
        }
        if (blocked) {
            if (typeof Swal === 'undefined') return;
            Swal.fire({
                title: 'Celda ocupada',
                text: blocked.name + ' (Grupo ' + blocked.group + ') ya tiene clase ahí. Elija otra celda o duración.',
                icon: 'warning',
                confirmButtonText: 'Entendido',
                confirmButtonColor: '#7460ee'
            });
            return;
        }

        if (ctx.mode === 'edit') {
            var oldMap = slotsOf(ctx.groupId);
            for (var j = 0; j < count; j++) delete oldMap[cellKey(state.modalWeekday, ctx.start + j)];
        }
        var map = slotsOf(newGroupId);
        keys.forEach(function (key) { map[key] = true; });

        hideModal();
        state.modalContext = null;
        markBusy(newGroupId);
        paintAll();
    }

    function removeBlock(ctx) {
        var subject = subjectOf(ctx.groupId);
        var submit = function () {
            var map = slotsOf(ctx.groupId);
            for (var i = 0; i < ctx.count; i++) delete map[cellKey(ctx.weekday, ctx.start + i)];
            hideModal();
            state.modalContext = null;
            markBusy(null);
            paintAll();
        };
        if (typeof Swal === 'undefined') { submit(); return; }
        Swal.fire({
            title: '\u00BFQuitar la clase?',
            text: 'Se quitar\u00E1 ' + (subject ? subject.name : 'la materia') + ' el ' + dayName(ctx.weekday) +
                ' periodo' + (ctx.count === 1 ? ' ' : 's ') + ctx.start + (ctx.count === 1 ? '' : '\u2013' + (ctx.start + ctx.count - 1)) + '.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'S\u00ED, quitar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#f62d51',
            cancelButtonColor: '#6c757d'
        }).then(function (result) {
            if (result && (result.isConfirmed === true || result.value === true)) submit();
        });
    }

    /* ------------------------------------------------------------ Eventos */
    function bind() {
        /* Chips: seleccionar materia (marca sus celdas TeacherBusy). */
        state.chips.addEventListener('click', function (e) {
            var chip = e.target.closest ? e.target.closest('.hg-chip') : null;
            if (!chip) return;
            var groupId = Number(chip.getAttribute('data-course-group-id'));
            markBusy(state.selected === groupId ? null : groupId);
        });

        if (state.config.readOnly === true) {
            /* Solo lectura: los bloques solo muestran el tooltip y permiten elegir la materia. */
            state.grid.addEventListener('click', function (e) {
                var block = e.target.closest ? e.target.closest('.hc-block') : null;
                if (!block) return;
                markBusy(Number(block.getAttribute('data-course-group-id')));
            });
            return; /* solo lectura: sin edición ni guardado */
        }

        state.modalSubject.addEventListener('change', function () {
            if (!state.modalContext) return;
            if (state.modalContext.mode === 'add') {
                fillCountSelect(state.modalStart, maxRunDown(state.modalWeekday, state.modalStart, []), 1);
            } else {
                /* Cambio de materia: el bloque antiguo se libera y la nueva materia no puede pisarse a sí misma. */
                var ids = [state.modalContext.groupId, Number(state.modalSubject.value)];
                fillCountSelect(state.modalStart, maxRunDown(state.modalWeekday, state.modalStart, ids), 1);
            }
            refreshModalWarning();
        });
        state.modalCount.addEventListener('change', refreshModalWarning);
        state.modalAccept.addEventListener('click', applyModalChange);
        state.modalRemove.addEventListener('click', function () {
            if (state.modalContext && state.modalContext.mode === 'edit') removeBlock(state.modalContext);
        });
        state.modalMove.addEventListener('click', function () {
            var ctx = state.modalContext;
            if (!ctx || ctx.mode !== 'edit') return;
            state.moving = { courseGroupId: ctx.groupId, weekday: ctx.weekday, start: ctx.start, count: Number(state.modalCount.value) || 1 };
            state.modalContext = null;
            hideModal();
            markBusy(state.moving.courseGroupId);
            paintBlocks();
            markTargets(state.moving);
        });

        state.grid.addEventListener('click', function (e) {
            if (state.moving) {
                var target = e.target.closest ? e.target.closest('button.hc-cell.hc-cell--target') : null;
                if (target) {
                    e.preventDefault();
                    var weekday = Number(target.getAttribute('data-weekday'));
                    var start = Number(target.getAttribute('data-order'));
                    var moving = state.moving;
                    state.moving = null;
                    clearTargets();
                    if (weekday === moving.weekday && start === moving.start) { paintBlocks(); return; }   /* mismo lugar: nada */
                    var map = slotsOf(moving.courseGroupId);
                    var i;
                    for (i = 0; i < moving.count; i++) delete map[cellKey(moving.weekday, moving.start + i)];
                    for (i = 0; i < moving.count; i++) map[cellKey(weekday, start + i)] = true;
                    markBusy(moving.courseGroupId);
                    paintAll();
                    return;
                }
                /* Clic sobre el bloque en mano: cancela el movimiento; otros bloques se ignoran. */
                var movingBlock = e.target.closest ? e.target.closest('.hc-block') : null;
                if (movingBlock && Number(movingBlock.getAttribute('data-course-group-id')) === Number(state.moving.courseGroupId)) {
                    cancelMove();
                }
                return;
            }

            var block = e.target.closest ? e.target.closest('.hc-block') : null;
            if (block) {
                e.preventDefault();
                var groupId = Number(block.getAttribute('data-course-group-id'));
                var subject = subjectOf(groupId);
                if (!subject) return;
                openModal('edit', Number(block.getAttribute('data-weekday')), Number(block.getAttribute('data-start')),
                    subject, Number(block.getAttribute('data-count')));
                return;
            }

            var cell = e.target.closest ? e.target.closest('button.hc-cell') : null;
            if (!cell || cell.disabled) return;
            var weekday = Number(cell.getAttribute('data-weekday'));
            var order = Number(cell.getAttribute('data-order'));
            if (occupantsOf(cellKey(weekday, order), null).length > 0) return;   /* ocupada: solo los bloques capturan */
            openModal('add', weekday, order, null, 1);
        });

        /* Esc cancela el modo mover. */
        document.addEventListener('keydown', function (e) {
            if ((e.key === 'Escape' || e.key === 'Esc') && state.moving) {
                e.preventDefault();
                cancelMove();
            }
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
                    text: 'Se reemplazar\u00E1 el horario de las materias del cohorte ' +
                        (state.config.cohortLabel || 'seleccionado') + '.',
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

        /* Aviso de cambios sin guardar al cerrar la página. */
        window.addEventListener('beforeunload', function (e) {
            if (!state || payloadJson() === state.initialJson) return;
            e.preventDefault();
            e.returnValue = '';
        });
    }

    /* ------------------------------------------------------------ Init */
    function init() {
        var configEl = document.getElementById('hc-config');
        var chips = document.getElementById('hc-chips');
        var grid = document.getElementById('hc-grid');
        if (!configEl || !chips || !grid) return;

        var config;
        try { config = JSON.parse(configEl.textContent); } catch (err) { return; }
        if (!config || !config.subjects || !config.subjects.length) return;

        state = {
            config: config,
            chips: chips,
            grid: grid,
            summaries: document.getElementById('hc-summaries'),
            unsaved: document.getElementById('hc-unsaved'),
            form: document.getElementById('hc-form'),
            saveBtn: document.getElementById('hc-save'),
            slotsInput: document.getElementById('SlotsJson'),
            modalTitle: document.getElementById('hc-modal-title'),
            modalSubject: document.getElementById('hc-modal-subject'),
            modalCount: document.getElementById('hc-modal-count'),
            modalWarning: document.getElementById('hc-modal-warning'),
            modalAccept: document.getElementById('hc-modal-accept'),
            modalMove: document.getElementById('hc-modal-move'),
            modalRemove: document.getElementById('hc-modal-remove'),
            selected: null,
            slots: {},
            cells: {},
            busyCells: {},
            moving: null,
            modalContext: null,
            modalWeekday: 1,
            modalStart: 1,
            initialJson: '{}'
        };

        Array.prototype.forEach.call(grid.querySelectorAll('button.hc-cell'), function (btn) {
            state.cells[cellKey(Number(btn.getAttribute('data-weekday')), Number(btn.getAttribute('data-order')))] = btn;
        });

        config.subjects.forEach(function (s) {
            var map = {};
            (s.slots || []).forEach(function (x) {
                map[cellKey(Number(x.weekday), Number(x.periodOrder))] = true;
            });
            state.slots[s.courseGroupId] = map;
        });
        state.initialJson = payloadJson();

        renderChips();
        renderSummaries();
        paintBlocks();
        refreshIndicator();
        bind();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
