(function () {
    'use strict';

    // Resalta y desplaza a la fila que volvió de registrar stock (?focus=art-<articleId>).
    var params = new URLSearchParams(window.location.search);
    var focus = params.get('focus');
    if (!focus) return;

    // El valor viene de la URL: se escapa para que un focus con comillas no rompa el selector (SyntaxError).
    var rows;
    try {
        rows = document.querySelectorAll('[data-focus-key="' + focus.replace(/["\\]/g, '\\$&') + '"]');
    } catch (e) {
        return;
    }
    if (rows.length === 0) return;

    rows.forEach(function (row) { row.classList.add('row-focus'); });
    rows[0].scrollIntoView({ block: 'center' });

    // El pulso dura 4 s; al terminar se retira la clase junto con el focus de la URL.
    setTimeout(function () {
        rows.forEach(function (row) { row.classList.remove('row-focus'); });
    }, 4000);

    params.delete('focus');
    var search = params.toString();
    window.history.replaceState(null, '', window.location.pathname + (search ? '?' + search : '') + window.location.hash);
})();
