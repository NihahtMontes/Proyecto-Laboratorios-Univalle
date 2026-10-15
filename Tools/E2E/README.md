# Prueba de punta a punta (solo lectura)

Verifica que cada pantalla de la app muestre todo lo que hay en `DB_Laboratorios_Univalle_REAL` (cargada desde el Excel V12).

Requisitos: app corriendo en `http://localhost:5000` (`ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build --urls http://localhost:5000`),
`pip install pyodbc`, `npm install` en esta carpeta y `npx playwright install chromium`. La contraseña va por entorno (`E2E_PW`, usuario `E2E_USER`, por defecto `nihaht`); nunca se guarda en archivos.

| Script | Qué comprueba |
|---|---|
| `checks_inventario.py` | Equipos 121, unidades 644, laboratorios, ciudades, personas, usuarios, Kardex/Detalles |
| `checks_procesos.py` | L-6 1.599, L-7 329, L-8 1.209 (costos), gestiones 27, adquisiciones/salidas, L-48 |
| `checks_academico.py` | Artículos 1.374, categorías, unidades de medida, materias, prácticas + recetas 2.111, existencias 700, grupos, planificaciones L-3 1.037 |
| `checks_nuevas.py` | Pantallas añadidas: `/EquipmentUnits` (644), `/Maintenances/Plan` (387), `/Schedules/Practices` (869) |
| `checks_muestra.py` | Muestra aleatoria: los campos importados aparecen en cada detalle (170 comprobaciones) |
| `browser.mjs` | Chromium real: login por interfaz y 49 pantallas, errores de consola/JS/HTTP, capturas en `outputs/e2e/` |

Ejecución: `E2E_PW=... python Tools/E2E/checks_<grupo>.py` y `E2E_PW=... node Tools/E2E/browser.mjs`.
