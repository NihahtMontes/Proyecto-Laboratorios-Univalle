# Area Infraestructura

## Build

El build normal puede fallar si la app esta corriendo o hay bloqueo de archivos. Para verificar cambios usar salida temporal:

```bash
dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\build-check"
```

Warnings de nullability existentes no bloquean si no fueron introducidos por el cambio.

## Desarrollo Local

- Preferir `dotnet run` cuando Visual Studio debugger cause problemas.
- Browser Link debe estar desactivado.
- `DataProtectionKeys/` es artefacto local y no debe versionarse.
- `.vs/`, `bin/`, `obj/`, logs y outputs son basura local.

## Configuracion

- Connection strings reales van en `appsettings.Development.json`, user-secrets o variables de entorno.
- No versionar credenciales.
- Produccion debe usar `Include Error Detail=false`.

## Despliegue

La documentacion humana de despliegue vive en `docs/deployment/` y debe usar placeholders, no passwords reales.

## PostgreSQL

- Produccion usa PostgreSQL local en el servidor de la universidad.
- Migraciones se aplican al arrancar con `Database.MigrateAsync()` si el flujo actual se mantiene.
- Hacer backup antes de migraciones de produccion.
