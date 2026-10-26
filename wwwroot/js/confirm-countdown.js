/* ==========================================================================
   Confirmación crítica con cuenta regresiva circular (SweetAlert2 v7).
   Componente reutilizable; hoy lo usa Pages/LaboratoryOperations/Planificacion.cshtml
   para insistir con un choque de docente (guardar o asignar horario).

   window.confirmWithCountdown({ seconds, title, html, confirmText, onConfirm })
   - Anillo SVG que se vacía en lineal durante la espera (stroke-dashoffset).
   - El botón de confirmación arranca deshabilitado ("Espere 5 s…") y se
     habilita al llegar a cero con el texto final.
   - La cuenta se limpia al cerrar el modal (onClose).
   ES5, IIFE. Requiere SweetAlert2.
   ========================================================================== */
(function () {
    'use strict';

    window.confirmWithCountdown = function (opts) {
        opts = opts || {};
        if (typeof window.Swal === 'undefined') {
            // Sin SweetAlert2 no hay confirmación deliberada: no se continúa.
            if (window.console) console.warn('confirmWithCountdown requiere SweetAlert2.');
            return;
        }

        var seconds = Math.max(1, Number(opts.seconds) || 5);
        var size = 120;
        var radius = 46;
        var circumference = 2 * Math.PI * radius;
        var half = size / 2;

        var ringHtml =
            '<div style="position:relative;width:' + size + 'px;height:' + size + 'px;margin:0 auto 12px;">' +
                '<svg width="' + size + '" height="' + size + '" viewBox="0 0 ' + size + ' ' + size + '" style="display:block;transform:rotate(-90deg);">' +
                    '<circle cx="' + half + '" cy="' + half + '" r="' + radius + '" fill="none" stroke="#e9ecef" stroke-width="9"></circle>' +
                    '<circle id="ccd-progress" cx="' + half + '" cy="' + half + '" r="' + radius + '" fill="none" stroke="#dc3545" stroke-width="9" stroke-linecap="round"' +
                        ' stroke-dasharray="' + circumference + '" stroke-dashoffset="0" style="transition:stroke-dashoffset ' + seconds + 's linear;"></circle>' +
                '</svg>' +
                '<div id="ccd-seconds" style="position:absolute;top:0;left:0;width:100%;height:100%;display:flex;align-items:center;justify-content:center;' +
                    'font-size:38px;font-weight:700;color:#dc3545;">' + seconds + '</div>' +
            '</div>';

        var timer = null;

        Swal.fire({
            title: opts.title || '¿Confirmar la acción?',
            html: ringHtml + '<div style="text-align:left;">' + (opts.html || '') + '</div>',
            type: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Espere ' + seconds + ' s…',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            allowOutsideClick: false,
            onOpen: function (modal) {
                var confirmBtn = modal ? modal.querySelector('.swal2-confirm') : null;
                if (confirmBtn) confirmBtn.disabled = true;
                try {
                    // v7: helper estático sobre el modal abierto; si no existe, ya quedó deshabilitado arriba.
                    if (typeof Swal.disableConfirmButton === 'function') Swal.disableConfirmButton();
                } catch (e) { /* no-op */ }

                var secondsEl = modal ? modal.querySelector('#ccd-seconds') : null;
                var ringEl = modal ? modal.querySelector('#ccd-progress') : null;

                // El anillo se vacía de 0 a la circunferencia en "seconds" (transición linear).
                window.setTimeout(function () {
                    if (ringEl) ringEl.style.strokeDashoffset = String(circumference);
                }, 50);

                var remaining = seconds;
                timer = window.setInterval(function () {
                    remaining -= 1;
                    if (secondsEl) secondsEl.textContent = String(Math.max(0, remaining));
                    if (remaining > 0) {
                        if (confirmBtn) confirmBtn.textContent = 'Espere ' + remaining + ' s…';
                        return;
                    }
                    window.clearInterval(timer);
                    timer = null;
                    if (confirmBtn) {
                        confirmBtn.textContent = opts.confirmText || 'Sí, confirmar';
                        confirmBtn.disabled = false;
                    }
                    try {
                        if (typeof Swal.enableConfirmButton === 'function') Swal.enableConfirmButton();
                    } catch (e) { /* no-op */ }
                }, 1000);
            },
            onClose: function () {
                if (timer) {
                    window.clearInterval(timer);
                    timer = null;
                }
            }
        }).then(function (result) {
            if (timer) {
                window.clearInterval(timer);
                timer = null;
            }
            // v7: comprobar value además de isConfirmed.
            if (result && (result.isConfirmed === true || result.value === true) && typeof opts.onConfirm === 'function') {
                opts.onConfirm();
            }
        });
    };
})();
