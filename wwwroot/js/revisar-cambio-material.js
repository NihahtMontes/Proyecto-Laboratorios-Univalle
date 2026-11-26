// Revisión de un pedido de cambio de material (Pages/LaboratoryOperations/Solicitudes/RevisarCambio).
// Confirmaciones con SweetAlert2 v7 (se acepta result.isConfirmed o result.value); sin JS en línea.
(function () {
    'use strict';

    function confirmed(r) { return r && (r.isConfirmed === true || r.value === true); }

    var approve = document.getElementById('rc-approve');
    if (approve) {
        approve.addEventListener('click', function () {
            var shortageBox = document.getElementById('rc-allow-shortage');
            var withShortage = shortageBox != null && shortageBox.checked === true;
            var shortageText = withShortage ? ' Se aprueba con artículos faltantes y quedará registrado en el historial.' : '';
            Swal.fire({
                title: '¿Aprobar el cambio de material?',
                text: 'El L-3 de la práctica queda actualizado con lo pedido, se revalida el stock en ese horario y el docente recibe el aviso.' + shortageText,
                type: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, aprobar cambio',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#36bea6'
            }).then(function (r) {
                if (!confirmed(r)) return;
                approve.disabled = true;
                HTMLFormElement.prototype.submit.call(document.getElementById('rcApproveForm'));
            });
        });
    }

    var reject = document.getElementById('rc-reject');
    if (reject) {
        reject.addEventListener('click', function () {
            Swal.fire({
                title: 'Rechazar el pedido de cambio',
                text: 'El L-3 vigente se mantiene como está. Indica el motivo para el docente.',
                input: 'textarea',
                inputPlaceholder: 'Motivo del rechazo (mínimo 5 caracteres)…',
                type: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Rechazar pedido',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#f62d51',
                inputValidator: function (value) {
                    return new Promise(function (resolve) {
                        resolve(value && value.trim().length >= 5 ? undefined : 'Escribe al menos 5 caracteres.');
                    });
                }
            }).then(function (r) {
                if (!r || typeof r.value !== 'string') return;
                var form = document.getElementById('rcRejectForm');
                form.querySelector('[name=comment]').value = r.value.trim();
                HTMLFormElement.prototype.submit.call(form);
            });
        });
    }
})();
