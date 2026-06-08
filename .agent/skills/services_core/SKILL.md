---
name: services_core
description: Servicios transversales no-reporting: usuario actual, usuario sistema, contexto de gestion, errores DB e Identity claims.
trigger: Cambios en `Services/CurrentUserService.cs`, `SystemUserService.cs`, `ManagementContextService.cs`, `DatabaseErrorHandler.cs`, `UserClaimsPrincipalFactory.cs` o DI en `Program.cs`.
scope: Services/Core
context: .agent/context/areas/backend-page-models.md
---
# Skill Services Core

## 1. Contexto Del Modulo

Los servicios core encapsulan datos transversales que no pertenecen a una pagina concreta: usuario actual, usuario sistema, contexto de gestion activa, errores de base de datos y claims de Identity.

Flujo: PageModel/Controller -> servicio inyectado -> DbContext/Identity/Cache -> resultado reusable para UI o logica de negocio.

## 2. Arquitectura Y Archivos Clave

- Usuario actual: `Services/CurrentUserService.cs`.
- Usuario sistema: `Services/SystemUserService.cs`.
- Contexto de gestion: `Services/ManagementContextService.cs`.
- Errores DB: `Services/DatabaseErrorHandler.cs`.
- Claims Identity: `Services/UserClaimsPrincipalFactory.cs`.
- Registro DI: `Program.cs`.

## 3. Integracion Con NiceAdmin

- Servicios de usuario alimentan topbar, auditoria, roles visibles y permisos.
- `ManagementContextService` puede influir en dashboard/wizard cuando una gestion activa es necesaria.
- Errores DB deben convertirse en mensajes amigables para Razor/SweetAlert cuando lleguen a UI.

## 4. Patrones Y Convenciones

- Mantener interfaces cuando existan (`ICurrentUserService`, `IManagementContextService`).
- No consultar HttpContext directamente desde muchas paginas si un servicio ya resuelve el dato.
- Cache de gestion debe invalidarse/revisarse si cambia la gestion activa.
- No exponer detalles internos de DB al usuario final.

## 5. Contexto Para Agente

- Revisar `Program.cs` antes de agregar o cambiar DI.
- No crear servicios paralelos para usuario/gestion si ya existe uno.
- Si un cambio afecta roles/claims, probar login, layout y autorizacion.
- Si una gestion depende de tipo preventivo/correctivo, no caer silenciosamente a la gestion incorrecta.
