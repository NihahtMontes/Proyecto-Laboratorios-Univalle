# Reference / Restore — Details Pages

Backup completo de `Requests/Details.cshtml` (L-7) y `EquipmentUnits/Details.cshtml`.
Incluye estilos auxiliares para restauración.

---

## 1. Requests/Details.cshtml (L-7)

**Path:** `Pages/Requests/Details.cshtml`

### Full Content

```cshtml
@page
@model Proyecto_Laboratorios_Univalle.Pages.Requests.DetailsModel
@using Proyecto_Laboratorios_Univalle.Models.Enums

@if (TempData["Error"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
        <i class="fas fa-exclamation-triangle mr-2"></i> @TempData["Error"]
        <button type="button" class="close" data-dismiss="alert" aria-label="Close">
            <span aria-hidden="true">&times;</span>
        </button>
    </div>
}

<div class="row">
    <div class="col-lg-8">

        @{
            bool isWizard = Model.IsWizard;
            var equipmentName = Model.Request.Equipment?.Name ?? Model.Request.EquipmentUnit?.Equipment?.Name ?? "Equipo historico no disponible";
            var inventoryNumber = Model.Request.EquipmentUnit?.InventoryNumber ?? "Sin inventario";
            var equipmentCategory = Model.Request.Equipment?.Category.ToString() ?? Model.Request.EquipmentUnit?.Equipment?.Category.ToString() ?? "Historico";
            if (isWizard)
            {
                await Html.RenderPartialAsync("_WizardSteps", 2);
            }
        }

        <div class="card shadow-sm border-0 mb-4 printable-card">
            <div class="card-body p-4">
                <div class="d-flex no-block align-items-center mb-4">
                    <div>
                        <h4 class="card-title text-dark font-weight-bold">Paso 2: Solicitud Técnica (L-7)</h4>
                        <h6 class="card-subtitle text-muted small">Detalle del requerimiento técnico registrado. (Expediente: #@Model.Request.Id)</h6>
                    </div>
                    <div class="ml-auto d-print-none">
                        <i class="fas fa-clipboard-check fa-2x text-info opacity-5"></i>
                    </div>
                </div>

                <div class="row mb-4">
                    <div class="col-12">
                        <label class="small text-muted font-weight-bold uppercase mb-1">Equipamiento Afectado</label>
                        <div class="p-3 bg-light rounded border-left border-info font-weight-bold text-dark d-flex justify-content-between align-items-center">
                            <span>
                                <i class="fas fa-tools mr-2 text-info"></i> @equipmentName
                                <span class="text-muted font-weight-normal small ml-1">(@inventoryNumber)</span>
                            </span>
                            <span class="badge badge-pill badge-info px-3">@equipmentCategory</span>
                        </div>
                    </div>
                </div>

                <h5 class="font-weight-bold text-dark mb-3">
                    @if (Model.Request.Type == RequestType.Technical)
                    {
                        <i class="fas fa-align-left mr-2 text-info"></i> <span>Detalles Técnicos de Intervención</span>
                    }
                    else
                    {
                        <i class="fas fa-shopping-cart mr-2 text-success"></i> <span>Detalles de Compra</span>
                    }
                </h5>

                <div class="row">
                    <div class="col-12">
                        <div class="mb-4">
                            <label class="control-label font-weight-bold small uppercase text-muted mb-1">Contenido de la Solicitud / Justificación</label>
                            <div class="p-3 bg-white border rounded text-dark" style="min-height: 80px; white-space: pre-wrap;">@Model.Request.Description</div>
                        </div>

                        <div class="row mb-4">
                            @if (Model.Request.Type == RequestType.Technical)
                            {
                                <div class="col-md-6">
                                    <label class="control-label font-weight-bold small uppercase text-muted mb-1">Tiempo de Reparación Registrado</label>
                                    <div class="p-2 px-3 bg-light rounded border text-dark font-weight-bold">
                                        <i class="far fa-clock mr-2 text-info"></i> @(string.IsNullOrEmpty(Model.Request.EstimatedRepairTime) ? "No especificado" : Model.Request.EstimatedRepairTime)
                                    </div>
                                </div>
                            }
                            else
                            {
                                <div class="col-md-6">
                                    <label class="control-label font-weight-bold small uppercase text-success mb-1">Código de Inversión (Activos)</label>
                                    <div class="p-2 px-3 bg-light rounded border border-success text-dark font-weight-bold">
                                        <i class="fas fa-barcode mr-2 text-success"></i> @(string.IsNullOrEmpty(Model.Request.InvestmentCode) ? "No aplica" : Model.Request.InvestmentCode)
                                    </div>
                                </div>
                            }

                            <div class="col-md-6 mt-3 mt-md-0">
                                <label class="control-label font-weight-bold small uppercase text-muted mb-1">Nivel de Prioridad</label>
                                <div class="p-2 px-3 bg-light rounded border text-dark font-weight-bold">
                                    @{
                                        var pIcon = Model.Request.Priority == RequestPriority.High ? "fa-bolt text-danger" : (Model.Request.Priority == RequestPriority.Medium ? "fa-info-circle text-warning" : "fa-check-circle text-info");
                                    }
                                    <i class="fas @pIcon mr-2"></i> @Html.DisplayFor(m => m.Request.Priority)
                                </div>
                            </div>
                        </div>

                        <div class="mb-4">
                            <label class="control-label font-weight-bold small uppercase text-muted mb-1">Observaciones Adicionales / Notas de Campo</label>
                            <div class="p-3 bg-light border rounded text-muted small italic">
                                @(string.IsNullOrEmpty(Model.Request.Observations) ? "Sin observaciones registradas." : Model.Request.Observations)
                            </div>
                        </div>
                    </div>
                </div>

                @if (Model.Request.CostDetails != null && Model.Request.CostDetails.Any())
                {
                    <div class="mt-4 mb-4">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <h5 class="font-weight-bold text-success mb-0 small uppercase"><i class="fas fa-list-ol mr-2"></i> Ítems del Presupuesto (Costos Registrados)</h5>
                        </div>
                        <div class="table-responsive border rounded bg-white shadow-none">
                            <table class="table table-sm table-hover mb-0">
                                <thead class="bg-light text-muted small uppercase">
                                    <tr>
                                        <th class="border-top-0 pl-3">Concepto</th>
                                        <th class="border-top-0 text-center">Cant.</th>
                                        <th class="border-top-0 text-center">Unidad</th>
                                        <th class="border-top-0 text-right">P. Unit.</th>
                                        <th class="border-top-0 text-right pr-3">Subtotal</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    @{ decimal grandTotal = 0; }
                                    @foreach (var item in Model.Request.CostDetails)
                                    {
                                        var subtotal = item.Quantity * item.UnitPrice;
                                        grandTotal += subtotal;
                                        <tr>
                                            <td class="pl-3 font-weight-bold text-dark">@item.Concept</td>
                                            <td class="text-center font-weight-bold">@item.Quantity.ToString("0.##")</td>
                                            <td class="text-center small text-muted">@item.UnitOfMeasure</td>
                                            <td class="text-right">@item.UnitPrice.ToString("N2")</td>
                                            <td class="text-right pr-3 font-weight-bold text-dark">@subtotal.ToString("N2")</td>
                                        </tr>
                                    }
                                </tbody>
                                <tfoot class="bg-light">
                                    <tr class="font-weight-bold">
                                        <td colspan="4" class="text-right text-dark uppercase pr-3 pt-2 pb-2 small">Total Estimado:</td>
                                        <td class="text-right text-success pr-3 pt-2 pb-2" style="font-size: 1rem;">Bs @grandTotal.ToString("N2")</td>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </div>
                }

                @if (Model.Request.Status != RequestStatus.Pending)
                {
                    <div class="mt-5 border-top pt-4">
                        <h5 class="font-weight-bold text-dark mb-3">
                            <i class="fas fa-check-double mr-2 text-info"></i> Resolución Administrativa
                        </h5>
                        @{
                            bool isSuccess = Model.Request.Status == RequestStatus.Approved || Model.Request.Status == RequestStatus.Completed;
                            var resClass = isSuccess ? "bg-light-success text-success border-success" : "bg-light-danger text-danger border-danger";
                            var resIcon = isSuccess ? "fa-check-circle" : "fa-times-circle";
                        }
                        <div class="p-4 @resClass rounded border-left border-3 shadow-none">
                            <div class="d-flex align-items-center">
                                <i class="fas @resIcon fa-2x mr-3"></i>
                                <div>
                                    <h6 class="mb-0 font-weight-bold uppercase">Estado: @Html.DisplayFor(m => m.Request.Status)</h6>
                                    <small class="opacity-7">Evaluado el @Model.Request.ApprovalDate?.ToString("dd/MM/yyyy HH:mm")</small>
                                </div>
                            </div>
                            
                            @if (!string.IsNullOrEmpty(Model.Request.RejectionReason))
                            {
                                <div class="mt-3 p-3 bg-white rounded border border-danger small text-danger">
                                    <i class="fas fa-info-circle mr-1"></i> <strong>Motivo de Rechazo:</strong> @Model.Request.RejectionReason
                                </div>
                            }
                        </div>
                    </div>
                }
                else
                {
                    <div class="mt-5 border-top pt-4">
                        <div class="alert alert-warning border-0 p-4 bg-light-warning text-warning d-flex align-items-center rounded shadow-none">
                            <i class="fas fa-clock fa-2x mr-3 opacity-5"></i>
                            <div>
                                <h6 class="font-weight-bold mb-0 text-warning uppercase small">Trámite en Revisión Pendiente</h6>
                                <p class="mb-0 small">Esta solicitud está en cola para evaluación por el equipo administrativo.</p>
                            </div>
                        </div>
                    </div>
                }
            </div>
        </div>
    </div>

    <div class="col-lg-4 d-print-none">
        <div class="card shadow-sm border-0 mb-4 border-left border-3" style="background: #f5f3ff; border-color: #7c3aed !important;">
            <div class="card-body">
                <h5 class="font-weight-bold small uppercase mb-3" style="color: #7c3aed;"><i class="fas fa-shield-alt mr-2"></i> Estado de Trámite</h5>
                <hr class="mb-4" />
                
                <div class="mb-3">
                    <label class="text-muted uppercase font-weight-bold d-block" style="font-size: 0.65rem;">Identificador Único</label>
                    <span class="font-weight-bold text-dark" style="font-size: 1.2rem;">#REQ-@Model.Request.Id.ToString("D5")</span>
                </div>

                <div class="mb-0">
                    <label class="text-muted uppercase font-weight-bold d-block" style="font-size: 0.65rem;">Estatus Actual</label>
                    @{
                        var sBadge = Model.Request.Status == RequestStatus.Approved || Model.Request.Status == RequestStatus.Completed ? "badge-success" : (Model.Request.Status == RequestStatus.Rejected ? "badge-danger" : "badge-warning");
                    }
                    <span class="badge badge-pill @sBadge px-3 py-1 font-weight-bold">@Html.DisplayFor(m => m.Request.Status)</span>
                </div>
            </div>
        </div>

        <div class="card shadow-sm border-0 mb-4">
            <div class="card-body">
                <h5 class="card-title text-dark font-weight-bold mb-4 small uppercase"><i class="fas fa-history mr-2 text-warning"></i> Bitácora de Auditoría</h5>
                <hr />
                <div class="audit-log">
                    <div class="mb-3">
                        <label class="text-muted d-block small uppercase mb-0 font-weight-bold" style="font-size: 0.65rem;">Origen de Solicitud</label>
                        <span class="font-weight-bold text-dark d-block">@(Model.Request.CreatedBy?.FullName ?? "Sesión de Usuario")</span>
                        <span class="text-muted small">@Model.Request.CreatedDate.ToString("dd-MM-yyyy HH:mm")</span>
                    </div>
                    @if (Model.Request.LastModifiedDate.HasValue)
                    {
                        <div class="mb-0 border-top pt-3 mt-3">
                            <label class="text-muted d-block small uppercase mb-0 font-weight-bold" style="font-size: 0.65rem;">Última Actualización</label>
                            <span class="font-weight-bold text-dark d-block">@(Model.Request.ModifiedBy?.FullName ?? "Administración")</span>
                            <span class="text-muted small">@Model.Request.LastModifiedDate.Value.ToString("dd-MM-yyyy HH:mm")</span>
                        </div>
                    }
                </div>
            </div>
        </div>

        <div class="form-actions mt-4">
            <a asp-page="./Edit" asp-route-id="@Model.Request.Id"
               asp-route-isWizard="@isWizard" asp-route-managementId="@ViewData["ManagementId"]"
               class="btn btn-primary btn-block btn-rounded shadow-sm font-weight-bold py-3 mb-3">
                <i class="fas fa-edit mr-2"></i> Editar Registro
            </a>

            <a href="/api/reports/download/l7?requestId=@Model.Request.Id&managementId=@ViewData["ManagementId"]"
               class="btn btn-success btn-block btn-rounded shadow-sm font-weight-bold py-3 mb-3">
                <i class="fas fa-file-excel mr-2"></i> Descargar Solicitud L-7
            </a>

            @if (isWizard)
            {
                <a asp-page="/Index" asp-route-ShowWizard="true" asp-route-Step="3" asp-route-ManagementId="@ViewData["ManagementId"]"
                   class="btn btn-outline-secondary btn-block btn-rounded font-weight-bold py-2 mb-4">
                    <i class="fas fa-arrow-left mr-2"></i> Volver al Wizard
                </a>
            }
            else
            {
                <a asp-page="./Index" class="btn btn-outline-secondary btn-block btn-rounded font-weight-bold py-2 mb-4">
                    <i class="fas fa-arrow-left mr-2"></i> Volver al Listado
                </a>
            }
        </div>
    </div>
</div>
```

### Auxiliary Styles — L-7 Requests Details

```css
/* Estilos del Sistema Originales */
.bg-light-success { background-color: rgba(40, 167, 69, 0.05); }
.bg-light-danger { background-color: rgba(220, 53, 69, 0.05); }
.bg-light-warning { background-color: rgba(255, 193, 7, 0.05); }
.border-left-3 { border-left-width: 4px !important; }
.italic { font-style: italic; }
.opacity-5 { opacity: 0.15; }
.audit-log label { letter-spacing: 0.5px; }

/* Estilos del Wizard (Paso 5) */
.wizard-steps { margin-bottom: 40px; }
.step-line { position: absolute; top: 15px; left: 5%; width: 90%; height: 2px; background: #e9ecef; z-index: 1; }
.step-item { position: relative; z-index: 2; text-align: center; width: 15%; }
.step-number { width: 32px; height: 32px; line-height: 32px; border-radius: 50%; background: #fff; border: 2px solid #e9ecef; display: block; margin: 0 auto 5px; font-weight: bold; color: #adb5bd; }
.step-label { font-size: 11px; font-weight: bold; text-transform: uppercase; color: #adb5bd; }
.step-item.active .step-number { background: #2962ff; border-color: #2962ff; color: #fff; }
.step-item.active .step-label { color: #2962ff; }

/* Estilos de Impresión */
@media print {
    body * { visibility: hidden; }
    .printable-card, .printable-card * { visibility: visible; }
    .printable-card { position: absolute; left: 0; top: 0; width: 100%; border: none !important; box-shadow: none !important; }
    .d-print-none { display: none !important; }
}
```

---

## 2. EquipmentUnits/Details.cshtml

**Path:** `Pages/EquipmentUnits/Details.cshtml`

### Full Content

```cshtml
@page
@model Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits.DetailsModel
@using Proyecto_Laboratorios_Univalle.Helpers
@using Proyecto_Laboratorios_Univalle.Models.Enums
@{
    ViewData["Title"] = "Detalles de Unidad Física";
    var showMaintenancesTab = HttpContext.Request.Query.Keys.Any(k => k.StartsWith("Maintenance"));
}

<div class="row">
    <div class="col-lg-4">
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-body p-4">
                <div class="text-center mb-4 pb-2">
                    <div class="display-4 text-info mb-2"><i class="fas fa-barcode"></i></div>
                    <h4 class="font-weight-bold mb-0 text-dark">@Model.EquipmentUnit.InventoryNumber</h4>
                    <span class="text-muted small uppercase font-weight-bold">Control de Inventario</span>
                </div>
                
                <div class="row pt-2 text-center border-top border-bottom py-3 mb-4">
                    <div class="col-6 border-right">
                        <label class="text-muted small uppercase d-block mb-0 font-weight-bold">Ciudad / Sede</label>
                        <span class="text-dark font-weight-bold">@(Model.EquipmentUnit.Equipment?.City?.Name ?? "N/A")</span>
                    </div>
                    <div class="col-6">
                        <label class="text-muted small uppercase d-block mb-0 font-weight-bold">País / Origen</label>
                        <span class="text-dark font-weight-bold">@(Model.EquipmentUnit.Equipment?.Country?.Name ?? "N/A")</span>
                    </div>
                </div>

                <div class="mb-4">
                    <label class="text-muted small uppercase font-weight-bold d-block mb-1"><i class="fas fa-microscope mr-1 text-info"></i> Equipo (Definición)</label>
                    <a asp-page="/Equipment/Details" asp-route-id="@Model.EquipmentUnit.EquipmentId" class="h5 font-weight-bold text-info d-block mb-1">
                        @Model.EquipmentUnit.Equipment?.Name
                    </a>
                    <div class="small text-muted font-weight-bold">
                        @(Model.EquipmentUnit.Equipment?.Brand ?? "Sin Marca") - @(Model.EquipmentUnit.Equipment?.Model ?? "Sin Modelo")
                    </div>
                </div>

                @if (Model.EquipmentUnit.Career != null)
                {
                    <div class="mb-4">
                        <label class="text-muted small uppercase font-weight-bold d-block mb-1"><i class="fas fa-graduation-cap mr-1 text-info"></i> Carrera / Programa</label>
                        <span class="font-weight-bold text-dark h6">@Model.EquipmentUnit.Career.Name</span>
                    </div>
                }

                <div class="mb-4">
                    <label class="text-muted small uppercase font-weight-bold d-block mb-1"><i class="fas fa-hospital mr-1 text-info"></i> Laboratorio Asignado</label>
                    <span class="font-weight-bold text-dark h6">@(Model.EquipmentUnit.Laboratory?.Name ?? "No asignado")</span>
                    <div class="small text-muted italic">@(Model.EquipmentUnit.Laboratory?.Faculty?.Name ?? "Sin Facultad")</div>
                </div>

                <div class="row mb-4">
                    <div class="col-4">
                        <label class="text-muted small uppercase font-weight-bold d-block mb-1">Estado</label>
                        <span class="badge badge-pill badge-info px-2 py-1 font-weight-bold shadow-none small">@Html.DisplayFor(m => m.EquipmentUnit.CurrentStatus)</span>
                    </div>
                    <div class="col-4">
                        <label class="text-muted small uppercase font-weight-bold d-block mb-1">Condición</label>
                        <span class="badge badge-pill badge-warning px-2 py-1 font-weight-bold shadow-none small text-white">@Html.DisplayFor(m => m.EquipmentUnit.PhysicalCondition)</span>
                    </div>
                    <div class="col-4">
                        <label class="text-muted small uppercase font-weight-bold d-block mb-1">Serie</label>
                        <span class="text-dark font-weight-bold small">@(Model.EquipmentUnit.SerialNumber ?? "N/A")</span>
                    </div>
                </div>

                @if (Model.EquipmentUnit.AcquisitionValue.HasValue)
                {
                    <div class="mb-4">
                        <label class="text-muted small uppercase font-weight-bold d-block mb-1">Valor Contable</label>
                        <span class="text-dark font-weight-bold h5">Bs @Model.EquipmentUnit.AcquisitionValue.Value.ToString("N2")</span>
                    </div>
                }

                <hr />
                <div class="mt-4 pt-2">
                    <a asp-page="./Edit" asp-route-id="@Model.EquipmentUnit.Id" class="btn btn-warning btn-block font-weight-bold text-white mb-2 btn-rounded shadow-sm py-2">
                        <i class="fas fa-edit mr-1"></i> Editar Unidad
                    </a>
                    <a asp-page="/Equipment/Details" asp-route-id="@Model.EquipmentUnit.EquipmentId" class="btn btn-outline-secondary btn-block font-weight-bold btn-rounded py-2">
                        <i class="fas fa-arrow-left mr-1"></i> Volver al Equipo
                    </a>
                </div>
            </div>
        </div>

        <div class="card shadow-sm border-0 mb-4 bg-light border-left border-info">
            <div class="card-body p-4">
                <h6 class="font-weight-bold text-info mb-3 uppercase small"><i class="fas fa-clipboard-check mr-2"></i> Ficha Técnica del Catálogo</h6>
                
                <div class="mb-3">
                    <label class="text-muted small uppercase font-weight-bold d-block mb-1">Tipo de Recurso</label>
                    <span class="text-dark font-weight-bold">@(Model.EquipmentUnit.Equipment != null ? EnumHelper.GetDisplayName(Model.EquipmentUnit.Equipment.Category) : "Sin dato")</span>
                </div>

                <div class="mb-3">
                    <label class="text-muted small uppercase font-weight-bold d-block mb-1">Clasificación / Material</label>
                    <span class="text-dark font-weight-bold">
                        @if (Model.EquipmentUnit.Equipment?.Category == Proyecto_Laboratorios_Univalle.Models.Enums.EquipmentCategory.Utensil)
                        {
                            @(Model.EquipmentUnit.Equipment != null ? EnumHelper.GetDisplayName(Model.EquipmentUnit.Equipment.UtensilType) : "Sin dato")
                        }
                        else
                        {
                            @(Model.EquipmentUnit.Equipment != null ? EnumHelper.GetDisplayName(Model.EquipmentUnit.Equipment.TypeClassification) : "Sin dato")
                        }
                    </span>
                </div>

                @if (!string.IsNullOrEmpty(Model.EquipmentUnit.Equipment?.Description))
                {
                    <div>
                        <label class="text-muted small uppercase font-weight-bold d-block mb-1">Descripción / Notas Adicionales</label>
                        <p class="small text-dark mb-0">@Model.EquipmentUnit.Equipment.Description</p>
                    </div>
                }
            </div>
        </div>
    </div>

    <div class="col-lg-8">
        <div class="card shadow-sm border-0 mb-4">
            <div class="card-body p-4">
                @if (!string.IsNullOrEmpty(Model.EquipmentUnit.Notes))
                {
                    <div class="alert alert-soft-info p-3 mb-4 rounded shadow-none border border-info">
                        <label class="text-info small font-weight-bold uppercase mb-1 d-block"><i class="fas fa-sticky-note mr-1"></i> Notas de la Unidad:</label>
                        <span class="text-dark small">@Model.EquipmentUnit.Notes</span>
                    </div>
                }

                @if (Model.EquipmentUnit.Equipment?.Notes != null && Model.EquipmentUnit.Equipment.Notes.Any())
                {
                    <div class="card border shadow-sm mb-4">
                        <div class="card-header bg-white border-bottom-0 pt-3 px-4">
                            <h6 class="font-weight-bold mb-0 text-dark">
                                <i class="fas fa-clipboard-list mr-2 text-info"></i> Notas del Fabricante / Prevención (@Model.EquipmentUnit.Equipment.Notes.Count)
                            </h6>
                        </div>
                        <div class="card-body pt-2 px-4">
                            <ul class="mb-0 pl-3">
                                @foreach (var note in Model.EquipmentUnit.Equipment.Notes)
                                {
                                    <li class="mb-2 small text-dark font-weight-bold">@note.Note</li>
                                }
                            </ul>
                            <small class="text-muted italic">Estas pautas aplican a todas las unidades de este modelo.</small>
                        </div>
                    </div>
                }

                <ul class="nav nav-pills custom-pills mb-4" id="pills-tab" role="tablist">
                    <li class="nav-item">
                        <a class="nav-link @(showMaintenancesTab ? "" : "active")" id="pills-history-tab" data-toggle="pill" href="#history" role="tab" aria-controls="pills-history" aria-selected="true">
                            <i class="fas fa-history mr-1"></i> Historial de Estados
                        </a>
                    </li>
                    <li class="nav-item pl-2">
                        <a class="nav-link @(showMaintenancesTab ? "active" : "")" id="pills-maintenances-tab" data-toggle="pill" href="#maintenances" role="tab" aria-controls="pills-maintenances" aria-selected="false">
                            <i class="fas fa-tools mr-1"></i> Mantenimientos
                        </a>
                    </li>
                </ul>

                <div class="tab-content" id="pills-tabContent">
                    <div class="tab-pane fade @(showMaintenancesTab ? "" : "show active")" id="history" role="tabpanel" aria-labelledby="pills-history-tab">
                        <div class="table-responsive border rounded">
                            <table class="table table-hover mb-0">
                                <thead class="bg-light">
                                    <tr class="text-muted uppercase small font-weight-bold">
                                        <th class="pl-4">Fecha</th>
                                        <th>Estado Detectado</th>
                                        <th>Motivo del Cambio</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    @if (Model.EquipmentUnit.StateHistory != null && Model.EquipmentUnit.StateHistory.Any())
                                    {
                                        foreach (var h in Model.EquipmentUnit.StateHistory.OrderByDescending(x => x.StartDate))
                                        {
                                            <tr>
                                                <td class="font-weight-bold text-dark pl-4">@h.StartDate.ToString("dd/MM/yyyy HH:mm")</td>
                                                <td><span class="badge badge-pill badge-light border px-2 py-1">@EnumHelper.GetDisplayName(h.Status)</span></td>
                                                <td class="small text-muted">@(h.Reason ?? "Sin detalle")</td>
                                            </tr>
                                        }
                                    }
                                    else
                                    {
                                        <tr><td colspan="3" class="text-center py-5 text-muted italic">No hay registros historicos para esta unidad fisica.</td></tr>
                                    }
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <div class="tab-pane fade @(showMaintenancesTab ? "show active" : "")" id="maintenances" role="tabpanel" aria-labelledby="pills-maintenances-tab">
                        <div class="bg-light p-3 rounded border-left border-info mb-3">
                            <form method="get" asp-page="./Details">
                                <input type="hidden" name="id" value="@Model.EquipmentUnit.Id" />
                                <div class="row align-items-end">
                                    <div class="col-lg-2 col-md-6 mb-2">
                                        <label class="small font-weight-bold text-muted uppercase">Gestion</label>
                                        <select asp-for="MaintenanceManagementId" class="form-control custom-select" asp-items="Model.ManagementOptions">
                                            <option value="">Todas las gestiones</option>
                                        </select>
                                    </div>
                                    <div class="col-lg-2 col-md-6 mb-2">
                                        <label class="small font-weight-bold text-muted uppercase">Tipo de gestion</label>
                                        <select asp-for="MaintenanceManagementType" class="form-control custom-select" asp-items="Html.GetEnumSelectList<ManagementType>()">
                                            <option value="">Todos</option>
                                        </select>
                                    </div>
                                    <div class="col-lg-2 col-md-6 mb-2">
                                        <label class="small font-weight-bold text-muted uppercase">Satisfaccion</label>
                                        <select asp-for="MaintenanceSatisfactionFilter" class="form-control custom-select" asp-items="Html.GetEnumSelectList<MaintenanceSatisfaction>()">
                                            <option value="">Todas</option>
                                        </select>
                                    </div>
                                    <div class="col-lg-3 col-md-6 mb-2">
                                        <label class="small font-weight-bold text-muted uppercase">Busqueda</label>
                                        <input asp-for="MaintenanceSearchTerm" class="form-control" placeholder="Tecnico o descripcion..." />
                                    </div>
                                    <div class="col-lg-3 col-md-12 mb-2">
                                        <label class="small font-weight-bold text-muted uppercase d-block">&nbsp;</label>
                                        <div class="d-flex justify-content-lg-end">
                                            <button type="submit" class="btn btn-dark btn-rounded px-4 shadow-sm mr-2"><i class="fas fa-filter mr-1"></i> Filtrar</button>
                                            <a asp-page="./Details" asp-route-id="@Model.EquipmentUnit.Id" asp-route-MaintenanceManagementId="" class="btn btn-outline-secondary btn-rounded px-3" title="Limpiar filtros"><i class="fas fa-sync-alt"></i></a>
                                        </div>
                                    </div>
                                </div>
                            </form>
                        </div>

                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <span class="small text-muted font-weight-bold">Mostrando @Model.MaintenanceFirstItem-@Model.MaintenanceLastItem de @Model.MaintenanceHistory.TotalCount mantenimientos</span>
                        </div>

                        <div class="table-responsive border rounded">
                            <table class="table table-hover mb-0 v-middle">
                                <thead class="bg-light">
                                    <tr class="text-muted uppercase small font-weight-bold">
                                        <th class="pl-4">Gestion</th>
                                        <th>Fecha</th>
                                        <th>Trabajo</th>
                                        <th>Tecnico</th>
                                        <th>Estado</th>
                                        <th class="text-right">Costo</th>
                                        <th>Satisfaccion</th>
                                        <th class="text-right">Accion</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    @if (Model.MaintenanceHistory.Any())
                                    {
                                        foreach (var m in Model.MaintenanceHistory)
                                        {
                                            var date = m.EndDate ?? m.StartDate ?? m.ScheduledDate ?? m.CreatedDate;
                                            var totalCost = m.ActualCost ?? m.CalculatedTotal;
                                            <tr>
                                                <td class="pl-4 small font-weight-bold text-dark">@Model.FormatManagement(m.Management)</td>
                                                <td class="small font-weight-bold">@date.ToString("dd/MM/yyyy")</td>
                                                <td style="max-width: 260px;">
                                                    <div class="small font-weight-bold text-dark">@(string.IsNullOrWhiteSpace(m.Description) ? "Sin descripcion" : m.Description)</div>
                                                    <span class="text-muted small">@EnumHelper.GetDisplayName(m.MaintenanceType)</span>
                                                </td>
                                                <td class="small text-muted">@(m.Technician?.FullName ?? "Sin tecnico")</td>
                                                <td><span class="badge badge-pill badge-primary px-2 py-1">@EnumHelper.GetDisplayName(m.Status)</span></td>
                                                <td class="text-right small font-weight-bold text-success">@(totalCost > 0 ? $"Bs {totalCost:N2}" : "Sin costo")</td>
                                                <td class="small">
                                                    @if (m.SatisfactionLevel.HasValue)
                                                    {
                                                        <span class="font-weight-bold text-info">@EnumHelper.GetDisplayName(m.SatisfactionLevel.Value)</span>
                                                    }
                                                    else
                                                    {
                                                        <span class="text-muted">Sin dato</span>
                                                    }
                                                </td>
                                                <td class="text-right">
                                                    <a asp-page="/Maintenances/Details" asp-route-id="@m.Id" class="btn btn-sm btn-outline-info btn-rounded font-weight-bold px-3"><i class="fas fa-eye mr-1"></i> Ver detalle</a>
                                                </td>
                                            </tr>
                                        }
                                    }
                                    else
                                    {
                                        <tr><td colspan="8" class="text-center py-5 text-muted italic">No existen servicios de mantenimiento con los filtros actuales.</td></tr>
                                    }
                                </tbody>
                            </table>
                        </div>

                        @if (Model.MaintenanceHistory.TotalPages > 1)
                        {
                            <nav aria-label="Paginacion de mantenimientos" class="mt-3">
                                <ul class="pagination justify-content-center mb-0">
                                    <li class="page-item @(Model.MaintenanceHistory.HasPreviousPage ? "" : "disabled")">
                                        <a class="page-link" asp-page="./Details" asp-route-id="@Model.EquipmentUnit.Id" asp-route-MaintenancePageIndex="@(Model.MaintenanceHistory.PageIndex - 1)" asp-route-MaintenanceManagementId="@Model.MaintenanceManagementId" asp-route-MaintenanceManagementType="@Model.MaintenanceManagementType" asp-route-MaintenanceSatisfactionFilter="@Model.MaintenanceSatisfactionFilter" asp-route-MaintenanceSearchTerm="@Model.MaintenanceSearchTerm">Anterior</a>
                                    </li>
                                    @for (var i = 1; i <= Model.MaintenanceHistory.TotalPages; i++)
                                    {
                                        <li class="page-item @(i == Model.MaintenanceHistory.PageIndex ? "active" : "")">
                                            <a class="page-link" asp-page="./Details" asp-route-id="@Model.EquipmentUnit.Id" asp-route-MaintenancePageIndex="@i" asp-route-MaintenanceManagementId="@Model.MaintenanceManagementId" asp-route-MaintenanceManagementType="@Model.MaintenanceManagementType" asp-route-MaintenanceSatisfactionFilter="@Model.MaintenanceSatisfactionFilter" asp-route-MaintenanceSearchTerm="@Model.MaintenanceSearchTerm">@i</a>
                                        </li>
                                    }
                                    <li class="page-item @(Model.MaintenanceHistory.HasNextPage ? "" : "disabled")">
                                        <a class="page-link" asp-page="./Details" asp-route-id="@Model.EquipmentUnit.Id" asp-route-MaintenancePageIndex="@(Model.MaintenanceHistory.PageIndex + 1)" asp-route-MaintenanceManagementId="@Model.MaintenanceManagementId" asp-route-MaintenanceManagementType="@Model.MaintenanceManagementType" asp-route-MaintenanceSatisfactionFilter="@Model.MaintenanceSatisfactionFilter" asp-route-MaintenanceSearchTerm="@Model.MaintenanceSearchTerm">Siguiente</a>
                                    </li>
                                </ul>
                            </nav>
                        }
                    </div>
                </div>
            </div>
        </div>
        <div class="card shadow-sm border-0">
            <div class="card-body p-4">
                <h5 class="font-weight-bold text-dark mb-3 small uppercase"><i class="fas fa-info-circle mr-2 text-info"></i> Detalles de Ciclo de Vida Individual</h5>
                <hr />

                <div class="row mb-4">
                    <div class="col-md-6">
                        <div class="card shadow-sm border-0 bg-info text-white">
                            <div class="card-body p-4 text-center">
                                <i class="fas fa-clock fa-3x mb-3 opacity-7"></i>
                                <h5 class="card-title text-white font-weight-bold small uppercase mb-1">Vida Util del Ciclo</h5>
                                <span class="font-weight-bold h3 mb-0">@(Model.EquipmentUnit.Equipment?.UsefulLifeYears ?? 0)</span>
                                <span class="text-white opacity-7 d-block">anios de vida estimada</span>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="card shadow-sm border-0 bg-dark text-white">
                            <div class="card-body p-4 text-center">
                                <i class="fas fa-cog fa-3x mb-3 opacity-7"></i>
                                <h5 class="card-title text-white font-weight-bold small uppercase mb-1">En Operacion</h5>
                                <span class="font-weight-bold h3 mb-0">@(Model.EquipmentUnit.YearsInOperation ?? 0)</span>
                                <span class="text-white-50 d-block">anios desde fabricacion</span>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="row pt-2 text-center">
                    <div class="col-md-4 mb-3 border-right">
                        <label class="text-muted small uppercase d-block mb-1 font-weight-bold">Fecha Adquisicion</label>
                        <span class="text-dark font-weight-bold">@(Model.EquipmentUnit.AcquisitionDate?.ToString("dd/MM/yyyy") ?? "-")</span>
                    </div>
                    <div class="col-md-4 mb-3 border-right">
                        <label class="text-muted small uppercase d-block mb-1 font-weight-bold">Fecha de Fabricacion</label>
                        <span class="text-info font-weight-bold">@(Model.EquipmentUnit.ManufacturingDate?.ToString("dd/MM/yyyy") ?? "-")</span>
                    </div>
                    <div class="col-md-4 mb-3">
                        <label class="text-muted small uppercase d-block mb-1 font-weight-bold">Vida Util Restante (Est.)</label>
                        @{
                            var usefulLife = Model.EquipmentUnit.Equipment?.UsefulLifeYears ?? 0;
                            var yearsInOp = Model.EquipmentUnit.YearsInOperation ?? 0;
                            var remaining = usefulLife - yearsInOp;
                        }
                        <span class="font-weight-bold h4 mb-0 @(remaining <= 0 ? "text-danger" : remaining <= 2 ? "text-warning" : "text-success")">
                            @(remaining > 0 ? $"{remaining} anios" : "Excedido")
                        </span>
                    </div>
                </div>
            </div>
        </div>

        <!-- Equipment Product Image -->
        <div class="card shadow-sm border-0 mt-4">
            <div class="card-body p-4 text-center">
                <h5 class="font-weight-bold text-dark mb-3 small uppercase"><i class="fas fa-image mr-2 text-info"></i> Imagen del Producto</h5>
                <hr />
                @if (!string.IsNullOrEmpty(Model.EquipmentUnit.Equipment?.ImageUrl))
                {
                    <img src="~/uploads/equipment/@Model.EquipmentUnit.Equipment.ImageUrl" alt="@Model.EquipmentUnit.Equipment.Name" class="img-fluid rounded shadow-sm" style="max-height: 350px; object-fit: contain;" />
                    <p class="text-muted small mt-2 mb-0">@Model.EquipmentUnit.Equipment.Name - @(Model.EquipmentUnit.Equipment.Brand ?? "") @(Model.EquipmentUnit.Equipment.Model ?? "")</p>
                }
                else
                {
                    <div class="py-4 text-muted">
                        <i class="fas fa-image fa-3x mb-2 opacity-5 d-block"></i>
                        <span class="small font-weight-bold uppercase">Sin imagen de referencia</span>
                        <p class="small mt-1 mb-0 opacity-7">Puede agregar una imagen editando la ficha técnica del equipo.</p>
                    </div>
                }
            </div>
        </div>
    </div>
</div>
```

### Auxiliary Styles — EquipmentUnits Details

```css
.italic { font-style: italic; }

.alert-soft-info {
    background-color: #e1f5fe;
    color: #01579b;
    border-left: 5px solid #03a9f4;
}

.nav-pills .nav-link.active {
    background-color: #1e88e5;
    box-shadow: 0 4px 10px rgba(30, 136, 229, 0.2);
}

.nav-pills .nav-link {
    color: #607d8b;
    font-weight: 600;
    padding: 10px 20px;
    border-radius: 30px;
}
```
