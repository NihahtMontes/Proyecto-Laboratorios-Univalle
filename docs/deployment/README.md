# Despliegue

Guia sanitizada para preparar el sistema. Las credenciales reales deben vivir fuera del repositorio. Para iniciar otro agente, copiar `PROMPT_AGENTE_MONSTERASP.md` y consultar `INICIO_AGENTE_MONSTERASP.md` en la raiz.

## Nuevo destino MonsterASP: comprobar antes de operar

El responsable mostro el **hostname interno** `db70630.databaseasp.net` (SQL Server, puerto `1433`). Solo funciona desde sitios alojados en la red del proveedor; **no suponer que responde desde el equipo local**. El login mostrado es un usuario SQL, **no demuestra el nombre fisico de la base**: verificar este ultimo en el panel del proveedor o con la autoridad de datos antes de configurar una cadena, aplicar migraciones o crear usuarios. No usar una IP fija, no reutilizar la conexion de la base anterior ni copiar al repositorio la contrasena de una captura o chat. Como esa contrasena ya fue publicada en una conversacion/captura, rotarla en el panel y distribuir la nueva solo por canal seguro.

La configuracion de la aplicacion se toma de `ConnectionStrings:DefaultConnection` (`Program.cs`), no de un host codificado en C#. El `appsettings.json` local versionable tiene esa clave vacia: al arrancar sin un valor externo la aplicacion falla de manera intencional. En el alojamiento establecer **`ConnectionStrings__DefaultConnection`** por un mecanismo privado del panel/IIS; no guardar su valor en `.md`, `.pubxml`, `.env` versionado, shell history o argumentos de CLI.

## Precondiciones

- Backup reciente de la base de datos.
- Runtime .NET 9 instalado si el publish es framework-dependent.
- Confirmar plan IIS/ASP.NET Core Hosting Bundle 9, arquitectura Windows x64, dominio y certificado HTTPS del sitio.
- Confirmar nombre fisico, estado y compatibilidad SQL de la base nueva; backup recuperable y estado de `__EFMigrationsHistory` antes de preparar el esquema.
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
- Cuenta SQL para la web con privilegios minimos; identidad administrativa separada para aplicar migraciones **solo bajo una mision de produccion autorizada**, respaldo comprobado y destino validado. No usar LocalDB ni SQL Express como sustitutos silenciosos.
- HTTPS con certificado/binding valido; si un proxy termina TLS, verificar en el hosting si `UseHttpsRedirection()` percibe correctamente el esquema (no hay configuracion explicita de forwarded headers).
- Revisar `AllowedHosts` (hoy `*`) y captura de logs de consola/IIS; no habilitar logs de inicio permanentes con informacion sensible.
- Verificar licencias EPPlus `NonCommercial` y QuestPDF `Community` para el uso real.

## Connection String

Usar configuracion segura del hosting. Ejemplo de **formato solamente**, nunca con credenciales verdaderas:

```text
Server=tcp:db70630.databaseasp.net,1433;Database=[NOMBRE_FISICO_VERIFICADO];User Id=[LOGIN];Password=[SECRETO_ROTADO];Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True
```

`TrustServerCertificate=False` requiere una cadena de certificados valida para ese hostname; confirmar el requisito de TLS con el proveedor. Si el proveedor exige otra opcion, registrarla en el gestor privado de configuracion, no debilitar la validacion por conjetura. El hostname interno **no es** un endpoint SQL publico para ejecutar comandos desde la maquina del desarrollador. User Secrets es una facilidad de desarrollo y no se instala automaticamente en IIS.

Alternativa con autenticacion de Windows **solo si el proveedor la soporta** (no asumirla para esta base alojada):

```text
Server=[SERVIDOR_AUTORIZADO];Database=[DB_VERIFICADA];Trusted_Connection=True;Encrypt=True;MultipleActiveResultSets=True
```

## Migraciones y primer usuario (no se ejecutan en esta guia)

`Data/SqlServerMigrations/` conserva las migraciones EF; el ultimo archivo por identificador es `20260830202548_AddEquipmentClassificationGovernance`. No equivale a afirmar que se aplico en la base nueva. `Program.cs` no crea la base automaticamente en Production. Antes de aplicar cualquier cambio a la nube, verificar respaldo, permisos de una cuenta administrativa, nombre fisico y estado de migraciones desde un entorno que realmente alcance el SQL interno.

`Tools/AdminBootstrap` crea el primer SuperAdmin **despues** del esquema; el login de la web no lo crea. Por defecto esta herramienta acepta solo `localhost` o `.` con `DB_Laboratorios_Univalle`. Para un destino remoto exige simultaneamente `--expected-server` y `--expected-database`, que deben coincidir exactamente con `DataSource` e `InitialCatalog` de la conexion configurada. Ejemplo de argumentos **sin credenciales**:

```text
dotnet run --project Tools/AdminBootstrap -- --expected-server "tcp:db70630.databaseasp.net,1433" --expected-database "[NOMBRE_FISICO_VERIFICADO]" --check
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
