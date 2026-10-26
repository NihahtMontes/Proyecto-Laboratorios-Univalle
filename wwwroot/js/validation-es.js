// Mensajes de jQuery Validation en español (UX fase 2). Se carga después de jquery.validate en _ValidationScriptsPartial.
(function ($) {
    'use strict';
    if (!$ || !$.validator) return;
    $.extend($.validator.messages, {
        required: 'Este campo es obligatorio.',
        remote: 'Revise este campo.',
        email: 'Escriba un correo electrónico válido.',
        url: 'Escriba una dirección web válida.',
        date: 'Escriba una fecha válida.',
        dateISO: 'Escriba una fecha válida (AAAA-MM-DD).',
        number: 'Escriba un número válido.',
        digits: 'Escriba solo dígitos.',
        equalTo: 'Escriba el mismo valor otra vez.',
        maxlength: $.validator.format('Escriba como máximo {0} caracteres.'),
        minlength: $.validator.format('Escriba al menos {0} caracteres.'),
        rangelength: $.validator.format('Escriba entre {0} y {1} caracteres.'),
        range: $.validator.format('Escriba un valor entre {0} y {1}.'),
        max: $.validator.format('Escriba un valor menor o igual a {0}.'),
        min: $.validator.format('Escriba un valor mayor o igual a {0}.'),
        step: $.validator.format('Escriba un múltiplo de {0}.')
    });
})(window.jQuery);
