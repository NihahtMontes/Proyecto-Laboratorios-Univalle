---
name: presentar-sprint
description: Prepara la presentación de un sprint de Laboratorios Univalle — backlog con épicas e historias de usuario, informe de pruebas, arquitectura SOLID y patrones, estado de la base de datos y guion de defensa — con evidencia verificada (git, base REAL en solo lectura, tests, scripts E2E, capturas) y genera los PDF. Usar cuando el usuario pida presentar o defender un sprint, armar el backlog o las historias del sprint, informes de pruebas o de avance, o documentos/PDF para la presentación.
---

# Presentar un sprint con evidencia

Produce 4 documentos formales en PDF y un guion privado. Cada cifra sale de una consulta, de un archivo de resultados o de git, y queda guardada en `outputs/presentacion-sprint<N>/datos/`.

Ejemplo completo y aprobado por el usuario (Sprint 2, 26/10/2026):
- fuentes en `docs/sprint/*.md` y `docs/sprint/img/arquitectura.svg`;
- PDF y datos en `outputs/presentacion-sprint2/`.

**Antes de redactar, léelo**: es la referencia de tono, nivel de detalle y formato.

## Reglas del usuario (no negociables)

- **Responder en español.** El nombre del archivo fuente se escribe tal cual: `BaseGastronomia_v1.xlsx`, no «Excel V12».
- **Solo el sprint que se presenta.** Del anterior, unas 5 líneas: qué se hizo, qué se descartó, por qué (con datos medidos) y dónde está la evidencia.
- **No citar códigos de Linear** (PRO-152…).
- **Transparencia ante todo.** Separar lo ✅ hecho, lo 🟡 parcial y lo ⚪ pendiente (❌ si se descartó), y explicar por qué no se completó cada cosa. Mejor decir el porcentaje real que vender algo como terminado.
- **No saturar.** Seguridad y roles van en una línea, no como épica. Las épicas se nombran por el valor que aportan. La épica de datos se llama «Base de datos real: inventario, procesos e historial».
- **El guion de defensa es privado:** va solo en Markdown y nunca en un PDF.
- **No inventar ni redondear a favor.** Si un dato no se pudo verificar hoy, se cita con su fecha y su fuente.
- **Solo lectura sobre la base REAL** y sobre el código de la app. No hacer commit salvo pedido explícito; nunca `Co-Authored-By: Claude`.

## Paso 0 · Preguntas (AskUserQuestion)

1. **Contenido:**
   - nombres de las épicas, si hay dudas;
   - capturas existentes o nuevas (recomendar las existentes, con su fecha);
   - qué PDF armar (los 4 estándar);
   - dónde va el guion (recomendar: solo en Markdown).
2. **Configuración global de `CLAUDE.md`:** Ejecutor, Paralelo, Revisión y Verificar. Para esta tarea, recomendar Claude directamente, 1 trabajador y verificación completa: la exactitud de cada cifra es crítica.

## Paso 1 · Contexto

- Leer `context.md`, `docs/GUIA_NUEVA_CONVERSACION.md`, el `docs/INFORME_ESTADO_FUNCIONAL_*.md` más reciente y las memorias del proyecto (QA, UX, objetivos).
- **Informe del sprint anterior (.docx), sin dependencias:**
  ```bash
  PYTHONIOENCODING=utf-8 python -c "import zipfile,re;x=zipfile.ZipFile(r'RUTA.docx').read('word/document.xml').decode();[print(''.join(re.findall(r'<w:t(?:\s[^>]*)?>([^<]*)</w:t>',p))) for p in re.findall(r'<w:p\b[^>]*>.*?</w:p>',x,re.S)]"
  ```
- **Historial:**
  - `git log --all --date=short --pretty=format:'%h %ad %an %s'`;
  - los commits desde el inicio del sprint, por autor;
  - las ramas descartadas (por ejemplo `migration/react`) y su `git show --shortstat`.
- **Estado del árbol:** `git status --short | wc -l`. **Ojo:** puede haber otra sesión editando. Compruébalo con `find Pages Services Helpers wwwroot -newer <archivo_de_hoy>`. No toques esos archivos y cita la cifra con su hora.

## Paso 2 · Evidencia (todo a `outputs/presentacion-sprint<N>/datos/`)

```bash
D=outputs/presentacion-sprint<N>/datos; mkdir -p $D
S=.claude/skills/presentar-sprint/scripts
# Base REAL, solo lectura (-I por los índices filtrados, -f 65001 para UTF-8)
sqlcmd -S localhost -E -I -d DB_Laboratorios_Univalle_REAL -W -s "|" -f 65001 -i $S/consultas_bd.sql > $D/bd.txt
# Fidelidad contra el Excel (solo lectura; tarda ~1 min)
cd Tools/ExcelImport && PYTHONIOENCODING=utf-8 python verify_v12_full.py > ../../$D/verify_<fecha>.txt; cd ../..
# Build a carpeta temporal (por si la app está corriendo) y tests
dotnet build -m:1 "Proyecto Laboratorios Univalle.csproj" "-p:OutDir=C:\Temp\pres\bin\\" "-p:IntermediateOutputPath=C:\Temp\pres\obj\\" -v q
dotnet test Tests/Laboratorios.Tests -m:1 --nologo --logger "console;verbosity=minimal" > $D/dotnet_test_<fecha>.txt
# Pruebas E2E: el último resultado de cada script
PYTHONIOENCODING=utf-8 python $S/resumen_e2e.py outputs/qa-usuario/<carpeta_qa> --fallas > $D/resumen_e2e.txt
# Git
git log <rama> --since=<inicio> --date=short --pretty=format:'%h %ad %an %s' > $D/git_evidencia.txt
```

- **Resultados E2E:** cada `.txt` termina en `== N comprobaciones, M fallas`. `sql_invariantes` entra en el total. Explica cada falla que quede: ¿es del sistema o de la copia de prueba?
- **Otras sesiones pueden sobrescribir** los resultados de `outputs/`. **Copia a `datos/`** cualquier archivo que cites como evidencia de un hallazgo.
- **Antes de escribir una afirmación sobre el código, verifícala con grep.** Por ejemplo:
  - cuántos servicios se registran por interfaz en `Program.cs`;
  - cuántas `ConflictAction` hay;
  - cuántos `HasQueryFilter` hay;
  - cuántos PageModels usan `ApplicationDbContext` directamente.
- Los informes de QA anteriores (`outputs/qa-usuario/*/INFORME_QA_*.md`, `outputs/qa-obs-*/`) tienen la historia de hallazgos, del tipo «de 71 fallas a 0». Esa historia demuestra que las pruebas sirven.

## Paso 3 · Capturas

- **Inventario:** `outputs/qa-usuario/img/*.png` (prefijos por ronda: `P0*` planificación, `W0*` asistente de planificación, `E1*` reprogramación, `RC*` cierre, `qa18*` mantenimiento, `final21_*`, `obs19v_*`) y `outputs/e2e/*.png`. Saca la fecha de cada una con `ls -la --time-style=+%F`.
- **Abrir cada candidata con Read antes de usarla.**
  - Descarta los estados vacíos («No hay gestión académica activa»). Las de `outputs/e2e/` del 15/10 de Planificación son así.
  - Descarta también las pantallas sin datos, como «Selecciona un laboratorio…».
- **Pie de cada captura:** qué demuestra y su fecha. Aclara en el documento que se tomaron sobre copias desechables de la base real.
- **Ruta:** relativa al Markdown (desde `docs/sprint/`: `../../outputs/...`).

## Paso 4 · Redacción

Partir de `plantillas/` (copiar a `docs/sprint/`, o a `docs/sprint/sprint-<N>/` y usar `--src`):

| Plantilla | Documento | PDF |
|---|---|---|
| `BACKLOG_SPRINTN.md` | Backlog: antecedente breve, resumen de avance, épicas e historias, «lo que no se completó y por qué» | sí |
| `INFORME_PRUEBAS_SPRINTN.md` | Pruebas: tabla de resultados, 4 tipos, hallazgos corregidos y abiertos, tabla por script, capturas, lo no probado | sí |
| `ARQUITECTURA_SOLID_PATRONES.md` | Capas, cada principio con una clase real y su fragmento de código, patrones, equivalencias, límites | sí |
| `ESTADO_BASE_DATOS.md` | «¿Está fina?», contenido, verificación, huecos y su impacto, observaciones de calidad, qué pedir al cliente | sí |
| `GUION_DEFENSA_SPRINTN.md` | Mensaje, guion de 6–7 minutos, preguntas difíciles, lista previa | **no** |

- **Historias:** «Como **rol**, quiero X, para Y», con un criterio verificable, su estado y su evidencia (cifra, commit o captura). Los ID tienen la forma `XX-00` (BD-01, PL-01, MT-01, QA-01, OP-01, UX-01).
- **Resumen de avance:** **cuenta** las historias por estado y verifica que los totales y los porcentajes cuadren (suman 100 %). El verificador cuenta los ID: deben coincidir con el total.
- **Corregido pero sin commit:** márcalo con **†** y explícalo en la leyenda.
- **Diagrama de arquitectura:** un SVG hecho a mano en `docs/sprint/img/`, con las clases reales (ver `docs/sprint/img/arquitectura.svg`).
- **Formato que entiende el generador:**
  - la portada es el `# título` más las líneas `**Etiqueta:** valor` hasta el primer `---`;
  - para un asterisco literal se escribe `\*`;
  - una celda que es solo un emoji de estado (con o sin †) se convierte en etiqueta de color.

## Paso 5 · Generar los PDF

```bash
python .claude/skills/presentar-sprint/scripts/generar_pdfs.py --sprint <N> --fecha "<d de mes de aaaa>" [--src docs/sprint] [--out outputs/presentacion-sprint<N>]
```

El generador:
- usa solo la biblioteca estándar;
- incrusta las imágenes en base64;
- imprime con Chrome o Edge en modo headless (perfil temporal).

Salida: `outputs/presentacion-sprint<N>/*.pdf` más `html/`.

## Paso 6 · Verificar

1. Estructura:
   ```bash
   PYTHONIOENCODING=utf-8 python .claude/skills/presentar-sprint/scripts/verificar_pdfs.py outputs/presentacion-sprint<N>
   ```
   Comprueba páginas, imágenes, restos de Markdown, códigos de Linear, emoji sueltos y que el guion no esté.
2. Aspecto: `node .claude/skills/presentar-sprint/scripts/capturas_html.cjs "$TEMP/prevpdf" outputs/presentacion-sprint<N>/html/*.html`. Después abre los tramos con Read: portada, tablas, diagrama y capturas. Borra `$TEMP/prevpdf` al terminar.
   - **No intentes renderizar el PDF con PowerShell `-ExecutionPolicy Bypass`: el clasificador lo bloquea.** Tampoco Read puede renderizar PDF (no hay `pdftoppm`).
3. Contrasta cada cifra del texto con `datos/`.
4. Revisa `git status`: solo deben aparecer como nuevos `docs/sprint/*`; nada de la app ni de la base.

## Paso 7 · Entrega

- **Resumen al usuario:**
  - tabla de los PDF (páginas y contenido);
  - avance real (hechas, parciales y pendientes);
  - hallazgos importantes de los datos;
  - qué se volvió a correr hoy y qué se cita con fecha;
  - qué no se pudo verificar y por qué;
  - otras sesiones activas, si las hay;
  - qué conviene hacer antes de presentar: commits pendientes y autorías.
- **Sin commit**, salvo que el usuario lo pida.
- Para cambiar algo, editar el `.md` y volver a correr el Paso 5.

## Trampas conocidas

- **Base REAL, nombres y enumeraciones:**
  - las tablas se llaman `Equipments` y `People` (no `Equipment`/`Persons`);
  - `ManagementType` 9 es una gestión histórica «por confirmar»;
  - la severidad 0 a 3 va de Información a Crítica (ver la cabecera de `consultas_bd.sql`).
- **Consola de Git Bash en cp1252:** usar `PYTHONIOENCODING=utf-8`. La salida de `sqlcmd -f 65001` se ve mal en la terminal, pero queda bien en el archivo.
- **Heredocs con barras invertidas** (regex de JS) se corrompen en Git Bash: escribir los `.js`/`.cjs` con la herramienta Write.
- **`git mv -k` sobre un archivo sin seguimiento** no hace nada y sale con 0: usar `mv`.
- **Los datos de la base REAL vienen tal cual del cliente.** Que un dato falte no es un defecto del sistema: va en «lo que falta y qué impide» y en la lista de datos para el cliente. Ejemplos: vida útil sin fecha de adquisición, aforo sin capacidad, recetas sin existencias.
