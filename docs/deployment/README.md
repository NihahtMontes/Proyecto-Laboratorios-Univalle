# Despliegue

Guia sanitizada para preparar el sistema. Las credenciales reales deben vivir fuera del repositorio. Para iniciar otro agente, copiar `PROMPT_AGENTE_MONSTERASP.md` y consultar `INICIO_AGENTE_MONSTERASP.md` en la raiz.

## Nuevo destino MonsterASP: comprobar antes de operar

El panel confirma base **`db70630`**, Local access `db70630.databaseasp.net:1433` y Remote access `db70630.public.databaseasp.net:1433`. El origen local tiene 11 migraciones. La captura muestra `.bak` subido, Restore **Done** y backup `SAFETY` previo. En esta sesión DNS/TCP público pasan; autenticación SQL y cotejo de filas/migraciones del destino siguen pendientes. Rota la contraseña expuesta en capturas.

La configuracion de la aplicacion se toma de `ConnectionStrings:DefaultConnection` (`Program.cs`), no de un host codificado en C#. El `appsettings.json` local versionable tiene esa clave vacia: al arrancar sin un valor externo la aplicacion falla de manera intencional. En el alojamiento establecer **`ConnectionStrings__DefaultConnection`** por un mecanismo privado del panel/IIS; no guardar su valor en `.md`, `.pubxml`, `.env` versionado, shell history o argumentos de CLI.

## Precondiciones

- Backup reciente de la base de datos.
- Runtime .NET 9 instalado si el publish es framework-dependent.
- Confirmar plan IIS/ASP.NET Core Hosting Bundle 9, arquitectura Windows x64, dominio y certificado HTTPS del sitio.
- Habilitar Remote access para administrar desde PC; su endpoint difiere del interno. Verificar que se selecciona `db70630`, contenido/estado, cuota y `__EFMigrationsHistory`; descargar un backup recuperable del destino antes de reemplazarlo.
- El sitio debe tener acceso de red al servidor SQL interno, y el panel debe admitir inyeccion privada de `ConnectionStrings__DefaultConnection`.
- Si se necesita ejecutar herramientas EF/Bootstrap en la nube, hacerlo desde un entorno con acceso autorizado al host SQL interno y verificar el destino antes de cualquier escritura.
- Asegurar escritura y permanencia de `DataProtectionKeys/` y `wwwroot/uploads/{equipment,users}/` entre publicaciones.

## Publicacion Recomendada

Framework-dependent:

```bash
dotnet publish "Proyecto Laboratorios Univalle.csproj" -c Release -r win-x64 --self-contained false -o ./publish
```

Self-contained:

```bash
dotnet publish "Proyecto Laboratorios Univalle.csproj" -c Release -r win-x64 --self-contained true -o ./publish-selfcontained
```

## Configuracion Produccion

- `ASPNETCORE_ENVIRONMENT=Production`.
- `Database__AutoMigrate=false` y `Database__RunSeed=false`. La aplicacion prohibe ambas operaciones automaticas fuera de Development.
- Desarrollo local usa el endpoint público **Remote access**; hostname interno `db70630.databaseasp.net` es para webs alojadas dentro de MonsterASP. En este equipo mantener `ConnectionStrings:DefaultConnection` solo en User Secrets (o env local); no usar la cadena interna para la PC.
- Cuenta SQL para la web con privilegios minimos; identidad administrativa separada para aplicar migraciones **solo bajo una mision de produccion autorizada**, respaldo comprobado y destino validado. No usar LocalDB ni SQL Express como sustitutos silenciosos.
- HTTPS con certificado/binding valido; si un proxy termina TLS, verificar en el hosting si `UseHttpsRedirection()` percibe correctamente el esquema (no hay configuracion explicita de forwarded headers).
- Revisar `AllowedHosts` (hoy `*`) y captura de logs de consola/IIS; no habilitar logs de inicio permanentes con informacion sensible.
- Verificar licencias EPPlus `NonCommercial` y QuestPDF `Community` para el uso real.

## Connection String

La aplicación corriendo dentro de MonsterASP usa la cadena de **Local access** del panel, configurada privadamente en IIS. Formato sin secreto:

```text
Server=db70630.databaseasp.net;Database=db70630;User Id=[LOGIN];Password=[SECRETO_ROTADO];Encrypt=False;MultipleActiveResultSets=True
```

Para desarrollar o administrar desde el PC, usar el endpoint **Remote access** que aparece en el panel. Formato de referencia sin credenciales:

```text
Server=db70630.public.databaseasp.net,1433;Database=db70630;User Id=[LOGIN];Password=[SECRETO_ROTADO];Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

MonsterASP recomienda `Encrypt=True;TrustServerCertificate=True` para conexiones remotas porque su certificado no es de una autoridad pública. Para la web dentro de MonsterASP, se usa el endpoint **Local access** interno y el proveedor recomienda no añadir cifrado extra. User Secrets es solo de desarrollo, no se instala automáticamente en IIS.

Alternativa con autenticacion de Windows **solo si el proveedor la soporta** (no asumirla para esta base alojada):

```text
Server=[SERVIDOR_AUTORIZADO];Database=[DB_VERIFICADA];Trusted_Connection=True;Encrypt=True;MultipleActiveResultSets=True
```

## Migraciones y primer usuario (no se ejecutan en esta guia)

`Data/SqlServerMigrations/` conserva las migraciones EF; la fuente local tiene 11 aplicadas hasta `20260830202548_AddEquipmentClassificationGovernance`. El panel indica Restore `Done` y conserva backup `SAFETY` previo. Aún falta consultar por SQL la nube y cotejar `__EFMigrationsHistory`, filas y datos. El `.bak` pasó `RESTORE VERIFYONLY` y una restauración local temporal pasó `DBCC CHECKDB`. `Program.cs` no migra automáticamente en Production.

## Copiar la base local a MonsterASP

MonsterASP documenta carga directa de `.bak`: Panel → Databases → seleccionar `db70630` → Backups management → backup del destino → Upload Backup → `.bak` del origen → Restore. El panel reporta que esta restauración terminó `Done`; mantiene además un backup `SAFETY` anterior. Restore reemplaza la base seleccionada.

El `.bak` de transferencia debe ser COPY_ONLY con checksum, compresión y `RESTORE VERIFYONLY`. El backup automático local está cifrado con un certificado privado local y **no debe subirse** sin importar el certificado/clave compatible: el backup de transferencia creado por este procedimiento no usa ese cifrado. La base contiene información sensible: conserva el archivo en la carpeta protegida, cárgalo por el panel oficial, comprueba la restauración y elimina la copia temporal solo al completar la validación y retención requerida.

`Tools/AdminBootstrap` crea el primer SuperAdmin **después** del esquema; el login de la web no lo crea. Por defecto acepta solo `localhost` o `.` con `DB_Laboratorios_Univalle`. Para un destino remoto exige `--expected-server` y `--expected-database` coincidentes exactamente con la conexión. El `.bak` completo incluye las 11 migraciones aplicadas; después de restaurarlo, comprueba el historial en nube. Para `--check` desde el PC se requiere Remote access habilitado y el endpoint remoto del panel (no el host interno):

```text
dotnet run --project Tools/AdminBootstrap -- --expected-server "[REMOTE_SERVER,PORT]" --expected-database "db70630" --check
```

`--check` **si conecta** para comprobar migraciones; no ejecutarlo desde un equipo sin acceso al host interno. La creacion real del usuario es una escritura de produccion y requiere autorizacion especifica, verificacion previa de destino y datos del administrador; la contrasena se introduce de manera interactiva. `Tools/SyncCloudDb` fue retirado del arbol actual: borraba tablas de nube y contenia credenciales; **sigue existiendo en la historia Git**. Tratar las credenciales antiguas como comprometidas y rotarlas.

## Publicacion y almacenamiento

El perfil `Properties/PublishProfiles/FTPProfile1.pubxml` contiene datos de un sitio anterior: **no usar ese perfil** para la base/sitio nuevos. Se ha desactivado su eliminacion de archivos para evitar borrar `wwwroot/uploads` de forma accidental. Preferir la publicacion local en carpeta, verificar `web.config` generado y subir solo el contenido publicado mediante el mecanismo autorizado por el nuevo hosting. `Proyecto Laboratorios Univalle.csproj` excluye `opencode.json` del contenido web; comprobarlo tambien en el paquete final. Preservar uploads y claves Data Protection; no copiar claves ni backups locales del desarrollador. Confirmar en el panel permisos de escritura, dominio, certificado, nombres de bases y versiones de runtime.

## Verificacion

1. Resolver el estado del arbol Git sucio y validar `dotnet build -m:1`; no desplegar accidentalmente cambios sin revisar.
2. Tras autorizacion para produccion, comprobar backup restaurable, destino SQL y migraciones antes de modificar base o usuarios.
3. Generar `publish` en carpeta temporal y verificar que incluya `web.config`, `wwwroot` y plantillas Excel; no se ha ejecutado aqui.
4. Configurar valores secretos fuera del repositorio, desplegar sin borrar uploads ni claves, comprobar logs y estado del proceso.
5. Probar login, wizard, carga de imagenes, reportes y conexion BD contra el destino aprobado, registrar evidencia.
