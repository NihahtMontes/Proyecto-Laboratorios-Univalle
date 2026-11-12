// Recorrido documentado del circuito docente para el informe: usa la interfaz real (clic por clic), toma una captura por
// paso y guarda pasos.json (título, rol, explicación y evidencia de la base). Corre sobre una COPIA desechable.
//
// Uso: E2E_BASE=http://localhost:5099 E2E_DB=DB_Laboratorios_Univalle_EXP E2E_CRED_FILE=<archivo de Tools/QaTestUsers> \
//      node capturas_informe.mjs
import { chromium } from "playwright";
import { execSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE || "http://localhost:5099";
const DB = process.env.E2E_DB || "";
if (!DB || /^(DB_Laboratorios_Univalle|DB_Laboratorios_Univalle_REAL)$/i.test(DB)) {
  console.error("SEGURIDAD: indique E2E_DB con una COPIA desechable (nunca REAL ni la oficial).");
  process.exit(2);
}
const creds = Object.fromEntries([...readFileSync(process.env.E2E_CRED_FILE, "utf8").matchAll(/\|\s*`([^`]+)`\s*\|[^|]*\|\s*`([^`]+)`\s*\|/g)].map(m => [m[1], m[2]]));

const A = { person: 41, group: 128, user: "henny.villegas", first: "Henny", last: "Villegas" };
const B = { person: 46, group: 129, user: "pedro.guereca", first: "Pedro", last: "Guereca" };
const DAY = "2026-11-17", MONDAY = "2026-11-16", LAB = 1, LAB_B = 6;
const PALA = 522, FRASCO = 1270, COLADOR = 262;
const TEMP = "Temporal#2026xY", NEWPW = "Docente#2026Seguro";

const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "informe-docente");
const IMG = resolve(OUT, "img");
mkdirSync(IMG, { recursive: true });
const sql = (q) => execSync(`sqlcmd -S localhost -d ${DB} -E -C -W -h -1 -I -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`, { encoding: "utf8" }).trim();

const steps = [];
let n = 0;
async function step(page, { title, role, text, evidence = "", full = false, focus = null }) {
  n++;
  const file = `${String(n).padStart(2, "0")}.png`;
  if (focus) await page.locator(focus).first().scrollIntoViewIfNeeded().catch(() => {});
  await page.waitForTimeout(400);
  // En capturas de página completa, el menú y la barra fijos se repiten a mitad de la imagen: se fijan arriba solo al capturar.
  const css = full ? await page.addStyleTag({ content: ".left-sidebar,.topbar,header.topbar{position:absolute!important}" }).catch(() => null) : null;
  await page.screenshot({ path: resolve(IMG, file), fullPage: full });
  if (css) await css.evaluate(el => el.remove()).catch(() => {});
  steps.push({ n, title, role, text, evidence, img: `img/${file}`, url: page.url().replace(BASE, "") });
  console.log(`${n}. ${title}${evidence ? " · " + evidence : ""}`);
}

const browser = await chromium.launch({ headless: true });
const errors = [];
async function newPage() {
  const ctx = await browser.newContext({ locale: "es-BO", viewport: { width: 1366, height: 820 }, deviceScaleFactor: 1.5 });
  const page = await ctx.newPage();
  page.on("pageerror", e => errors.push(`${page.url()} · ${e.message}`));
  return page;
}
async function login(page, user, pw) {
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', user);
  await page.fill('[name="Input.Password"]', pw);
  await Promise.all([page.waitForLoadState("networkidle"), page.locator('form button[type="submit"]').first().click()]);
}
const settle = (p) => p.waitForLoadState("networkidle");
const swal = (p) => p.locator(".swal2-popup").waitFor();
const swalInput = (p, text) => p.locator(".swal2-input:visible, .swal2-textarea:visible").first().fill(text);
const swalOk = async (p) => { await p.locator(".swal2-confirm").click(); await settle(p); };

async function createTeacher(admin, t, shoot) {
  await admin.goto(`${BASE}/Users/Create?role=4&personId=${t.person}`);
  await admin.fill('[name="Input.UserName"]', t.user);
  await admin.fill('[name="Input.Email"]', `${t.user}@univalle.edu`);
  await admin.fill('[name="Input.FirstName"]', t.first);
  await admin.fill('[name="Input.LastName"]', t.last);
  await admin.fill('[name="Input.IdentityCard"]', `QA${t.person}${String(Date.now()).slice(-4)}`);
  await admin.fill('[name="Input.PhoneNumber"]', "70000041");
  await admin.fill('[name="Input.Password"]', TEMP);
  if (shoot) await shoot();
  await Promise.all([settle(admin), admin.locator('form button[type="submit"]').last().click()]);
}

try {
  // Datos de partida de la copia: personas docentes libres y una práctica futura del cronograma sin solicitud.
  sql(`UPDATE Users SET PersonId=NULL, Role=99, Status=2 WHERE PersonId IN (${A.person},${B.person})`);
  sql(`UPDATE Plannings SET Status='Cancelada' WHERE PracticeScheduleId IN (SELECT Id FROM PracticeSchedules WHERE CourseGroupId=${A.group} AND ExecutionDate='2026-11-27')`);
  const practiceA = Number(sql(`SELECT TOP 1 p.Id FROM Practices p JOIN CourseGroups g ON g.SubjectId=p.SubjectId WHERE g.Id=${A.group} AND p.Status=0 ORDER BY p.Id`));
  const practiceB = Number(sql(`SELECT TOP 1 p.Id FROM Practices p JOIN CourseGroups g ON g.SubjectId=p.SubjectId WHERE g.Id=${B.group} AND p.Status=0 ORDER BY p.Id`));
  const frascoAntes = sql(`SELECT SUM(AvailableQuantity) FROM ArticleStocks WHERE ArticleId=${FRASCO}`);

  // ── 1. Alta del docente ──
  const admin = await newPage();
  await admin.goto(`${BASE}/Login`);
  await step(admin, { title: "Inicio de sesión", role: "Todos", text: "Cada persona entra con su usuario. El sistema decide qué ve según su rol: el docente solo ve su portal, el revisor la bandeja y el encargado de cierre las prácticas del día." });
  await login(admin, "admin.qa", creds["admin.qa"]);
  await createTeacher(admin, A, () => step(admin, { title: "Alta de la cuenta del docente", role: "Administrador",
    text: "En Usuarios › Nuevo se elige el rol Docente y la persona del catálogo (Henny Villegas). La cuenta queda vinculada a esa persona, que no puede tener otra cuenta. La casilla «Pedir cambio de contraseña» obliga a cambiarla en el primer ingreso.",
    focus: '[name="Input.PersonId"]' }));
  await createTeacher(admin, B, null);
  const linked = sql(`SELECT CONCAT(UserName,' → persona ',PersonId,' · rol ',Role,' · cambiar clave ',MustChangePassword) FROM Users WHERE UserName='${A.user}'`);
  steps[steps.length - 1].evidence = linked;

  // ── 2. Primer ingreso ──
  const doc = await newPage();
  await login(doc, A.user, TEMP);
  await step(doc, { title: "Cambio de contraseña obligatorio", role: "Docente",
    text: "En el primer ingreso el sistema no deja usar ninguna pantalla hasta que el docente cambie la contraseña entregada por el administrador (mínimo 12 caracteres).", evidence: `Redirigido a ${doc.url().replace(BASE, "")}` });
  await doc.fill('[name="Input.CurrentPassword"]', TEMP);
  await doc.fill('[name="Input.NewPassword"]', NEWPW);
  await doc.fill('[name="Input.ConfirmPassword"]', NEWPW);
  await Promise.all([settle(doc), doc.locator('button:has-text("Guardar contraseña")').click()]);
  const docB = await newPage();
  await login(docB, B.user, TEMP);
  await docB.fill('[name="Input.CurrentPassword"]', TEMP);
  await docB.fill('[name="Input.NewPassword"]', NEWPW);
  await docB.fill('[name="Input.ConfirmPassword"]', NEWPW);
  await Promise.all([settle(docB), docB.locator('button:has-text("Guardar contraseña")').click()]);

  await doc.goto(`${BASE}/Docente`);
  await step(doc, { title: "Portal del docente: Mi semana", role: "Docente",
    text: "El docente ve su próxima práctica, contadores de sus solicitudes, la agenda de la semana y las prácticas de su cronograma que todavía no pidió, con un botón «Solicitar». El menú lateral solo tiene su portal.",
    full: true });
  await doc.goto(`${BASE}/Users`);
  const blocked = doc.url().replace(BASE, "");

  // ── 3. Asistente de 4 pasos ──
  await doc.goto(`${BASE}/Docente/NuevaSolicitud`);
  await doc.locator(`[data-group="${A.group}"]`).click();
  await doc.locator(`[data-practice="${practiceA}"]`).click();
  await doc.fill("#students", "20");
  await doc.locator("#students").dispatchEvent("change");
  await doc.locator(".swal2-cancel").click({ timeout: 1500 }).catch(() => {});
  await step(doc, { title: "Paso 1 · Qué: grupo, práctica y estudiantes", role: "Docente",
    text: "Solo aparecen los grupos del docente en la gestión activa y las prácticas del catálogo de esa materia, con cuántas veces ya se hicieron o están pedidas. La cantidad de estudiantes escala el kit de materiales.", full: true,
    evidence: `Intento de abrir Usuarios como docente → ${blocked} (acceso bloqueado)` });
  await doc.locator("#btnNext").click();
  await doc.locator("#btnSuggest").click();
  await doc.locator("[data-sug]").first().waitFor({ timeout: 60000 });
  await doc.selectOption("#labSelect", String(LAB));
  await doc.fill("#weekDate", MONDAY);
  await doc.locator("#weekDate").dispatchEvent("change");
  const cell = doc.locator(`#weekGrid td[data-date="${DAY}"][data-period="3"]`);
  await cell.waitFor();
  await cell.click();
  await doc.waitForTimeout(1500);
  await step(doc, { title: "Paso 2 · Cuándo y dónde", role: "Docente",
    text: "«Sugerir horario» propone tres opciones libres considerando el horario de clase del grupo, el laboratorio habitual y el material. La grilla semanal muestra cada periodo como libre, ocupado, limpieza (un periodo entre prácticas) o en revisión, sin revelar datos de otros docentes. Al elegir un periodo se validan en vivo los choques de laboratorio, del docente y del grupo.",
    full: true });
  for (let i = 0; i < 5 && !(await doc.locator("#articleTerm").isVisible()); i++) { await doc.locator("#btnNext").click(); await doc.waitForTimeout(800); }
  await doc.waitForFunction(() => !document.getElementById("linesBody").innerText.includes("Cargando"));
  for (const term of ["Pala de hielo", "Frasco de vidrio con tapa"]) {
    await doc.fill("#articleTerm", term);
    await doc.locator("[data-add]:not([disabled])").first().waitFor();
    await doc.locator("[data-add]:not([disabled])").first().click();
    const q = doc.locator("#linesBody [data-qty]").last();
    await q.fill(term.startsWith("Pala") ? "7" : "2");
    await q.dispatchEvent("change");
    await doc.waitForTimeout(700);
  }
  await doc.waitForTimeout(1200);
  await step(doc, { title: "Paso 3 · Material con semáforo por horario", role: "Docente",
    text: "Se precarga el kit de la práctica escalado a 20 estudiantes y se agregan artículos con el buscador (aquí 7 palas de hielo y 2 frascos de vidrio, que son consumibles). Cada artículo muestra stock total, libre en ese horario y un semáforo con la explicación: quién lo usa y a qué hora se libera.",
    full: true });
  await doc.locator("#btnNext").click();
  await doc.locator("#btnSubmit").waitFor({ state: "visible" });
  await step(doc, { title: "Paso 4 · Resumen", role: "Docente", full: true,
    text: "Resumen de la práctica, el horario, el laboratorio y el material, con los avisos pendientes (por ejemplo, otra solicitud en revisión para el mismo laboratorio). Una solicitud no reserva nada hasta que el revisor la aprueba." });
  await doc.locator("#btnSubmit").click();
  await swal(doc);
  await step(doc, { title: "Confirmación del envío", role: "Docente", text: "Antes de enviar se confirma con un resumen corto." });
  await swalOk(doc);
  const req = Number(new URL(doc.url()).searchParams.get("id"));
  await step(doc, { title: "Solicitud enviada: seguimiento", role: "Docente", full: true,
    text: "El docente ve el estado (En revisión), la línea de tiempo, el material pedido y el historial. Puede retirarla mientras no se haya decidido.",
    evidence: `Planificación ${req}: ${sql(`SELECT Status FROM Plannings WHERE Id=${req}`)} · ${sql(`SELECT COUNT(*) FROM PlanningRequirements WHERE PlanningId=${req}`)} artículos` });

  // ── 4. Revisión ──
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes`);
  await step(admin, { title: "Bandeja del revisor", role: "Revisor (Supervisor / Administrador)", full: true,
    text: "Las solicitudes pendientes se ordenan por fecha de la práctica (lo más urgente primero) y marcan choques, competencia y faltantes. Arriba, el control semanal compara las prácticas programadas del cronograma con las que ya tienen solicitud y nombra las que faltan." });
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${req}`);
  await step(admin, { title: "Revisión en una pantalla", role: "Revisor",
    text: "A la izquierda, los datos de la práctica y el material con su disponibilidad; a la derecha, la ocupación del laboratorio ese día y el historial. El revisor puede cambiar cantidades (queda registrado lo pedido frente a lo aprobado)." });
  await step(admin, { title: "Origen del material (asignación V13)", role: "Revisor", focus: "#originCard",
    text: "Por cada artículo, el sistema sugiere de qué laboratorio o almacén sale: primero el Economato y luego otros ambientes según lo libre en ese horario. Si la suma por artículo no coincide con lo pedido, el botón Aprobar se desactiva." });
  await admin.locator("#btnObserve").click();
  await swal(admin);
  await swalInput(admin, "Por favor confirma si son 20 estudiantes; si son menos, ajusta el kit.");
  await step(admin, { title: "Observar con comentario", role: "Revisor", text: "Observar devuelve la solicitud al docente con un comentario obligatorio. Rechazar funciona igual, pero cierra la solicitud." });
  await swalOk(admin);

  await doc.goto(`${BASE}/Docente/Solicitud?id=${req}`);
  await step(doc, { title: "El docente ve la observación", role: "Docente", full: true,
    text: "La solicitud pasa a Observada, con el comentario del revisor y el botón para corregir. El docente también recibe un aviso en la campana.",
    evidence: `Estado: ${sql(`SELECT Status FROM Plannings WHERE Id=${req}`)}` });
  await doc.goto(`${BASE}/Docente/NuevaSolicitud?id=${req}`);
  await doc.locator(`[data-group="${A.group}"].is-selected`).waitFor();
  await step(doc, { title: "Corrección: el asistente se abre con todo precargado", role: "Docente", full: true,
    text: "Se reabre el asistente con el grupo, la práctica, el horario y el material de la solicitud, junto con el comentario del revisor. Al reenviar vuelve a En revisión." });
  for (let i = 0; i < 6 && !(await doc.locator("#btnSubmit").isVisible()); i++) { await doc.waitForTimeout(900); await doc.locator("#btnNext").click(); }
  await doc.locator("#btnSubmit").click();
  await swal(doc);
  await swalOk(doc);

  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${req}`);
  await admin.locator("#btnApprove").click();
  await swal(admin);
  await swalOk(admin);
  await admin.waitForTimeout(800);
  const blockedMsg = (await admin.locator(".swal2-popup").innerText().catch(() => "")).replace(/\s+/g, " ");
  if (/no alcanza el material/.test(blockedMsg)) {
    await step(admin, { title: "Bloqueo por material insuficiente", role: "Revisor",
      text: "Al aprobar, el sistema vuelve a revisar el material en ese horario. Aquí un artículo del kit pide más de lo libre (otras prácticas aprobadas lo usan a la misma hora), así que no deja aprobar y explica qué falta y cómo resolverlo.",
      evidence: blockedMsg.replace(/^No se pudo completar\s*/, "").replace(/\s*OK$/, "") });
    await admin.locator(".swal2-confirm").click().catch(() => {});
    await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${req}`);
    for (const m of blockedMsg.matchAll(/(?:material en ese horario: |; )([^:;]+): pide (\d+(?:[.,]\d+)?), libre (\d+(?:[.,]\d+)?)/g)) {
      const articleId = sql(`SELECT TOP 1 r.ArticleId FROM PlanningRequirements r JOIN Articles a ON a.Id=r.ArticleId WHERE r.PlanningId=${req} AND a.Name=N'${m[1].trim().replace(/'/g, "''")}'`);
      const input = admin.locator(`[name="qty[${articleId}]"]`);
      await input.fill(m[3].replace(",", "."));
      await input.dispatchEvent("input");
    }
    await admin.waitForTimeout(500);
    await step(admin, { title: "Aprobar con cambios de cantidad", role: "Revisor", focus: "#originCard",
      text: "El revisor baja la cantidad a lo libre y el reparto por origen se recalcula solo. El cambio queda registrado como «Aprobada con cambios», y el docente ve lo que pidió frente a lo aprobado." });
    await admin.locator("#btnApprove").click();
    await swal(admin);
  }
  await step(admin, { title: "Confirmar la aprobación", role: "Revisor", text: "La confirmación indica cuántas cantidades se cambiaron. Al aprobar, el sistema revalida horario y material bajo bloqueo (dos aprobaciones simultáneas no reservan lo mismo) y guarda el reparto por origen." });
  await swalOk(admin);
  const assigned = sql(`SELECT STRING_AGG(CONCAT(a2.Name,' ',l.Code,' ',CAST(a.Quantity AS int)), ' · ') FROM PlanningAssignments a JOIN PlanningRequirements r ON r.Id=a.PlanningRequirementId JOIN Laboratories l ON l.Id=a.LaboratoryId JOIN Articles a2 ON a2.Id=r.ArticleId WHERE r.PlanningId=${req} AND r.ArticleId IN (${PALA},${FRASCO})`);
  await doc.goto(`${BASE}/Docente/Solicitud?id=${req}`);
  await step(doc, { title: "Aprobada: el docente la ve en su agenda", role: "Docente", full: true,
    text: "La solicitud queda Aprobada, con el historial completo (enviada, observada, reenviada, aprobada). Ahora puede descargar la ficha PDF de la práctica.",
    evidence: `Asignaciones guardadas: ${assigned}` });
  await doc.goto(`${BASE}/Docente/Calendario`);
  await doc.waitForTimeout(1500);
  await step(doc, { title: "Calendario del docente", role: "Docente", text: "Vista mensual de sus prácticas por estado, en español, con un panel de detalle al hacer clic." });

  // ── 5. Economato: entrega y cierre ──
  await admin.goto(`${BASE}/LaboratoryOperations/L3?id=${req}`);
  await step(admin, { title: "Economato: preparar la entrega", role: "Encargado de cierre", full: true,
    text: "El día de la práctica, el encargado abre «Entrega y devolución de material». Las cantidades vienen precargadas con lo aprobado." });
  await admin.locator(".js-departure-confirm").click();
  await swal(admin);
  await swalOk(admin);
  const items = sql(`SELECT STRING_AGG(CONCAT(a.Name,' desde ',ISNULL(o.Code,'(origen de la salida)'),': ',CAST(i.Quantity AS int)), ' · ') FROM DepartureItems i JOIN Departures d ON d.Id=i.DepartureId LEFT JOIN Laboratories o ON o.Id=i.OriginLaboratoryId JOIN Articles a ON a.Id=i.ArticleId WHERE d.PlanningId=${req} AND i.ArticleId IN (${PALA},${FRASCO})`);
  const frascoDespues = sql(`SELECT SUM(AvailableQuantity) FROM ArticleStocks WHERE ArticleId=${FRASCO}`);
  await step(admin, { title: "Salida registrada desde varios orígenes", role: "Encargado de cierre", full: true,
    text: "La salida se arma con una línea por origen. Los utensilios no cambian la existencia (quedan «pendientes de devolver») y los consumibles se descuentan en ese momento y aparecen como «Consumible · no se devuelve».",
    evidence: `${items} · Frascos (consumible) en existencias: ${frascoAntes} → ${frascoDespues}` });
  await admin.locator("#btnFinalizeClean").click();
  await admin.locator(".swal2-select").waitFor();
  await admin.selectOption(".swal2-select", { index: 1 });
  await step(admin, { title: "Finalizar sin novedades", role: "Encargado de cierre",
    text: "Si todo volvió bien, un solo paso: se elige quién recibe el material, el sistema registra la devolución de todo lo pendiente y finaliza la práctica. Si algo faltó, se registra un incidente de material antes de finalizar." });
  await swalOk(admin);
  const closed = sql(`SELECT CONCAT('Planificación ',p.Status,' · salida ',CASE d.Status WHEN 1 THEN 'devuelta' ELSE CAST(d.Status AS varchar) END) FROM Plannings p JOIN Departures d ON d.PlanningId=p.Id WHERE p.Id=${req}`);
  await step(admin, { title: "Práctica cerrada", role: "Encargado de cierre", full: true, text: "La salida queda devuelta y la práctica finalizada.", evidence: closed });
  await doc.goto(`${BASE}/Docente/Solicitud?id=${req}`);
  await step(doc, { title: "El docente ve la práctica Realizada", role: "Docente", full: true,
    text: "La línea de tiempo llega a Realizada. Si hubo incidentes, se muestra «Realizada con incidentes (parcial)», como pide el estado Parcial de la V13." });

  // ── 6. Faltantes: dos prácticas a la misma hora por el mismo artículo escaso ──
  const colador = sql(`SELECT CAST(SUM(AvailableQuantity) AS int) FROM ArticleStocks WHERE ArticleId=${COLADOR}`);
  async function quick(page, t, practice, lab) {
    await page.goto(`${BASE}/Docente/NuevaSolicitud`);
    await page.evaluate(([g, p, d, l, a, q]) => {
      const set = (id, v) => { document.getElementById(id).value = v; };
      set("f_group", g); set("f_practice", p); set("f_students", 20); set("f_date", d); set("f_start", 8); set("f_count", 2); set("f_lab", l);
      set("f_lines", JSON.stringify([{ articleId: a, quantity: q }]));
      HTMLFormElement.prototype.submit.call(document.getElementById("submitForm"));
    }, [t.group, practice, DAY, lab, COLADOR, Number(colador)]);
    await settle(page);
    return Number(new URL(page.url()).searchParams.get("id"));
  }
  const r1 = await quick(doc, A, practiceA, LAB);
  const r2 = await quick(docB, B, practiceB, LAB_B);
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r1}`);
  await admin.locator("#btnApprove").click(); await swal(admin); await swalOk(admin);
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r2}`);
  await step(admin, { title: "Faltante detectado al revisar", role: "Revisor", full: true,
    text: `Dos docentes piden los ${colador} coladores que existen, a la misma hora y en laboratorios distintos. Aprobada la primera, la segunda muestra el faltante con la explicación y la casilla «Aprobar igual con faltantes».` });
  await admin.locator("#btnApprove").click(); await swal(admin); await admin.locator(".swal2-confirm").click();
  await admin.waitForTimeout(1500);
  await step(admin, { title: "Sin la casilla, no se aprueba", role: "Revisor",
    text: "Esto corrige la sobreventa que tenía la versión original: dos solicitudes pendientes que pedían el mismo material escaso se aprobaban ambas. Ahora la aprobación vuelve a revisar el material y bloquea.",
    evidence: `Estado de la segunda: ${sql(`SELECT Status FROM Plannings WHERE Id=${r2}`)}` });
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r2}`);
  await admin.locator('label[for="allowShortage"]').click();
  await admin.locator("#btnApprove").click(); await swal(admin); await swalOk(admin);
  const note = sql(`SELECT TOP 1 Comment FROM PlanningReviews WHERE PlanningId=${r2} ORDER BY Id DESC`);
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r2}`);
  await step(admin, { title: "Aprobación consciente con faltantes", role: "Revisor", focus: "text=Historial",
    text: "Con la casilla marcada se aprueba igual (por ejemplo, porque se consigue prestado), y queda anotado en el historial para auditoría.", evidence: note });

  // ── 7. Catálogo ──
  await admin.goto(`${BASE}/Catalogs/Articles?searchString=Frasco`);
  await step(admin, { title: "Catálogo de artículos con tipo", role: "Administrador",
    text: "El catálogo muestra el tipo de cada artículo. «Consumible» (los 16 artículos de Material operativo, como papel film, servilletas o frascos) se descuenta al entregarse y no se devuelve." });
} catch (e) {
  console.error("FALLA:", e.message.split("\n")[0]);
  process.exitCode = 1;
} finally {
  writeFileSync(resolve(OUT, "pasos.json"), JSON.stringify({ base: BASE, db: DB, fecha: new Date().toISOString(), steps, errors }, null, 2));
  await browser.close();
  console.log(`\n${steps.length} pasos capturados · errores de JavaScript: ${errors.length}`);
}
