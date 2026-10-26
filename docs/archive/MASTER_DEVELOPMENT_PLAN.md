# 📐 Especificación Ejecutiva de Desarrollo: Sistema de Laboratorios Univalle

Este documento es una guía técnica exhaustiva para la implementación de los módulos pendientes. El objetivo es que cualquier desarrollador pueda seguir estas instrucciones y replicar fielmente la lógica de negocio requerida.

---

## 🔬 Módulo L6: Verificaciones (Paso 1 del Wizard)

**Contexto**: El sistema debe permitir una inspección masiva por ambiente (sala/laboratorio).

### 1. Interfaz de Usuario (Index)
- **Filtro de Ambiente**: En la parte superior, debe haber un selector de **Ambiente / Laboratorio**.
- **Tablero de Unidades**: Al seleccionar un ambiente, se debe mostrar una tabla con todas las **Unidades Físicas** existentes en dicho espacio. No es un listado genérico de equipos, sino de cada instancia física con su número de inventario.
- **Indicador de Estado**: Cada fila debe indicar claramente si la unidad ha sido verificada o no en el periodo actual.
  - **No Verificado**: Botón "Verificar" prominente.
  - **Verificado**: Badge de éxito y link a los resultados.

### 2. Flujo de Trabajo
1.  El usuario selecciona un ambiente.
2.  El sistema presenta la tabla de unidades físicas de ese ambiente.
3.  Al hacer clic en "Verificar", el sistema **redirige** al usuario a la página de verificación correspondiente para esa unidad física específica. **No es un popup**, es una navegación formal que mantiene el contexto de la unidad.
4.  Tras guardar la verificación, el sistema **regresa al usuario al listado de unidades del ambiente** para que pueda proceder con la siguiente unidad o continuar con el Wizard.

---

## 🛠️ Módulos L-7 (Solicitud Técnica) y L-8 (Mantenimiento)

**Objetivo**: Siguiendo la misma filosofía de L6, estos módulos gestionan la respuesta técnica ante desperfectos.

### 1. Lógica por Ambientes (Idéntica a L6)
- Todos estos módulos deben tener en su parte superior la opción de **filtrar por ambientes**.
- Tras filtrar, el sistema debe mostrar una tabla de unidades físicas que actúe como "Registro de Index" o instancias.
- Se debe visualizar explícitamente si se ha realizado o no la acción correspondiente:
  - **L7**: ¿Se emitió la solicitud técnica para esta unidad? (Sí/No).
  - **L8**: ¿Se ejecutó el mantenimiento para esta unidad? (Sí/No).

### 2. Navegación y Wizard
- Del listado de unidades por ambiente, el botón de acción debe redirigir a la página de edición/creación del L7 o L8 correspondiente.
- Al terminar la acción, el flujo retorna al listado de unidades para mantener la continuidad de la ronda de trabajo.

---

## 📒 Módulos Administrativos: Kardex y Desembolso

**Objetivo**: Automatizar la generación de formularios que actualmente se manejan de forma manual en Excel.

### 1. Automatización de Kardex
- **Análisis de Datos**: El sistema debe consolidar todo el historial de una unidad física (Verificaciones L6 + Mantenimientos L8).
- **Generación desde Plantilla**: Utilizando archivos Excel base (`wwwroot/templates/`), el sistema debe inyectar estos datos en las celdas correspondientes.
- **Resultado**: El usuario hace clic en "Descargar Kardex" y obtiene un archivo `.xlsx` o `.pdf` idéntico al formulario manual, pero autogenerado con datos reales del sistema.

### 2. Automatización de Desembolso
- **Captura de Costos**: Extraer los repuestos y mano de obra registrados en el módulo L8.
- **Generación de Formulario**: Adaptar los datos al formato de "Solicitud de Desembolso" administrativa.
- **Propósito**: Facilitar la descarga de formularios ya completados para su firma y trámite administrativo, reduciendo errores humanos de transcripción.

---

## 📅 Módulo de Calendario Interactivo

**Objetivo**: Proporcionar una vista temporal y organizada de todas las actividades planeadas.

1.  **Visualización**: Implementar `FullCalendar` para mostrar eventos de Verificaciones (L6) y Mantenimientos (L8).
2.  **Interactividad**: Los eventos deben ser clicables, abriendo un resumen con:
    - Nombre del Equipo y Ambiente.
    - Estado de la tarea (Pendiente/Vencida).
    - Botón de acceso directo para realizar la tarea correspondiente.
3.  **Descriptividad**: Los colores de los eventos deben reflejar la prioridad o urgencia basada en el cierre de la Gestión L-48.

---

> [!IMPORTANT]
> **Consistencia técnica**: Todos los módulos deben heredar el diseño premium (Nice Admin), utilizar `SweetAlert2` y garantizar que la navegación entre ellos sea fluida, manteniendo siempre el contexto del ambiente y la unidad física seleccionada.
