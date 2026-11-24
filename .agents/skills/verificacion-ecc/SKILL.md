---
name: verificacion-ecc
description: Revisión y verificación antes de dar por terminada una tarea de código en Laboratorios Univalle. Adapta tres piezas de ECC (affaan-m/ECC v2.2.3): csharp-reviewer, silent-failure-hunter y verification-loop. Usar al terminar cualquier cambio de C#, Razor o JS, y al revisar el diff de otro trabajador.
---

# Verificación al estilo ECC, adaptada al proyecto

Origen: `~/.claude/plugins/cache/ecc/ecc/2.2.3/agents/csharp-reviewer.md`, `agents/silent-failure-hunter.md` y `skills/verification-loop/SKILL.md`. Se dejó solo lo que aplica a ASP.NET Core 9, EF Core, Razor y este repositorio.

## 1. Ciclo de verificación
Se sigue en orden. Si una fase falla, se corrige antes de pasar a la siguiente.

1. **Build:** `dotnet build -m:1 --artifacts-path obj/wk-<nombre>`. Debe dar 0 errores y ningún warning nuevo en los archivos tocados.
2. **Formato**, solo sobre lo tocado: `dotnet format --verify-no-changes --include <archivos>`. Es informativo; nunca se reformatean archivos ajenos.
3. **Pruebas:** filtradas mientras se itera, y la suite completa al final. Se informa total, OK y fallidas con los números reales. El proyecto no tiene coverlet, así que no se mide cobertura.
4. **Seguridad.** Se busca en el diff:
   - secretos o cadenas de conexión;
   - código de diagnóstico olvidado (`Console.WriteLine`, `console.log`);
   - `catch {}` vacíos;
   - SQL interpolado (`FromSqlRaw` o `ExecuteSqlRaw` con `$"`);
   - `Html.Raw` con datos del usuario;
   - formularios POST sin antiforgery.
5. **Diff:** `git diff --stat` y revisión de cada archivo. Se buscan cambios que nadie pidió, manejo de errores y casos borde.
6. **Informe**, con el formato de la sección 4.

## 2. Prioridades de revisión de C#
Vienen de csharp-reviewer, filtradas para este proyecto.

**CRÍTICO (bloquea la entrega):**
- inyección SQL;
- XSS o CSRF en Razor;
- secretos en el código;
- `catch` vacío, o que devuelve null sin contexto;
- `.Result`, `.Wait()` o `GetAwaiter().GetResult()` dentro de código async.

**ALTO (bloquea la entrega):**
- `async void`;
- `!` para silenciar nulos sin justificarlo;
- en código nuevo, métodos de más de unas 50 líneas o anidación de más de 4 niveles;
- estado estático mutable;
- consultas EF dentro de bucles (N+1);
- modificar entidades sin `.AsTracking()` (el proyecto usa NoTracking global).

**MEDIO (se avisa):**
- un `IEnumerable` recorrido varias veces;
- concatenar strings dentro de bucles;
- clases que nadie hereda sin `sealed`;
- cadenas sueltas donde el proyecto ya tiene constantes (`PlanningStatus`, `AuthorizationHelper`, `PlanningSlot`).

**Reglas del proyecto que también bloquean:**
- las migraciones existentes no se tocan;
- avisos con `TempData.Success/Error/Warning/Info`;
- SweetAlert2 v7 (`result.value`);
- textos visibles en español y sin códigos L-x;
- Bootstrap 4 y NiceAdmin, sin librerías nuevas.

## 3. Fallas silenciosas
Vienen de silent-failure-hunter.
- Excepciones tragadas, o convertidas en una lista vacía o en null sin avisar.
- Valores por defecto que esconden un error real. Por ejemplo, «sin periodo» que termina como P1.
- Varias tablas escritas sin transacción.
- Stack traces perdidos al relanzar una excepción.
- Mensajes de error genéricos que no dicen qué hay que corregir.

## 4. Informe
Es obligatorio al terminar, y va dentro del informe final del trabajador.
```
VERIFICACIÓN
Build:     OK | FALLA
Formato:   OK | AVISOS (n)
Pruebas:   OK | FALLA (x/y)
Seguridad: OK | HALLAZGOS (n)
Diff:      n archivos
Hallazgos: [CRÍTICO|ALTO|MEDIO] archivo:línea — problema — arreglo
Resultado: LISTO | NO LISTO
Destino:   VERIFICADO | NO REALIZADO | NO APLICA   (p. ej. «probado en la copia de EXP; EXP real pendiente»)
```
Con un hallazgo CRÍTICO o ALTO abierto, la tarea **no** está lista. Pasar las pruebas locales no prueba que funcione en la base o la pantalla reales: eso se informa aparte, en «Destino».
