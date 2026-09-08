# QA-DATA-001 — Base sintetica reproducible

## Estado

Completado el 2026-09-04 sobre SQL Server local.

- Fuente consultada en modo de solo lectura:
  DB_Laboratorios_Univalle.
- Destino independiente:
  DB_Laboratorios_Univalle_SCENARIOS_QA.
- MonsterASP y la base remota no fueron utilizados.
- La aplicacion web no fue iniciada.
- Los lotes QA-DATA-001-FOUNDATION-V1 y QA-DATA-001-FUNCTIONAL-V1
  identifican los datos sinteticos. Estos datos nunca se promueven a una base
  de negocio.

## Datos disponibles

| Entidad | Filas QA |
|---|---:|
| Catalogos | 107 |
| Unidades fisicas | 556 |
| Usuarios Identity | 3 |
| Actores | 30 |
| Articulos | 30 |
| Gestiones | 7 |
| Decisiones de clasificacion | 107 |
| Verificaciones | 1.668 |
| Fallas de verificacion | 669 |
| Solicitudes | 669 |
| Relaciones solicitud-unidad | 834 |
| Mantenimientos | 333 |
| Relaciones mantenimiento-solicitud | 498 |
| Participantes de mantenimiento | 444 |
| Tareas de mantenimiento | 999 |
| Costos | 834 |
| Historiales de estado | 1.668 |
| Salidas L-3 | 111 |
| Detalles de salida | 222 |
| Planes de mantenimiento | 278 |
| Planes de gestion | 1.668 |

La semilla cubre ciclos 2025-II, 2026-I y 2026-II, combinaciones de estados,
prioridades, mantenimiento interno/externo, solicitudes con varias unidades,
mantenimientos con varias solicitudes y participantes, costos con padre
exclusivo, salidas patrimoniales y consumibles.

## Evidencia de integridad y rendimiento

- Las 11 migraciones del modelo estan aplicadas en la base QA.
- La segunda ejecucion de la semilla no produjo filas adicionales.
- DBCC CHECKDB y DBCC CHECKCONSTRAINTS: aprobados.
- FK y CHECK deshabilitados o no confiables: cero.
- Inventarios y codigos de catalogo duplicados: cero.
- Pruebas negativas aprobadas:
  costo sin padre exclusivo, detalle L-3 sin referencia exclusiva,
  inventario repetido y relacion solicitud-unidad repetida.
- Rollback transaccional y control de concurrencia RowVersion: aprobados.
- Cinco lecturas concurrentes: aprobadas.
- Cada consulta de rendimiento se midio con tres calentamientos y veinte
  iteraciones. El peor P95 observado fue 11,926 ms, por debajo del presupuesto
  de 750 ms para operacion y 2.000 ms para reporte.

Evidencia principal:

- outputs/qa-data-001/qa-data-001-manifest.json
- outputs/qa-data-001/qa-data-001-benchmark.json

## Backup restaurable

- Archivo:
  C:\ProgramData\LaboratoriosUnivalle\QA-DATA-001\Backups\DB_Laboratorios_Univalle_SCENARIOS_QA_20260904_052012.bak
- SHA-256:
  F8C18BB976DE8F2CC8CFA24DC827B32D5859D3CC2B45B1E3F3667B9A6D4FAF1D
- Tamano: 2.336.768 bytes.
- RESTORE VERIFYONLY: aprobado.
- Restauracion real en
  DB_Laboratorios_Univalle_SCENARIOS_RESTORE_QA: aprobada.
- Los conteos restaurados coincidieron y la base temporal se elimino despues
  de la comprobacion.

## Herramienta y operacion

La herramienta reproducible esta en Tools/DatabaseQaScenario.

    dotnet run --project Tools/DatabaseQaScenario --no-build -- plan --profile functional
    dotnet run --project Tools/DatabaseQaScenario --no-build -- verify --profile functional
    dotnet run --project Tools/DatabaseQaScenario --no-build -- benchmark --profile functional
    dotnet run --project Tools/DatabaseQaScenario --no-build -- backup --profile functional

El modo all crea, siembra, repite la semilla, verifica, mide, respalda y
restaura. Las escrituras se procesan secuencialmente en grupos de 30 unidades,
por debajo del maximo de 100. Cada operacion vuelve a comprobar memoria y usa
un pool maximo de diez conexiones.

Los usuarios son:

- qa.superadmin
- qa.administrador
- qa.supervisor

La contrasena compartida esta fuera del repositorio, en User Secrets. Para
copiarla al portapapeles sin imprimirla:

    dotnet run --project Tools/DatabaseQaScenario --no-build -- credentials

## Puerta para las pruebas manuales

No iniciar la aplicacion con su configuracion actual: el User Secret principal
puede apuntar a otra base. Cuando el usuario autorice la prueba manual, se debe
inyectar temporalmente una conexion integrada dirigida exactamente a
DB_Laboratorios_Univalle_SCENARIOS_QA, conservar AutoMigrate=false y
RunSeed=false, iniciar un solo servidor y ejecutar la matriz de navegacion con
la tarea QA, navegacion y regresiones.

El perfil de estres no fue ejecutado: continua requiriendo autorizacion
especifica y al menos 6 GB de RAM libres.
