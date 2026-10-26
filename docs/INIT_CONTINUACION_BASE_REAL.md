# INIT: continuación del trabajo de la base REAL (pegar al inicio de una conversación nueva)

> **Desde 2026-10-18 el punto de entrada es `docs/GUIA_NUEVA_CONVERSACION.md`** (contexto, herramientas, reglas y prompt) junto con
> `docs/INFORME_ESTADO_FUNCIONAL_2026-10-18.md` (qué funciona, qué no se probó y pendientes priorizados). Este documento queda como historia.

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

## Actualización 2026-10-15: SuperAdmin y prueba de punta a punta
- **SuperAdmin creado** (`Tools/AdminBootstrap` ahora acepta `ADMIN_BOOTSTRAP_PASSWORD`; usar `--expected-server localhost --expected-database DB_Laboratorios_Univalle_REAL`):
  usuario `nihaht`, correo `montesnihaht@gmail.com`, CI 1000001 y teléfono 70000001 provisionales (editar en /Users). La contraseña inicial se entregó en el chat; cambiarla al ingresar.
- **Bases antiguas eliminadas** (9) con respaldo verificado en `C:\Users\Public\BackupsUnivalle\bases_antiguas_20261015\`. Respaldo nuevo de REAL (con usuario):
  `C:\Users\Public\BackupsUnivalle\DB_Laboratorios_Univalle_REAL_V12_E2E_20261015.bak`.
- **Prueba E2E** (`Tools/E2E/`, ver su README): 345 comprobaciones HTTP/BD y 49 pantallas en Chromium real, **0 fallas**; 158 tests unitarios OK; `verify_v12_full.py` 0 diferencias.
- **Hallazgos corregidos**: `/Faculties` daba 500 («Multiple handlers matched»); `Managements/Details` cargaba SweetAlert desde una ruta inexistente; la Sugerencia de L-7 y el Título de
  personas no se mostraban; datos sin pantalla → nuevas pantallas de solo lectura `/EquipmentUnits` (644 unidades), `/Maintenances/Plan` (387 planes L-48) y `/Schedules/Practices` (869 programaciones).
- **Trabajadores**: GLM-5.3-Flash escribió los chequeos y las pantallas. ChatGPT/Codex NO se pudo conectar: el clasificador de permisos bloqueó lanzar `codex exec` desde Claude Code
  (el CLI ya tiene sesión de ChatGPT Plus, pero su config usa MiniMax por defecto). Para habilitarlo el usuario debe añadir una regla de permiso Bash para `codex exec` o registrar él mismo un MCP.
- Pendiente derivado: los trabajos de SQL Server Agent apuntaban a la base eliminada (reapuntar a `_REAL` o desactivar).

## Actualización 2026-10-15 (tarde): clasificación del catálogo y L-48 por gestión
- **Clasificación de los 121 catálogos** (`Equipments`): `BaseGastronomia_v1.xlsx` trae «Por confirmar» en Clasificación (121/121) y Categoría (120/121), así que el panel
  /AssetView mostraba Equipos/Utensilios/Otros = 0. Se clasificaron por análisis de datos con `Tools/CatalogClassify` (regla en `Helpers/CatalogClassificationInference.cs`:
  pista de la columna `Tipoutensilio`, serie física —con serie nunca es utensilio—, palabras del nombre y categoría del artículo de igual nombre).
  Resultado: 116 equipos y 5 utensilios (Pinza kelly, Porta utensilios, Porta tablas, Porta bandejas, Taburete metálico), 121 decisiones `Confirmed` en
  `EquipmentClassificationDecisions` (clave `CLS-INFER-v1-<CatalogCode>`, evidencia «Análisis de datos…», responsable Nihaht Montes). 12 casos de confianza media/baja
  (Garrafa ×2, Bebedero, Termo tanque, Carrito, Filtro ablandador, Laminadora, Lavaplatos ×2, Bacha, Pinza kelly, Taburete) quedan marcados «Pendiente de revisión del cliente»
  en la evidencia y listados en `outputs/clasificacion_catalogo_revision.csv`.
- **Es un paso posterior a la importación**: si se reimporta el Excel hay que volver a ejecutarlo. Simulación (no escribe):
  `dotnet run --project Tools/CatalogClassify -- --expected-server localhost --expected-database DB_Laboratorios_Univalle_REAL`; añadir `--apply --user nihaht --responsible "Nombre"`
  para registrar. Es idempotente y no sobrescribe catálogos ya confirmados por el cliente. Respaldo previo:
  `C:\Users\Public\BackupsUnivalle\DB_Laboratorios_Univalle_REAL_PRE_CLASIFICACION_20261015.bak`.
- **/AssetView**: nueva tarjeta «Por clasificar» (pendientes + inferidos), contador «Existencias» (`ArticleStocks`, enlace a /Catalogs/Stock), «Sin serie» ya es informativo
  (las unidades se identifican por N.º de inventario). Los enlaces a /Catalogs/Stock y /EquipmentUnits quedan inactivos para el rol Supervisor (esas páginas son solo Administrator/SuperAdmin).
- **Gestiones**: las gestiones históricas (`Type = PorConfirmar` sin `ManagementPlans`) muestran su L-48 importado (`MaintenancePlans`, solo lectura) en la ficha y contadores
  L-48/L-8/L-6/L-7 en el listado. Solo 2024-2 (196 planes) y 2025-1 (191) tienen plan en el archivo; las demás indican «Sin plan L-48 cargado». Descripción y responsable no existen en el archivo.
- **QA exhaustivo (2026-10-15)**: 5 baterías previas (301 comprobaciones) + `Tools/E2E/checks_qa_clasificacion.py` (89) + Chromium 49 pantallas + `Tools/E2E/qa_visual.mjs` (escritorio/móvil): 0 fallas, 0 errores
  de consola/JS/HTTP, 0 excepciones en el log del servidor. Correcciones del QA: (1) /AssetView/EquipmentClassifications ahora tiene 13 tarjetas (se añadieron «Equipos informáticos» y «Otros equipos»;
  antes 56 de 567 unidades eran inalcanzables); (2) las balanzas pasaron de Medición a Electrónico (el panel las define como «Equipos electrónicos (balanzas)»): 4 decisiones `CLS-INFER-v2-*`, las v1 quedan
  cerradas (125 decisiones, 121 vigentes; la herramienta re-aplica solo lo que cambió); (3) en el L-48 histórico, Servicio/Estado nulos se muestran «Por confirmar» (los 387 planes tienen NULL en la base);
  (4) `PaginatedList.CreateAsync` limita la página a ≥ 1 (pageIndex negativo daba OFFSET negativo). Respaldos: `..._PRE_CLASIFICACION_20261015.bak` y `..._POST_CLASIFICACION_V1_20261015.bak`.
- Verificación: build 0 errores, 277 tests OK, `verify_v12_full.py` 0 diferencias. No probado: rol Supervisor (enlaces inactivos a Existencias/Unidades) ni una gestión nueva sin planes (no hay datos de ese tipo).

## Actualización 2026-10-16: QA de usuario (Chromium visible + HTTP) sobre una copia de REAL
- Se probó la app como un usuario y con un motor HTTP sobre `DB_Laboratorios_Univalle_QA_E2E` (copia `COPY_ONLY` de REAL; **REAL no se tocó**). Informe completo en `outputs/qa-usuario/INFORME_QA.md` (no versionado) y casos repetibles en `Tools/E2E/` (`qa_driver.mjs`, `qa.mjs`, `http_forms.py`, `http_cases_*.py`, `sql/qa_conflict_fixtures_min.sql`; `e2e_lib.py` acepta `E2E_DB`; los scripts con escritura se niegan a correr si la base no es la copia de QA).
- **Corregido en código (sin commit)**: Edit de País/Ciudad/Facultad/Laboratorio no guardaba (faltaba `Attach` con NoTracking global); la baja de Carrera y de Práctica no persistía; "Agregar a la receta" fallaba en silencio (dos `[BindProperty]` en `Practices/Details`); Edit de Persona daba 500; Edit de Usuario reactivaba cuentas y borraba la fecha de alta; XSS en los toasts (el `title` de SweetAlert2 v7 es HTML: `escapeHtml` en `_Layout` y `Conflictos`); el Supervisor caía en `/Error` al iniciar sesión (la portada lo mandaba al dashboard de administradores); Existencias aceptaba −0.01 (500), 999999.01 y vacío = 0; validaciones de facultad inexistente, límites de piso/descripción, mensajes de Identity y de enlace de modelo en español; error falso tras reponer el último bloque de un día; alerta de faltantes de L-3 antes de registrar la salida.
- **Reportado, a decidir**: con los datos reales las pantallas de planificación (Panel, Grilla, Calendario, Reposiciones, Demanda) abren en «No hay una gestión académica activa» porque `ManagementContextService` solo busca la gestión Preventiva Activa y todas las importadas son «Por confirmar»; los códigos `AAAA-S` de las gestiones importadas impiden crear la preventiva de esos semestres y el índice único permite una sola gestión Activa global; 717 artículos pedidos por planificaciones no tienen existencias (cientos de avisos «Sin stock» en Conflictos); checklist L-7 de refrigeración para cualquier equipo; reglas de L-3/L-8/L-12 y purga de grupos con borrado físico (ver informe).
- La copia `DB_Laboratorios_Univalle_QA_E2E` y el respaldo `C:/Users/Public/BackupsUnivalle/QA_COPY_20261015.bak` son desechables.

## Actualización 2026-10-17: correcciones R-01 y R-02 del QA (sin commit)
- **R-01**: «Rehacer L-8» y «Rehacer Kardex» (`WizardRollbackController`) y el cierre de `Kardex/Create` ya no fallan. **R-02**: `Helpers/WizardPhaseGuard` valida la fase del plan en L-6, L-7, L-8, L-3, Kardex y L-12 (en otra fase no se guarda nada, se avisa y se vuelve al paso real; un plan completado no se reabre); L-8 «Completado» exige 100 % y una tarea completada, y solo un L-8 Completado avanza a L-3; fuera del wizard también se exige técnico. Además, validaciones nuevas en `EquipmentUnits` (crear/editar) y en `Departures` (crear/editar): `En Préstamo` solo lo asigna una salida L-3.
- Los guardados definitivos de L-6/L-7/L-8 **no cambian `PlanStatus`** (solo los borradores y las fases tardías); un trabajador lo añadió en L-8 y se revirtió.
- Verificación: build 0 errores, **353 tests OK**, `verify_v12_full.py` 0 diferencias. Segunda pasada HTTP sobre una copia nueva de REAL (ya eliminada): fallas de los scripts de procesos 155 → 89; el resto está clasificado en `outputs/qa-usuario/INFORME_R01_R02.md` (13 expectativas de los scripts quedaron obsoletas por las reglas nuevas, 3 son el checklist de 500 caracteres, 2 son decisiones de diseño y 71 son hallazgos de otro alcance).
- **A decidir**: R-03 (checklist L-7 truncado, requiere migración); al hacer «Rehacer Kardex» la unidad queda Operativa aunque el Kardex no esté cerrado; reabrir una salida devuelta solo avisa y no reasigna la unidad; `Kardex/Edit` permite dar de baja la unidad o dejarla En Préstamo sin salida; en `EquipmentUnits/Create` el precio fuera de `decimal(18,2)` da 500 y se puede crear una unidad «Eliminada».

## Pendiente (lo decide el usuario)
1. ~~Crear el SuperAdmin~~ **Hecho el 2026-10-15** (ver arriba). Comando de referencia:
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

## QA funcional 2026-10-18 (resumen)
- Decisiones de diseño:
  - pueden estar activas a la vez una gestión preventiva y una correctiva (una por tipo);
  - la correctiva es semestral (`CORR-AAAA-S`) y no aparece en lo académico;
  - un solo proceso correctivo abierto por unidad;
  - «Nueva L-7» lleva al asistente correctivo (paso 2);
  - no se puede dar de baja un país, facultad o materia con hijos activos.
- La migración `20261018152946_CorrectiveCoexistenceAndL7Checklist` (índice único por tipo, un plan abierto por unidad, L-7 `Observations` de 2000) **se aplicó en REAL el 2026-10-18** a pedido del usuario, después del respaldo `C:\Users\Public\BackupsUnivalle\REAL_PRE_MIGRACION_20261018_CorrectiveCoexistence.bak`. `verify_v12_full.py` siguió en 0 diferencias. Commit `256256a`.
- Informe y evidencia: `outputs/qa-usuario/qa-2026-10-18/INFORME_QA_2026-10-18.md`. La copia de QA y su snapshot se eliminaron; para repetir, restaurar `C:\Users\Public\BackupsUnivalle\QA_COPY_20261018.bak` (pasos en el plan v2, §4).
