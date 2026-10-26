# Guía para empezar una conversación nueva (desde cero)

Última actualización: 2026-10-18, después de la QA funcional completa. Sirve para que un agente nuevo (Claude, Codex u OpenCode) tenga todo el contexto, las herramientas y las reglas, y pueda **agregar mejoras o hacer correcciones** sin repetir lo ya hecho.

---

## A. Prompt para pegar al inicio de la conversación nueva

> Proyecto Laboratorios Univalle (ASP.NET Core 9 Razor Pages + EF Core + SQL Server local). Rama `integration/planificacion-2026-10`.
> Antes de tocar nada, lee en este orden:
> 1. `docs/GUIA_NUEVA_CONVERSACION.md`
> 2. `docs/INFORME_ESTADO_FUNCIONAL_2026-10-18.md`: qué funciona, qué no se probó y la lista de pendientes priorizada (§4).
> 3. `AGENTS.md` y `context.md`, más el `AGENTS.md` de la carpeta que vayas a tocar (`Pages/`, `Models/`, `Services/`).
>
> La base de producción local es `DB_Laboratorios_Univalle_REAL` y debe seguir idéntica a `BaseGastronomia_v1.xlsx`: no la modifiques sin mi orden explícita y sin un respaldo previo. Prueba siempre en una copia desechable.
> Quiero trabajar en: **<describir la mejora o corrección>**.
> Antes de empezar, resume en una línea qué vas a hacer y qué archivos tocarás, y hazme las preguntas de configuración (Ejecutor, Paralelo, Revisión, Verificar).

---

## B. Qué es el sistema (en 2 minutos)

**Gestión de laboratorios de Gastronomía de Univalle.** Tiene dos grandes bloques.

1. **Mantenimiento de equipos**, organizado por **gestiones semestrales**:
   - **Preventivo** (asistente, en orden): L-6 Verificación → L-7 Solicitud técnica (solo si hay falla) → L-8 Mantenimiento → L-3 Salida → Kardex → L-12 Adquisición/desembolso → Completados (paso 7).
   - **Correctivo:** empieza en L-7 (paso 2). Su gestión es `CORR-AAAA-S`.
   - Pueden estar activas a la vez **una preventiva y una correctiva**. Solo puede haber **un proceso correctivo abierto por unidad**.
   - «Nueva solicitud» en L-7 lleva al asistente: no se crean L-7 sueltas.
   - Cronograma L-48 y calendario por gestión. Informes Excel con las plantillas institucionales (`wwwroot/templates`).
2. **Planificación académica** (`Pages/LaboratoryOperations`): prácticas por materia y grupo, recetas de artículos, disponibilidad y conflictos de material, grilla semanal, reposiciones y «Cierre de práctica» (rol restringido).

Además: equipos y unidades (644), artículos (1.374) y existencias por laboratorio, catálogos académicos, maestros (país, ciudad, facultad, carrera, laboratorio, personas) y usuarios.

**Roles:** SuperAdmin, Administrador, Supervisor (asistente, L-7, informes y planificación) y Cierre de práctica (solo Cierre y Notificaciones).

**Datos:** la base REAL se construyó **tal cual** desde `C:\Users\monte\Downloads\BaseGastronomia_v1.xlsx` (llamarlo así, no «Excel V12»). Los vacíos se subieron con la fecha de la importación, y cada fila original está en `ImportSourceRows`.

---

## C. Estado al 2026-10-18

- QA completa:
  - 3.950 comprobaciones HTTP, 21 invariantes SQL, 363 tests y 27 pantallas en el navegador;
  - **0 fallas nuevas**;
  - todo aprobado salvo la **planificación académica**, que no se probó con la base actual.
- Commits locales `256256a` (QA y correcciones) y `d21137c` (docs). **Sin push.**
- Migración `20261018152946_CorrectiveCoexistenceAndL7Checklist` **aplicada en la base REAL**, con el respaldo previo `C:\Users\Public\BackupsUnivalle\REAL_PRE_MIGRACION_20261018_CorrectiveCoexistence.bak`.
- La base REAL sigue con 0 diferencias frente al Excel (`Tools/ExcelImport/verify_v12_full.py`).
- **2026-10-19:** a pedido del usuario se hizo commit de todo lo anterior y se subió a la rama **`qa`** (origin/qa) para la ronda de QA con varias personas. La gestión `2026-2` (Id 4) pasó a **Preventiva** y se creó **`CORR-2026-2`** (Id 28) activa en REAL (respaldo previo `REAL_PRE_GESTIONES_QA_20261019.bak`, verify_v12_full con 0 diferencias). Paquete para la nube y pasos: `docs/deployment/ACTUALIZAR_NUBE_QA_2026-10-19.md`.
- **Lo primero que falta** (detalle en el informe, §4):
  1. Restaurar el `.bak` de QA en la nube y crear las cuentas de los probadores (lo hace el usuario; ver `docs/deployment/ACTUALIZAR_NUBE_QA_2026-10-19.md`).
  2. Los 3 trabajos de SQL Server Agent respaldan una base que ya no existe. **No hay respaldos automáticos de la base REAL.**
  3. Probar la planificación académica (adaptar `Tools/E2E/http_cases_planificacion.py`).

---

## D. Reglas que no se negocian

**Del usuario y del proyecto** (`AGENTS.md`, `CLAUDE.md` global):
- Responder **en español**. Antes de implementar, hacer las 4 preguntas de configuración con AskUserQuestion: Ejecutor, Paralelo, Revisión, Verificar.
- **La base REAL no se toca** sin una orden explícita. Antes de cualquier escritura o migración, hacer un respaldo `COPY_ONLY` y comprobarlo con `RESTORE VERIFYONLY`. Después, correr `verify_v12_full.py`.
- **Migraciones existentes: intocables.** Las nuevas se crean con `dotnet ef migrations add`, se revisan su `Up` y su `Down`, se aplican primero en la copia y solo después en la base REAL, a pedido.
- **Commit solo si el usuario lo pide**, push solo a pedido, nunca `--force`. Antes del commit, revisar que no haya secretos ni archivos de `outputs/`.
- No mezclar trabajo de otros chats: revisar `git status` antes de un commit.

**Del código:**
- Nada de borrado físico: usar estados, `IsVoided` o desvinculación.
- NoTracking global, así que hay que usar `.AsTracking()` para modificar.
- SweetAlert2 es v7: usar `result.value`.
- Mensajes con `TempData.Success/Error/Warning/Info`.
- Crear muestra el éxito después; editar y dar de baja piden confirmación antes.
- Preservar `ManagementId` en el asistente.
- Los guardados definitivos de L-6/L-7/L-8 no cambian `PlanStatus`.
- Todos los textos visibles, en español. Interfaz NiceAdmin/Bootstrap 4.

**Del equipo de trabajo:**
- **GLM** (`glm_worker`, GLM-5.3: tier `strong` para tareas pesadas, `normal` para el resto) implementa; Claude orquesta, revisa (`git diff`, build, tests) y verifica.
- GLM **no**:
  - levanta servidores ni mata procesos (si el exe está bloqueado, compila con `-p:OutDir=` temporal);
  - hace commits ni git destructivo;
  - aplica migraciones ni escribe en la base de datos.
- GLM se corta a los 1500 s y, en tareas de pura exploración, a veces no escribe nada: darle el archivo y la línea exactos. Si se acaba su cupo, esperar o pedirle al usuario que autorice a Claude.
- **Un solo proceso pesado a la vez** (`dotnet build -m:1`, un script a la vez). El equipo tiene 15,7 GB y casi siempre menos de 4 GB libres. Si el sistema mata la app o Chromium, **preguntar antes de reiniciarlos**.

---

## E. Herramientas y comandos

Raíz: `C:\Users\monte\OneDrive\Desktop\Presentar Sprint\Proyecto-Laboratorios-Univalle`. Shell: Git Bash.

### Compilar y testear
```bash
dotnet build -m:1 "Proyecto Laboratorios Univalle.csproj"
dotnet test Tests/Laboratorios.Tests -m:1          # se esperan 363 o más, todos OK
# con la app corriendo (exe bloqueado): compilar a otra carpeta
dotnet build -m:1 "Proyecto Laboratorios Univalle.csproj" "-p:OutDir=C:\Temp\b\bin\" "-p:IntermediateOutputPath=C:\Temp\b\obj\"
```

### Levantar y detener la app
- **Contra la base REAL:** el user-secret `ConnectionStrings:DefaultConnection` ya apunta a ella.
  ```bash
  ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build --project "Proyecto Laboratorios Univalle.csproj" --urls http://localhost:5000
  ```
- **Contra la copia de QA:** pasar la cadena de conexión por variable de entorno, sin tocar el user-secret.
  ```bash
  ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__DefaultConnection="Server=localhost;Database=DB_Laboratorios_Univalle_QA_E2E;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True" \
    dotnet run --no-build --project "Proyecto Laboratorios Univalle.csproj" --urls http://localhost:5000
  ```
- Correr la app en segundo plano y esperar a que `curl http://localhost:5000/Login` devuelva 200.
- **Detener:**
  ```bash
  powershell -NoProfile -ExecutionPolicy Bypass -File Tools/E2E/stop_app.ps1
  ```
  Imprime 0 cuando el puerto queda libre.

### Crear la copia de QA (para probar sin tocar la base REAL)
1. Respaldar la base REAL y restaurarla como copia:
   ```sql
   BACKUP DATABASE DB_Laboratorios_Univalle_REAL TO DISK=N'C:\Users\Public\BackupsUnivalle\QA_COPY_<fecha>.bak' WITH COPY_ONLY, CHECKSUM, INIT;
   RESTORE DATABASE DB_Laboratorios_Univalle_QA_E2E FROM DISK=N'...QA_COPY_<fecha>.bak'
     WITH MOVE 'DB_Laboratorios_Univalle_REAL' TO N'C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\QA_E2E.mdf',
          MOVE 'DB_Laboratorios_Univalle_REAL_log' TO N'C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\QA_E2E_log.ldf';
   ```
2. Preparar la copia con `sqlcmd -S localhost -E -I -d DB_Laboratorios_Univalle_QA_E2E`. **Hace falta `-I`** por los índices filtrados.
   - `UPDATE Users SET Role=1 WHERE UserName='nihaht'; DELETE FROM UserRoles;` (se degrada al SuperAdmin importado).
   - `UPDATE Managements SET Status=1 WHERE Id=4;` (la gestión importada `2026-2` pasa a Inactiva).
   - `DBCC CHECKIDENT('Managements', RESEED, 29);` (los scripts esperan que la preventiva sea el Id 30).
3. SuperAdmin de QA. La herramienta usa `DefaultConnection`, así que hay que apuntarla a la copia por variable de entorno:
   ```bash
   ConnectionStrings__DefaultConnection="Server=localhost;Database=DB_Laboratorios_Univalle_QA_E2E;Trusted_Connection=True;TrustServerCertificate=True" \
   ADMIN_BOOTSTRAP_PASSWORD="$(cat "$TEMP/qa_pw.txt")" dotnet run --project Tools/AdminBootstrap -- \
     --expected-server localhost --expected-database DB_Laboratorios_Univalle_QA_E2E \
     --username qa_super --email qa_super@qa.local --first-name QA --last-name Super --identity-card 9900001 --phone 70099001
   ```
   La clave aleatoria se genera antes en `%TEMP%\qa_pw.txt` (12 caracteres o más, con mayúscula, minúscula, dígito y símbolo) y se borra al terminar.
4. Levantar la app contra la copia. Desde `/Users/Create`, crear `qa_adm` (Administrador), `qa_sup` (Supervisor), `qa_cierre` (Cierre de práctica) y `qa_u01…qa_u25`. Crear la preventiva **2027-2** activa: debe quedar con Id 30.
5. Opcional: crear un snapshot para volver a ese punto en segundos:
   ```sql
   CREATE DATABASE QA_E2E_BASE ON (NAME='DB_Laboratorios_Univalle_REAL', FILENAME='...\QA_E2E_BASE.ss')
     AS SNAPSHOT OF DB_Laboratorios_Univalle_QA_E2E;
   ```
   Para restaurarlo, con la app detenida:
   ```sql
   ALTER DATABASE DB_Laboratorios_Univalle_QA_E2E SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   RESTORE DATABASE DB_Laboratorios_Univalle_QA_E2E FROM DATABASE_SNAPSHOT='QA_E2E_BASE';
   ALTER DATABASE DB_Laboratorios_Univalle_QA_E2E SET MULTI_USER;
   ```

**Trampas conocidas:**
- Un intento fallido de crear una gestión igual consume un Id.
- Los códigos `AAAA-S` preventivos están ocupados por la importación.
- Los años lejanos se rechazan.
- La copia restaurada no trae ninguna correctiva: los scripts crean `CORR-2027-2`.

**Al terminar:**
1. Detener la app y cerrar Chromium.
2. Borrar `%TEMP%\qa_pw.txt`.
3. Eliminar **primero el snapshot** y después la copia (`SINGLE_USER` → `DROP`).
4. Comprobar que solo queda `DB_Laboratorios_Univalle_REAL`.

### Pruebas E2E (`Tools/E2E`)
```bash
# requisitos: pip install pyodbc · npm install · npx playwright install chromium (dentro de Tools/E2E)
bash Tools/E2E/run_scripts.sh "<carpeta_salida_absoluta>" http_cases_correctivo.py http_cases_procesos_flujo.py ...
```
- Cada script deja su salida en `<carpeta>/<script>.txt` con `[OK]` / `[FALLA]` y una línea `== N comprobaciones, M fallas`.
- Los scripts que escriben **se niegan a correr** si `E2E_DB` no es la copia de QA.
- **Inventario:**
  - `checks_*.py`: lectura; la copia es fiel al Excel.
  - `sql_invariantes.py`: 21 reglas.
  - `http_cases_procesos_*` (10 scripts: flujo, rollback y cada módulo).
  - `http_cases_correctivo*`, `http_cases_l48_calendario`, `http_cases_costos`, `http_cases_informes`, `http_cases_paginacion`.
  - `http_cases_usuarios_auth`, `http_cases_roles`, `http_cases_equipos_stock`.
  - `http_cases_catalogos_*`, `http_cases_maestros_*` y `http_cases_gestiones`.
  - `http_cases_planificacion`: **pendiente de adaptar**.
- Helpers: `http_forms.Client`, `e2e_lib`, `http_cases_procesos_common` (`lab_propio`, `nueva_unidad`, `planificar`, `llevar_a`, `post_form` y otros) y `http_cases_correctivo_common`.
- Una corrida completa tarda unos 25 minutos. Conviene restaurar el snapshot antes, porque los `checks_*` necesitan la copia limpia.

### Navegador visible (Chromium)
```bash
E2E_PW="$(cat "$TEMP/qa_pw.txt")" E2E_USER=qa_super node Tools/E2E/qa_driver.mjs     # en segundo plano; deja Chromium abierto
node Tools/E2E/qa.mjs 'login' 'goto /Requests' 'shot nombre' 'swal' 'errors'     # acciones: ver la cabecera de qa.mjs
```
Las capturas se guardan en `outputs/qa-usuario/img/`. `login qa_sup` cambia de usuario (antes usar `clearcookies`).

### Verificar la base REAL contra el Excel
```bash
cd Tools/ExcelImport && PYTHONIOENCODING=utf-8 python verify_v12_full.py      # debe decir «RESULTADO: 0 diferencias»
```

---

## F. Dónde está cada cosa

| Tema | Archivos |
|---|---|
| Asistente (preventivo y correctivo) | `Pages/Index.cshtml(.cs)`, `Pages/Shared/_WizardStep*.cshtml`, `Helpers/WizardPhaseGuard.cs`, `Controllers/WizardRollbackController.cs` |
| Gestiones | `Pages/Managements/*`, `Services/ManagementActivationService.cs`, `Services/ManagementContextService.cs` |
| L-6 / L-7 / L-8 / L-3 / Kardex / L-12 | `Pages/Verifications`, `Pages/Requests`, `Pages/Maintenances`, `Pages/Departures`, `Pages/Kardex`, `Pages/Acquisitions` |
| Informes Excel | `Controllers/ReportsController.cs`, `Services/Reports/ReportService.*.cs`, `wwwroot/templates/*.xlsx` |
| Permisos y menú | `Program.cs`, `Helpers/AuthorizationHelper.cs`, `Helpers/PracticeClosureAccess.cs`, `Pages/Shared/_Sidebar.cshtml` |
| Mensajes de validación en español | `Helpers/SpanishValidationMetadataProvider.cs` (registrado en `Program.cs`) |
| Paginación | `Helpers/PaginatedList.cs` (20 por página; acota a la última) |
| Modelo y base de datos | `Models/`, `Data/ApplicationDbContext.cs`, `Data/SqlServerMigrations/` |
| Planificación académica | `Pages/LaboratoryOperations/*`, `Services/ArticleAvailabilityService.cs`, `Services/Conflict*` |
| Importación y verificación del Excel | `Tools/ExcelImport/` (`gen_v12_full_*.py`, `verify_v12_full.py`), `docs/GUIA_IMPORTACION_HISTORICA.md` |
| Historia de trabajo | `docs/INIT_CONTINUACION_BASE_REAL.md`; evidencia local (no está en git) en `outputs/qa-usuario/` |

---

## G. Pendientes priorizados (copia corta del informe, §4)

1. 🔴 **Preparar la base REAL para operar:**
   - ✅ 2026-10-19: `2026-2` convertida en Preventiva y `CORR-2026-2` creada (ambas activas);
   - corregir la cédula y el teléfono provisionales de `nihaht`.
2. 🔴 **Respaldos automáticos:** reapuntar los 3 trabajos de SQL Server Agent a `DB_Laboratorios_Univalle_REAL` (hoy apuntan a una base eliminada) o desactivarlos.
3. ⚪ **Probar la planificación académica** (adaptar `http_cases_planificacion.py`) y el **Cierre de práctica**.
4. ⚪ Probar la importación de existencias desde Excel, la concurrencia en costos y la matriz completa de permisos.
5. 🟡 **Decisiones de diseño:**
   - L-6, L-8 y L-3 sueltos: ¿solo dentro del asistente, como la L-7?
   - Páginas `/Schedules` sin enlace: ¿se enlazan o se retiran?
   - ¿Se puede deshacer la activación de una gestión?
6. Hoja de ruta:
   - reforma de grupos F4–F5;
   - rediseño de conflictos, fases 2–4;
   - subida a la nube (MonsterASP);
   - push y PR.
7. 🟢 Menores:
   - SweetAlert en las páginas «Dar de baja» de los maestros;
   - avisos de EF por filtros globales;
   - retirar el PDF sin uso y las pantallas técnicas (`Prototype`, `Migration`, `Admin`).

**Para cada tarea nueva:**
1. Leer solo el módulo afectado.
2. Proponer un plan si toca más de 5 archivos.
3. Implementar (GLM o Claude).
4. Build y tests.
5. Probar en la copia de QA con los scripts del módulo y, si cambia la interfaz, en el navegador.
6. Actualizar el informe o este documento.
7. Hacer commit solo si el usuario lo pide.
