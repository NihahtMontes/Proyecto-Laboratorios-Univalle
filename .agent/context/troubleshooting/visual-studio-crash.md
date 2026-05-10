# Troubleshooting Visual Studio Crash

## Sintoma

La app termina con codigo `-1 (0xffffffff)` al usar file picker o depurar desde Visual Studio.

## Causa Diagnosticada

Visual Studio Browser Link/Hot Reload puede inyectar WebSocket y matar Kestrel cuando el dialogo de archivos congela JS.

## Mitigaciones

- Ejecutar con `dotnet run` desde terminal para confirmar si el bug es Visual Studio.
- Mantener Browser Link desactivado en configuracion.
- En Visual Studio, desactivar Hot Reload y Diagnostic Tools si sigue fallando.
- Evitar diagnosticar como bug de Razor si solo ocurre en debugger.

## Build Seguro

Usar salida temporal:

```bash
dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\build-check"
```

## Archivos Locales

- `.vs/` puede quedar bloqueada por Visual Studio.
- `DataProtectionKeys/`, logs, `bin/`, `obj/` no deben versionarse.
