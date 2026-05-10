# Modulo Wizard

## Flujo

Preventivo:

```text
L-6 Verificacion -> L-7 Solicitud -> L-8 Mantenimiento -> L-3 Salida -> Kardex/L-48 -> L-12 Adquisicion -> Completados
```

Correctivo:

```text
L-7 Solicitud -> L-8 Mantenimiento -> L-3 Salida -> Kardex/L-48 -> L-12 Adquisicion -> Completados
```

## Estado Persistente

- `ManagementPlan.CurrentPhase` rastrea la fase.
- `ManagementPlan.CurrentState` rastrea el estado operativo.
- `ManagementPlan.Status` marca progreso general.
- `Step` es visual en `/Index`, no siempre coincide 1:1 con enum.

## Reglas Criticas

- `ManagementId` es obligatorio en redirects y acciones del wizard.
- Reconstruir verdad desde `ManagementPlanId` y relaciones de BD.
- No confiar en selects disabled.
- Los combos bloqueados solo son correctos en preventivo wizard.
- Correctivo debe poder seleccionar laboratorio/equipo al crear falla.
- Completados es `Step = 7` visual.

## Pasos Visuales

Preventivo:

| Step | Fase |
|---|---|
| 1 | L-6 |
| 2 | L-7 |
| 3 | L-8 |
| 4 | L-3 |
| 5 | Kardex/L-48 |
| 6 | L-12 |
| 7 | Completados |

Correctivo empieza en Step 2 para L-7 y conserva Step 7 como Completados.

## Archivos Clave

- `Pages/Index.cshtml` y `.cshtml.cs`: orquestacion visual.
- `Pages/Shared/_WizardSteps.cshtml` y `_WizardStep.cshtml`: stepper/tarjetas.
- PageModels Create/MassCreate de L-6, L-7, L-8, L-3, Kardex, L-12.

## Antes De Tocar

Leer tambien el contexto del modulo especifico que corresponda.
