# Resumen De Publicacion

Este resumen reemplaza las guias antiguas que contenian credenciales reales.

## Politica

- No versionar passwords.
- No versionar strings de conexion reales.
- No versionar credenciales FTP.
- Usar placeholders en documentacion y secrets fuera del repositorio.

## Artefactos

- Publish framework-dependent: `./publish`.
- Publish self-contained: `./publish-selfcontained`.
- Logs: revisar ubicacion configurada por el servicio/hosting.

## Datos Requeridos Fuera Del Repo

- Host de BD.
- Nombre de BD.
- Usuario de BD.
- Password de BD.
- Host/usuario/password de transferencia si aplica.
- Ruta final del servicio.
