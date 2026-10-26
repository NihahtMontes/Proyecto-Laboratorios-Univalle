/* ==========================================================================
   Etiquetas de conflictos de material (reutilizable, sin dependencias)
   - Al pasar el ratón o enfocar una etiqueta [data-conflict-key] resalta los
     elementos relacionados del alcance [data-conflict-scope] y atenúa el resto.
   - Interruptor [data-conflict-toggle] que alterna una clase en [data-target]
     y guarda su estado en localStorage con la clave [data-storage-key].
   Usado por: Grilla (grilla.css) y Disponibilidad (estilos locales).
   ========================================================================== */
(function () {
    'use strict';

    var activeScope = null;
    var activeHoverClass = '';
    var activeRelatedClass = '';
    var activeTag = null;

    function keysContain(element, key) {
        var raw = element.getAttribute('data-conflict-keys');
        if (!raw || !key) return false;
        return (' ' + raw.trim().replace(/\s+/g, ' ') + ' ').indexOf(' ' + key + ' ') !== -1;
    }

    function clearHover() {
        if (!activeScope) return;
        if (activeHoverClass) activeScope.classList.remove(activeHoverClass);
        if (activeRelatedClass) {
            var marked = activeScope.querySelectorAll('[data-conflict-keys]');
            for (var i = 0; i < marked.length; i++) marked[i].classList.remove(activeRelatedClass);
        }
        activeScope = null;
        activeHoverClass = '';
        activeRelatedClass = '';
        activeTag = null;
    }

    function hover(tag) {
        if (tag === activeTag) return;
        clearHover();
        var scope = tag.closest ? tag.closest('[data-conflict-scope]') : null;
        if (!scope) return;
        var hoverClass = scope.getAttribute('data-conflict-scope');
        var relatedClass = scope.getAttribute('data-conflict-related-class') || '';
        var key = tag.getAttribute('data-conflict-key');
        var color = tag.getAttribute('data-cf-color');
        if (color) scope.style.setProperty('--cf-active', color);
        if (hoverClass) scope.classList.add(hoverClass);
        if (relatedClass) {
            var marked = scope.querySelectorAll('[data-conflict-keys]');
            for (var i = 0; i < marked.length; i++) {
                if (keysContain(marked[i], key)) marked[i].classList.add(relatedClass);
            }
        }
        activeScope = scope;
        activeHoverClass = hoverClass || '';
        activeRelatedClass = relatedClass;
        activeTag = tag;
    }

    function tagFromEvent(e) {
        var node = e.target;
        return node && node.closest ? node.closest('[data-conflict-key]') : null;
    }

    document.addEventListener('mouseover', function (e) {
        var tag = tagFromEvent(e);
        if (tag) hover(tag);
    });
    document.addEventListener('mouseout', function (e) {
        var tag = tagFromEvent(e);
        if (tag && tag === activeTag && !(e.relatedTarget && tag.contains(e.relatedTarget))) clearHover();
    });
    document.addEventListener('focusin', function (e) {
        var tag = tagFromEvent(e);
        if (tag) hover(tag);
    });
    document.addEventListener('focusout', function (e) {
        if (activeTag && (!e.relatedTarget || !activeTag.contains(e.relatedTarget))) clearHover();
    });

    function initToggles() {
        var switches = document.querySelectorAll('[data-conflict-toggle]');
        for (var i = 0; i < switches.length; i++) {
            (function (sw) {
                if (sw.getAttribute('data-cf-bound') === '1') return;
                sw.setAttribute('data-cf-bound', '1');
                var selector = sw.getAttribute('data-target');
                var cls = sw.getAttribute('data-class');
                var storageKey = sw.getAttribute('data-storage-key');
                if (!selector || !cls) return;
                var targets = document.querySelectorAll(selector);

                function apply(on) {
                    for (var j = 0; j < targets.length; j++) targets[j].classList.toggle(cls, on);
                }

                var stored = null;
                if (storageKey) {
                    try { stored = window.localStorage.getItem(storageKey); } catch (err) { stored = null; }
                }
                if (stored === 'true') { apply(true); sw.checked = true; }
                else if (stored === 'false') { apply(false); sw.checked = false; }

                sw.addEventListener('change', function () {
                    apply(sw.checked);
                    if (storageKey) {
                        try { window.localStorage.setItem(storageKey, sw.checked ? 'true' : 'false'); } catch (err) { /* almacenamiento no disponible */ }
                    }
                });
            })(switches[i]);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initToggles);
    } else {
        initToggles();
    }
})();
