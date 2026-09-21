# @lu/api-client

Cliente HTTP ESM compartido por React para consumir la API versionada.

- Usa `credentials: include`; las sesiones nunca se exponen a JavaScript.
- Obtiene y conserva en memoria el token CSRF de doble envío.
- Por defecto usa mismo origen, adecuado para un despliegue BFF/proxy.
- Solo admite HTTPS, excepto loopback local.
- No acepta connection strings, hosts de tenant, secretos ni writer labels.
