# Despliegue

Guia sanitizada para publicar el sistema. Las credenciales reales deben vivir fuera del repositorio.

## Precondiciones

- Backup reciente de la base de datos.
- Runtime .NET 9 instalado si el publish es framework-dependent.
- PostgreSQL configurado en el servidor de produccion.
- Variables de entorno o secrets configurados para connection string.

## Publicacion Recomendada

Framework-dependent:

```bash
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```

Self-contained:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-selfcontained
```

## Configuracion Produccion

- `ASPNETCORE_ENVIRONMENT=Production`.
- `Include Error Detail=false`.
- `DetailedErrors=false`.
- Usuario PostgreSQL dedicado, no superusuario.
- HTTPS obligatorio si se expone fuera de red local.

## Connection String

Usar variables de entorno o user-secrets. Ejemplo con placeholders:

```text
Host=[DB_HOST];Port=5432;Database=[DB_NAME];Username=[DB_USER];Password=[DB_PASSWORD];Include Error Detail=false
```

## Verificacion

1. Publicar en carpeta temporal.
2. Copiar artefactos al servidor.
3. Reiniciar servicio.
4. Revisar logs.
5. Probar login, wizard, reportes y conexion BD.
