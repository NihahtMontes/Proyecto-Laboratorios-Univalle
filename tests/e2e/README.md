# Pruebas E2E — explícitamente diferidas a la Fase 5

**Trabajo**: MIG-F2-TEST-001 · **Propietario**: `test-worker` · **Fecha**: 18-09-2026
**Estado**: `DIFERIDO — no activo`. Este directorio esta reservado por
documentacion; **no contiene** configuracion de Playwright, ni browsers, ni
pruebas, ni `package.json`, ni dependencias. Que este README exista no habilita
nada: E2E solo se escribe y ejecuta cuando se cumplen los criterios de la
seccion 3.

Fuentes normativas: `docs/PLAN_MIGRACION_REACT_NESTJS.md` (Fases 3, 4, 5 y
reglas transversales), `docs/CONTRATO_MULTISEDE.md`, `docs/AGENT_WORKFLOW.md`,
`docs/COORDINACION_MULTI_CHAT.md` (formato de hallazgo y handoff).

---

## 1. Por que esta diferido

El plan ubica el primer recorrido vertical de extremo a extremo en la **Fase 5**
(piloto React/NestJS solo lectura: `AssetView`, `Environments`,
`EnvironmentUnits`). En Fase 2 solo existen scaffolds y smokes sin puerto; no
hay frontend servido, identidad con `ActiveSiteSession` ni datos operacionales.
Una prueba E2E hoy no tendria recorrido real. Levantar stack "de prueba" ahora violaria ademas las
reglas vigentes:

- **Sin Playwright ni browsers instalados**: ninguna descarga de navegadores o
  drivers esta autorizada; requiere decision de manifiestos en Fase 2/5 y
  autorizacion explicita del usuario.
- **Sin servidores persistentes ni puertos escuchando**: `AGENT_WORKFLOW.md` y
  `AGENTS.md` prohiben iniciar la aplicacion o servicios en segundo plano salvo
  pedido expreso; este trabajo no inicia nada.
- **Sin conexiones a bases**: nada de produccion, nada de MonsterASP, nada de
  fuentes institucionales; las E2E futuras corren solo contra entorno no
  productivo con base `_QA` o equivalente.
- **Sin pruebas de estres**: opt-in, minimo 6 GB libres y autorizacion
  explicita (`AGENTS.md`).

En Fase 2 el gate ejecutable de contratos es typecheck/build de
`@lu/contracts` consumido por web/api (ver `tests/contract/README.md`), no una
suite runtime. Este README es la contraparte: deja constancia de lo que se
difiere y de cuanto, para que nadie presente E2E inexistentes como cobertura.

## 2. Prohibiciones vigentes mientras este directorio este inactivo

1. No anadir specs, configs (`playwright.config.*`), fixtures ni binarios bajo
   `tests/e2e/` sin declarar primero un trabajo con los criterios de la
   seccion 3 cumplidos y el orquestador aceptando la reservacion.
2. No ejecutar ni documentar como "prueba E2E" ninguna navegacion manual de
   la app .NET actual: eso pertenece al QA funcional legado, no a este plan.
3. No instalar dependencias de prueba por implicacion de este README.
4. No usar puertos, hooks ni scripts que levanten procesos al instalar.

## 3. Criterios de activacion (todos obligatorios, con evidencia)

E2E pasa de `DIFERIDO` a `ACTIVO` solo cuando el orquestador registre:

- [ ] **CA-1** Fase 3 cerrada: identidad implementada (SSO OIDC/SAML o
      Keycloak, segun verificacion previa obligatoria PLAN seccion 8.3), login y
      navegacion probados en entorno no productivo con `_QA` o equivalente,
      revision `Luna max` (auth) cumplida y aprobacion explicita del usuario;
      sesion con sede activa explicita y sin JWT en `localStorage`.
- [ ] **CA-2** Fase 4 cerrada para la capacidad a probar: nucleo NestJS
      servido bajo `/api/v1`, contratos publicados y versionados, QA
      reproducible en `_QA` por capacidad y handoffs aceptados.
- [ ] **CA-3** Pilotables las tres vistas del piloto (Fase 5): `AssetView`,
      `Environments`, `EnvironmentUnits` renderizadas desde React + Vite
      consumiendo NestJS; builds secuenciales con 0 errores.
- [ ] **CA-4** Runner y versiones fijados en los manifiestos del monorepo
      (decision de paquetes de Fase 2, no inventada aqui).
- [ ] **CA-5** Autorizacion explicita del usuario para: entorno de pruebas,
      instalacion del runner y navegadores, y ventana de ejecucion; verificada
      la politica de recursos (>= 2 GB libres, un proceso pesado a la vez).
- [ ] **CA-6** Trabajo `test-worker` declarado con el protocolo de propiedad
      temporal, con rutas exactas bajo `tests/e2e/`.

Cualquier criterio sin evidencia vigente (artefacto, hash, fecha o
identificador de ejecucion) se registra como `Pendiente de verificar`, no como
cumplido.

## 4. Alcance de diseno cuando se active (solo diseio, no ejecucion hoy)

Familias obligatorias heredadas del rol de pruebas y de los invariantes
I-1..I-12 de `tests/contract/README.md`:

- **Felices**: recorrido de solo lectura de las tres vistas del piloto con
  sede activa valida; navegacion y render consistentes (la puerta de Fase 5
  exige consistencia visual verificada por `Luna high`).
- **Autorizacion**: usuario sin membresia en la sede activa, rol insuficiente
  (Administrador/Supervisor por sede vs `SuperAdmin` global), operacion sin
  `ActiveSiteSession` y tentativas cross-site; todas deben fallar cerradas sin
  filtrar datos de otra sede.
- **Validaciones**: payloads fuera de contrato contra `/api/v1` (el contrato
  no admite connection strings ni `WriterLabel` de cliente: I-5, I-7).
- **Concurrencia**: dos sesiones en sedes distintas no se degradan (fallos
  aislados, CM 3.5); un solo escritor por capacidad y sede (CM 5.2). Solo
  lecturas en Fase 5; la concurrencia de escritura se prueba en su fase.
- **Historico**: ningun recorrido E2E del piloto espera hard-delete; el
  principio de integridad historica (`AGENTS.md`) exige que flujos de baja se
  modelen por estados/soft-delete cuando existan.
- **Errores relevantes**: sede en migracion o degradada segun el control
  plane; caida de una tenant DB sin afectar a las demas; respuestas de error
  tipadas del contrato.

Criterio de exito de cada suite futura: determinista (sin orden implicito, sin
estado compartido entre specs, datos sembrados por fixture `_QA`), secuencial,
y autocontenida (todo proceso que levante se cierra en el teardown y se
verifica).

## 5. Reporte y handoff

- Hallazgos con el formato exacto de `docs/COORDINACION_MULTI_CHAT.md` (ID,
  fecha y base/entorno, modulo/ruta, precondiciones, pasos reproducibles,
  esperado, actual, severidad P0-P3, evidencia, datos afectados, archivos
  sospechosos sin editar).
- El `test-worker` **no corrige codigo productivo**: reporta al propietario de
  la superficie (react/nestjs/database/devops) y conserva la reproduccion.
- Handoff de cierre de trabajo segun `docs/AGENT_WORKFLOW.md`, incluyendo
  explicitamente las validaciones NO ejecutadas y su motivo.

## 6. Registro

| Fecha      | Cambio                                                                                                                                                                               | Autor                           |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------- |
| 2026-09-18 | Creacion: E2E diferido formalmente a Fase 5; criterios CA-1..CA-6; sin Playwright, browsers, puertos ni servidores; prohibiciones de directorio inactivo. Sin ejecucion de comandos. | `test-worker` (MIG-F2-TEST-001) |
