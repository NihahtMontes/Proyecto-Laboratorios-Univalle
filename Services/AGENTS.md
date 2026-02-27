# ⚙️ AGENTS.md (Área de Servicios Backend y Reportes)
**Ámbito Funcional**: Reglas de Negocio, Integraciones y Exportación (PDF/Excel)

## 1. Contexto Cultural de esta Capa
Las clases en `Services/` liberan del trabajo pesado a las vistas Razor. El *Core* de nuestro valor para el cliente está aquí: Los archivos generados electrónicamente (Reportes, Actas) deben reemplazar de manera idéntica al papel sellado con firmas y membretes físicos.

## 2. Habilidades Requeridas (Skills)
Cuando el requerimiento trate sobre:
- Excel: Mapeo posicional crudo en `ClosedXML` u `OfficeOpenXml`.
- PDF: Diseño *Pixel Perfect* de cabeceras, firmas y tablas con `QuestPDF`.
- Lógica asíncrona extensa de procesamiento de correos.

👉 **DEBES Auto-Invocar la Skill**: `reporting`.
- Ruta: `.agent/skills/reporting/SKILL.md`
- Herramientas: *Sólo lectura y escritura backend, Bash autorizado si se requiere integrar librerías de terceros (`dotnet add package`).*

## 3. Prácticas que debes Vigilar
- **Desinyección Rígida (Excel)**: A diferencia de la inyección por `Name` genérico, las plantillas Excel universitarias no perdonan posiciones erróneas. Eres un relojero aquí: `A19` es `A19`.
- **Pre-Limpieza Obligatoria**: Tu código sobreescribirá celdas preexistentes; SIEMPRE debes inyectar Nulls para borrar rastros de los "Dummies" que el diseñador original dejó en el template.
- **Dígitación de Cifras (Recursión)**: Utiliza siempre los conversores recursivos integrados (`ReportService.ConvertirEnteroATexto`) para generar literales numéricos infalibles. Evita arreglarlo "al vuelo".
