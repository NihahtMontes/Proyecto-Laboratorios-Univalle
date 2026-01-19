# Arquitectura y Planificación Técnica: Proyecto Laboratorios Univalle

Este documento define la estrategia técnica profunda para la evolución del sistema, dividida en dominios de responsabilidad ("Agentes").

## 1. Agente de Infraestructura: Auditoría y Transaccionalidad (Misión 3)
**El Problema:**
Actualmente, la lógica de auditoría (`CreatedBy`, `ModifiedDate`, etc.) está dispersa en los Controladores o Modelos. Esto viola el principio DRY (Don't Repeat Yourself) y es propenso a errores humanos (olvidar actualizar un campo). Además, la validación de integridad (campos únicos) depende de validaciones previas en memoria que pueden fallar en condiciones de concurrencia.

**Solución Arquitectónica:**
1.  **Interceptación en Nivel de Datos (DbContext):** Implementar la auditoría sobreescribiendo `SaveChangesAsync` en `ApplicationDbContext`. Detectaremos automáticamente cualquier entidad que implemente la interfaz `IAuditable`.
2.  **Servicio de Usuario Contextual (`ICurrentUserService`):** Abstraer el acceso al `HttpContext` para inyectar el ID del usuario actual de forma segura en la capa de datos.
3.  **Manejo Global de Excepciones de BD:** No confiar solo en `AnyAsync()` para validar unicidad. Capturar `DbUpdateException` (código de error SQL 2601/2627) en una capa de filtro o servicio base para transformar errores de "índice duplicado" en mensajes amigables para el usuario ("El nombre de usuario ya existe").

## 2. Agente de Dominio y Localización (Misión 2 - UI/UX Lenguaje)
**El Problema:**
La interfaz mezcla inglés y español. El "problema de raíz" no es solo traducir textos, sino la falta de estandarización en cómo se presentan los metadatos de los modelos.

**Solución Arquitectónica:**
1.  **Centralización de Metadatos:** Utilizar exhaustivamente `DataAnnotations` (`[Display(Name="...")]`) en los ViewModels. Esto garantiza que si cambiamos "First Name" a "Nombres" en el modelo, se actualice en *todos* los formularios y tablas automáticamente.
2.  **Convención de Nomenclatura:** Establecer reglas estrictas para los mensajes de error en los DTOs (Data Transfer Objects) y ViewModels.

## 3. Agente de Visualización (Misión 1 - Nice Admin)
**El Problema:**
Adoptar una plantilla administrativa (Nice Admin) no es solo copiar CSS. El problema técnico es el **acoplamiento**. Si pegamos el HTML de la plantilla directamente en cada vista, será imposible mantener el sitio a futuro.

**Solución Arquitectónica:**
1.  **Layout Modular:** Descomponer la plantilla en `Partial Views` funcionales: `_Sidebar.cshtml`, `_TopBar.cshtml`, `_Footer.cshtml`, `_Scripts.cshtml`.
2.  **Wrapper de Componentes:** Crear "Tag Helpers" o Vistas Parciales para elementos comunes de la plantilla (ej. "Cards", "Alertas", "Tablas con paginación"). Así, para crear una tarjeta, no escribimos 20 líneas de HTML, sino `<app-card title="Usuario">...</app-card>`.

## 4. Agente de Reporting (Misión 4)
**El Problema:**
Imprimir "pantallazos" del navegador es poco profesional (no respeta saltos de página, pierde estilos, no es legalmente válido).

**Solución Arquitectónica:**
1.  **Motor de Renderizado PDF Backend:** Utilizar una librería robusta como **QuestPDF** (código declarativo) o **Rotativa** (HTML a PDF). Recomendación: **QuestPDF** por su precisión milimétrica en facturas y reportes oficiales.
2.  **Servicio de Exportación (`IExportService`):** Una interfaz genérica que reciba un `IEnumerable<T>` y retorne un `FileStream`. Esto desacopla la lógica de negocio del formato (Excel/PDF). Para Excel usaremos **ClosedXML**.

## 5. Agente de Experiencia de Usuario (Misión 5 - Navegación)
**El Problema:**
Una estructura de menú plana (`Home > User`, `Home > Equipment`) hace la navegación cognitiva costosa para el usuario.

**Solución Arquitectónica:**
1.  **Taxonomía de Navegación:** Reagrupar las rutas basándonos en los "Contextos Delimitados" (Bounded Contexts) del negocio:
    *   *Administración* (Usuarios, Roles)
    *   *Inventario* (Equipos, Insumos)
    *   *Operativa* (Mantenimientos, Solicitudes)
    *   *Configuración* (Tablas maestras: Ciudades, Paises)

---
### Plan de Ejecución Inmediata (Fase 1: Cimientos)

1.  **Reactiva la Auditoría Automática (Código seguro):** Implementar `ICurrentUserService` y conectarlo al `DbContext` correctamente para que *nunca más* tengas que escribir `user.CreatedDate = DateTime.Now`.
2.  **Refactorización de Layout y Menú:** Aplicar la taxonomía definida en el punto 5.
3.  **Prueba de Concepto en "Usuarios":** Aplicar la centralización de metadatos (Punto 2) en **Create/Edit User** para demostrar el poder de los DataAnnotations.
