// Motivo de reprogramaciones y suspensiones: cualquier texto vale (incluso "0"), pero con menos de 5 palabras
// se recomienda ampliarlo antes de guardar. Uso: Motivo.check(texto, continuar, campoParaEnfocar).
(function () {
    'use strict';
    var MIN_WORDS = 5;

    function words(text) {
        return (text || '').trim().split(/\s+/).filter(Boolean).length;
    }

    function check(text, proceed, focusEl) {
        if (words(text) >= MIN_WORDS || typeof Swal === 'undefined') { proceed(); return; }
        Swal.fire({
            title: 'Motivo muy corto',
            text: 'Se recomienda escribir al menos ' + MIN_WORDS + ' palabras para que la descripción sea precisa. Puede guardarlo igual.',
            type: 'info',
            showCancelButton: true,
            confirmButtonText: 'Guardar igual',
            cancelButtonText: 'Corregir',
            confirmButtonColor: '#7460ee',
            cancelButtonColor: '#6c757d'
        }).then(function (r) {
            if (r && (r.isConfirmed === true || r.value === true)) {
                // El siguiente SweetAlert (la confirmación de la acción) se abre cuando este ya cerró.
                setTimeout(proceed, 150);
            } else if (focusEl) {
                setTimeout(function () { focusEl.focus(); }, 150);
            }
        });
    }

    window.Motivo = { words: words, check: check, minWords: MIN_WORDS };
})();
