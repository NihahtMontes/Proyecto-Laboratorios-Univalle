---
name: reporting
description: Generación Pixel-Perfect de Documentos Institucionales en Excel (ClosedXML/EPPlus) y Actas en PDF (QuestPDF). Mapeo de plantillas rígidas e impresiones numéricas lógicas.
trigger: Modificación o Creación de servicios en `Services/Reporting/` y mapeos de datos a `.xlsx`.
author: Antigravity Orquestador Alpha
version: 1.0.0
license: MIT
scope: Services/Reporting
tools_allowed: [view_file, write_to_file, multi_replace_file_content]
---
# 📊 SKILL: Inteligencia de Datos, Mapeo y Reportes 

La *Joya de la Corona* del sistema: exportación idéntica y precisa a plantillas preexistentes. Un trabajo descuidado arruinará el impreso y la legalidad del documento.

## 1. Reglas Rígidas (Excel - ClosedXML/EPPlus)
El sistema llena plantillas físicas de Adquisición/Mantenimiento ("dummy templates" vacías).

### 1.1 Coordenadas Intocables
Si inyectas solicitudes a una plantilla (`.xlsx`), rígete fielmente a su mapeo espacial exacto:
- **Nro. Solicitud:** Mapeado usualmente en `Q8` (Tamaño 14, Bold).
- **Subtotal/Total Iterativo:** Inicia calculando los rangos verticales en loop (Ej. `P19` a `P37`) finalizando con un global en `P38`.
- **Justificaciones Anchas:** (Ej. `C14`) deben estar acompañadas de `.Style.WrapText = true`.

### 1.2 "La Trampa Dummy"
**Error gravísimo:** Antes de sobreescribir las celdas o rangos `A19` a `P38`, **TIENES QUE LIMPIARLAS**. Esto se debe a que la matriz original trae nombres o números de ejemplo. Destruye los remanentes ejecutando la asignación nula directa (`cell.Value = null;`) antes de insertar los datos nuevos asíncronamente desde EfCore.

### 1.3 Cantidades y Montos Literales (Recursividad Aritmética)
Si la impresión Excel oficial requiere desglosar el gran total "35,100" a "TREINTA Y CINCO MIL CIEN", **bajo NINGUNA circunstancia** intentes hacer arreglos manuales u operadores de indexado directos que terminarán en arrays fuera de rango.
- **Acción Obligatoria:** Llama AL SIEMPRE SEGURO servicio interno de cast a letras `ReportService.ConvertirEnteroATexto(monto)`.

### 1.4 Texto Cortado Horizontal y Formato Físico
Para asegurar que los reportes largos no expulsen texto del impreso A4/Landscape:
- `.Style.WrapText = true;` en todo campo tipo Descripción o Mantenimiento Físico.
- Para Excel invoca funciones adaptadoras de Altura de Fila (si el texto rebalsa), y bloquea la orientación: `ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;` con factor `FitToPages(1, 0)`.

## 2. Creadores de Actas en PDF (QuestPDF)
Cuando diseñes documentos nuevos (desde cero, sin plantilla Excel base):
- Ubícalos rígidamente en la carpeta segregada `Services/Reporting/`.
- La cabecera SIEMPRE anida los Logos Universitarios. Carga imágenes estáticas en Base64 o Paths locales desde `wwwroot/assets/images/`.
- Construye las Tablas HTML / Bloques de Texto asumiendo el margen lateral y alinéalo usando la fluidez del formato en bloques (Grid) de QuestPDF, rematando con un bloque footer inferior de **Múltiples Firmas** centrado.
