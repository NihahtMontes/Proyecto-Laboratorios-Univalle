// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ==========================================
// REDO WIZARD PHASE (GLOBAL SWEETALERT2)
// ==========================================
window.confirmRedoPhase = function(planId, targetPhaseInt, targetPhaseName, currentPhaseInt = null) {
    let alertText = 'Se anulará el progreso actual y los registros generados después de esta fase para que puedas empezar de nuevo. ¡Esta acción no se puede deshacer!';
    let isHighImpact = false;

    if (currentPhaseInt != null) {
        let diff = parseInt(currentPhaseInt) - parseInt(targetPhaseInt);
        if (diff >= 2) {
            alertText = `Atención: Para editar esta fase, se resetearán las ${diff - 1} fase(s) intermedia(s) y perderás su progreso actual. ¿Deseas continuar?`;
            isHighImpact = true;
        } else if (diff === 1) {
            // If diff is 1, it's usually just an edit, but if called here, it's a rollback.
            alertText = `Volverás a la fase de ${targetPhaseName}. El progreso de la fase actual se descartará.`;
        }
    }

    Swal.fire({
        title: isHighImpact ? '¿Retroceder múltiples fases?' : '¿Rehacer Fase ' + targetPhaseName + '?',
        text: alertText,
        icon: isHighImpact ? 'error' : 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: '<i class="fas fa-undo"></i> Sí, rehacer progreso',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Anulando registros...',
                text: 'Por favor espera',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            $.ajax({
                url: '/api/WizardRollback/redo',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    planId: parseInt(planId),
                    targetPhase: parseInt(targetPhaseInt)
                }),
                success: function(response) {
                    Swal.fire({
                        title: '¡Éxito!',
                        text: response.message || 'Fase revertida correctamente. Redirigiendo...',
                        icon: 'success',
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        window.location.reload();
                    });
                },
                error: function(xhr) {
                    Swal.fire('Error', xhr.responseText || 'Ocurrió un error al rehacer la fase.', 'error');
                }
            });
        }
    });
};
