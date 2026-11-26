# Publicación del Sprint 3 con la base EXP — 2026-11-25

Publicación de la rama `sprint-3` en el sitio de MonsterASP junto con una copia completa de la base de
desarrollo **EXP**. Por decisión del equipo, la nube queda con EXP (la versión más completa del Sprint 3)
aunque no sea la base oficial importada del Excel.

| Dato | Valor |
|---|---|
| Commit publicado | `0a868da` (`sprint-3`) |
| Sitio | `site94647` · https://losherederosgastro.runasp.net |
| Paquete | `dotnet publish -c Release -r win-x86` desde un worktree limpio del commit |
| Subida | `Tools/Deploy/Publicar-MonsterAsp.ps1`: simulación previa con `-WhatIf`, luego 2487 archivos, **0 borrados** |

## Base de datos `db70630`

| | Antes | Después |
|---|---|---|
| Migraciones | 34 (última `PlanningAssignmentsAndConsumables`) | 38 (última `20261124121019_WidenPlanningReviewActions`) |
| Usuarios | 4 | 58 (los de EXP, con sus mismas contraseñas) |

- Respaldo de origen: `.bak` `COPY_ONLY`, comprimido y con `CHECKSUM` de `DB_Laboratorios_Univalle_EXP`,
  verificado con `RESTORE VERIFYONLY`. Se guarda fuera del repositorio, en `BackupsUnivalle\NUBE_EXP_20261125`.
- La restauración se hizo desde el panel de MonsterASP (Backups → Upload → Restore). Antes se descargó un
  backup de la nube como estaba.
- **No** se ejecuta `dotnet ef database update` contra la nube: el respaldo ya trae las 38 migraciones.
- Los usuarios de prueba viejos de EXP (`docente.e2e…`, `….old…`) quedan inactivos.

## Correcciones visuales incluidas en esta publicación

1. **Campana de avisos en el teléfono:** el panel se salía por la izquierda de la pantalla. Ahora mide como
   máximo 400 px y, por debajo de 768 px, se fija a la pantalla con 16 px a cada lado.
2. **Menú mini (solo íconos):** mostraba los íconos de todas las secciones aunque estuvieran plegadas, así que
   no coincidía con el menú completo. Ahora cada sección tiene su propio ícono, y al hacer clic se abre o se cierra.
   El plegado es el mismo en los dos modos.
3. **Inicio:** al expandir el menú, la barra de pestañas (Resumen, Prácticas, Bandeja…) quedaba por encima de él.
   Ahora queda por debajo (z-index 5 frente al 10 del menú).

## Verificación

- `/Login` responde 200, y el sitio sirve el CSS nuevo del menú.
- La nube tiene 38 migraciones y 58 usuarios (consulta de solo lectura).
- Las correcciones se probaron en el navegador sobre una instancia local contra EXP: a 390 px el panel de
  avisos queda entre 16 y 374 px; a 1366 px mide 400 px; el menú mini de Inicio muestra Inicio, las 5
  secciones y Ayuda; al expandirlo, las pestañas quedan por debajo.

## Volver atrás

- **Base:** restaurar desde el panel el backup de la nube que se descargó antes de la restauración.
- **Web:** publicar de nuevo el commit anterior con el mismo script, desde un worktree limpio.
