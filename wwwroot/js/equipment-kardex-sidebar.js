(function (global) {
    'use strict';

    function escapeHtml(value, fallback) {
        if (value === null || value === undefined || value === '') return fallback || '—';
        var div = document.createElement('div');
        div.textContent = String(value);
        return div.innerHTML;
    }

    function badgeClass(value) {
        var allowed = ['badge-success', 'badge-warning', 'badge-danger', 'badge-info', 'badge-secondary'];
        return allowed.indexOf(value) >= 0 ? value : 'badge-secondary';
    }

    function renderLastState(state) {
        if (!state) {
            return '<p class="text-muted small mb-0">No existen cambios de estado registrados.</p>';
        }

        return `
            <div class="d-flex flex-wrap align-items-center mb-2">
                <span class="badge badge-pill ${badgeClass(state.badgeClass)} px-3 mr-2 mb-1">${escapeHtml(state.status)}</span>
                <span class="small font-weight-bold text-dark mb-1">${escapeHtml(state.startDate)}</span>
            </div>
            <p class="small text-muted mb-1">${escapeHtml(state.reason, 'Sin motivo registrado')}</p>
            <small class="text-muted">Registrado por ${escapeHtml(state.registeredBy, 'Sistema')}</small>`;
    }

    function renderLastMaintenance(maintenance) {
        if (!maintenance) {
            return `
                <div class="alert alert-light border mb-0 small">
                    <i class="fas fa-info-circle text-info mr-1"></i>
                    No existe un mantenimiento formal para esta unidad. El cambio de estado mostrado arriba no es un mantenimiento.
                </div>`;
        }

        var maintenanceId = encodeURIComponent(maintenance.id);
        return `
            <div class="d-flex flex-wrap align-items-center mb-2">
                <span class="badge badge-pill ${badgeClass(maintenance.badgeClass)} px-3 mr-2 mb-1">${escapeHtml(maintenance.status)}</span>
                <span class="small font-weight-bold text-dark mb-1">${escapeHtml(maintenance.date)}</span>
            </div>
            <div class="small text-dark font-weight-bold mb-1">${escapeHtml(maintenance.type)} · ${escapeHtml(maintenance.serviceType)}</div>
            <div class="small text-muted mb-2">${escapeHtml(maintenance.management)} · ${escapeHtml(maintenance.technician)}</div>
            <p class="small text-muted mb-2">${escapeHtml(maintenance.description, 'Sin descripción registrada')}</p>
            <div class="d-flex flex-wrap mb-3">
                <span class="badge badge-light border mr-2 mb-1">${escapeHtml(maintenance.taskCount, '0')} tarea(s)</span>
                <span class="badge badge-light border mr-2 mb-1">${escapeHtml(maintenance.costItemCount, '0')} costo(s)</span>
                <span class="badge badge-light border mb-1">${escapeHtml(maintenance.totalCost, 'Sin costo registrado')}</span>
            </div>
            <a href="/Maintenances/Details?id=${maintenanceId}" target="_blank" rel="noopener"
               class="btn btn-info btn-block btn-rounded font-weight-bold">
                <i class="fas fa-tools mr-1"></i> Ver detalle del mantenimiento
            </a>`;
    }

    function render(data, equipmentUnitId) {
        var unitId = encodeURIComponent(data.equipmentUnitId || equipmentUnitId);
        return `
            <div class="bg-light p-4 rounded mb-4">
                <h5 class="font-weight-bold text-dark mb-1">${escapeHtml(data.name, 'Sin nombre')}</h5>
                <code class="text-primary font-weight-bold">${escapeHtml(data.inventoryNumber, 'Sin inventario')}</code>
            </div>
            <div class="mb-4">
                <label class="text-xs-premium text-muted d-block mb-1">Estado actual</label>
                <span class="badge badge-pill ${badgeClass(data.currentStatusBadgeClass)} px-3">${escapeHtml(data.currentStatus)}</span>
            </div>
            <section class="border-top pt-3 mb-4">
                <h6 class="small text-uppercase font-weight-bold text-dark mb-3">
                    <i class="fas fa-exchange-alt text-info mr-1"></i> Último cambio de estado
                </h6>
                ${renderLastState(data.lastStateChange)}
            </section>
            <section class="border-top pt-3 mb-4">
                <h6 class="small text-uppercase font-weight-bold text-dark mb-3">
                    <i class="fas fa-tools text-info mr-1"></i> Último mantenimiento formal
                </h6>
                ${renderLastMaintenance(data.lastMaintenance)}
            </section>
            <div class="border-top pt-3">
                <a href="/EquipmentUnits/Kardex/${unitId}" target="_blank" rel="noopener"
                   class="btn btn-outline-info btn-block btn-rounded font-weight-bold mb-2">
                    <i class="fas fa-history mr-1"></i> Ver historial integral
                </a>
                <a href="/EquipmentUnits/Details?id=${unitId}" target="_blank" rel="noopener"
                   class="btn btn-outline-secondary btn-block btn-rounded font-weight-bold">
                    <i class="fas fa-clipboard-list mr-1"></i> Ver ficha de la unidad
                </a>
            </div>`;
    }

    global.EquipmentKardexSidebar = { render: render };
})(window);
