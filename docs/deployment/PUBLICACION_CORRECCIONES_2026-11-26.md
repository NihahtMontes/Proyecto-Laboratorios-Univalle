# Correcciones publicadas en la nube — 2026-11-26

Fallas que encontró el equipo al probar la nube (25/11, de noche) y las que aparecieron al recorrer todas las
páginas de cada rol sobre una copia de la base. Rama `fix/nube-redirecciones-docentes`, que sale de `master`
(PR #19).

| Dato | Valor |
|---|---|
| Sitio | `site94647` · https://losherederosgastro.runasp.net |
| Publicación 1 (urgente) | `36fee2a`: bucle de redirecciones, campana y Login |
| Publicación 2 | último commit de la rama: docentes nuevos, enlaces por rol, ícono y aviso flotante |
| Paquete | `dotnet publish -c Release -r win-x86` desde un worktree limpio; `Tools/Deploy/Publicar-MonsterAsp.ps1` con simulación previa (0 borrados) |
| Base `db70630` | Sin cambios: no hubo migraciones ni escrituras |

## Lo que se corrigió

### 1. «Demasiadas redirecciones» (ERR_TOO_MANY_REDIRECTS)
- **Síntoma:** las cuentas de «Cierre de práctica» creadas con «Pedir cambio de contraseña en el primer ingreso»
  (marcado por defecto) no podían entrar (dos cuentas de prueba creadas esa noche).
- **Causa:** el cambio obligatorio manda a `/Account/CambiarClave`, pero el rol de cierre no tenía permitida esa
  ruta y lo devolvía a `/Cierre`, así que rebotaba entre las dos.
- **Arreglo:** la ruta se agregó a las permitidas del rol.
- **Prueba:** `RoleScopeRedirectTests` recorre las redirecciones de los 6 roles, con y sin cambio obligatorio,
  en el orden de `Program.cs`. Sin el arreglo, la prueba falla con 6 redirecciones.
- Las cuentas afectadas no necesitan nada: al entrar se les pide la contraseña nueva y luego llegan a Cierre.

### 2. Campana con el menú contraído
- **Causa:** el panel se abría hacia la izquierda desde una campana que está a la izquierda de la barra, y se
  salía de la pantalla.
- **Arreglo:** ahora se abre hacia la derecha. Además, cada aviso recorta su texto con «…» y la lista ya no
  muestra barra horizontal.

### 3. Login cortado abajo
- **Causa:** `body { overflow: hidden }` impedía desplazarse; en laptops y teléfonos el botón podía quedar
  fuera de alcance.
- **Arreglo:** la página se desplaza, la tarjeta se centra y se compacta en pantallas bajas y en el teléfono.

### 4. Crear un docente nuevo
- **Antes:** no se podía desde la web. «Registrar persona» guarda la ficha como «Otro», y la cuenta Docente
  exigía elegir a alguien del catálogo.
- **Ahora:** en Usuarios › Nuevo, rol Docente, se deja «Docente nuevo» y el sistema registra la ficha (categoría
  Docente) con los nombres de la cuenta, en la misma transacción.
- No duplica: si el docente ya está en el catálogo, pide elegirlo.
- La lista muestra los grupos por gestión, por ejemplo «sin grupos en 2026-2 (3 en otras gestiones)».

### 5. Enlaces que terminaban en «Acceso denegado» o rebotaban
- **Supervisor:** el panel de la gestión (desde Reportes), el listado de equipos (desde el detalle de un
  equipo) y el detalle de mantenimientos (desde el detalle de una unidad). Ahora solo los ve un administrador.
- **Rol de cierre:** «Panel» en el L-3 llevaba al panel de planificación; ahora lleva a `/Cierre`.

### 6. Detalles visibles
- El ícono de «Marcar leídas» (`fa-check-double`) no existe en el tema y se veía vacío en 11 pantallas: ahora
  es `mdi-check-all`.
- El aviso «Tienes N avisos que requieren atención» sale abajo a la derecha; arriba tapaba 7 s los botones
  de acción.
- `swal-compat.js` ya no deja advertencias de SweetAlert en la consola.

## Cómo se verificó
- Pruebas automáticas: 755/755 (77 nuevas).
- Copia `DB_Laboratorios_Univalle_QA_FIX` restaurada desde el respaldo de EXP del 25/11 y borrada al terminar.
- Navegador, recorrido real:
  - cuenta de cierre con cambio obligatorio: entra, cambia la clave y llega a `/Cierre`;
  - docente nuevo: se crea, aparece para asignarle grupos y, al entrar, el portal le dice que pida sus grupos;
  - duplicado rechazado junto al campo;
  - campana con el menú expandido y contraído;
  - Login en 1366×625 y 390×700.
- Recorrido automático de todos los enlaces visibles:

| Rol | Páginas recorridas | Errores 500 o «Acceso denegado» |
|---|---|---|
| SuperAdmin | 251 | 0 |
| Administrador | 252 | 0 |
| Supervisor | 80 | 0 (eran 4 antes del arreglo) |
| Cierre | 12 | 0 |
| Docente | 23 | 0 |

- También: sin excepciones en el registro del servidor y sin errores de JavaScript en las pantallas principales.

## Pendientes encontrados (no se tocaron)
- **Zona horaria:** no hay una definida. 68 usos de `DateTime.Now`/`Today` toman la hora del servidor, que no
  está en UTC. Hay que confirmar en qué zona está el de MonsterASP: si no es la de Bolivia, «hoy», las horas
  de registro y los saludos pueden quedar corridos. Arreglo sugerido: un reloj único con `America/La_Paz`.
- **Datos:** el catálogo tiene dos fichas para un mismo docente (una con nombre completo y otra solo con el apellido), cada
  una con 3 grupos de 2027-1. Probablemente vienen de la importación del Excel; hay que unificarlas a mano.
- **Consultas:** dos listas «los primeros N» usan `Skip`/`Take` sin `OrderBy`. Es de bajo impacto: el orden
  puede variar entre cargas.

## Volver atrás
Publicar de nuevo el commit `739368e` (`master` del PR #19) con el mismo script. La base no cambió.
