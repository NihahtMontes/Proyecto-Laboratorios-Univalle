// Compatibilidad SweetAlert2: la versión 7 instalada en el proyecto solo entiende `type`.
// Este envoltorio copia `icon` -> `type` cuando el llamador usa el nombre moderno y no
// envía `type`, sin cambiar el comportamiento de quien ya usa `type`.
// ES5: sin arrow functions, ni let/const, ni spread.
(function () {
    'use strict';

    function copyIconToType(args) {
        var opts = args && args.length > 0 ? args[0] : null;
        if (opts && typeof opts === 'object' && opts.icon && !opts.type) {
            opts.type = opts.icon;
        }
    }

    if (!window.Swal) {
        return;
    }

    if (typeof window.Swal.fire === 'function') {
        var originalFire = window.Swal.fire;
        window.Swal.fire = function () {
            copyIconToType(arguments);
            return originalFire.apply(this, arguments);
        };
    }

    if (typeof window.Swal.mixin === 'function') {
        var originalMixin = window.Swal.mixin;
        window.Swal.mixin = function (mixinOptions) {
            // La clase resultante conserva su .fire (Toasts) y se devuelve intacta,
            // solo se normalizan las opciones de cada llamada.
            copyIconToType([mixinOptions]);
            var result = originalMixin.call(this, mixinOptions);
            var resultFire = result && typeof result.fire === 'function' ? result.fire : null;
            if (resultFire) {
                result.fire = function () {
                    copyIconToType(arguments);
                    return resultFire.apply(result, arguments);
                };
            }
            return result;
        };
    }
})();
