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
        if (result && (result.isConfirmed === true || result.value === true)) {
            const token = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content');
            if (!token) {
                Swal.fire('Error', 'No se pudo validar la seguridad de la solicitud. Recargue la página.', 'error');
                return;
            }

            Swal.fire({
                title: 'Anulando registros...',
                text: 'Por favor espera',
                allowOutsideClick: false,
                onOpen: () => {
                    Swal.showLoading();
                }
            });

            $.ajax({
                url: '/api/WizardRollback/redo',
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': token
                },
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

// ==========================================
// FILTROS GET EN VIVO (SMART INDEX)
// ==========================================
(function () {
    'use strict';

    var openFilterPill = null;

    function getFilterIcon(label) {
        var normalized = (label || '').toLocaleLowerCase();

        if (normalized.indexOf('tipo de recurso') >= 0) return 'fa-cube';
        if (normalized.indexOf('subclasific') >= 0) return 'fa-tags';
        if (normalized.indexOf('revisi') >= 0) return 'fa-clipboard-check';
        if (normalized.indexOf('ambiente') >= 0 || normalized.indexOf('laboratorio') >= 0) return 'fa-door-open';
        if (normalized.indexOf('prioridad') >= 0) return 'fa-exclamation-circle';
        if (normalized.indexOf('salida') >= 0) return 'fa-truck-loading';
        if (normalized.indexOf('disponibilidad') >= 0) return 'fa-file-alt';
        if (normalized.indexOf('gesti') >= 0) return 'fa-calendar-alt';
        if (normalized.indexOf('técnico') >= 0 || normalized.indexOf('tecnico') >= 0) return 'fa-user-cog';
        if (normalized.indexOf('categor') >= 0) return 'fa-tags';
        if (normalized.indexOf('satisfacci') >= 0) return 'fa-smile';
        if (normalized.indexOf('estado') >= 0) return 'fa-flag';

        return 'fa-filter';
    }

    function closeFilterPill(pill, returnFocus) {
        if (!pill) return;

        pill.menu.classList.remove('show');
        pill.wrapper.classList.remove('show');
        pill.button.setAttribute('aria-expanded', 'false');

        if (openFilterPill === pill) {
            openFilterPill = null;
        }

        if (returnFocus) {
            pill.button.focus();
        }
    }

    function openPillMenu(pill, focusLast) {
        if (openFilterPill && openFilterPill !== pill) {
            closeFilterPill(openFilterPill, false);
        }

        pill.menu.classList.add('show');
        pill.wrapper.classList.add('show');
        pill.button.setAttribute('aria-expanded', 'true');
        openFilterPill = pill;

        var options = pill.menu.querySelectorAll('[role="option"]:not([disabled])');
        if (!options.length) return;

        var selected = pill.menu.querySelector('[role="option"][aria-selected="true"]');
        (focusLast ? options[options.length - 1] : (selected || options[0])).focus();
    }

    function semanticIndicatorClass(option) {
        if (option.classList.contains('text-success')) return 'text-success';
        if (option.classList.contains('text-warning')) return 'text-warning';
        if (option.classList.contains('text-danger')) return 'text-danger';
        if (option.classList.contains('text-info')) return 'text-info';
        return '';
    }

    function cleanOptionText(text) {
        return (text || '').replace(/^\s*●\s*/, '').trim();
    }

    function enhanceFilterSelect(select) {
        if (select.dataset.liveFilterEnhanced === 'true') return;
        select.dataset.liveFilterEnhanced = 'true';

        var label = select.getAttribute('aria-label')
            || cleanOptionText(select.options.length ? select.options[0].textContent : '')
            || 'Filtro';
        var wrapper = document.createElement('div');
        var button = document.createElement('button');
        var icon = document.createElement('i');
        var value = document.createElement('span');
        var chevron = document.createElement('i');
        var menu = document.createElement('div');
        var pill = { select: select, wrapper: wrapper, button: button, menu: menu };

        wrapper.className = 'live-filter-pill-wrap mr-2 mb-2';
        button.type = 'button';
        button.className = 'btn btn-outline-secondary btn-rounded live-filter-pill';
        button.setAttribute('aria-haspopup', 'listbox');
        button.setAttribute('aria-expanded', 'false');
        icon.className = 'fas ' + getFilterIcon(label) + ' live-filter-pill-icon';
        value.className = 'live-filter-pill-value';
        chevron.className = 'fas fa-chevron-down live-filter-pill-chevron';
        chevron.setAttribute('aria-hidden', 'true');
        menu.className = 'dropdown-menu live-filter-menu';
        menu.setAttribute('role', 'listbox');
        menu.setAttribute('aria-label', label);

        button.appendChild(icon);
        button.appendChild(value);
        button.appendChild(chevron);
        wrapper.appendChild(button);
        wrapper.appendChild(menu);
        select.parentNode.insertBefore(wrapper, select.nextSibling);
        select.classList.add('live-filter-native');

        Array.prototype.forEach.call(select.options, function (option, index) {
            var menuOption = document.createElement('button');
            var indicatorClass = semanticIndicatorClass(option);

            menuOption.type = 'button';
            menuOption.className = 'dropdown-item live-filter-option';
            menuOption.setAttribute('role', 'option');
            menuOption.dataset.optionIndex = index.toString();
            menuOption.disabled = option.disabled;

            if (indicatorClass) {
                var indicator = document.createElement('i');
                indicator.className = 'fas fa-circle live-filter-status-dot ' + indicatorClass;
                indicator.setAttribute('aria-hidden', 'true');
                menuOption.appendChild(indicator);
            }

            var optionText = document.createElement('span');
            optionText.textContent = cleanOptionText(option.textContent);
            menuOption.appendChild(optionText);

            menuOption.addEventListener('click', function () {
                select.selectedIndex = index;
                updatePill();
                closeFilterPill(pill, false);
                select.dispatchEvent(new Event('change', { bubbles: true }));
            });

            menuOption.addEventListener('keydown', function (event) {
                var options = Array.prototype.slice.call(menu.querySelectorAll('[role="option"]:not([disabled])'));
                var currentIndex = options.indexOf(menuOption);

                if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                    event.preventDefault();
                    var direction = event.key === 'ArrowDown' ? 1 : -1;
                    options[(currentIndex + direction + options.length) % options.length].focus();
                } else if (event.key === 'Home') {
                    event.preventDefault();
                    options[0].focus();
                } else if (event.key === 'End') {
                    event.preventDefault();
                    options[options.length - 1].focus();
                } else if (event.key === 'Escape' || event.key === 'Tab') {
                    closeFilterPill(pill, event.key === 'Escape');
                }
            });

            menu.appendChild(menuOption);
        });

        function updatePill() {
            var selectedOption = select.options[select.selectedIndex] || select.options[0];
            var active = select.selectedIndex > 0;
            var displayValue = active && selectedOption
                ? cleanOptionText(selectedOption.textContent)
                : label;

            value.textContent = displayValue;
            button.setAttribute('aria-label', label + ': ' + displayValue);
            button.classList.toggle('is-active', active);
            wrapper.classList.toggle('d-none', select.disabled || select.classList.contains('d-none'));

            Array.prototype.forEach.call(menu.querySelectorAll('[role="option"]'), function (menuOption) {
                var selected = Number(menuOption.dataset.optionIndex) === select.selectedIndex;
                menuOption.setAttribute('aria-selected', selected ? 'true' : 'false');
                menuOption.classList.toggle('is-selected', selected);
            });
        }

        button.addEventListener('click', function (event) {
            event.stopPropagation();
            if (openFilterPill === pill) {
                closeFilterPill(pill, false);
            } else {
                openPillMenu(pill, false);
            }
        });

        button.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                openPillMenu(pill, false);
            } else if (event.key === 'ArrowUp') {
                event.preventDefault();
                openPillMenu(pill, true);
            } else if (event.key === 'Escape') {
                closeFilterPill(pill, false);
            }
        });

        select.addEventListener('change', updatePill);
        updatePill();
    }

    function submitFilterForm(form) {
        if (form.dataset.filterSubmitting === 'true') {
            return;
        }

        form.dataset.filterSubmitting = 'true';
        HTMLFormElement.prototype.submit.call(form);
    }

    function initializeLiveFilters() {
        document.querySelectorAll('[data-live-filter-form]').forEach(function (form) {
            form.querySelectorAll('[data-live-filter-control]').forEach(function (control) {
                if (control.tagName === 'SELECT') {
                    enhanceFilterSelect(control);
                }

                control.addEventListener('change', function () {
                    window.setTimeout(function () {
                        submitFilterForm(form);
                    }, 0);
                });
            });

            form.querySelectorAll('[data-live-filter-search]').forEach(function (searchInput) {
                var searchTimer;
                var isComposing = false;

                searchInput.addEventListener('compositionstart', function () {
                    isComposing = true;
                });

                searchInput.addEventListener('compositionend', function () {
                    isComposing = false;
                    searchInput.dispatchEvent(new Event('input'));
                });

                searchInput.addEventListener('input', function () {
                    window.clearTimeout(searchTimer);

                    if (isComposing) {
                        return;
                    }

                    searchTimer = window.setTimeout(function () {
                        submitFilterForm(form);
                    }, 500);
                });

                searchInput.addEventListener('keydown', function (event) {
                    if (event.key !== 'Enter' || isComposing) {
                        return;
                    }

                    event.preventDefault();
                    window.clearTimeout(searchTimer);
                    submitFilterForm(form);
                });
            });
        });
    }

    document.addEventListener('click', function (event) {
        if (openFilterPill && !openFilterPill.wrapper.contains(event.target)) {
            closeFilterPill(openFilterPill, false);
        }
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeLiveFilters);
    } else {
        initializeLiveFilters();
    }

    window.addEventListener('pageshow', function () {
        document.querySelectorAll('[data-live-filter-form]').forEach(function (form) {
            delete form.dataset.filterSubmitting;
        });
    });
})();
