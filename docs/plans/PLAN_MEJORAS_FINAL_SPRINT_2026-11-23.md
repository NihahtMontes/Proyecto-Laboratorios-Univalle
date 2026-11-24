# Plan: Mejoras Final Sprint (2026-11-23)

Fuente: lista «Mejoras Final Sprint» del usuario y la queja de la defensa: la grilla del laboratorio H-05 no se ve igual para el docente que para el SuperAdministrador.

## Diagnóstico verificado

| # | Problema | Causa en el código |
|---|---|---|
| 1 | La grilla del docente y la del admin no coinciden | Hay dos motores. Docente: `Pages/Docente/Disponibilidad.cshtml.cs` y `NuevaSolicitud.cshtml.cs`, sobre `IOccupancyService` (`Services/Occupancy/PlanningOccupancySource.cs`), que excluye `PlanningStatus.NonOccupying` y no filtra por gestión. Admin: `Services/LaboratoryGridService.cs:239`, con consulta propia que filtra por `ManagementId` y solo excluye Cancelled/Rejected. Cada pantalla calcula la limpieza por su cuenta (`IsBuffer` y los estados `gap`). |
| 2 | La limpieza bloquea un periodo entero | `PlanningSlot.OverlapsWithGap(gap = 1)` en `Services/PlanningSlot.cs:34`, usado por `OccupancyService`, `TeacherRequestService` (:229, :537) y `PlanningImportService` (:806, :826). Las vistas marcan como «Limpieza» el periodo anterior y el siguiente a cada bloque. |
| 3 | «Tuya» no se entiende | Etiqueta fija en `Pages/Docente/Disponibilidad.cshtml(.cs)` y `wwwroot/js/docente-solicitud.js:272`. |
| 4 | Material por periodos | El cálculo ya se hace por franja horaria real (`DemandPeak`, `ConflictGridService.LoadOfPeriod`); «8/4» = pedidos/stock. Falta mostrar las horas. **Esperando indicación del usuario.** |
| 5 | L-9 con reprogramaciones y reposiciones | **El L-9 no existe** (no hay plantilla, ni reporte, ni rastro en el Excel). Los datos sí están: `Rescheduling`, `DayRecovery`. **Pendiente de que el usuario lo defina.** |

Periodos reales (base REAL): P1 07:35–08:25, P2 08:35–09:25, P3 09:25–10:15, P4 10:25–11:15, P5 11:15–12:05, P6 12:15–13:05, P7 13:05–13:55, P8 14:05–14:55, P9 14:55–15:45, P10 15:55–16:45, P11 16:45–17:35, P12 17:45–18:35. Hay recesos de 10 minutos entre P1-P2, P3-P4, P5-P6, P7-P8, P9-P10 y P11-P12.

## Decisiones del usuario (2026-11-23)

- **Limpieza:** dura 10 minutos y se permiten prácticas en periodos contiguos. Ocupa el receso de 10 minutos; si no hay receso, los últimos 10 minutos de la práctica. Se dibuja como una franja delgada y deja de bloquear un periodo.
- **Grilla global:** una sola fuente de verdad, igual para todos los roles. Sin preferencia sobre privacidad, así que se aplica la recomendación: todos ven lo mismo que el admin (práctica, materia, grupo y docente).
- «Tuya» se reemplaza por el nombre de la práctica.
- L-9 y material: en espera de indicación.

## Paquetes de trabajo

- **A. Backend (GLM Worker, GLM-5.3-Flash):**
  - Un solo constructor de la grilla de laboratorio sobre `IOccupancyService`. Lo usan `LaboratoryGridService`, `Docente/Disponibilidad` y `Docente/NuevaSolicitud`, así como la reprogramación y la reposición.
  - La regla de limpieza pasa a `gap = 0`.
  - Pruebas unitarias.
- **B. Interfaz (ZCode, GLM-5.3):** vistas y JS. Muestran el nombre de la práctica en lugar de «Tuya», la limpieza como franja de 10 minutos y la misma leyenda en todas las grillas. Empieza cuando A fije el contrato.
- **Claude:** reparte el trabajo, revisa los diffs, ejecuta `dotnet build -m:1` y los tests, y prueba en el navegador con un docente y con el SuperAdministrador sobre el H-05.

## Generador de la Inge para 1-2027 (análisis de GLM, 2026-11-23)

El informe completo está en `outputs/insumos-2026-11-23/ANALISIS_GENERADOR_2027.md` (carpeta ignorada por git).

- **PLANIFICA:** 422 filas para 41 grupos. Ninguna fila trae periodo (`HORA = 0`): el generador programa el día completo. En 79 casos de fecha + laboratorio caen 2 o 3 grupos el mismo día, y el detector de choques actual los bloquearía (`PlanningImportService.cs:804-813`). **Decisión pendiente:** ¿esos grupos comparten el laboratorio al mismo tiempo o van por turnos?
- **Recetas:** 38 hojas de grupo con 11.046 líneas (Producto, Cantidad, Unidad, PRACTICA, ELABORACION). Todas coinciden con PRODUCTO por nombre. La fórmula es la misma del sistema: Cantidad × estudiantes ÷ FACTOR_MATERIA.
- **PRODUCTO:** 1.121 insumos con costo unitario (Bs), marca, presentación, mínimo de compra y estado ACTIVO/INACTIVO. `Article` no tiene costo, marca, presentación ni mínimo.
- **CONFIG y MATERIAS:** traen las 20 semanas, 21 docentes, los grupos y las materias habilitadas. Hoy el importador exige que los grupos ya existan (línea 561).
- **Prácticas E1P, E2P, E3P y 11.1:** `TryInt` convierte «E1P» en 1 y la confunde con la práctica 1, así que hay que tratar la práctica como texto.
- **Datos sucios:**
  - la semana 6 repite el rango de la semana 3;
  - 314 insumos de receta están INACTIVOS;
  - en PRODUCTO hay un código duplicado (50258) y 5 nombres repetidos;
  - en 26 líneas la unidad no coincide con el catálogo (Gr/GR, ml/ML);
  - la hoja AYB4T4 no figura en CONFIG, MATERIAS ni PLANIFICA;
  - las hojas TDC1N7 y NAG1N7 están vacías.
