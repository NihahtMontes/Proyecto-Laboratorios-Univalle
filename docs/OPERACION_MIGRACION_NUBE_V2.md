# Operacion de migracion V2 en la nube

## Estado al 27-08-2026

- Destino: `db65393` en MonsterASP.NET.
- Produccion conserva siete migraciones aplicadas y dos pendientes:
  `AlignHistoricalWorkbookV2` y `AddHistoricalRequestSuggestion`.
- El BACPAC previo se importo con un nombre nuevo en
  `DB_Laboratorios_Univalle_CLOUD_20260827_1418_QA`.
- El script aprobado tiene SHA-256
  `E641942DB56349034BB9C7E9BBB1C1B95F66C3C8DB14F5628F393641462F47CD`.
- QA paso preflight, `DBCC CHECKDB`, `DBCC CHECKCONSTRAINTS`, postflight,
  pruebas de restricciones y una segunda ejecucion idempotente.
- Produccion **no ha sido modificada**. Falta la ventana de mantenimiento y
  el respaldo final del proveedor.
- La carga de negocio del Excel permanece bloqueada por 45 P0: 41 campos
  obligatorios vacios y cuatro costos sin padre exclusivo. Despues de completar
  localmente el modelo, la auditoria contrato 4 conserva 11 clases de bloqueo
  V2↔SQL, 10 advertencias nullable y una revision por fila del manifiesto
  legacy→V2. Estas no alteran el ensayo de Fase A, pero impiden la Fase B.
- Existe una migracion posterior local,
  `20260828161956_CompleteHistoricalWorkbookV2Model`, que no forma parte del
  script aprobado de esta operacion y no se aplico a QA ni produccion.

## Prioridades

| Prioridad | Accion | Responsable | Estado |
|---|---|---|---|
| P0 - Critica | Poner el sitio en mantenimiento y crear un backup final de `db65393` en MonsterASP.NET | Operador del panel | Pendiente |
| P0 - Critica | Repetir preflight y confirmar exactamente dos migraciones pendientes, cero bloqueos y cero objetos V2 parciales | Responsable tecnico | Pendiente |
| P0 - Critica | Aplicar exclusivamente el script aprobado y ejecutar postflight | Responsable tecnico | Pendiente |
| P1 - Alta | Probar login, equipos, solicitudes, mantenimientos, salidas, dashboard y reportes | Responsable funcional | Pendiente |
| P1 - Alta | Observar logs de aplicacion y SQL por 30 minutos | Operador + responsable tecnico | Pendiente |
| P0 - Critica de datos | Reducir las 45 incidencias P0 del Excel a cero | Cliente + responsable de datos | Pendiente; no bloquea el esquema |

## Paso manual del operador en MonsterASP.NET

Codex no opera el navegador ni solicita credenciales. El operador realiza
estos pasos en `https://admin.monsterasp.net`:

1. Abrir **Websites** y ubicar el sitio que usa `db65393`.
2. Crear en la raiz publicada un `app_offline.htm` menor de 512 KB con un
   mensaje de mantenimiento. IIS detiene la aplicacion mientras exista ese
   archivo. Verificar desde una ventana privada que el sitio ya no permite
   nuevas escrituras.
3. Anotar la hora local exacta en que comenzo el mantenimiento.
4. Abrir **Databases**, seleccionar `db65393` y entrar a
   **Backups management**.
5. Seleccionar **Create Backup**, esperar a que termine y descargar el archivo.
   No restaurar ni reemplazar la base desde el panel.
6. Conservar el identificador/nombre del backup y su hora. Calcular SHA-256 al
   archivo descargado con:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath "C:\ruta\backup-db65393.bak"
```

7. Informar al responsable tecnico solamente:
   - `app_offline activo`;
   - hora de inicio;
   - nombre o identificador del backup;
   - ruta local y SHA-256 del backup descargado.

No enviar usuario, contrasena, cadena de conexion ni codigos de acceso.

## Preflight y postflight

La conexion se obtiene de User Secrets o de
`ConnectionStrings__DefaultConnection`; nunca se pasa como argumento.

```powershell
dotnet run --project Tools/CloudMigrationGuard -- \
  preflight \
  --output "<EVIDENCIA>\preflight-final.json" \
  --expected-database db65393
```

La salida debe indicar `PREFLIGHT APROBADO`. Si los datos cambiaron desde el
BACPAC ensayado, se exporta otro BACPAC, se importa en otra base `_QA` y se
repite el ensayo antes de produccion.

La ejecucion usa el mismo archivo y exige `QUOTED_IDENTIFIER ON` mediante
`sqlcmd -I`; sin `-I`, SQL Server rechaza el indice filtrado y la transaccion
se revierte.

```powershell
sqlcmd -S "<SERVIDOR>" -d db65393 -b -I \
  -i "<EVIDENCIA>\normalization-v2.idempotent.sql"

dotnet run --project Tools/CloudMigrationGuard -- \
  postflight \
  --baseline "<EVIDENCIA>\preflight-final.json" \
  --output "<EVIDENCIA>\postflight-final.json" \
  --expected-database db65393
```

El postflight exige las dos migraciones, 520 relaciones solicitud-unidad,
121 mantenimiento-solicitud, conteos preexistentes sin reduccion, cero
huerfanos, cero restricciones deshabilitadas/no confiables y
`DBCC CHECKCONSTRAINTS` limpio.

## Reactivacion y observacion

1. Retirar `app_offline.htm` solo despues de un postflight aprobado.
2. Verificar que `Database:AutoMigrate=false` y `Database:RunSeed=false`.
3. Probar los flujos funcionales criticos.
4. Observar durante al menos 30 minutos los logs de MonsterASP.NET y de la
   aplicacion.
5. Si falla la aplicacion pero la integridad SQL es correcta, restaurar la
   version previa de la aplicacion y conservar el esquema aditivo.
6. Si fallan integridad o conteos, mantener el sitio detenido y restaurar el
   backup del proveedor. No ejecutar migraciones `Down` en produccion.

## Carga posterior del Excel

`Tools/HistoricalDataApply` separa la promocion de datos de la migracion de
esquema. `plan` es de solo lectura; `apply` rechaza planes con P0, hashes
distintos, base distinta o SQL destructivo. En QA ejecuta el delta dos veces y
compara conteos y huellas del contenido de 27 tablas. En produccion exige
adicionalmente los hashes del paquete, snapshot y respaldo.

El paquete actual produce un plan bloqueado y `apply` no escribe. Esta es la
conducta esperada. Llegar a P0 = 0 no sera suficiente: tambien deben quedar en
cero los bloqueos semanticos del contrato 4 y aprobarse una decision por cada
fila del manifiesto de reconciliacion de claves legacy→V2. Las dos migraciones de esta operacion
corrigen el esquema inmediato; no cargan ni certifican los datos historicos.

## Migracion posterior DB-002

El contrato completo del Excel requiere una tercera migracion independiente:
`20260828161956_CompleteHistoricalWorkbookV2Model`. Su script idempotente local
tiene SHA-256
`9482A00632F486D073529BA25366807E5DAB54CDB8FE2FD34ECE1F93F18127D9`.
No debe anexarse silenciosamente al script DB-001 ya aprobado. Antes de usarla
se debe generar un BACPAC QA vigente, ensayarla dos veces, ejecutar integridad y
regresion de salidas/notificaciones, y solicitar una autorizacion explicita
separada.
