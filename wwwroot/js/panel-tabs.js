// Pestañas de las páginas de inicio (ver wwwroot/css/panel-home.css).
//
// Marcado que debe generar el servidor (con la pestaña activa ya marcada, para que no haya parpadeo):
//   <div class="ph-tabs" role="tablist" aria-label="…" data-ph-param="activeTab">
//     <a class="ph-tab" role="tab" id="ph-tab-resumen" data-ph-key="resumen" href="?activeTab=resumen"
//        aria-selected="true" tabindex="0">Resumen</a>
//     <a class="ph-tab" role="tab" id="ph-tab-semana" data-ph-key="semana" href="?activeTab=semana"
//        aria-selected="false" tabindex="-1">Prácticas</a>
//   </div>
//   <div class="ph-panel" role="tabpanel" data-ph-key="resumen" aria-labelledby="ph-tab-resumen">…</div>
//   <div class="ph-panel" role="tabpanel" data-ph-key="semana" aria-labelledby="ph-tab-semana" hidden>…</div>
//
// Cualquier enlace con data-ph-tab="semana" abre esa pestaña. data-ph-alias="a b" en una pestaña acepta otros
// valores del parámetro en la URL. La primera pestaña es la predeterminada y no se escribe en la URL.
//
// Para trabajo diferido (p. ej. gráficos, que en un panel oculto miden 0):
//   PanelTabs.whenShown("graficos", function (panel) { … });   // una sola vez, al verse por primera vez
// También se emite el evento "ph:tabshown" (burbujea desde el panel) con detail = { key, panel }.
(function () {
    "use strict";

    var tablist = document.querySelector('.ph-tabs[role="tablist"]');
    var pending = {};
    var shown = {};

    window.PanelTabs = {
        whenShown: function (key, callback) {
            if (shown[key]) {
                callback(shown[key]);
                return;
            }
            (pending[key] = pending[key] || []).push(callback);
        },
        select: function () { }
    };

    if (!tablist) return;

    var param = tablist.getAttribute("data-ph-param") || "tab";
    var tabs = Array.prototype.slice.call(tablist.querySelectorAll('[role="tab"][data-ph-key]'));
    var panels = Array.prototype.slice.call(document.querySelectorAll('[role="tabpanel"][data-ph-key]'));
    if (tabs.length === 0) return;

    var defaultKey = tabs[0].getAttribute("data-ph-key");

    function keyOf(element) {
        return element.getAttribute("data-ph-key");
    }

    function tabFor(key) {
        return tabs.filter(function (tab) { return keyOf(tab) === key; })[0] || null;
    }

    function panelFor(key) {
        return panels.filter(function (panel) { return keyOf(panel) === key; })[0] || null;
    }

    function activeKey() {
        var selected = tabs.filter(function (tab) { return tab.getAttribute("aria-selected") === "true"; })[0];
        return selected ? keyOf(selected) : defaultKey;
    }

    function keyFromUrl() {
        var value = (new URLSearchParams(window.location.search).get(param) || "").toLowerCase();
        if (!value) return null;
        var match = tabs.filter(function (tab) {
            var aliases = (tab.getAttribute("data-ph-alias") || "").toLowerCase().split(/\s+/);
            return keyOf(tab).toLowerCase() === value || aliases.indexOf(value) !== -1;
        })[0];
        return match ? keyOf(match) : null;
    }

    function urlFor(key) {
        var url = new URL(window.location.href);
        if (key === defaultKey) {
            url.searchParams.delete(param);
        } else {
            url.searchParams.set(param, key);
        }
        url.hash = "";
        return url.pathname + url.search;
    }

    // Solo desplaza la barra de pestañas, nunca la página.
    function revealTab(tab) {
        var left = tab.offsetLeft - tablist.offsetLeft;
        var right = left + tab.offsetWidth;
        var margin = 32;
        if (left - margin < tablist.scrollLeft) {
            tablist.scrollLeft = Math.max(0, left - margin);
        } else if (right + margin > tablist.scrollLeft + tablist.clientWidth) {
            tablist.scrollLeft = right + margin - tablist.clientWidth;
        }
    }

    function announce(key) {
        var panel = panelFor(key);
        if (!panel) return;
        var firstTime = !shown[key];
        shown[key] = panel;
        if (firstTime && pending[key]) {
            pending[key].splice(0).forEach(function (callback) { callback(panel); });
        }
        panel.dispatchEvent(new CustomEvent("ph:tabshown", { bubbles: true, detail: { key: key, panel: panel } }));
    }

    function setRovingFocus(activeTab) {
        tabs.forEach(function (tab) {
            tab.setAttribute("tabindex", tab === activeTab ? "0" : "-1");
        });
    }

    function select(key, options) {
        options = options || {};
        var tab = tabFor(key);
        if (!tab) return;
        var changed = activeKey() !== key;

        tabs.forEach(function (item) {
            item.setAttribute("aria-selected", item === tab ? "true" : "false");
        });
        setRovingFocus(tab);
        panels.forEach(function (panel) {
            panel.hidden = keyOf(panel) !== key;
        });
        revealTab(tab);

        if (options.push && changed) {
            window.history.pushState({ phTab: key }, "", urlFor(key));
        }
        if (changed || options.force) {
            announce(key);
        }
    }

    window.PanelTabs.select = function (key) {
        select(key, { push: true });
    };

    function isPlainClick(event) {
        return event.button === 0 && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey;
    }

    tablist.addEventListener("click", function (event) {
        var tab = event.target.closest('[role="tab"]');
        if (!tab || !isPlainClick(event)) return;
        event.preventDefault();
        select(keyOf(tab), { push: true });
    });

    // Cambiar de pestaña modifica la URL: las flechas solo mueven el foco y Enter o Espacio la activan.
    tablist.addEventListener("keydown", function (event) {
        var index = tabs.indexOf(event.target);
        if (index === -1) return;
        var target = null;
        switch (event.key) {
            case "ArrowRight": target = tabs[(index + 1) % tabs.length]; break;
            case "ArrowLeft": target = tabs[(index - 1 + tabs.length) % tabs.length]; break;
            case "Home": target = tabs[0]; break;
            case "End": target = tabs[tabs.length - 1]; break;
            case " ":
                event.preventDefault();
                select(keyOf(event.target), { push: true });
                return;
            default:
                return;
        }
        event.preventDefault();
        setRovingFocus(target);
        target.focus({ preventScroll: true });
        revealTab(target);
    });

    // Al salir de la barra, Tab vuelve a entrar por la pestaña seleccionada.
    tablist.addEventListener("focusout", function (event) {
        if (!tablist.contains(event.relatedTarget)) {
            setRovingFocus(tabFor(activeKey()));
        }
    });

    // Tarjetas e indicadores que abren una pestaña.
    document.addEventListener("click", function (event) {
        var link = event.target.closest("[data-ph-tab]");
        if (!link || !isPlainClick(event)) return;
        var key = link.getAttribute("data-ph-tab");
        var tab = tabFor(key);
        if (!tab) return;
        event.preventDefault();
        select(key, { push: true });
        var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        tablist.scrollIntoView({ behavior: reduceMotion ? "auto" : "smooth", block: "start" });
        tab.focus({ preventScroll: true });
    });

    var initialKey = activeKey();
    window.history.replaceState({ phTab: initialKey }, "", window.location.href);

    window.addEventListener("popstate", function (event) {
        var key = (event.state && event.state.phTab) || keyFromUrl() || defaultKey;
        select(key);
    });

    // Espera a que los scripts de la página registren sus whenShown / escuchas.
    function start() {
        window.setTimeout(function () { select(initialKey, { force: true }); }, 0);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
})();
