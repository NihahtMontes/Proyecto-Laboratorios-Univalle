# 📋 Trazabilidad Institucional por Gestión (L6, L7, L8)
**Fecha:** 2026-04-04  
**Rama / Sprint:** Alison Login – Sistema de Trazabilidad Semestral  
**Autor del cambio:** Antigravity AI + Equipo Univalle

---

## 🎯 Objetivo

Implementar una arquitectura de **Contexto de Gestión Activa** a nivel de sistema para asegurar que todas las operaciones técnicas (Verificaciones, Solicitudes y Mantenimientos) estén vinculadas automáticamente al periodo institucional vigente. 

Esta mejora permite:
1. **Trazabilidad Total:** Cada registro sabe exactamente en qué año y semestre (gestión) se realizó.
2. **Reportes Segmentados:** Capacidad de filtrar indicadores de laboratorios por periodos académicos (ej. Gestión 2026-1).
3. **Auditoría Semestral:** Facilitar el cierre administrativo y la exportación de documentos oficiales agrupados por gestión.
4. **Automatización:** Eliminar la necesidad de que el usuario seleccione la gestión manualmente en cada formulario.

---

## 📁 Archivos Modificados / Creados

### 🆕 Archivos Nuevos

| Archivo | Propósito |
|---|---|
| `Services/IManagementContextService.cs` | Interfaz del servicio de contexto global para obtener la gestión activa. |
| `Services/ManagementContextService.cs` | Implementación con lógica de fallback (si no hay una gestión marcada como "Activa", selecciona el periodo más reciente por año/semestre). |
| `features/trazabilidad-gestion.md` | Este documento de documentación técnica. |

---

### ♻️ Archivos de Dominio (Modelos)

Se integró la propiedad `ManagementId` (FK obligatoria) en las entidades operativas del sistema:

#### `Models/Verification.cs` (L6), `Models/Request.cs` (L7), `Models/Maintenance.cs` (L8)
- **Cambio:** Se añadió la relación con `Management`.
- **Propósito:** Permitir que cada actividad técnica tenga un "padre" administrativo.
- **Relación:** `Required` con comportamiento `DeleteBehavior.Restrict` (no se puede borrar una Gestión si tiene registros técnicos asociados).

```csharp
[Required]
[Display(Name = "Gestión Institucional")]
public int ManagementId { get; set; }

[ForeignKey("ManagementId")]
public virtual Management Management { get; set; } = null!;
```

---

### ⚙️ Infraestructura y Datos

#### `Data/ApplicationDbContext.cs`
- Se configuraron los filtros globales para la nueva entidad `Management`.
- Se definieron las relaciones de clave foránea explícitas en `OnModelCreating` para asegurar la integridad referencial y el comportamiento de borrado restringido.

#### `Program.cs`
- Se registró el nuevo servicio en el contenedor de Inyección de Dependencias (DI):
```csharp
builder.Services.AddScoped<IManagementContextService, ManagementContextService>();
```

---

### 🖥️ Lógica de Negocio (PageModels - "Create")

Se automatizó la asignación del ID de gestión en los controladores de creación:

#### `Pages/Verifications/Create.cshtml.cs`, `Pages/Requests/Create.cshtml.cs`, `Pages/Maintenances/Create.cshtml.cs`
1. **Inyección:** Se inyecta `IManagementContextService`.
2. **Asignación Automática:** En el método `OnPostAsync`, justo antes de guardar en la DB, se recupera el ID activo:
```csharp
var currentManagement = await _managementService.GetCurrentManagementAsync();
Input.ManagementId = currentManagement.Id;
```

---

## 🚀 Mejoras Específicas en Solicitudes (L7)

Además de la trazabilidad, se realizaron mejoras críticas en el módulo de solicitudes:

### 1. Renombrado de Etiquetas
Se ajustaron los nombres de los campos en la interfaz (`Create.cshtml`) para mayor claridad técnica:
- **"Descripción del Fallo"** → **"FALLAS O PROBLEMAS DEL EQUIPO"**.
- **"Observaciones Adicionales"** → **"SUGERENCIAS U OBSERVACIONES PARA MANTENIMIENTO"**.

### 2. Precarga del Protocolo Técnico (13 Puntos)
En el flujo del **Wizard**, el campo de observaciones ahora se inicializa automáticamente con los 13 ítems del protocolo técnico estándar (Limpieza, cables, refrigeración, etc.).

**Propósito:**
- Ahorrar tiempo en la captura de datos (Data Entry).
- Asegurar que el solicitante mencione los puntos básicos que deben revisarse.
- Estandarizar la comunicación entre el laboratorio y el área de mantenimiento.

```csharp
// Lógica de precarga en Requests/Create.cshtml.cs
if (isFromWizard) {
    Input.Observations = "Protocolo Sugerido:\n1. Desconexión eléctrica...\n2. Limpieza interna...\n...\n13. Mantenimiento externo.";
}
```

---

## 🛠️ Cómo Funciona el Sistema de Gestión Activa

1. El administrador marca una Gestión como **"Activa"** (ej. 2026 - Segundo Semestre).
2. El `ManagementContextService` detecta este estado mediante:
   - `managementContext.Managements.FirstOrDefaultAsync(m => m.Status == ManagementStatus.Active)`.
3. Si por alguna razón no hay ninguna activa, el sistema utiliza un **Fallback Inteligente**:
   - Ordena por `Year` descendentemente y por `Semester` descendentemente para encontrar el periodo más próximo a la fecha actual.
4. Esto garantiza que el sistema **NUNCA falle** por falta de una gestión configurada, manteniendo la operatividad continua de los laboratorios.

---

> [!NOTE]
> **Integridad Histórica:** Al migrar datos antiguos que no tenían `ManagementId`, el sistema asignará el ID de la gestión que cubra el rango de fechas (`StartDate <= FechaCreacion <= PlannedEndDate`) para mantener la coherencia retrospectiva.
