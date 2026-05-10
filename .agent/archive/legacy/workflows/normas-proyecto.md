---
description: Normas y estándares del Proyecto Laboratorios Univalle
---

Este flujo de trabajo define las reglas que deben seguir todos los "agentes" o modos de trabajo en este proyecto para asegurar la consistencia.

### 1. Idioma y Localización (Misión 2)
- **Interfaz de Usuario:** Todos los textos visibles para el usuario (títulos, botones, labels, placeholders) deben estar en **Español**.
- **Mensajes de Error:** Las validaciones de DataAnnotations en los Modelos deben usar mensajes en español.
- **Formato:** Se prefiere el uso de DataAnnotations `[Display(Name = "...")]` para las etiquetas.

### 2. Auditoría Automática (Misión 3)
- Todas las entidades que requieran seguimiento deben implementar `IAuditable`.
- La auditoría debe ser gestionada automáticamente en el `ApplicationDbContext` reemplazando los métodos `SaveChanges` y `SaveChangesAsync`.
- No se debe asignar manualmente el `CreatedBy` o `ModifiedBy` en las páginas a menos que sea estrictamente necesario.

### 3. Validaciones de Negocio (Misión 3)
- Los campos únicos (Username, CI, Números de Inventario) deben validarse tanto en el cliente (jQuery Validation) como en el servidor.
- Las excepciones de base de datos para campos duplicados deben ser capturadas y transformadas en mensajes amigables en español.

### 4. Estilos y Plantilla (Misión 1 - Pendiente)
- Por ahora, mantener el estilo base. En el futuro, se aplicará **Nice Admin**.

### 5. Reportes y Documentos (Misión 4)
- Los reportes para impresión deben ser sobrios, profesionales, con el logo de la institución y datos claramente tabulados.
- Formatos requeridos: PDF y Excel.

### 6. Organización de Menús (Misión 5)
- El menú lateral debe ser jerárquico.
- Ejemplo: "Gestión de Activos" -> ["Equipos", "Tipos de Equipo", "Ubicaciones"].
