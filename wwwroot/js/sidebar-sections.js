// Secciones plegables del menú lateral.
// Cada título (.sidebar-section-toggle) muestra u oculta los <li> que le siguen hasta el próximo título.
// Al cargar, todas quedan plegadas salvo la sección de la página actual
// (data-current-section del menú, que viene de ViewData["Section"], o la que contiene el enlace de la ruta actual).
(function () {
    "use strict";

    var nav = document.getElementById("sidebarnav");
    if (!nav) return;

    var HIDDEN = "sidebar-section-item--hidden";
    var currentSection = nav.getAttribute("data-current-section") || "";
    var currentPath = window.location.pathname.toLowerCase().replace(/\/+$/, "");

    function sectionItems(header) {
        var items = [];
        var next = header.nextElementSibling;
        while (next && !next.classList.contains("nav-small-cap")) {
            items.push(next);
            next = next.nextElementSibling;
        }
        return items;
    }

    function containsCurrentPage(items) {
        if (!currentPath) return false;
        return items.some(function (item) {
            return Array.prototype.some.call(item.querySelectorAll("a[href]"), function (link) {
                var path = link.pathname.toLowerCase().replace(/\/+$/, "");
                return path && path === currentPath;
            });
        });
    }

    function setOpen(button, items, open) {
        button.setAttribute("aria-expanded", open ? "true" : "false");
        items.forEach(function (item) {
            item.classList.toggle(HIDDEN, !open);
        });
    }

    Array.prototype.forEach.call(nav.querySelectorAll(".sidebar-section-toggle"), function (button) {
        var header = button.closest(".nav-small-cap");
        var items = sectionItems(header);

        if (items.length === 0) {
            header.classList.add("sidebar-section--empty");
            return;
        }

        var open = button.getAttribute("data-section") === currentSection || containsCurrentPage(items);
        setOpen(button, items, open);

        button.addEventListener("click", function () {
            setOpen(button, items, button.getAttribute("aria-expanded") !== "true");
        });
    });
})();
