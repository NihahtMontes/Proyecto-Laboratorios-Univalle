// Textos de Select2 en español por defecto (UX fase 2). Se carga justo después de select2 en cada página;
// las páginas que pasan su propio `language` lo conservan.
(function ($) {
    'use strict';
    if (!$ || !$.fn || !$.fn.select2 || !$.fn.select2.defaults) return;
    $.fn.select2.defaults.set('language', {
        errorLoading: function () { return 'No se pudieron cargar los resultados.'; },
        inputTooLong: function (args) { return 'Borre ' + (args.input.length - args.maximum) + ' carácter(es).'; },
        inputTooShort: function (args) { return 'Escriba ' + (args.minimum - args.input.length) + ' carácter(es) más.'; },
        loadingMore: function () { return 'Cargando más resultados…'; },
        maximumSelected: function (args) { return 'Solo puede elegir ' + args.maximum + ' elemento(s).'; },
        noResults: function () { return 'Sin coincidencias'; },
        searching: function () { return 'Buscando…'; },
        removeAllItems: function () { return 'Quitar todos los elementos'; },
        removeItem: function () { return 'Quitar elemento'; }
    });
})(window.jQuery);
