# Despliegue

Guia sanitizada para publicar el sistema. Las credenciales reales deben vivir fuera del repositorio.

## Precondiciones

- Backup reciente de la base de datos.
- Runtime .NET 9 instalado si el publish es framework-dependent.
- SQL Server completo configurado en el servidor de produccion o en `localhost` para despliegue local.
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
- `DetailedErrors=false`.
- Usuario SQL Server dedicado con permisos minimos necesarios, o autenticacion de Windows si el backend corre con una identidad confiable.
- No usar LocalDB, SQL Express ni `SQLEXPRESS`.
- HTTPS obligatorio si se expone fuera de red local.

## Connection String

Usar variables de entorno, user-secrets o configuracion segura del hosting. Ejemplo con placeholders:

```text
Server=localhost;Database=[DB_NAME];User Id=[DB_USER];Password=[DB_PASSWORD];MultipleActiveResultSets=true;TrustServerCertificate=True
```

Alternativa con autenticacion de Windows:

```text
Server=localhost;Database=[DB_NAME];Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

## Verificacion

1. Publicar en carpeta temporal.
2. Copiar artefactos al servidor.
3. Reiniciar servicio.
4. Revisar logs.
5. Probar login, wizard, reportes y conexion BD.
