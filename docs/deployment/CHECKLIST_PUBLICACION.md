# Checklist De Publicacion

**Estado:** lista de verificaciones futuras, no constancia de que se hayan ejecutado. Ver `docs/deployment/README.md` e `INICIO_AGENTE_MONSTERASP.md`.

## Destino y seguridad (antes de cualquier escritura externa)

- [ ] Contraseña expuesta en captura/chat y credenciales antiguas que aparecen en historia Git **rotadas** y distribuidas fuera del repositorio.
- [ ] Confirmado el **nombre fisico** de la nueva base; login, nombre de base y hostname son conceptos distintos. Verificados host interno y puerto del panel sin usar una IP fija.
- [ ] Confirmados plan IIS/Hosting Bundle .NET 9, x64, dominio/HTTPS, acceso del sitio al SQL interno, compatibilidad SQL y soporte de configuracion privada.
- [ ] Destino de cualquier herramienta administrativa comprobado contra host **y** base; backup restaurable y estado de `__EFMigrationsHistory` revisados. Produccion/migraciones/creacion de usuarios solo con mision y autorizacion especificas.
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
