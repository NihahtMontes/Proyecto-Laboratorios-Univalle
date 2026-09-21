# Contrato multi-sede: control plane central y bases operacionales por sede

**Estado**: `Aprobado para implementar desde la Fase 2`; las operaciones de datos y migraciones continúan sujetas a sus puertas y autorizaciones específicas.
**Alcance**: define el modelo de datos y operación multi-sede del sistema migrado (React + NestJS + PostgreSQL) y sus reglas de transición. Complementa `docs/PLAN_MIGRACION_REACT_NESTJS.md`; este documento no ejecuta ni autoriza operaciones de datos, migraciones ni SQL.
**Registro**: creado por el agente `documentation` (GLM 5.3 Flash) el 18-09-2026, por encargo del orquestador (trabajo MIG-F1-DOC-003). No se ejecutaron builds, conexiones ni operaciones sobre bases de datos.

---

## 1. Decisiones vinculantes (aprobadas 18-09-2026)

1. **Una base PostgreSQL operacional por sede**. Cada sede tiene su propia base operacional (tenant DB). No existe una única DB operacional central; la idea previa de DB central queda **sustituida**.
2. **Mismo sistema, sede explícita**. El sistema es uno solo; la sede se selecciona de forma explícita en la sesión del usuario. Ninguna operación de negocio ocurre sin sede activa definida.
3. **Unicidad dentro de la sede**. Los códigos de laboratorio y de gestión son únicos **dentro de cada sede**; no se garantiza unicidad global de estos códigos.
4. **`EquipmentUnit` no tiene `SiteId` propio**. La sede de una unidad de equipo se deriva a través de su `Laboratory`. En el destino operacional `LaboratoryId` es obligatorio; los registros legacy sin laboratorio quedan fuera de operaciones normales hasta resolverse o clasificarse explícitamente. Ninguna consulta de unidades debe intentar filtrar por un `SiteId` inexistente en la unidad.
5. **Usuarios y roles**. Los usuarios son multi-sede. Los roles `Administrador` y `Supervisor` se asignan **por sede**. `SuperAdmin` es global. La autorización de negocio se evalúa contra la membresía y el rol en la sede activa.
6. **Gestiones**. Dentro de cada tenant existe **una gestión activa por `Type`**; preventiva y correctiva coexisten. `ManagementPlan` conserva su unicidad actual `ManagementId + EquipmentUnitId` (la que exige el wizard vigente, referida en DATA-002).
7. **`Management.FacultyId` opcional durante la transición**. Queda nulable temporalmente y **no define autorización ni sede**; la sede del management es la del tenant y la autorización la dan las membresías del usuario.

## 2. Topología de datos: control plane vs tenant DB

| Componente | Base | Contenido |
|---|---|---|
| **Control plane** | PostgreSQL central (única) | `SiteIdentity` (catálogo de sedes: identidad, estado, salud), identidad de usuario, membresías y roles por sede (Administrador/Supervisor por sede, SuperAdmin global), `Faculty` y `Career` (globales), `SiteCareer` (puente carreras por sede), estado de migración y salud por sede, referencias a secretos (identificadores, **nunca valores**). |
| **Tenant DB** | Una base PostgreSQL **por sede** | Laboratorios, inventario (equipos, unidades, ambientes), gestiones (`Management`), `ManagementPlan`/wizard, auditoría local de la sede. |

Reglas derivadas:

- **No cross-DB en consultas de negocio**: el control plane no se junta con datos de tenant en JOINs operativos; la aplicación resuelve primero sede y autorización en el control plane y después opera en la tenant DB.
- **La entidad física de cada tabla de negocio se define en el contrato de dominio por entidad** (fase 1 del plan). Este documento fija en qué base vive cada dominio, no el esquema.
- El control plane es la única fuente de verdad sobre qué sedes existen, están habilitadas, en migración o degradadas.

### Contrato conceptual de `SiteCareer`

`SiteCareer` pertenece al control plane y contiene: identificador técnico, `SiteId` obligatorio, `CareerId` obligatorio, `IsActive`, vigencia (`ValidFrom`/`ValidTo`) y auditoría. Solo puede existir una relación activa por pareja `(SiteId, CareerId)`. Desactivar una oferta conserva su historia; no se elimina físicamente. Una carrera sin sede activa permanece en el catálogo global, pero no puede seleccionarse en operaciones de una tenant DB.

## 3. Routing server-side y acceso a bases

1. **Routing server-side**: la selección de la tenant DB ocurre exclusivamente en el backend (NestJS). El backend resuelve `sede activa → conexión` consultando el control plane y su referencia al secreto correspondiente. El frontend nunca conoce endpoints ni identificadores de conexión por sede.
2. **Prohibición de connection strings aportados por cliente**: ninguna cadena de conexión provista por el cliente/navegador o por payloads del usuario puede usarse para conectar. Solo se aceptan conexiones cuya referencia provenga del control plane y cuyo valor resida en el gestor de secretos.
3. **Pools bajo demanda y con límites**: cada tenant DB abre su pool solo cuando la sede está activa en una sesión/servicio, con límite máximo de conexiones por pool y por sede (valor exacto: `Pendiente`, sección 7). Pools inactivos se cierran.
4. **Transacciones solo dentro de una DB**: está prohibida cualquier transacción que cruce control plane y tenant DB, o dos tenant DBs. Las operaciones que hoy cruzarían bases se modelan como pasos compensables con auditoría local, no como transacciones distribuidas.
5. **Fallos aislados**: la caída, lentitud o corrupción de una tenant DB no degrada al resto de las sedes ni al control plane. El estado de salud de cada sede vive en el control plane y debe reflejar estos fallos.

## 4. Migraciones y alta de sedes

1. **Migraciones serializadas por sede**: las migraciones de una tenant DB se ejecutan una sede a la vez, sin ejecución paralela entre sedes (coherente con la política de bajo consumo del proyecto).
2. **Credenciales separadas runtime/migrator**: cada tenant DB tiene dos identidades de credencial: `runtime` (lectura/escritura de negocio, sin DDL) y `migrator` (DDL, usada solo en ventana de migración). Ambas se referencian por identificador desde el control plane; sus valores viven en el gestor de secretos.
3. **Alta de una sede (proceso obligatorio, en orden)**: (a) registrar `SiteIdentity` en el control plane; (b) provisionar la tenant DB; (c) ejecutar migraciones serializadas con credencial `migrator`; (d) registrar estado de migración/salud en el control plane; (e) habilitar la sede para routing y crear membresías iniciales. Una sede sin estos pasos completos no recibe tráfico de negocio.

## 5. Reportes cross-site y tratamiento legacy

1. **Reportes cross-site**: solo se permiten reportes entre sedes como **parciales explícitos** seleccionados por el usuario (por sede), agregados por la capa de aplicación. No existen JOINs cross-DB ni una "vista global" transaccional.
2. **Escritor único por capacidad y sede**. Durante la transición, cada capacidad/tabla en cada sede tiene exactamente un escritor activo, etiquetado:
   - `LEGACY_SQLSERVER`: el sistema .NET actual escribe esa capacidad.
   - `NEST_SQLSERVER`: NestJS escribe esa capacidad sobre el SQL Server compartido (transición).
   - `NEST_POSTGRES`: NestJS escribe esa capacidad ya en la tenant DB PostgreSQL de la sede (destino).
   El cambio de etiqueta por capacidad y sede se registra con el protocolo de propiedad temporal de `docs/AGENT_WORKFLOW.md`. Nunca coexisten dos escritores sobre la misma capacidad en la misma sede.
3. **Tratamiento legacy**: el SQL Server y el pipeline histórico permanecen como fuente legacy hasta el cutover de cada sede; no se reescriben para el modelo multi-sede. Las herramientas históricas (`Tools/HistoricalDataImport/`, `Tools/HistoricalDataApply/`) no se portan automáticamente; su destino se decide en la fase 10 del plan.

## 6. `GastroExample` (sandbox de desarrollo)

El usuario confirmó el 18-09-2026 que `GastroExample` puede tratarse como **sandbox local descartable de desarrollo**. No contiene datos que deban considerarse autoridad, no representa una sede productiva y no participa en la reconciliación histórica.

Reglas:

1. Puede destruirse y recrearse únicamente cuando exista una migración PostgreSQL aprobada para una fase posterior.
2. No se usa como evidencia de datos legacy ni como fuente de conteos.
3. Su `SiteIdentity` de desarrollo y escritor se registran antes de que la aplicación la enrute.
4. La credencial compartida por conversación no se usa; debe reemplazarse y suministrarse mediante un secreto local no versionado.
5. El usuario `postgres` no será la identidad runtime de la aplicación; se separan los roles `runtime` y `migrator`.
6. La autorización general para continuar no adelanta migraciones: cada operación real espera la puerta de su fase y su declaración de trabajo.

## 7. Pendiente (no aprobado aún)

- Límite numérico de conexiones por pool y por sede: no definido.
- Destino físico de cada tenant DB y del control plane (servidores, entornos, respaldos): no decidido por el usuario.
- Tratamiento definitivo de unidades legacy sin `LaboratoryId`: deben resolverse o quedar en una categoría histórica/no operativa antes del cutover de su sede; no se les inventa una sede mediante heurísticas.
- Procedimiento y calendario de rotación de credenciales expuestas: el usuario debe ejecutarla externamente; este documento solo exige que ocurra antes de usar cualquier credencial recibida.

## 8. Credenciales

Ninguna contraseña ni connection string recibidos por conversación se registra, reproduce ni almacena en este documento ni en ningún otro del repositorio. **Todas las credenciales compartidas por conversación se consideran expuestas y deben rotarse antes de cualquier uso.** El control plane referencia secretos únicamente por identificador; los valores residen en el gestor de secretos elegido en su momento (`Pendiente`).

## 9. Registro de cambios

| Fecha | Cambio | Autor |
|---|---|---|
| 2026-09-18 | Creación del contrato con las decisiones multi-sede vinculantes del 18-09-2026 (base PostgreSQL por sede, control plane central, unicidad por sede, `EquipmentUnit` sin `SiteId`, usuarios/roles multi-sede, gestión activa por `Type`, `Management.FacultyId` opcional, routing server-side, migraciones serializadas, reportes cross-site parciales, escritor único LEGACY/NEST, `GastroExample` bloqueado). Sin ejecución técnica. | `documentation` (GLM 5.3 Flash), por encargo del orquestador (MIG-F1-DOC-003) |
| 2026-09-18 | Contrato marcado apto para Fase 2; `SiteCareer` definido conceptualmente; `GastroExample` clasificado como sandbox local descartable, sin autoridad histórica y sin autorización anticipada de migraciones. | `orchestrator` (MIG-F1-DOC-004) |
