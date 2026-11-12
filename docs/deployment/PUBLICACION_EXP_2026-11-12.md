# Publicación de prueba del circuito docente — 2026-11-12

Publicación de la rama experimental `exp/docente-v13` en el sitio de QA para que el equipo la pruebe desde el navegador
o el teléfono. **No es la rama `qa`:** si la prueba no convence, se vuelve a publicar `qa` y se revierten los cambios
de base indicados abajo.

| Dato | Valor |
|---|---|
| Commit publicado | `24a49d9` (`origin/exp/docente-v13`), tag `publicacion-exp-20261112-1` |
| Sitio | `site94647` · https://losherederosgastro.runasp.net |
| Paquete | `dotnet publish -c Release -r win-x86` desde un worktree limpio del commit subido |
| Subida | `Tools/Deploy/Publicar-MonsterAsp.ps1`: 6 agregados, 2447 actualizados, **0 borrados** (simulación previa con `-WhatIf`) |

## Base de datos `db70630`

- Ya tenía la migración de Danilo `20261109172523_TeacherRequestsAndIntervalStock`.
- Se aplicó `20261110201202_PlanningAssignmentsAndConsumables`: es aditiva (tabla `PlanningAssignments` y columna
  `DepartureItems.OriginLaboratoryId` que admite nulos). Antes de aplicarla, la nube tenía 0 salidas registradas y 0 solicitudes en revisión.
- Se marcaron como **Consumible** (`ArticleType = 3`) los 16 artículos de la categoría «Material operativo». El valor
  anterior era nulo en los 16 (lista guardada fuera del repositorio, en `BackupsUnivalle\PUBLICACION_EXP_20261112`).
- No se pudo sacar un `.bak` desde aquí (se hace en el panel). Por eso se eligieron solo cambios reversibles.

## Verificación

- `/Login` responde 200; las rutas nuevas (`/Docente`, `/LaboratoryOperations/Solicitudes`) piden inicio de sesión.
- Los archivos publicados contienen los cambios de las tres fases (asistente docente, reparto por origen e inicio desde el cronograma).
- Antes de publicar: 508/508 pruebas y recorridos en navegador sobre una copia local (flujo 12/12, casos 23/23,
  recorrido documentado de 30 pasos).

## Cómo probar

Entrar con una cuenta SuperAdmin y crear en Usuarios › Nuevo un docente vinculado a una persona con grupos en 2026-2
(por ejemplo Henny Villegas o Pedro Guereca). Las solicitudes admiten fechas desde el 16/11/2026 hasta el 17/12/2026.
El recorrido completo está en el informe `Informe_circuito_docente.pdf`.

## Volver atrás

1. Publicar de nuevo el tag `publicacion-qa-20261104-1` (`qa`) con el mismo script.
2. En la base: `dotnet ef database update 20261109172523_TeacherRequestsAndIntervalStock` contra `db70630`, que quita
   la tabla y la columna nuevas, y `UPDATE Articles SET ArticleType = NULL` sobre los 16 artículos de la lista guardada.
