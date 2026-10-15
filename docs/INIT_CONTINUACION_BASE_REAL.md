# INIT: continuación del trabajo de la base REAL (pegar al inicio de una conversación nueva)

**Proyecto**: Laboratorios Univalle · rama `integration/planificacion-2026-10` · commit de referencia `2d272fd`.
Leer primero: `AGENTS.md`, `context.md`, `docs/GUIA_IMPORTACION_HISTORICA.md` (sección «Versión REAL») y `docs/AUDITORIA_BRECHAS_V12.md`.

## Qué se hizo (2026-10-13 a 2026-10-15)
- Se construyó una base **nueva y local** `DB_Laboratorios_Univalle_REAL` (SQL Server `localhost`, Windows auth) **solo desde**
  `C:\Users\monte\Downloads\BaseGastronomia_v1.xlsx` (V12, SHA-256 `edca5f8b4a6601091a4e85939f9cf1aaa586a4e27c2e4df0dc1278afbb47a3f2`).
  No parte de DEMO_QA ni de la nube.
- Regla del equipo (Fernando): **el Excel se sube tal cual**; los vacíos no se ignoran. Donde el esquema exige fecha y el Excel no la trae se usa la
  **fecha de la importación (2026-10-14)** con incidencia. Los grupos (`A`, `A2`, …) se cargan como vienen: **no** aplicar la conversión de
  `AddStudentGroups` (A3/A5).
- Migraciones nuevas: `AddV12FullFidelity` (año de gestión 1900–2100, `QualityStatus` en `Maintenances`/`MaintenancePlans`, tablas
  `UndatedL3Requests`/`UndatedL3RequestLines` hoy vacías) y `RelaxPlanningChecksForExcelFidelity` (sin `CK_Plannings_Laboratory`; cantidad L-3 `>= 0`).
  Total aplicadas en REAL: 31.
- Importador (en `Tools/ExcelImport/`): `v12_common.py`, `gen_v12_full_a.py` (maestros, actores, catálogo, unidades), `gen_v12_full_b1.py`
  (L-6, L-7, L-8, L-48), `gen_v12_full_b2.py` (académico, artículos, recetas, existencias, programación 2027-1), `gen_v12_full_b3.py` (circuito L-3).
  Cada uno escribe `%TEMP%\v12full\parte_*.sql`; se ejecuta en orden A → B1 → B2 → B3 con
  `sqlcmd -S localhost -E -d DB_Laboratorios_Univalle_REAL -b -f 65001 -i <sql>`. Idempotentes (lotes de 100 con `GO`).
- Verificador: `python Tools/ExcelImport/verify_v12_full.py` → **0 diferencias** (copia cruda de las 27 hojas, claves, ~43.000 celdas tipadas, `DBCC CHECKCONSTRAINTS`).
- Cada fila del Excel está en `ImportSourceRows.OriginalDataJson` (23.542 filas); ~7.450 incidencias en `DataQualityIssues` (lote `XLSX-V12-FULL-EDCA5F8B4A66`).

## Estado actual
- Conteos clave: 1.037 `Plannings`, 11.451 `PlanningRequirements`, 1.209 `Maintenances` (Bs 496.545), 1.599 `Verifications`, 644 unidades, 1.374 artículos.
- **User-secret `ConnectionStrings:DefaultConnection` ya apunta a `localhost/DB_Laboratorios_Univalle_REAL`** (Windows auth). `AutoMigrate=false`, `RunSeed=false`.
- Respaldos (verificados con `RESTORE VERIFYONLY`):
  - `C:\Users\Public\BackupsUnivalle\DB_Laboratorios_Univalle_REAL_V12_20261014.bak`
  - `C:\Users\Public\BackupsUnivalle\bases_antiguas_20261015\*.bak` (9 bases antiguas) y los `NUBE_*.bak` anteriores.
- La nube (MonsterASP) no se tocó.

## Pendiente (lo decide el usuario)
1. **Crear el SuperAdmin** (pide contraseña oculta; lo corre el usuario):
   `! dotnet run --project Tools/AdminBootstrap -- --username admin --email <correo> --first-name <nombre> --last-name <apellido> --identity-card <ci> --phone <telefono>`
2. ~~Eliminar las bases locales antiguas~~ **Hecho el 2026-10-15** con autorización del usuario: las 9 bases antiguas (`DB_Laboratorios_Univalle`,
   `_MIGRACION`, `_QA`, `_CLOUD_20260827_1418_QA`, `_PRE/POST_NORMALIZACION_20260902_QA`, `_SCENARIOS_QA`, `_STRESS_QA`, `_DEMO_QA`) se eliminaron tras
   respaldarlas y verificar los `.bak`. En el servidor solo queda `DB_Laboratorios_Univalle_REAL`. **Pendiente derivado**: los 3 trabajos de SQL Server Agent
   («Backup FULL diario», «Backup LOG 15 minutos», «Verificación restauración semanal») apuntaban a `DB_Laboratorios_Univalle` y fallarán: reapuntarlos a `_REAL` o desactivarlos.
3. Probar la app en el navegador con la base nueva (lo hace el usuario). La app muestra «Sin ambiente» en las 4 planificaciones sin laboratorio.
4. Cambios nuevos que traiga el usuario (correcciones del Excel o de datos): repetir A → B1 → B2 → B3 y correr el verificador. Si cambia el Excel,
   actualizar `EXPECTED_SHA` y `EXPECTED_ROWS` en `v12_common.py`.
5. Subida a la nube (paso 5 de la hoja de ruta en memoria): aplicar migraciones en MonsterASP, restaurar el `.bak` de REAL, publicar. Requiere pedido específico.

## Desviaciones conocidas (el original está en el JSON de `ImportSourceRows`)
- `ART-L33-0256` se guarda como «nombre (código)» (índice único de nombres de artículo).
- Los 120 equipos del catálogo con categoría «Por confirmar» quedaron como `Equipment` (columna obligatoria) con revisión pendiente.
- `Direccion` de actores internos solo en el JSON; `RequestDate` de todas las solicitudes L-3 = fecha de la importación.

## Trabajo sin commit de otros chats (no mezclar)
Conflictos de planificación (`Pages/LaboratoryOperations/Conflictos*`, `Services/Conflict*`, `wwwroot/css/conflictos.css`, tests), grilla, disponibilidad,
layout y `Program.cs`. Siguen modificados en el árbol de trabajo; no se incluyeron en `2d272fd`.

## Reglas de trabajo que rigen (CLAUDE.md global + AGENTS.md)
Responder en español. Preguntar con `AskUserQuestion` (Ejecutor, Paralelo, Revisión, Verificar) antes de tareas de código. `glm_worker` solo
GLM-5.3-Flash máx., sin servidores, sin commit/migraciones a BD; Claude revisa con `git diff`. Un solo proceso pesado a la vez (`dotnet build -m:1`).
Sin hard-delete de historia; no tocar migraciones existentes; las pruebas en navegador las hace el usuario.

## Prompt sugerido para la conversación nueva
> Lee `docs/INIT_CONTINUACION_BASE_REAL.md`. La base `DB_Laboratorios_Univalle_REAL` ya está cargada desde el Excel V12 y verificada.
> Tengo estas correcciones: <lista>. Aplícalas sobre la base REAL (tal cual el Excel; los vacíos se suben con la fecha de hoy) y vuelve a correr el verificador.
