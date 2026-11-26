// «Pedir cambio de material» del portal docente (Pages/Docente/CambioMaterial).
// Tabla editable del L-3 con buscador de artículos, recálculo del kit y resumen en vivo.
// El servidor revalida todo al enviar (TeacherRequestService.RequestMaterialChangeAsync).
// Sin dependencias además de SweetAlert2 v7 del layout.
(function () {
    'use strict';

    var form = document.getElementById('cm-form');
    if (!form) return;
    var body = document.getElementById('cm-body');
    var search = document.getElementById('cm-search');
    var results = document.getElementById('cm-results');
    var studentsInput = document.getElementById('cm-students');
    var recalcBtn = document.getElementById('cm-recalc');
    var recalcNote = document.getElementById('cm-recalc-note');
    var recalcText = document.getElementById('cm-recalc-text');
    var reason = document.getElementById('cm-reason');
    var reasonCount = document.getElementById('cm-reason-count');
    var summary = document.getElementById('cm-summary');
    var alertWarning = document.getElementById('cm-alert-warning');
    var submitBtn = document.getElementById('cm-submit');

    var articlesUrl = form.getAttribute('data-articles-url') || '';
    var kitUrl = form.getAttribute('data-kit-url') || '';
    var daysAhead = parseInt(form.getAttribute('data-days-ahead') || '0', 10);
    var onTimeDays = parseInt(form.getAttribute('data-on-time-days') || '7', 10);
    var warningItems = parseInt(form.getAttribute('data-warning-items') || '3', 10);

    // ───────── utilidades ─────────
    function esc(v) { return String(v == null ? '' : v).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
    function num(v) { var n = parseFloat(v); return isNaN(n) || n < 0 ? 0 : n; }
    function fmt(n) { return String(Math.round(n * 100) / 100); }
    function rows() { return body ? Array.prototype.slice.call(body.querySelectorAll('tr.cm-row')) : []; }
    function rowOf(articleId) {
        return rows().filter(function (r) { return r.getAttribute('data-article-id') === String(articleId); })[0] || null;
    }
    function qtyInputOf(tr) { return tr ? tr.querySelector('input.cm-qty') : null; }
    function currentOf(tr) { return num(tr.getAttribute('data-current')); }

    function getJson(url) {
        return fetch(url, { credentials: 'same-origin' }).then(function (r) {
            if (!r.ok) throw new Error('HTTP ' + r.status);
            return r.json();
        });
    }

    // ───────── filas ─────────
    function setRemoved(tr, removed) {
        var input = qtyInputOf(tr);
        if (!input) return;
        if (removed) {
            input.setAttribute('data-prev', input.value);
            input.value = '0';
            tr.classList.add('cm-row-removed');
        } else {
            input.value = input.getAttribute('data-prev') || input.getAttribute('data-original') || '0';
            tr.classList.remove('cm-row-removed');
        }
        var restore = tr.querySelector('.cm-restore');
        var remove = tr.querySelector('.cm-remove');
        if (restore) restore.style.display = removed ? 'inline-block' : 'none';
        if (remove) remove.style.display = removed ? 'none' : 'inline-block';
    }

    function addRow(article) {
        var tr = document.createElement('tr');
        tr.className = 'cm-row cm-row-new';
        tr.setAttribute('data-article-id', article.articleId);
        tr.setAttribute('data-current', '0');
        tr.innerHTML =
            '<td><strong class="text-dark">' + esc(article.name) + '</strong><div class="small text-muted">' + esc(article.code) +
            (article.stock != null ? ' · libre ' + esc(article.stock) : '') + '</div></td>' +
            '<td class="small text-muted align-middle">' + esc(article.category) + '</td>' +
            '<td class="text-center align-middle">— <small class="text-muted">' + esc(article.unit) + '</small></td>' +
            '<td class="align-middle"><div class="d-flex align-items-center">' +
            // Un artículo agregado desde el buscador no trae cantidad: arranca en 1 (con 0 no se pediría y no hay aviso);
            // los que vienen del recálculo del kit traen la suya.
            '<input type="number" class="form-control form-control-sm cm-qty" value="' + fmt(article.quantity != null ? num(article.quantity) : 1) + '" min="0" step="0.01" data-original="0" aria-label="Nueva cantidad de ' + esc(article.name) + '" />' +
            '<button type="button" class="btn btn-sm btn-outline-danger cm-remove ml-2" title="Pone la cantidad en 0 (el artículo se quita del pedido)"><i class="fas fa-times"></i></button>' +
            '<button type="button" class="btn btn-sm btn-light border cm-restore ml-1" style="display:none;" title="Recupera la cantidad anterior"><i class="fas fa-undo"></i></button>' +
            '</div></td>';
        body.appendChild(tr);
        refresh();
        var input = qtyInputOf(tr);
        if (input) { input.focus(); input.select(); }
        return tr;
    }

    // ───────── resumen en vivo ─────────
    function counts() {
        var qtyChanges = 0, added = 0, removed = 0;
        rows().forEach(function (tr) {
            var current = currentOf(tr);
            var qty = num(qtyInputOf(tr) ? qtyInputOf(tr).value : 0);
            if (current === 0 && qty > 0) { added++; }
            else if (current > 0 && qty === 0) { removed++; }
            else if (current > 0 && qty > 0 && qty !== current) { qtyChanges++; }
        });
        return { qtyChanges: qtyChanges, items: added + removed };
    }

    function refresh() {
        var c = counts();
        if (c.qtyChanges === 0 && c.items === 0) {
            summary.textContent = 'Sin cambios por ahora: el pedido sería igual al L-3 vigente.';
        } else {
            summary.textContent = c.qtyChanges + ' cambio(s) de cantidad · ' + c.items + ' artículo(s) agregado(s) o quitado(s).';
        }
        alertWarning.style.display = c.items >= warningItems ? 'block' : 'none';
        if (reasonCount) reasonCount.textContent = (reason ? reason.value.length : 0) + '/1000';
        var countBadge = document.getElementById('cm-count');
        if (countBadge) countBadge.textContent = String(rows().length);
    }

    // ───────── eventos ─────────
    if (body) {
        body.addEventListener('click', function (e) {
            var target = e.target;
            if (!target || !target.closest) return;
            var removeBtn = target.closest('.cm-remove');
            var restoreBtn = target.closest('.cm-restore');
            if (removeBtn) setRemoved(removeBtn.closest('tr.cm-row'), true);
            else if (restoreBtn) setRemoved(restoreBtn.closest('tr.cm-row'), false);
        });
        body.addEventListener('input', function (e) {
            if (e.target && e.target.classList && e.target.classList.contains('cm-qty')) refresh();
        });
    }

    if (reason) reason.addEventListener('input', refresh);

    // Buscador de artículos (catálogo activo, mismo endpoint del asistente de solicitudes).
    var searchTimer = null;
    function closeResults() {
        if (results) { results.style.display = 'none'; results.innerHTML = ''; }
    }
    function pickArticle(a) {
        closeResults();
        if (search) search.value = '';
        var existing = rowOf(a.articleId);
        if (existing) {
            var input = qtyInputOf(existing);
            if (input) { input.focus(); input.select(); }
            return;
        }
        addRow(a);
    }
    if (search && results) {
        search.addEventListener('input', function () {
            window.clearTimeout(searchTimer);
            var term = search.value.trim();
            if (term.length < 2) { closeResults(); return; }
            searchTimer = window.setTimeout(function () {
                getJson(articlesUrl + '&term=' + encodeURIComponent(term)).then(function (list) {
                    results.innerHTML = '';
                    (list || []).slice(0, 8).forEach(function (a) {
                        var btn = document.createElement('button');
                        btn.type = 'button';
                        btn.className = 'list-group-item list-group-item-action cm-result';
                        btn.innerHTML = '<strong>' + esc(a.name) + '</strong> <small class="text-muted">' + esc(a.code) +
                            ' · ' + esc(a.category) + (a.stock != null ? ' · libre ' + esc(a.stock) : '') + '</small>';
                        btn.addEventListener('click', function () { pickArticle(a); });
                        results.appendChild(btn);
                    });
                    if (!results.childElementCount) {
                        results.innerHTML = '<span class="list-group-item text-muted small">Sin resultados en el catálogo.</span>';
                    }
                    results.style.display = 'block';
                }).catch(function () { closeResults(); });
            }, 250);
        });
        search.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeResults();
        });
        document.addEventListener('click', function (e) {
            if (results.style.display === 'block' && !results.contains(e.target) && e.target !== search) closeResults();
        });
    }

    // Recalcular el kit de ESTA práctica para N estudiantes.
    if (recalcBtn) {
        recalcBtn.addEventListener('click', function () {
            var n = parseInt(studentsInput ? studentsInput.value : '', 10);
            if (!n || n < 1 || n > 500) {
                Swal.fire({ title: 'Falta el número de estudiantes', text: 'Escribe cuántos estudiantes tendrán la sesión (entre 1 y 500).', type: 'info', confirmButtonText: 'Entendido', confirmButtonColor: '#7460ee' });
                return;
            }
            recalcBtn.disabled = true;
            getJson(kitUrl + '&students=' + n).then(function (kit) {
                (kit || []).forEach(function (k) {
                    var existing = rowOf(k.articleId);
                    if (existing) {
                        var input = qtyInputOf(existing);
                        if (input) input.value = fmt(num(k.quantity));
                    } else {
                        addRow(k);
                    }
                });
                recalcBtn.disabled = false;
                if (recalcNote && recalcText) {
                    recalcText.textContent = 'Cantidades del kit recalculadas para ' + n + ' estudiantes.';
                    recalcNote.style.display = 'block';
                }
                refresh();
            }).catch(function () {
                recalcBtn.disabled = false;
                Swal.fire({ title: 'No se pudo recalcular', text: 'Intenta nuevamente en unos segundos.', type: 'error', confirmButtonText: 'Entendido', confirmButtonColor: '#f62d51' });
            });
        });
    }

    // Envío: resumen en la confirmación y líneas completas (incluidas las de 0) como inputs ocultos.
    function buildLines() {
        var holder = document.getElementById('cm-lines');
        if (!holder) return;
        holder.innerHTML = '';
        var idx = 0;
        rows().forEach(function (tr) {
            var articleId = tr.getAttribute('data-article-id');
            var qty = fmt(num(qtyInputOf(tr) ? qtyInputOf(tr).value : 0));
            [['ArticleId', articleId], ['Quantity', qty]].forEach(function (pair) {
                var hidden = document.createElement('input');
                hidden.type = 'hidden';
                hidden.name = 'lines[' + idx + '].' + pair[0];
                hidden.value = pair[1];
                holder.appendChild(hidden);
            });
            idx++;
        });
    }

    if (submitBtn) {
        submitBtn.addEventListener('click', function () {
            var c = counts();
            var motivo = reason ? reason.value.trim() : '';
            if (c.qtyChanges === 0 && c.items === 0) {
                Swal.fire({ title: 'No hay cambios que pedir', text: 'El L-3 escrito es igual al vigente: ajusta cantidades, agrega o quita artículos antes de enviar.', type: 'info', confirmButtonText: 'Entendido', confirmButtonColor: '#7460ee' });
                return;
            }
            if (motivo.length < 10) {
                Swal.fire({ title: 'Falta el motivo', text: 'Explica por qué pides el cambio (entre 10 y 1000 caracteres): la Inge lo lee para decidir.', type: 'info', confirmButtonText: 'Entendido', confirmButtonColor: '#7460ee' });
                return;
            }
            var alerts = [];
            if (c.items >= warningItems) alerts.push('Quedará registrada como llamada de atención (' + warningItems + '+ artículos agregados o quitados).');
            if (daysAhead < onTimeDays) alerts.push('Quedará registrada como pedido de último momento (menos de ' + onTimeDays + ' días de anticipación).');
            Swal.fire({
                title: '¿Enviar el pedido de cambio de material?',
                text: c.qtyChanges + ' cambio(s) de cantidad y ' + c.items + ' artículo(s) agregado(s) o quitado(s).'
                    + (alerts.length ? ' ' + alerts.join(' ') : '')
                    + ' La práctica sigue aprobada mientras la Inge revisa.',
                type: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, enviar pedido',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#7460ee'
            }).then(function (result) {
                if (!(result && (result.isConfirmed === true || result.value === true))) return;
                buildLines();
                submitBtn.disabled = true;
                HTMLFormElement.prototype.submit.call(form);
            });
        });
    }

    refresh();
})();
