# Checklist De Publicacion

**Estado:** panel muestra el BAK subido y Restore `Done`; DNS/TCP remoto pasan. User Secrets sigue apuntando al SQL local, por lo que la autenticación en nube y verificación SQL de los datos restaurados están pendientes. Esto no es aprobación de producción. Ver `docs/deployment/README.md` e `INICIO_AGENTE_MONSTERASP.md`.

## Destino y seguridad (antes de cualquier escritura externa)

- [ ] Contraseña expuesta en captura/chat y credenciales antiguas que aparecen en historia Git **rotadas** y distribuidas fuera del repositorio.
- [x] El panel muestra base `db70630`, Local access interno y Remote access público; el login es un campo distinto del nombre de base.
- [x] El panel muestra un backup `SAFETY` creado antes de la restauración. Aún no se ha descargado y verificado su capacidad de recuperación.
- [x] El panel muestra el BAK local subido y Restore `Done` para `db70630`; falta cotejar por SQL el contenido/migraciones.
- [ ] Confirmados plan IIS/Hosting Bundle .NET 9, x64, dominio/HTTPS, acceso del sitio al SQL interno, compatibilidad SQL y soporte de configuracion privada.
- [ ] Destino de cualquier herramienta administrativa comprobado contra host **y** base; comparar tablas, conteos e `__EFMigrationsHistory` con el origen. Producción/migraciones/creación de usuarios solo con misión y autorización específicas.
- [ ] Establecidas `ASPNETCORE_ENVIRONMENT=Production` y `ConnectionStrings__DefaultConnection` **fuera de Git**. `Database__AutoMigrate=false` y `Database__RunSeed=false`.
- [ ] Certificado SQL y opcion TLS verificados con el proveedor; no desactivar validacion del certificado por conjetura.
- [ ] Definido primer SuperAdmin en base nueva: `Tools/AdminBootstrap` requiere esquema completo, host/base declarados y ejecucion desde red que alcance el SQL interno. **No** usar la semilla demo en Production.

## Paquete y almacenamiento (no ejecutado hasta nueva mision de despliegue)

- [ ] Cambios locales sin commit revisados y origen exacto del paquete documentado; no incluir `Tools/SyncCloudDb` ni su historial como estrategia de migracion.
- [ ] `dotnet build -m:1` y `dotnet publish` Release verificados sobre el estado seleccionado.
- [ ] Paquete incluye `web.config` generado, DLL principal, `wwwroot/templates` y assets, pero excluye `opencode.json`, claves locales, backups y secretos.
- [ ] No usar perfiles FTP/WebDeploy del sitio antiguo para el destino nuevo; `DeleteExistingFiles` deshabilitado y persistencia de uploads/llaves acordada.
- [ ] Identidad IIS con permisos minimos de escritura para `DataProtectionKeys/` y `wwwroot/uploads/{equipment,users}/`; backup de imagenes y claves preparado.
- [ ] Servicio detenido/controlado antes de reemplazar archivos, y reiniciado tras inyectar configuracion privada.
- [ ] Logs de ANCM/IIS y consola revisados sin exponer contraseñas.
- [ ] HTTPS, login, roles, wizard, subida de imagen, L-3 y reportes principales probados con un caso pequeño en destino autorizado; evidencia y reversión registradas.
