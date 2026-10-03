# Documento de Handoff: Implementación de Módulo L-3 y L-5

Este documento resume el trabajo de implementación, configuración y la arquitectura del nuevo flujo operativo de Laboratorios (Formularios L-3 y L-5). Sirve como guía de transferencia para que el equipo de desarrollo pueda retomar el proyecto, corregir bugs pendientes e implementar mejoras.

---

## 1. Resumen de Configuración Inicial

Para llegar al estado actual de la rama, se realizaron las siguientes configuraciones de infraestructura y seguridad:

*   **Restauración de Base de Datos:** Se restauró el backup `.bak` local en la instancia `localhost\SQLEXPRESS01` bajo el nombre `DB_Laboratorios_Univalle`.
*   **Configuración de `appsettings.json`:** Se modificó la cadena de conexión (`DefaultConnection`) para apuntar a la instancia local mencionada.
*   **Backdoor Temporal de Autenticación:** Dado el hash de contraseñas de los usuarios restaurados, se implementó un *bypass* temporal en el `Login.cshtml.cs`. Actualmente se puede iniciar sesión forzadamente usando el correo `admin@univalle.edu` y la contraseña `Admin123!`. **⚠️ IMPORTANTE: Este código debe removerse antes de pasar a producción.**
*   **Migraciones Aplicadas:** Se generaron y aplicaron exitosamente a la base de datos las migraciones de Entity Framework Core:
    *   `ImplementacionL3L5`: Creación de la estructura base.
    *   `RefactorL5ResponsibleToText`: Modificación de `L5Incidents` para desligar la clave foránea de usuarios y usar texto libre en su lugar (`ResponsibleName` y `ResponsibleType`).

---

## 2. Flujo Operativo Implementado (Arquitectura)

El módulo se compone de 5 pasos secuenciales que simulan el ciclo de vida de un laboratorio práctico.

**Eje Central:** El identificador principal que viaja por la URL (como `planId`) es el `Id` de la tabla `L3Requests`. Este ID conecta todos los formularios y garantiza la integridad referencial.

*   **Paso 1 (Planificación):** Permite crear la cabecera del registro en `L3Requests` (Materia, Docente, Grupo, Fecha, Laboratorio). Se implementó un endpoint AJAX (`OnGetSearchPlansAsync`) que renderiza un modal para buscar planificaciones previamente creadas.
*   **Paso 2 (Requerimientos):** Formulario para definir qué utensilios/equipos se necesitan y en qué cantidad (`L3RequestDetails`). Cuenta con lógica de preservación: si el usuario es devuelto desde el Paso 3 por falta de inventario, el `OnGetAsync` recarga los ítems ya guardados para que el usuario pueda editar sus cantidades sin perder su trabajo.
*   **Paso 3 (Disponibilidad):** Compuerta lógica de seguridad. Compara el `ExpectedQuantity` de cada ítem con el stock físico disponible en `EquipmentUnits` (donde `Status == Operational`). Si hay un faltante, bloquea el paso al L-3 y redirige al Paso 2 anexando un flag `?conflict=true`.
*   **Paso 4 (Devolución L-3):** Pantalla de validación de entrega y devolución de equipos. 
    *   **Lógica de Interbloqueo:** Calcula automáticamente el `Faltante = (DeliveredQuantity - ReturnedQuantity)`. Si existe un faltante mayor a cero, la interfaz verifica si ya existe un reporte en `L5Incidents` para ese equipo. Si no existe, el botón de **Finalizar L-3** queda deshabilitado (`disabled`) obligando al docente a registrar el daño.
*   **Paso 5 (Incidentes L-5):** Formulario de reporte de anomalías (rotura, pérdida, etc.). El `<select>` de equipos afectados se renderiza dinámicamente *filtrado de forma exclusiva* para listar solo los equipos que fueron prestados en ese `planId`. Se modificó la persistencia para que el Involucrado sea guardado como texto libre (Estudiante/Docente) alineándose con los excels originales.

---

## 3. Estado Actual: Errores Conocidos y Tareas Pendientes (Bugs)

El flujo está construido y es funcional a nivel base de datos, pero el equipo debe resolver los siguientes bugs visuales/lógicos presentes en el Paso 1:

1.  **Comboboxes de Materia y Grupo Vacíos:** El despliegue de `Subjects` (Materias) y `Groups` (Grupos Paralelos) no está funcionando correctamente. Actualmente la lista desplegable aparece sin opciones. Esto se debe a que las tablas carecen de datos semilla en el ambiente de desarrollo, o el `PageModel` requiere una inyección explícita del `SelectList` en caso de un refresco de la vista. Se requiere insertar *Seed Data* o script SQL para poblar estas tablas.
2.  **Filtro de Docentes (Rol Erróneo):** El combobox "Docente Responsable" está trayendo a **todos** los usuarios de la base de datos (incluyendo a cuentas de Administrador Sistema). El filtro LINQ en el `PageModel` debe ser corregido para filtrar exclusivamente por la lógica de negocio que determine qué es un docente (ej. `Role == Supervisor` o mapear a un Cargo en específico) y excluir explícitamente a los administradores de la lista.

---

## 4. Propuestas de Mejora (UX/UI)

Para futuras iteraciones, se recomienda implementar las siguientes mejoras en la experiencia de usuario:

1.  **Búsqueda Predictiva (Select2/Choices.js):** En los pasos 1 y 2, los dropdowns (Materia, Docente, Utensilio) se volverán inmanejables al escalar. Implementar una búsqueda con autocompletado mejorará drásticamente la agilidad.
2.  **Alertas con SweetAlert (Toast Notifications):** Reemplazar los banners Bootstrap tradicionales de validación por notificaciones flotantes tipo Toast (ej. SweetAlert2 o Toastr) para un *feedback* más limpio (ej. "Ítem agregado", "Cantidad insuficiente").
3.  **Mini-Calendario Visual en Paso 1:** En lugar de una simple tabla en el modal de "Buscar Planificación", integrar una vista de calendario mensual (ej. FullCalendar) para ver las reservas distribuidas temporalmente.
4.  **Confirmaciones Inline:** En el Paso 4 (Devolución), agregar pequeños checkmarks animados u opacidad reducida a la fila (CSS) en tiempo real cuando un usuario devuelve el inventario completo, brindando una confirmación visual instantánea.
5.  **Persistencia en Modal L-5:** En vez de redirigir a una página nueva para el registro del incidente L-5 (Paso 5), cargar el formulario en un Modal o *Side-panel* sobre el mismo Paso 4 para no romper la inmersión de quien revisa el inventario.
