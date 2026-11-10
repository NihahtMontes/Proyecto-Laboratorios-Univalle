// Asistente "Solicitar práctica" del portal docente (Pages/Docente/NuevaSolicitud).
// Estado en memoria; el servidor revalida todo al enviar. Sin dependencias además de jQuery/SweetAlert2 v7 del layout.
(function () {
    'use strict';

    var cfgEl = document.getElementById('wizardConfig');
    if (!cfgEl) return;
    var cfg = JSON.parse(cfgEl.textContent);
    var initial = JSON.parse(document.getElementById('wizardState').textContent || 'null');
    var token = (document.querySelector('meta[name="request-verification-token"]') || {}).content || '';

    var state = {
        step: 1,
        planningId: null,
        groupId: null,
        practiceId: null,
        practiceName: null,
        students: null,
        date: null,
        startPeriodId: null,
        periodCount: 2,
        labId: null,
        lines: [],
        practices: [],
        check: null,
        weekDate: cfg.minDate,
        kitLoadedFor: null
    };

    // ───────── utilidades ─────────
    function $(id) { return document.getElementById(id); }
    function esc(v) { return String(v == null ? '' : v).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
    function url(handler, params) {
        var q = ['handler=' + encodeURIComponent(handler)];
        Object.keys(params || {}).forEach(function (k) {
            if (params[k] !== null && params[k] !== undefined && params[k] !== '') q.push(encodeURIComponent(k) + '=' + encodeURIComponent(params[k]));
        });
        return cfg.baseUrl + '?' + q.join('&');
    }
    function getJson(handler, params) {
        return fetch(url(handler, params), { credentials: 'same-origin' }).then(function (r) {
            if (!r.ok) throw new Error('HTTP ' + r.status);
            return r.json();
        });
    }
    function postJson(handler, body) {
        return fetch(url(handler), {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify(body)
        }).then(function (r) {
            if (!r.ok) throw new Error('HTTP ' + r.status);
            return r.json();
        });
    }
    function group() { return cfg.groups.filter(function (g) { return g.id === state.groupId; })[0] || null; }
    function lab() { return cfg.labs.filter(function (l) { return l.id === state.labId; })[0] || null; }
    function period(id) { return cfg.periods.filter(function (p) { return p.id === id; })[0] || null; }
    function periodByOrder(o) { return cfg.periods.filter(function (p) { return p.order === o; })[0] || null; }
    function timeText() {
        var s = period(state.startPeriodId);
        if (!s) return null;
        var e = periodByOrder(s.order + state.periodCount - 1);
        return s.start + '–' + (e ? e.end : s.end);
    }
    function fmtDate(iso) {
        if (!iso) return '';
        var d = new Date(iso + 'T00:00:00');
        return d.toLocaleDateString('es-BO', { weekday: 'long', day: 'numeric', month: 'long' });
    }
    // Fecha local formateada a mano: toISOString() pasa por UTC y puede cambiar el día según la hora local.
    function isoDate(d) {
        return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
    }
    function mondayOf(iso) {
        var d = new Date(iso + 'T00:00:00');
        var diff = (d.getDay() + 6) % 7;
        d.setDate(d.getDate() - diff);
        return isoDate(d);
    }
    function addDays(iso, n) {
        var d = new Date(iso + 'T00:00:00');
        d.setDate(d.getDate() + n);
        return isoDate(d);
    }
    // Contador de secuencia por tipo de petición: descarta respuestas que llegan fuera de orden.
    var seq = { practices: 0, week: 0, check: 0, kit: 0, articles: 0 };
    function stale(type, id) { return id !== seq[type]; }
    // Activa elementos con rol de botón también con Enter/Espacio (accesibilidad con teclado).
    function keyActivate(el, fn) {
        el.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ' || e.keyCode === 32) { e.preventDefault(); fn(); }
        });
    }
    function toast(type, title) {
        Swal.fire({ toast: true, position: 'top-end', type: type, title: title, showConfirmButton: false, timer: 2200 });
    }
    function linesPayload() { return state.lines.map(function (l) { return { articleId: l.articleId, quantity: Number(l.quantity) || 0 }; }); }
    function debounce(fn, ms) { var t; return function () { var a = arguments; clearTimeout(t); t = setTimeout(function () { fn.apply(null, a); }, ms); }; }

    // ───────── PASO 1: grupo y práctica ─────────
    function renderGroups() {
        $('groupList').innerHTML = cfg.groups.map(function (g) {
            return '<div class="col-md-6 col-xl-4 mb-3"><div class="dc-pick' + (g.id === state.groupId ? ' is-selected' : '') + '" data-group="' + g.id + '" role="button" tabindex="0">' +
                '<div class="dc-pick__title">' + esc(g.subject) + '</div>' +
                '<div class="dc-pick__meta"><i class="fas fa-users mr-1"></i>Grupo ' + esc(g.name) +
                (g.students ? ' · ' + g.students + ' estudiantes' : '') + (g.labCode ? ' · <i class="fas fa-building mx-1"></i>' + esc(g.labCode) : '') + '</div>' +
                '<div class="dc-pick__meta">' + esc(g.subjectCode) + '</div></div></div>';
        }).join('');
        Array.prototype.forEach.call(document.querySelectorAll('[data-group]'), function (el) {
            var pick = function () { selectGroup(Number(el.getAttribute('data-group'))); };
            el.addEventListener('click', pick);
            keyActivate(el, pick);
        });
    }

    function selectGroup(id, keepPractice) {
        var changed = state.groupId !== id;
        state.groupId = id;
        var g = group();
        if (changed && !keepPractice) {
            state.practiceId = null;
            state.practiceName = null;
            state.lines = [];
            state.kitLoadedFor = null;
            state.students = g && g.students ? g.students : null;
            if (!state.labId && g && g.labId) state.labId = g.labId;
        }
        $('students').value = state.students || '';
        $('studentsRow').style.display = '';
        $('practiceBlock').style.display = '';
        renderGroups();
        $('practiceList').innerHTML = '<div class="col-12 text-muted small py-3"><span class="spinner-border spinner-border-sm mr-2"></span>Cargando prácticas…</div>';
        var my = ++seq.practices;
        getJson('Practices', { groupId: id }).then(function (list) {
            if (stale('practices', my)) return;
            state.practices = list;
            if (state.practiceId) {
                var p = list.filter(function (x) { return x.id === state.practiceId; })[0];
                state.practiceName = p ? (p.number ? 'P-' + p.number + ' · ' : '') + p.name : state.practiceName;
            }
            renderPractices();
            renderSide();
        }).catch(function () {
            if (stale('practices', my)) return;
            $('practiceList').innerHTML = '<div class="col-12 text-danger small">No se pudieron cargar las prácticas. Recarga la página.</div>';
        });
        renderSide();
    }

    function renderPractices() {
        var term = ($('practiceSearch').value || '').toLowerCase();
        var list = state.practices.filter(function (p) {
            return !term || (p.name + ' ' + (p.number || '') + ' ' + (p.code || '')).toLowerCase().indexOf(term) >= 0;
        });
        if (list.length === 0) {
            $('practiceList').innerHTML = '<div class="col-12 dc-empty"><i class="fas fa-flask"></i>No hay prácticas en el catálogo de esta materia' + (term ? ' con ese texto' : '') + '.</div>';
            return;
        }
        $('practiceList').innerHTML = list.map(function (p) {
            // Solo informa: una práctica se puede pedir las veces que se necesite (cada solicitud es una sesión aparte).
            var badges = '';
            if (p.done) badges += '<span class="badge badge-info ml-1" title="Sesiones ya realizadas por este grupo">Realizada ' + p.done + 'x</span>';
            if (p.planned) badges += '<span class="badge badge-success ml-1" title="Sesiones aprobadas próximas">Aprobada ' + p.planned + 'x</span>';
            if (p.pending) badges += '<span class="badge badge-primary ml-1" title="Solicitudes en revisión u observadas">En revisión ' + p.pending + '</span>';
            return '<div class="col-md-6 mb-2"><div class="dc-pick' + (p.id === state.practiceId ? ' is-selected' : '') + '" data-practice="' + p.id + '" role="button" tabindex="0">' +
                '<div class="d-flex"><span class="badge badge-light border mr-2 align-self-start">' + esc(p.number ? 'P-' + p.number : p.code) + '</span>' +
                '<div class="flex-grow-1"><div class="dc-pick__title" style="font-size:.88rem;">' + esc(p.name) + badges + '</div>' +
                '<div class="dc-pick__meta"><i class="fas fa-box-open mr-1"></i>' + (p.kitCount > 0 ? 'Kit de ' + p.kitCount + ' artículo(s)' : 'Sin kit: agregarás el material a mano') + '</div></div></div></div></div>';
        }).join('');
        Array.prototype.forEach.call(document.querySelectorAll('[data-practice]'), function (el) {
            var pick = function () {
                var id = Number(el.getAttribute('data-practice'));
                var p = state.practices.filter(function (x) { return x.id === id; })[0];
                if (state.practiceId !== id) { state.lines = []; state.kitLoadedFor = null; }
                state.practiceId = id;
                state.practiceName = (p.number ? 'P-' + p.number + ' · ' : '') + p.name;
                if (p.done || p.planned || p.pending)
                    toast('info', 'Tu grupo ya tiene esta práctica ' + (p.done + p.planned + p.pending) + ' vez/veces: se agregará como una sesión más');
                renderPractices();
                renderSide();
            };
            el.addEventListener('click', pick);
            keyActivate(el, pick);
        });
    }

    // ───────── PASO 2: horario y laboratorio ─────────
    function suggest() {
        var btn = $('btnSuggest');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm mr-2"></span>Buscando los mejores horarios…';
        // Con el kit cargado las sugerencias también priorizan horarios donde alcanza el material.
        loadKit(false).catch(function () { }).then(function () {
            return postJson('Suggest', { groupId: state.groupId, periodCount: state.periodCount, labId: state.labId, lines: linesPayload() })
            .then(function (list) {
                if (!list.length) {
                    $('suggestions').innerHTML = '<div class="col-12"><div class="alert alert-light border small mb-0"><i class="fas fa-search mr-1"></i>No encontramos horarios libres con esa duración en tus laboratorios habituales. Prueba con menos periodos o elige en la grilla.</div></div>';
                    return;
                }
                $('suggestions').innerHTML = list.map(function (s, i) {
                    return '<div class="col-md-4 mb-3"><div class="dc-suggestion" data-sug="' + i + '">' +
                        '<div class="small text-uppercase font-weight-bold mb-1" style="color:#7460ee;">' + (i === 0 ? '<i class="fas fa-star mr-1"></i>Mejor opción' : 'Opción ' + (i + 1)) + '</div>' +
                        '<div class="dc-suggestion__date">' + esc(s.dateText) + '</div>' +
                        '<div class="font-weight-bold text-dark mb-1"><i class="far fa-clock mr-1"></i>' + esc(s.time) + ' <span class="text-muted font-weight-normal small">(' + esc(s.periodsText) + ')</span></div>' +
                        '<div class="mb-2"><span class="badge badge-light border"><i class="fas fa-building mr-1"></i>' + esc(s.laboratoryCode) + '</span> <span class="small text-muted">' + esc(s.laboratoryName) + '</span></div>' +
                        s.reasons.map(function (r) {
                            var bad = r.indexOf('no alcanzan') >= 0;
                            return '<div class="dc-suggestion__reason"><i class="fas ' + (bad ? 'fa-exclamation-triangle text-warning' : 'fa-check') + '"></i>' + esc(r) + '</div>';
                        }).join('') +
                        '</div></div>';
                }).join('');
                Array.prototype.forEach.call(document.querySelectorAll('[data-sug]'), function (el) {
                    el.addEventListener('click', function () {
                        var s = list[Number(el.getAttribute('data-sug'))];
                        state.date = s.date;
                        state.startPeriodId = s.startPeriodId;
                        state.labId = s.laboratoryId;
                        state.weekDate = s.date;
                        $('labSelect').value = String(s.laboratoryId);
                        $('weekDate').value = s.date;
                        Array.prototype.forEach.call(document.querySelectorAll('[data-sug]'), function (x) { x.classList.remove('is-selected'); });
                        el.classList.add('is-selected');
                        loadWeek();
                        runCheck();
                        renderSide();
                    });
                });
            })
            ;
        })
            .catch(function () { toast('error', 'No se pudo buscar horarios'); })
            .then(function () {
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-magic mr-2"></i>Buscar horario libre';
            });
    }

    var weekData = null;
    function loadWeek() {
        if (!state.labId) {
            $('weekGrid').innerHTML = '<div class="text-muted small py-3 text-center">Elige un laboratorio para ver su semana.</div>';
            return;
        }
        $('weekGrid').innerHTML = '<div class="text-muted small py-3 text-center"><span class="spinner-border spinner-border-sm mr-2"></span>Cargando la semana…</div>';
        var my = ++seq.week;
        getJson('Week', { labId: state.labId, date: state.weekDate, excludeId: state.planningId }).then(function (data) {
            if (stale('week', my)) return;
            weekData = data;
            renderWeek();
        }).catch(function () {
            if (stale('week', my)) return;
            $('weekGrid').innerHTML = '<div class="text-danger small">No se pudo cargar la semana.</div>';
        });
    }

    function renderWeek() {
        if (!weekData) return;
        var sel = period(state.startPeriodId);
        var html = '<table class="dc-grid"><thead><tr><th class="dc-grid__period"></th>';
        weekData.days.forEach(function (d) {
            var dt = new Date(d.date + 'T00:00:00');
            html += '<th>' + dt.toLocaleDateString('es-BO', { weekday: 'short' }) + ' ' + dt.getDate() + '</th>';
        });
        html += '</tr></thead><tbody>';
        weekData.periods.forEach(function (p) {
            html += '<tr><th class="dc-grid__period">P' + p.order + ' <small class="d-block text-muted">' + p.start + '</small></th>';
            weekData.days.forEach(function (d) {
                var cell = d.cells.filter(function (c) { return c.order === p.order; })[0];
                var selected = sel && state.date === d.date && p.order >= sel.order && p.order < sel.order + state.periodCount;
                var cls = 'dc-cell is-' + cell.state + (selected ? ' is-selected' : '');
                var label = selected ? 'Tu práctica' : cell.state === 'busy' ? 'Ocupado' : cell.state === 'gap' ? 'Limpieza' :
                    cell.state === 'pending' ? 'En revisión' : cell.state === 'mine' ? 'Tuya' : cell.state === 'off' ? (d.offReason || '') : '';
                html += '<td class="' + cls + '" data-date="' + d.date + '" data-period="' + cell.periodId + '" data-state="' + cell.state + '" title="' + esc(label) + '" role="button" tabindex="0">' + esc(label) + '</td>';
            });
            html += '</tr>';
        });
        html += '</tbody></table>';
        $('weekGrid').innerHTML = html;
        Array.prototype.forEach.call(document.querySelectorAll('#weekGrid td[data-period]'), function (td) {
            var activate = function () {
                var st = td.getAttribute('data-state');
                if (st === 'off') { toast('info', td.getAttribute('title') || 'Día no disponible'); return; }
                if (st === 'busy' || st === 'gap' || st === 'mine') { toast('warning', 'Ese periodo no está libre'); return; }
                state.date = td.getAttribute('data-date');
                state.startPeriodId = Number(td.getAttribute('data-period'));
                Array.prototype.forEach.call(document.querySelectorAll('[data-sug]'), function (x) { x.classList.remove('is-selected'); });
                renderWeek();
                runCheck();
                renderSide();
            };
            td.addEventListener('click', activate);
            keyActivate(td, activate);
        });
    }

    var runCheck = debounce(function () {
        if (!state.groupId || !state.date || !state.startPeriodId || !state.labId) { ++seq.check; state.check = null; renderIssues(); renderLines(); renderSide(); return; }
        var my = ++seq.check;
        $('slotIssues').innerHTML = '<div class="small text-muted"><span class="spinner-border spinner-border-sm mr-2"></span>Verificando choques y material…</div>';
        postJson('Check', {
            planningId: state.planningId, groupId: state.groupId, date: state.date, startPeriodId: state.startPeriodId,
            periodCount: state.periodCount, labId: state.labId, students: state.students, lines: linesPayload()
        }).then(function (res) {
            if (stale('check', my)) return;
            state.check = res;
            renderIssues();
            renderLines();
            renderSide();
        }).catch(function () {
            if (stale('check', my)) return;
            $('slotIssues').innerHTML = '<div class="small text-danger">No se pudo verificar. Intenta de nuevo.</div>';
        });
    }, 350);

    function blocks() { return state.check ? state.check.issues.filter(function (i) { return i.severity === 'Block'; }) : []; }
    function warnings() { return state.check ? state.check.issues.filter(function (i) { return i.severity !== 'Block'; }) : []; }

    function renderIssues() {
        if (!state.check) { $('slotIssues').innerHTML = ''; return; }
        var b = blocks(), w = warnings();
        var html = '';
        if (b.length === 0) {
            var l = lab();
            html += '<div class="alert border-0 mb-2 d-flex align-items-center" style="background:#e8f8f4;"><i class="fas fa-check-circle fa-lg mr-3" style="color:#36bea6;"></i><div><strong>Horario disponible</strong><div class="small">' +
                esc(fmtDate(state.date)) + ' · ' + esc(timeText()) + ' · ' + esc(l ? l.code : '') + '</div></div></div>';
        }
        b.forEach(function (i) {
            html += '<div class="alert alert-danger border-0 mb-2 small"><i class="fas fa-ban mr-2"></i><strong>' + esc(i.title) + ':</strong> ' + esc(i.message) + '</div>';
        });
        w.forEach(function (i) {
            var info = i.severity === 'Info';
            html += '<div class="alert border-0 mb-2 small" style="background:' + (info ? '#eaf0ff' : '#fff8e8') + ';"><i class="fas ' + (info ? 'fa-info-circle text-info' : 'fa-exclamation-triangle text-warning') + ' mr-2"></i><strong>' + esc(i.title) + ':</strong> ' + esc(i.message) + '</div>';
        });
        $('slotIssues').innerHTML = html;
    }

    // ───────── PASO 3: material ─────────
    function kitHintLoaded(count, students) {
        $('kitHint').textContent = !count
            ? 'Esta práctica no tiene kit registrado: agrega los artículos que necesitas.'
            : 'Cargamos el kit de la práctica' + (students ? ' para ' + students + ' estudiantes' : '') + '. Ajusta lo que necesites.';
    }

    function loadKit(force) {
        if (!state.practiceId || !state.groupId) return Promise.resolve();
        var key = state.practiceId + '|' + (state.students || '');
        if (!force && (state.kitLoadedFor === key || state.lines.length > 0)) return Promise.resolve();
        $('linesBody').innerHTML = '<tr><td colspan="5" class="text-center text-muted small py-3"><span class="spinner-border spinner-border-sm mr-2"></span>Cargando el kit de la práctica…</td></tr>';
        var my = ++seq.kit;
        return getJson('Kit', { practiceId: state.practiceId, groupId: state.groupId, students: state.students }).then(function (kit) {
            if (stale('kit', my)) return;
            state.lines = kit.map(function (k) { return { articleId: k.articleId, code: k.code, name: k.name, category: k.category, unit: k.unit, quantity: k.quantity, note: k.note }; });
            state.kitLoadedFor = key;
            kitHintLoaded(kit.length, state.students);
            renderLines();
            runCheck();
        }).catch(function () {
            if (stale('kit', my)) return;
            $('linesBody').innerHTML = '<tr><td colspan="5" class="text-center text-danger small py-3">No se pudo cargar el kit de la práctica. <button type="button" class="btn btn-sm btn-outline-danger rounded-pill ml-2" data-retry="kit"><i class="fas fa-redo mr-1"></i>Reintentar</button></td></tr>';
            var retry = document.querySelector('[data-retry="kit"]');
            if (retry) retry.addEventListener('click', function () { loadKit(true); });
        });
    }

    function materialFor(articleId) {
        if (!state.check) return null;
        return state.check.material.filter(function (m) { return m.articleId === articleId; })[0] || null;
    }

    function renderLines() {
        var body = $('linesBody');
        if (!body) return;
        if (state.lines.length === 0) {
            body.innerHTML = '<tr><td colspan="5" class="dc-empty"><i class="fas fa-box-open"></i>Aún no hay artículos. Usa el buscador de abajo.</td></tr>';
            renderSide();
            return;
        }
        body.innerHTML = state.lines.map(function (l, idx) {
            var m = materialFor(l.articleId);
            var css = m ? m.stateCss : 'secondary';
            var stock = m && m.stock != null ? Number(m.stock) : null;
            var free = m && m.free != null ? Number(m.free) : null;
            return '<tr>' +
                '<td><span class="dc-light dc-light--' + css + '" title="' + esc(m ? m.stateText : 'Elige horario para verificar') + '"></span>' +
                '<strong class="text-dark">' + esc(l.name) + '</strong><div class="small text-muted ml-4">' + esc(l.code) + (l.category ? ' · ' + esc(l.category) : '') + '</div>' +
                (m ? '<div class="dc-explain ml-4">' + esc(m.explanation) + '</div>' : '') + '</td>' +
                '<td class="text-center align-middle">' + (stock == null ? '<span class="text-muted">—</span>' : stock) + '</td>' +
                '<td class="text-center align-middle">' + (free == null ? '<span class="text-muted">—</span>' : '<span class="badge badge-pill badge-' + css + '">' + free + '</span>') + '</td>' +
                '<td class="text-center align-middle"><input type="number" class="form-control form-control-sm dc-qty mx-auto" min="0" step="0.01" value="' + esc(l.quantity) + '" data-qty="' + idx + '" /></td>' +
                '<td class="text-right align-middle"><button type="button" class="btn btn-sm btn-link text-danger" data-remove="' + idx + '" title="Quitar"><i class="fas fa-trash-alt"></i></button></td>' +
                '</tr>';
        }).join('');
        Array.prototype.forEach.call(body.querySelectorAll('[data-qty]'), function (input) {
            input.addEventListener('input', function () {
                state.lines[Number(input.getAttribute('data-qty'))].quantity = Number(input.value) || 0;
                runCheck();
                renderSide();
            });
        });
        Array.prototype.forEach.call(body.querySelectorAll('[data-remove]'), function (btn) {
            btn.addEventListener('click', function () {
                state.lines.splice(Number(btn.getAttribute('data-remove')), 1);
                renderLines();
                runCheck();
            });
        });
        renderSide();
    }

    var searchArticles = debounce(function () {
        var term = $('articleTerm').value.trim();
        var cat = $('articleCategory').value;
        if (term.length < 2 && !cat) { $('articleResults').innerHTML = ''; return; }
        var my = ++seq.articles;
        getJson('Articles', { term: term, categoryId: cat }).then(function (rows) {
            if (stale('articles', my)) return;
            if (!rows.length) { $('articleResults').innerHTML = '<div class="small text-muted p-2">Sin resultados.</div>'; return; }
            $('articleResults').innerHTML = rows.map(function (r, i) {
                var already = state.lines.some(function (l) { return l.articleId === r.articleId; });
                return '<button type="button" class="list-group-item list-group-item-action d-flex align-items-center" data-add="' + i + '"' + (already ? ' disabled' : '') + '>' +
                    '<div class="flex-grow-1 text-left"><strong>' + esc(r.name) + '</strong><div class="small text-muted">' + esc(r.code) + (r.category ? ' · ' + esc(r.category) : '') + '</div></div>' +
                    '<span class="small text-muted mr-3">Stock: ' + (r.stock == null ? 'sin registro' : r.stock) + '</span>' +
                    (already ? '<span class="badge badge-light">Agregado</span>' : '<span class="btn btn-sm btn-outline-primary rounded-pill"><i class="fas fa-plus"></i></span>') + '</button>';
            }).join('');
            Array.prototype.forEach.call(document.querySelectorAll('[data-add]'), function (btn) {
                btn.addEventListener('click', function () {
                    var r = rows[Number(btn.getAttribute('data-add'))];
                    state.lines.push({ articleId: r.articleId, code: r.code, name: r.name, category: r.category, unit: r.unit, quantity: 1 });
                    btn.disabled = true;
                    toast('success', r.name + ' agregado');
                    renderLines();
                    runCheck();
                });
            });
        }).catch(function () {
            if (stale('articles', my)) return;
            $('articleResults').innerHTML = '<div class="small text-danger p-2">No se pudo buscar artículos. <button type="button" class="btn btn-sm btn-outline-danger rounded-pill py-0 ml-2" data-retry="articles"><i class="fas fa-redo mr-1"></i>Reintentar</button></div>';
            var retry = document.querySelector('[data-retry="articles"]');
            if (retry) retry.addEventListener('click', function () { searchArticles(); });
        });
    }, 300);

    // ───────── resumen lateral y paso 4 ─────────
    function sideRow(icon, label, value, ok) {
        return '<div class="d-flex mb-3"><span class="dc-kpi__icon ' + (ok ? 'dc-soft-success' : 'dc-soft-secondary') + '" style="width:34px;height:34px;font-size:.9rem;margin-right:10px;"><i class="' + icon + '"></i></span>' +
            '<div class="small"><div class="text-muted text-uppercase font-weight-bold" style="font-size:.66rem;">' + label + '</div><div class="text-dark">' + (value || '<span class="text-muted">Pendiente</span>') + '</div></div></div>';
    }

    function materialSummary() {
        if (!state.check || !state.check.material.length) return null;
        var short = state.check.material.filter(function (m) { return m.state === 'Short'; }).length;
        var tight = state.check.material.filter(function (m) { return m.state === 'Tight'; }).length;
        var none = state.check.material.filter(function (m) { return m.state === 'NoStock'; }).length;
        return { short: short, tight: tight, none: none };
    }

    function renderSide() {
        var g = group(), l = lab();
        var ms = materialSummary();
        var html = '';
        html += sideRow('fas fa-users', 'Grupo', g ? esc(g.subject) + ' · ' + esc(g.name) + (state.students ? ' (' + state.students + ' est.)' : '') : null, !!g);
        html += sideRow('fas fa-flask', 'Práctica', state.practiceName ? esc(state.practiceName) : null, !!state.practiceId);
        html += sideRow('far fa-calendar', 'Fecha y hora', state.date && state.startPeriodId ? esc(fmtDate(state.date)) + '<br>' + esc(timeText()) : null, !!(state.date && state.startPeriodId && blocks().length === 0 && state.check));
        html += sideRow('fas fa-building', 'Laboratorio', l ? esc(l.code) + ' · ' + esc(l.name) : null, !!l);
        var matText = state.lines.length ? state.lines.length + ' artículo(s)' : null;
        if (ms) matText += ms.short ? '<br><span class="text-danger">' + ms.short + ' no alcanza(n)</span>' : '<br><span class="text-success">Todo disponible</span>';
        html += sideRow('fas fa-box-open', 'Material', matText, state.lines.length > 0 && (!ms || ms.short === 0));
        $('sideSummary').innerHTML = html;
    }

    function renderReview() {
        var g = group(), l = lab(), ms = materialSummary();
        // El resumen cuenta solo lo que se envía: líneas con cantidad mayor que cero.
        var active = state.lines.filter(function (x) { return Number(x.quantity) > 0; });
        var rows = active.map(function (x) {
            var m = materialFor(x.articleId);
            return '<tr><td><span class="dc-light dc-light--' + (m ? m.stateCss : 'secondary') + '"></span>' + esc(x.name) + '</td><td class="text-right font-weight-bold">' + esc(x.quantity) + ' <small class="text-muted">' + esc(x.unit || '') + '</small></td></tr>';
        }).join('');
        $('reviewSummary').innerHTML =
            '<div class="row">' +
            '<div class="col-md-6 mb-3"><div class="p-3 rounded" style="background:#f3f1fe;"><div class="small text-uppercase font-weight-bold" style="color:#7460ee;">Práctica</div><div class="font-weight-bold text-dark">' + esc(state.practiceName) + '</div><div class="small text-muted">' + esc(g ? g.subject + ' · Grupo ' + g.name : '') + (state.students ? ' · ' + state.students + ' estudiantes' : '') + '</div></div></div>' +
            '<div class="col-md-6 mb-3"><div class="p-3 rounded" style="background:#e8f8f4;"><div class="small text-uppercase font-weight-bold" style="color:#128a74;">Cuándo y dónde</div><div class="font-weight-bold text-dark text-capitalize">' + esc(fmtDate(state.date)) + '</div><div class="small text-muted">' + esc(timeText()) + ' · ' + esc(l ? l.code + ' · ' + l.name : '') + '</div></div></div>' +
            '</div>' +
            '<table class="table table-sm mb-0"><thead class="thead-light"><tr><th>Material (' + active.length + ')</th><th class="text-right">Cantidad</th></tr></thead><tbody>' + rows + '</tbody></table>';
        var warn = '';
        warnings().forEach(function (i) { if (i.severity === 'Warning') warn += '<div class="alert border-0 small mb-2" style="background:#fff8e8;"><i class="fas fa-exclamation-triangle text-warning mr-2"></i>' + esc(i.message) + '</div>'; });
        if (ms && ms.short) warn += '<div class="alert border-0 small mb-2" style="background:#fdecef;"><i class="fas fa-exclamation-circle text-danger mr-2"></i>' + ms.short + ' artículo(s) no alcanzan en tu horario. Puedes enviar igual: el revisor decidirá si ajusta cantidades o cambia el horario.</div>';
        if (ms && ms.none) warn += '<div class="alert alert-light border small mb-2"><i class="fas fa-info-circle mr-2"></i>' + ms.none + ' artículo(s) no tienen stock registrado: el revisor lo confirmará.</div>';
        $('reviewWarnings').innerHTML = warn;
    }

    // ───────── navegación entre pasos ─────────
    function canLeave(step) {
        if (step === 1) {
            if (!state.groupId) { toast('warning', 'Elige un grupo'); return false; }
            if (!state.practiceId) { toast('warning', 'Elige la práctica'); return false; }
            var s = Number($('students').value);
            if ($('students').value && (s < 1 || s > 500)) { toast('warning', 'Estudiantes entre 1 y 500'); return false; }
            return true;
        }
        if (step === 2) {
            if (!state.labId || !state.date || !state.startPeriodId) { toast('warning', 'Elige laboratorio, día y periodo'); return false; }
            if (!state.check) { toast('info', 'Verificando el horario…'); runCheck(); return false; }
            if (blocks().length) { toast('error', 'Ese horario tiene un choque: elige otro'); return false; }
            return true;
        }
        if (step === 3) {
            var valid = state.lines.filter(function (l) { return Number(l.quantity) > 0; });
            if (!valid.length) { toast('warning', 'Agrega al menos un artículo'); return false; }
            return true;
        }
        return true;
    }

    function go(step) {
        state.step = step;
        Array.prototype.forEach.call(document.querySelectorAll('[data-pane]'), function (p) {
            p.style.display = Number(p.getAttribute('data-pane')) === step ? '' : 'none';
        });
        Array.prototype.forEach.call(document.querySelectorAll('.dc-step'), function (s) {
            var n = Number(s.getAttribute('data-step'));
            s.classList.toggle('is-active', n === step);
            s.classList.toggle('is-done', n < step);
            s.querySelector('.dc-step__dot').innerHTML = n < step ? '<i class="fas fa-check"></i>' : n;
        });
        Array.prototype.forEach.call(document.querySelectorAll('.dc-step__bar'), function (b) {
            b.classList.toggle('is-done', Number(b.getAttribute('data-bar')) < step);
        });
        $('btnPrev').style.visibility = step > 1 ? 'visible' : 'hidden';
        $('btnNext').style.display = step < 4 ? '' : 'none';
        $('btnSubmit').style.display = step === 4 ? '' : 'none';
        if (step === 2) {
            if (state.labId) $('labSelect').value = String(state.labId);
            $('weekDate').value = state.weekDate;
            loadWeek();
            if (state.date) runCheck();
        }
        if (step === 3) { loadKit(false).then(function () { renderLines(); runCheck(); }); }
        if (step === 4) { renderReview(); }
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function submit() {
        if (!canLeave(1) || !canLeave(2) || !canLeave(3)) return;
        var g = group();
        // La confirmación cuenta solo lo que se envía: líneas con cantidad mayor que cero.
        var itemCount = linesPayload().filter(function (l) { return l.quantity > 0; }).length;
        Swal.fire({
            title: state.planningId ? '¿Reenviar la solicitud corregida?' : '¿Enviar la solicitud?',
            html: '<div class="text-left small">' + esc(state.practiceName) + '<br><b class="text-capitalize">' + esc(fmtDate(state.date)) + '</b> · ' + esc(timeText()) + ' · ' + esc((lab() || {}).code) +
                '<br>Grupo ' + esc(g ? g.name : '') + ' · ' + itemCount + ' artículo(s)<br><br>El laboratorio se reserva cuando el revisor la apruebe. Te avisaremos.</div>',
            type: 'question',
            showCancelButton: true,
            confirmButtonText: '<i class="fas fa-paper-plane"></i> Enviar',
            cancelButtonText: 'Revisar',
            confirmButtonColor: '#36bea6'
        }).then(function (result) {
            if (!(result && (result.isConfirmed === true || result.value === true))) return;
            $('f_planningId').value = state.planningId || '';
            $('f_group').value = state.groupId;
            $('f_practice').value = state.practiceId;
            $('f_students').value = state.students || '';
            $('f_date').value = state.date;
            $('f_start').value = state.startPeriodId;
            $('f_count').value = state.periodCount;
            $('f_lab').value = state.labId;
            $('f_obs').value = $('observations').value;
            $('f_lines').value = JSON.stringify(linesPayload().filter(function (l) { return l.quantity > 0; }));
            $('f_note').value = $('note') ? $('note').value : '';
            $('btnSubmit').disabled = true;
            $('btnSubmit').innerHTML = '<span class="spinner-border spinner-border-sm mr-2"></span>Enviando…';
            HTMLFormElement.prototype.submit.call($('submitForm'));
        });
    }

    // ───────── eventos ─────────
    $('btnNext').addEventListener('click', function () { if (canLeave(state.step)) go(state.step + 1); });
    $('btnPrev').addEventListener('click', function () { go(state.step - 1); });
    $('btnSubmit').addEventListener('click', submit);
    Array.prototype.forEach.call(document.querySelectorAll('.dc-step'), function (s) {
        s.addEventListener('click', function () {
            var n = Number(s.getAttribute('data-step'));
            if (n < state.step) go(n);
        });
    });
    $('practiceSearch').addEventListener('input', renderPractices);
    $('students').addEventListener('change', function () {
        var previous = state.students;
        var next = this.value ? Number(this.value) : null;
        state.students = next;
        state.kitLoadedFor = null;
        renderSide();
        // Si ya hay kit cargado, ofrecer recalcular; si no, la próxima carga ya usa el número nuevo.
        if (!state.practiceId || state.lines.length === 0 || next === null || next === previous) return;
        Swal.fire({
            title: '¿Recalcular el kit para ' + next + ' estudiantes?',
            text: 'Se reemplazan las cantidades actuales.',
            type: 'question', showCancelButton: true,
            confirmButtonText: 'Sí, recalcular', cancelButtonText: 'No, dejar así',
            confirmButtonColor: '#7460ee', cancelButtonColor: '#6c757d'
        }).then(function (r) {
            if (r && (r.isConfirmed === true || r.value === true)) {
                loadKit(true);
            } else {
                // Las cantidades siguen calculadas para otra cantidad de estudiantes: el hint no debe afirmar lo contrario.
                $('kitHint').textContent = 'Kit cargado para otra cantidad de estudiantes. Ajusta las cantidades o vuelve a cargar el kit.';
            }
        });
    });
    $('periodCount').addEventListener('change', function () {
        state.periodCount = Number(this.value);
        $('suggestions').innerHTML = '';
        renderWeek();
        runCheck();
        renderSide();
    });
    $('btnSuggest').addEventListener('click', suggest);
    $('labSelect').addEventListener('change', function () {
        state.labId = this.value ? Number(this.value) : null;
        loadWeek();
        runCheck();
        renderSide();
    });
    $('weekDate').addEventListener('change', function () { if (this.value) { state.weekDate = this.value; loadWeek(); } });
    $('weekPrev').addEventListener('click', function () { state.weekDate = addDays(mondayOf(state.weekDate), -7); $('weekDate').value = state.weekDate; loadWeek(); });
    $('weekNext').addEventListener('click', function () { state.weekDate = addDays(mondayOf(state.weekDate), 7); $('weekDate').value = state.weekDate; loadWeek(); });
    $('btnReloadKit').addEventListener('click', function () {
        Swal.fire({ title: '¿Volver a cargar el kit?', text: 'Se reemplazan los artículos actuales por el kit de la práctica.', type: 'warning', showCancelButton: true, confirmButtonText: 'Sí, recargar', cancelButtonText: 'Cancelar' })
            .then(function (r) { if (r && (r.isConfirmed === true || r.value === true)) loadKit(true); });
    });
    $('articleTerm').addEventListener('input', searchArticles);
    $('articleCategory').addEventListener('change', searchArticles);
    window.addEventListener('beforeunload', function (e) {
        if (state.groupId && $('btnSubmit').disabled !== true) { e.preventDefault(); e.returnValue = ''; }
    });

    // ───────── inicio ─────────
    renderGroups();
    if (initial) {
        state.planningId = initial.planningId || null;
        state.students = initial.students || null;
        state.practiceId = initial.practiceId || null;
        state.periodCount = initial.periodCount || 2;
        state.labId = initial.labId || null;
        state.date = initial.date || null;
        state.startPeriodId = initial.date ? initial.startPeriodId : null;
        if (initial.date) state.weekDate = initial.date;
        else if (initial.weekDate) state.weekDate = initial.weekDate;   // desde el cronograma: abre la semana programada
        state.lines = (initial.lines || []).map(function (l) { return { articleId: l.articleId, code: l.code, name: l.name, category: l.category, unit: l.unit, quantity: l.quantity }; });
        state.kitLoadedFor = state.lines.length ? state.practiceId + '|' + (state.students || '') : null;
        $('observations').value = initial.observations || '';
        $('periodCount').value = String(state.periodCount);
        if (initial.groupId) selectGroup(initial.groupId, true);
    } else if (cfg.groups.length === 1) {
        selectGroup(cfg.groups[0].id);
    }
    $('weekDate').value = state.weekDate;
    $('periodCount').value = String(state.periodCount);
    renderSide();
})();
