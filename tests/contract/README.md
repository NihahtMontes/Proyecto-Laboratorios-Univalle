# Pruebas de contrato — `@lu/contracts` (Fase 2)

**Trabajo**: MIG-F2-TEST-001 · **Propietario**: `test-worker` · **Fecha**: 18-09-2026
**Estado de este directorio**: solo documentacion normativa de pruebas. No hay
runner, ni specs runtime, ni `package.json`, ni dependencias instaladas en este
trabajo. La eleccion concreta de paquetes, scripts y versiones queda registrada
en los manifiestos del monorepo cuando el `devops-worker`/orquestador implemente
la Fase 2 (`docs/PLAN_MIGRACION_REACT_NESTJS.md`, secciones 5 Fase 2 y 7).

Fuentes normativas vinculantes:

- `docs/CONTRATO_MULTISEDE.md` (aprobado para implementar desde la Fase 2).
- `docs/PLAN_MIGRACION_REACT_NESTJS.md` (decisiones de la seccion 2 y fases).
- `docs/COORDINACION_MULTI_CHAT.md` (formatos de hallazgo y handoff).
- `docs/AGENT_WORKFLOW.md` (propiedad de superficies: `tests/` es del `test-worker`).

---

## 1. Que es el gate ejecutable de Fase 2 (y que NO es)

En la Fase 2 existen scaffolds NestJS y React verificables en memoria, pero no
hay aplicaciones desplegadas, identidad ni datos operacionales. Esta prohibido
levantar servicios persistentes o conectar bases. Por lo tanto:

- **El gate ejecutable de Fase 2 es el typecheck + build del paquete de
  contratos `@lu/contracts`, consumido por `apps/web` y `apps/api`.** Es decir:
  1. `typecheck` y `build` de `@lu/contracts` con 0 errores.
  2. `typecheck` y `build` de `apps/web` contra los tipos publicados por
     `@lu/contracts`, con 0 errores.
  3. `typecheck` y `build` de `apps/api` contra los mismos tipos, con 0 errores.
     Los tres builds se ejecutan **secuenciales**, nunca en paralelo, y se detiene
     el gate si alguno falla. El nombre exacto del paquete y de los scripts se
     fija en el manifiesto del monorepo al implementar la fase; este README usa
     `@lu/contracts` como identificador de trabajo aprobado por el orquestador.
- **NO es una suite de dominio runtime ficticia.** El smoke real de `/api/v1/healthz`
  vive en `apps/api` y usa Fastify `inject()` sin puerto. Las pruebas de
  autorización, sede y datos se difieren hasta que existan esas capacidades.
  Una prueba que no puede ejecutarse y fallar de verdad no es evidencia de
  calidad: es deuda disfrazada.

Lo que el gate typecheck/build **si** garantiza mecanicamente:

- Fuente unica de verdad simbolica: enums, tipos de entidad, shapes de request
  /response versionados y tipos de sesion viven solo en `@lu/contracts`; web y
  api los importan y el compilador rechaza divergencias de nombre, forma o
  variante de enum entre las dos puntas.
- No representabilidad donde el tipo lo expresa: por ejemplo, si el tipo de
  `EquipmentUnit` en el contrato no declara `siteId`, ningun codigo de web o
  api puede construir un filtro por `siteId` en la unidad sin tocar una
  conversión explicita que el review detecta (invariante I-2).
- Compatibilidad compile-time del prefijo versionado `/api/v1`: rutas, DTOs y
  versiones se referencian desde constantes/tipos del paquete, no desde
  literales dispersos (invariante I-4).

Lo que el gate **no** puede garantizar (diferido, ver seccion 5): validacion de
payloads en tiempo de ejecucion, autorizacion real del backend, comportamiento
de concurrencia y aislamientos por sede.

---

## 2. Invariantes del contrato multi-sede que este gate debe sostener

Enumerado normativo para todo el plan. Cada invariante cita la clausula de
`docs/CONTRATO_MULTISEDE.md` (CM) o `docs/PLAN_MIGRACION_REACT_NESTJS.md`
(PLAN) que la fija. La matriz de la seccion 3 distingue lo materializado en
Fase 2 de lo diferido a la fase que introduce cada capacidad; la ausencia
actual de un DTO futuro no se presenta como prueba mecanica de su invariante.

| ID   | Invariante                                                                                                                                                                                                                                                                                                                                                                                                   | Fuente                          |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------- |
| I-1  | **Site-scoped / unicidad dentro de la sede**: codigos de laboratorio y de gestion son unicos **dentro de cada sede**; el contrato no debe dar a ningun tipo la impresion de unicidad global de esos codigos (claves naturalezas llevan contexto de sede o pertenecen al tenant).                                                                                                                             | CM 1.1, 1.3                     |
| I-2  | **`EquipmentUnit` sin `SiteId` propio**: la sede de una unidad se deriva de su `Laboratory`; el tipo de la unidad en `@lu/contracts` NO expone `siteId` y ninguna operacion de consulta tipada sobre unidades acepta un filtro `siteId` directo. `LaboratoryId` es obligatorio en el destino.                                                                                                                | CM 1.4, PLAN 2.2                |
| I-3  | **`ActiveSiteSession` explicita**: el tipo de sesion activa transporta identificador de sede resuelto en el control plane; ninguna operacion de negocio tipada puede invocarse sin una sede activa definida. El frontend no conoce endpoints ni identificadores de conexion por sede.                                                                                                                        | CM 1.2, 3.1                     |
| I-4  | **API versionada `/api/v1`**: todo contrato HTTP entre web y api vive bajo el prefijo `/api/v1` definido en el paquete; el versionado es parte del contrato, no de literales en cada consumer.                                                                                                                                                                                                               | PLAN Fase 2 y 4                 |
| I-5  | **No conexiones aportadas por cliente**: ningun DTO, query param, header de negocio ni payload de sesion acepta connection strings, DSN, host/credenciales ni referencias de conexion: solo el control plane resuelve `sede -> conexion` server-side, con valores en el gestor de secretos. El contrato hace invalidable tipograficamente que un cliente porte una conexion.                                 | CM 3.2, CM 8                    |
| I-6  | **Sin cross-DB en el contrato de negocio**: los tipos de operacion no modelan JOINs ni transacciones que crucen control plane y tenant DB, o dos tenant DBs; lo que hoy cruzaria bases se representa como pasos compensables con auditoria local.                                                                                                                                                            | CM 2 (reglas derivadas), CM 3.4 |
| I-7  | **`WriterLabel` solo servidor**: las etiquetas `LEGACY_SQLSERVER` / `NEST_SQLSERVER` / `NEST_POSTGRES` existen exclusivamente dentro de `apps/api` y estan ausentes de `@lu/contracts`; ningun input, query ni body tipado permite al cliente fijar, elegir o influir el escritor activo de una capacidad en una sede. El cambio de etiqueta se registra por el protocolo de propiedad temporal, no por API. | CM 5.2                          |
| I-8  | **Gestion activa por `Type` por tenant**: dentro de cada sede existe una gestion activa por `Type`, con coexistencia preventiva/correctiva; `ManagementPlan` conserva la unicidad `ManagementId + EquipmentUnitId`.                                                                                                                                                                                          | CM 1.6                          |
| I-9  | **Roles por sede**: `Administrador` y `Supervisor` son membresias **por sede**; `SuperAdmin` es global. Los tipos de autorizacion del contrato separan rol-global de rol-en-sede-activa.                                                                                                                                                                                                                     | CM 1.5                          |
| I-10 | **`Management.FacultyId` opcional y no autoritativo**: nulable durante la transicion; no define autorizacion ni sede (la sede del management es la del tenant).                                                                                                                                                                                                                                              | CM 1.7, PLAN 2.3                |
| I-11 | **`SiteCareer`**: `SiteId` y `CareerId` obligatorios, una sola relacion activa por pareja `(SiteId, CareerId)`, desactivacion conserva historia (sin borrado fisico).                                                                                                                                                                                                                                        | CM 2                            |
| I-12 | **Secretos nunca viajan en el contrato**: el control plane referencia secretos por identificador; ningun tipo del contrato modela valores de secreto.                                                                                                                                                                                                                                                        | CM 8                            |

---

## 3. Matriz de cobertura del gate Fase 2

Leyenda: **TC** = cubierto mecanicamente por typecheck/build de
`@lu/contracts` + web + api en Fase 2 · **CR** = diferido a contract-tests
runtime cuando exista el backend (Fase 4, base `_QA` con autorizacion) ·
**E2E** = diferido a `tests/e2e/` (Fase 5; ver `tests/e2e/README.md`).

| Invariante                      | TC (Fase 2)                                                  | CR (Fase 4)                                                                      | E2E (Fase 5) |
| ------------------------------- | ------------------------------------------------------------ | -------------------------------------------------------------------------------- | ------------ |
| I-1 unicidad por sede           | Diferido: no hay DTO de laboratorio/gestion                  | CR (Fases 4/6: rechazo de duplicados por sede)                                   | E2E          |
| I-2 unidad sin `SiteId`         | Diferido a Fase 7: no hay DTO de unidad                      | CR (derivacion via Laboratory)                                                   | E2E          |
| I-3 `ActiveSiteSession`         | TC parcial (shape de sesion); operaciones diferidas          | CR (Fase 4: operaciones sin sede => 4xx)                                         | E2E          |
| I-4 `/api/v1`                   | TC (constantes/rutas tipadas compartidas)                    | CR (prefijo real servido)                                                        | E2E          |
| I-5 no conexiones cliente       | TC parcial en tipos actuales; DTOs de negocio diferidos      | CR (payloads con campos extras de conexion son rechazados/ignorados server-side) | E2E          |
| I-6 sin cross-DB                | Diferido a Fase 4: aun no hay operaciones de negocio         | CR                                                                               | E2E          |
| I-7 `WriterLabel` solo servidor | TC (tipo ausente de `@lu/contracts` y aislado en `apps/api`) | CR (ninguna ruta mutation acepta writer)                                         | E2E          |
| I-8 gestion activa por `Type`   | Diferido a Fase 8: no hay DTO de gestion                     | CR                                                                               | E2E          |
| I-9 roles por sede              | TC (tipos de membresia)                                      | CR (403 por rol-en-sede)                                                         | E2E          |
| I-10 `FacultyId` opcional       | Diferido a Fase 8: no hay DTO de gestion                     | CR                                                                               | E2E          |
| I-11 `SiteCareer`               | Diferido a Fase 6: no hay DTO de `SiteCareer`                | CR                                                                               | E2E          |
| I-12 sin secretos en contrato   | TC parcial: error publico sin `unknown`/excepcion arbitraria | CR (Fase 4: mapper server-side y auditoria de respuestas)                        | E2E          |

Cobertura por familias de prueba del rol (sin producto todavia, se materializa
en la fase correspondiente): felices y validaciones de shape → TC Fase 2;
autorizacion, validaciones de dominio, concurrencia de escritor unico por sede,
historico (sin hard-delete) y errores relevantes → CR Fase 4 y E2E Fase 5.

---

## 4. Reglas de determinismo y recursos para el gate

- Secuencial: un solo build a la vez; nada de workers ni watchers persistentes
  (politica de bajo consumo de `AGENTS.md` y PLAN seccion 5).
- Offline: el gate Fase 2 no abre sockets, no conecta bases, no levanta
  servidores, no toca MonsterASP ni fuentes institucionales.
- Sin artefactos generados dentro de `tests/`: los outputs de build quedan en
  los directorios que defina el monorepo, nunca versionados desde aqui.
- Idempotencia: repetir `typecheck`/`build` debe dar el mismo resultado; no se
  admiten pruebas con estado compartido, sleeps arbitrarios ni reloj de wall.
- Ninguna nueva dependencia sin autorizacion explicita del usuario
  (`docs/AGENT_WORKFLOW.md`, politica de consumo).

## 5. Limitaciones: que NO certifica este README ni este gate

1. No certifica comportamiento runtime: el compilador no valida payloads
   reales, codigos HTTP ni autorizacion efectiva. Eso exige contract-tests
   ejecutables en Fase 4 contra el nucleo NestJS en base `_QA`, y E2E en
   Fase 5. Hasta entonces quedan marcados **Pendiente de verificar**.
2. La existencia de `@lu/contracts` no certifica por si sola su correccion. El
   typecheck, build y consumo real por web/api deben ejecutarse y adjuntarse al
   handoff de cierre de la Fase 2.
3. No sustituye el contrato normativo: ante discrepancia, manda el codigo y el
   contrato serializado vigente (orden de `docs/AGENT_WORKFLOW.md`), luego esta
   documentacion.

## 6. Registro

| Fecha      | Cambio                                                                                                                                                                             | Autor                           |
| ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------- |
| 2026-09-18 | Creacion: gate Fase 2 = typecheck/build de `@lu/contracts` consumido por web/api, no suite runtime ficticia; invariantes I-1 a I-12 y matriz TC/CR/E2E. Sin ejecucion de comandos. | `test-worker` (MIG-F2-TEST-001) |
