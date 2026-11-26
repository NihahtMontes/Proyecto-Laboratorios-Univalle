---
name: pruebas-proyecto
description: Cómo están organizadas y cómo escribir las pruebas de Laboratorios Univalle (xUnit con EF InMemory en Tests/Laboratorios.Tests y recorridos E2E en Tools/E2E). Usar al crear, adaptar o correr pruebas, o al cambiar textos visibles que las pruebas comprueban.
---

# Pruebas de Laboratorios Univalle

## Pruebas unitarias e integración (`Tests/Laboratorios.Tests`)

- xUnit 2.9 + `Microsoft.EntityFrameworkCore.InMemory`. Un archivo `<Servicio|Pagina>Tests.cs` por área.
- Base común: `TestDb.cs` (clase `TestDb : IDisposable`). Reprodúcela antes de inventar otra:
  - NoTracking global, como la app; para modificar hay que usar `.AsTracking()`.
  - La siembra usa `SaveChanges()` sincrónico a propósito. La app solo sobrescribe `SaveChangesAsync` (auditoría, históricos, incidentes L5).
  - Constantes de escenario: `ManagementId`, `Week1Monday`, `SuspendedDay`, `LabA` / `LabB` / `LabInactive`, `Teacher1`… Reutilízalas.
- Si cambias un servicio, agrega o ajusta la prueba en su archivo existente. Busca con `grep -l "<NombreServicio>" Tests/Laboratorios.Tests`.
- Si una prueba vieja falla por tu cambio, decide si cambió el comportamiento esperado (actualiza la prueba y dilo en el informe) o si rompiste algo (arregla el código). Nunca la borres ni la marques `Skip` para que pase.
- Comando: `dotnet test Tests/Laboratorios.Tests/Laboratorios.Tests.csproj -m:1 --artifacts-path obj/wk-<nombre>`.

## Recorridos E2E (`Tools/E2E`): los ejecuta SOLO Claude

- `http_cases_*.py`: casos HTTP por módulo (formularios, permisos, CRUD). `checks_*.py`: conteos contra la base.
- `*.mjs`: Playwright con Chromium real (login, menú, Inicio, portal docente).
- Necesitan la app corriendo y escriben en la base EXP, así que el trabajador NO los ejecuta.
- Lo que sí puedes hacer:
  - Si cambias un texto visible, un título, un botón o una ruta, busca ese texto en `Tools/E2E` y en `Tests/` (`grep -rn "<texto viejo>" Tools/E2E Tests`) y actualiza el esperado.
  - Si tu tarea agrega una pantalla o flujo, propone en RISKS qué caso E2E debería añadir Claude, o escríbelo siguiendo un script vecino del mismo módulo sin ejecutarlo.
- Las contraseñas van por variables de entorno (`E2E_PW`, `E2E_USER`); nunca las escribas en archivos.

## Trampas conocidas

- Razor codifica los acentos de las cadenas C# (`é` → `&#xE9;`). Al comprobar HTML, compara el texto decodificado.
- El layout serializa los avisos de `TempData` en JSON.
- Los nombres de pantalla visibles no llevan códigos L-x; esos códigos solo aparecen en Excel y en `/Ayuda`.
