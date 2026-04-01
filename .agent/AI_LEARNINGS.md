# 🧠 AI Learnings & Master Rules: Proyecto Laboratorios Univalle

Este documento consolida el conocimiento técnico de agentes anteriores (`pasado`, `ahora`, `futuro.md`). Es la **fuente de verdad principal** para la IA en tareas complejas de UI y Reportes.

## 1. Reglas UI y Estéticas ("Premium UI")
- **Botones y Acciones:** Prohibido el uso de botones grises genéricos. Utilizar `.btn-rounded`, `.shadow-sm` y clases de colores semánticos (Verde=Success, Azul=Info, Rojo/Naranja=Danger/Warning).
- **Simetría de Vistas:** Las vistas de detalles (`Details.cshtml`) DEBEN replicar la misma distribución de columnas (ej. 2/3 + 1/3) que usan las vistas de edición (`Edit.cshtml`).
- **Navegación Intuitiva:** Redirigir siempre tras un `OnPost` utilizando `TempData["Success"]` o `TempData["Error"]` para detonar alertas SweetAlert2.

## 2. Reportes de Excel (ClosedXML y EPPlus)
- **Precisión Posicional:** Las Solicitudes de Adquisición requieren escribir en celdas exactas (`Q8` Num. Solicitud, `D6` Facultad, `N19:N37` Precios). NUNCA alterar el rango sin limpiar previamente celdas clave (como "Datos Dummy").
- **Conversión Literal:** Para montos literales, usar la función recursiva existente `ConvertirEnteroATexto` (en `ReportService`). Nunca reinventar la rueda por errores previos.
- **Troubleshooting Impresión:** Aplicar `.Style.WrapText = true` en textos lagos y habilitar `ws.PageSetup.PageOrientation = XLPageOrientation.Landscape` (1 página ancho, automático el alto).

## 3. Arquitectura y Entidades backend (`MVC` en Razor Pages)
- **Verificaciones L6:** Generación de Checklists detallados que evalúan `Good`/`Bad`.
- **Solicitudes L7 (Request):** Se gatillan a partir de equipos con desperfectos en el checklist L6. Mapear `EquipmentUnitId`.
- **Mantenimientos L8:** Representan la ejecución de las Solicitudes.
- **Borrado Lógico:** La universidad mantiene un log histórico de activos por lo que está prohibido aplicar llamadas directas `.Remove()` en BD, a menos que el negocio dicte estrictamente un Hard Delete. Usar "Soft Delete" en `Equipment.Status`.

## 4. Onboarding de IA y Componentes Modernos
- **React Frontend:** Existe una carpeta separada `Design/` que contiene maquetas Single-Page-App hechas en React + Vite. Estas maquetas sirven como inspiración visual para llevarlas a componentes Blade/Razor en C#.
