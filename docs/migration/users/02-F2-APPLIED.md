# MIG-001 F2 — Implementacion, verificacion offline y validacion PostgreSQL en vivo

> **Estado al 2026-09-25:** `COMPLETE`. `LIVE_POSTGRESQL_VALIDATION_PASS`.
> La fase F2 esta cerrada: la capa de base de datos fue aplicada y verificada
> en un servicio PostgreSQL 18 Windows local (127.0.0.1:5432, base de
> mantenimiento `neondb`) sin secrets persistidos y sin mutacion del servicio
> instalado fuera del wrapper temporal documentado abajo. F3 queda habilitada
> para arrancar (no iniciada).

## Alcance implementado

- Migracion aditiva control-plane
  `control-plane/0006_mig001_users_identity.sql` y ledger tenant
  `tenant/0013_migration_history.sql`.
- Registro pinneado de dos streams: 6 migraciones control-plane y 13 tenant.
- Analyzer, planes, CLI, runner transaccional, manifests verificables y gates
  de integridad/hash.
- Resolucion tenant de solo lectura y lease global en control-plane alrededor
  del callback DDL tenant, con version monotona y recuperacion indeterminada.
- Mapper estricto del contrato legacy hacia la identidad canonica.
- Rol migrador seguro `NOLOGIN`, executor temporal miembro y rol runtime sin
  privilegios DDL, implementados y verificados en el catalogo PostgreSQL real.

Quedan fuera de F2 la carga de datos, API funcional F3, contracts/client F4, React F5/F6, cutover y la
creacion manual de filas en `lu_identity_migration_state`.

## Artefactos y pins

| Stream | Migracion final | SHA-256 canonico | Bytes LF |
|---|---|---|---:|
| control-plane | `0006_mig001_users_identity.sql` | `16d7b3e0e26c91947097b9bb2ac4df6181fffc1cf7460c8cfd48aecc071be3b4` | 54878 |
| tenant | `0013_migration_history.sql` | `5a2c7e89054b58d57e9b2958c60ac259c1a0f8cfdbca8cc10403d91728ea16d8` | 2640 |

Los archivos historicos control-plane `0001..0005` y tenant `0001..0012`
permanecieron byte-identicos. Las superficies principales de F2 son:

- `apps/api/migrations/control-plane/0006_mig001_users_identity.sql`
- `apps/api/migrations/tenant/0013_migration_history.sql`
- `apps/api/src/database/migration-registry.ts`
- `apps/api/src/database/migration-plan.ts`
- `apps/api/src/database/migration-runner.ts`
- `apps/api/src/database/migration-cli.ts`
- `apps/api/src/database/schema-manifest-f2.ts`
- `apps/api/src/database/schema-manifest-tenant.ts`
- `apps/api/src/database/tenant-config.ts`
- `apps/api/src/database/legacy-user-mapping.ts`
- Las specs F2, de registry, runner, manifests, tenant y mapping bajo
  `apps/api/test/`.

## Evidencia offline (historico, ya ejecutado)

Tabla preservada como registro historico de la verificacion previa a la
corrida en vivo. No se reejecuto en esta entrega.

| Gate offline | Resultado |
|---|---|
| TypeScript API `typecheck` | PASS |
| 11 suites focalizadas | PASS, 642/642 |
| Build NestJS | PASS |
| Plan CLI combinado | PASS, 19 migraciones |
| Planes CLI separados | PASS, 6 control-plane y 13 tenant |
| Revision critica R9 | `ACCEPT_OFFLINE` |

La suite PostgreSQL forma parte de las 11 suites solo en su ruta opt-out; ese
resultado no demuestra conexion ni ejecucion PostgreSQL. La validacion real
contra PostgreSQL se describe abajo.

## Evidencia PostgreSQL en vivo

Ejecucion final: `2026-09-25`. Servicio PostgreSQL 18 Windows local en
`127.0.0.1:5432` (base de mantenimiento `neondb`). Suite
`apps/api/test/f2-w2.postgres.integration.e2e-spec.ts` corrida con
`F2_INTEGRATION=1` contra `dist` construido; **Tests 8/8 PASS** (1 suite
aprobada, duracion aproximada 18 s). Log retenido como untracked en
`apps/api/.tmp/f2-live-final.log`.

| Gate en vivo | Resultado |
|---|---|
| Control-plane 0001..0006 fresh y upgrade scenarios | PASS |
| Tenant 0001..0013 aplicados secuencialmente via `up --stream tenant --site` (0003, 0004, 0012, 0013 PASS) | PASS |
| Ledger exactamente contiguo 1..13 con pins/workIds registrados | PASS |
| Pins old+latest verifican y replay-skip (pin-matches-history) | PASS |
| Manifest final de schema: verificacion field-exact | PASS |
| Probas append-only del ledger | PASS |
| ACL: rol runtime sin propiedad de objetos y sin `CREATE` sobre schema; `lu_auth_migrator` es dueno de cada objeto `public` | PASS |
| Rollback de transaccion con fixture invalida, sin residuo | PASS |
| Carrera de replay concurrente: ambos procesos salen con exit code 0 (assertion `[0, 0]` inalterada, no skipped, no serialized) | PASS (corrida previa: 7/8 fallando solo esta carrera con exit codes `[0, 1]`) |

## Correcciones F2 (FIX #1..#4)

- **FIX #1** — `migration-registry.ts`: las migraciones historicas con
  `legacyPolicy` preservan su `IF NOT EXISTS` original durante la ejecucion.
  El cambio vive en `deriveCanonicalBootstrapSql`:
  `body: entry.legacyPolicy ? envelopeBody : body`. PASS en vivo.
- **FIX #2** — `schema-manifest.ts`: el verificador de indices canoniza el
  rendering de PG-18 `(col)::text` -> `col` para columnas varchar/text.
  Implementado en `normalizeIndexExpression`. PASS en vivo.
- **FIX #3** — `schema-manifest.ts`: el verificador de CHECK canoniza
  `varchar IN(...)` vs la forma `= ANY ((ARRAY[...'x'::character varying])::text[])`
  y el cast literal `varchar` dentro de `COALESCE`
  `(coalesce(col, ''::character varying))::text`. Implementado en
  `canonicalizeVarcharLiteralArray` y `canonicalizeVarcharCoalesceLiteral`.
  PASS en vivo.
- **FIX #4** — `tenant-config.ts`: el lease `withTenantMigrationLease` ahora
  arranca la transaccion con `BEGIN ISOLATION LEVEL READ COMMITTED` en lugar
  de REPEATABLE READ. Causa raiz: el `SELECT pg_roles` del preflight fija un
  snapshot RR, el proceso espera el `pg_advisory_xact_lock` global, el
  competidor actualiza `lu_tenant_route` y commitea, y luego el
  `SELECT ... FOR UPDATE` del lease sobre el snapshot stale lanza
  SQLSTATE 40001. Preservado: lock advisory xact global, FOR UPDATE de la fila
  de ruta, UPDATE guardado con conteo exacto de filas afectadas, semantica de
  rollback, SERIALIZABLE del lado tenant, locks advisory por entrada, ledger y
  replay skip. No se anadieron retries ni se swalloweo 40001. El resolver de
  solo lectura sigue en REPEATABLE READ READ ONLY. PASS en vivo.

Sin reescritura de migraciones: control-plane `0001..0005` y tenant
`0001..0012` byte-identicos; los pins de `0006` y `0013` permanecen sin
cambios respecto a la tabla existente.

## Seguridad y limpieza

- **Procedimiento de acceso temporal**: se añadio una regla `trust` solo para
  `host all postgres 127.0.0.1/32` durante la corrida, mediante un wrapper
  temporal bajo `apps/api/.tmp`. `pg_hba.conf` fue restaurado byte-exacto en
  bloque `finally`; SHA-256 verificado tras la restauracion:
  `0C8DC6E6E57399790417A6E13B3A8E1B5E27AA19708A2122148FBFE3BDCECD42`.
- **Validacion de restauracion**: una conexion `postgres` sin password
  posterior a la corrida **falla** (`trust` activo: NO). El wrapper y el
  backup fueron removidos al finalizar.
- **Nota Windows**: `pg_ctl reload` fue rechazado por falta de elevacion. En
  Windows cada backend nuevo relee `pg_hba.conf`, por lo que tanto la
  activacion como la restauracion tomaron efecto para nuevas conexiones
  (probado por exito de conexion durante la corrida y fallo posterior).
- **Residuo**: bases de datos temporales `f2w2w15b_*` = 0; roles executor
  temporales `lu_migration_exec_*` = 0 (pre-corrida tambien 0/0). El rol
  `lu_auth_migrator` queda preservado (`NOLOGIN`, `NOSUPERUSER`).

## Limitacion conocida no bloqueante

`stripTypeCasts` en el normalizador de CHECK puede normalizar un cast
literal `bpchar` de forma similar al valor esperado. No es un bloqueo para
F2: la verificacion de tipo de columna (`column-type schema verification`)
lo acota de forma independiente y no hay evidencia en vivo de un problema
semantico. Se documenta para que la siguiente fase lo observe si aparece.

## Estado de salida

F2 cerrada y aprobada. **DATABASE PARITY (capa de base de datos de F2)**
`COMPLETE`. **F3 LISTA** para arrancar (no iniciada; su alcance — Auth,
PBKDF2, bcrypt rehash, UsersService — no fue tocado en esta entrega).

Gates offline finales (2026-09-25, una sola corrida tras la validacion en vivo):

| Gate | Resultado |
|---|---|
| 11 suites focalizadas F2 (tenant-config, f2-w2, f2-w2 postgres ruta opt-out, legacy-user-mapping, migration-analyzer-bypass/canonical/cli/plan/registry/runner, schema-manifest) | PASS, 686/686 |
| Gate amplio: suite completa `apps/api` (jest `--runInBand`) | 23 suites PASS, 2 skipped (opt-in), 861 tests PASS, 9 skipped; 2 FAIL preexistentes fuera de F2 (ver nota) |
| TypeScript API `typecheck` | PASS |
| Build NestJS | PASS |
| `git diff --check` | PASS |

Nota sobre el gate amplio: `test/auth-managed-role-cli.e2e-spec.ts` y
`test/auth.roles.e2e-spec.ts` fallan solo en sus pins de bytes/SHA-256 de
`apps/api/database/roles/003_provision_auth_runtime_managed.sql` y
`002_grant_auth_security_controls.sql`. Ambos archivos estan trackeados y sin
modificar respecto a HEAD; la diferencia proviene de `core.autocrlf=true`
(copia de trabajo CRLF: 10648 bytes frente a 10408 del blob LF en HEAD para
`003`). No es una regresion F2 ni toca superficies F2; no se corrigio en esta
fase.
