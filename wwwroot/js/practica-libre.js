/**
 * Práctica libre: confirmación SweetAlert2 (v7) antes de enviar formularios que cambian datos existentes.
 * Uso: <form method="post" data-confirm="¿Aprobar la reserva?" data-confirm-text="Detalle opcional">.
 * Los campos obligatorios (p. ej. el motivo de un rechazo) se validan antes de preguntar.
 */
(function () {
    'use strict';
    var fire = function (options) { return (window.Swal && Swal.fire) ? Swal.fire(options) : Swal(options); };

    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.hasAttribute('data-confirm') || form.dataset.confirmed === '1') return;
        event.preventDefault();
        if (!form.reportValidity()) return;
        fire({
            title: form.getAttribute('data-confirm'),
            text: form.getAttribute('data-confirm-text') || '',
            type: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, continuar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        }).then(function (result) {
            if (result && (result.value || result.isConfirmed)) {
                form.dataset.confirmed = '1';
                HTMLFormElement.prototype.submit.call(form);
            }
        });
    });
})();
