Walkthrough — 6 Fases Ejecutadas
Fase 1: Modelo + BD (3 archivos + 1 migración)
Archivo	Cambio
Models/ManagementPlan.cs:44	KardexHistoryId (int?) entre DepartureId y AcquisitionRequestId
Models/ManagementPlan.cs:131	Nav prop KardexHistory → EquipmentStateHistory
Models/DepartureItem.cs	Nuevo. IAuditable, soft-delete con IsRemoved, Balance [NotMapped]
Models/Departure.cs:88	ICollection<DepartureItem> Items
Data/ApplicationDbContext.cs	DbSet<DepartureItem>, HasQueryFilter(i => !i.IsRemoved), relaciones Restrict + SetNull
Migración	Add_KardexHistoryId_And_DepartureItems aplicada
Fase 2: Departures/Create (L-3 multi-ítem)
- Create.cshtml.cs: Resuelve managementId desde ManagementPlanId → plan.ManagementId. Crea DepartureItems después del Departure. Fix TempData.Success/Error. Redirect con plan.ManagementId.
- Create.cshtml: Tabla dinámica de ítems con JS (add/remove rows). Wizard: 1 ítem pre-cargado con ProductName = equipo (inventario). Fuera de wizard: botón "Agregar Ítem".
Fase 3: Departures/Edit + Details
- Edit.cshtml.cs: Carga/sincroniza Departure.Items. Soft-delete (IsRemoved = true) para ítems quitados. Nuevos se agregan como DepartureItem. Fix TempData.
- Edit.cshtml: Tabla editable de ítems con JS add/remove.
- Details.cshtml.cs: Include(d => d.Items).
- Details.cshtml: Tabla Producto | Cantidad | Unidad | Devuelto | Saldo con badges de color.
Fase 4: Kardex/Create
- plan.KardexHistory = newHistory vincula el historial al ManagementPlan.
- Redirect: ManagementId = plan.ManagementId.
- Fix TempData.Success/Error.
- Fallback redirect: RedirectToPage("/Index") sin wizard params.
Fase 5: Acquisitions/Create
- Resuelve managementId desde ManagementPlanId antes de crear Request.
- ManagementId = managementId en el inicializador de Request.
- Clona CostDetail (nuevos objetos) en vez de mover los del mantenimiento.
- Redirect: ManagementId = plan.ManagementId.
Fase 6: Reporte L-3
- IReportService + ReportService: firma int unitId → int departureId.
- Genera desde cero (sin template L3.xlsx, elimina anti-patrón Worksheets.Add).
- Cabecera institucional, datos del Departure, tabla de ítems desde Departure.Items, firmas.
- ReportsController: parámetro departureId en DownloadL3.
