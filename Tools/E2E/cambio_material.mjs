// «Pedir cambio de material» en una práctica YA APROBADA (24/11/2026), en navegador real (Chromium) sobre una COPIA:
//  1) el docente pide cambiar su L-3 (aumenta un artículo, quita otro y agrega uno nuevo) y el menú de la Inge lo cuenta;
//  2) la Inge lo ve en «Cambios de material», lo aprueba y el L-3 cambia en la base;
//  3) un segundo pedido se rechaza (el L-3 no cambia) y un tercero lo retira el docente;
//  4) carrera: aprobar y rechazar a la vez dejan UNA sola decisión;
//  5) el reporte de cambios de material se descarga con la gestión de la práctica.
// Escribe en la base: se niega a correr contra REAL, EXP u oficial.
//
// Uso: E2E_BASE=http://localhost:5099 E2E_DB=DB_Laboratorios_Univalle_QA_E2E E2E_CRED_FILE=<archivo de Tools/QaTestUsers> \
//      E2E_TEACHER_PW=<clave de la cuenta docente de prueba> [E2E_TEACHER_USER=docente.b] node cambio_material.mjs
// La cuenta docente.b (persona 46) la crea docente_casos.mjs: correrlo antes en la misma copia.
import { chromium } from "playwright";
import { execSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE || "http://localhost:5099";
const DB = process.env.E2E_DB || "";
if (!DB || /^(DB_Laboratorios_Univalle|DB_Laboratorios_Univalle_REAL|DB_Laboratorios_Univalle_EXP)$/i.test(DB)) {
  console.error("SEGURIDAD: indique E2E_DB con una COPIA desechable (nunca REAL, EXP ni la oficial).");
  process.exit(2);
}
const creds = Object.fromEntries([...readFileSync(process.env.E2E_CRED_FILE, "utf8").matchAll(/\|\s*`([^`]+)`\s*\|[^|]*\|\s*`([^`]+)`\s*\|/g)].map(m => [m[1], m[2]]));
const TEACHER = process.env.E2E_TEACHER_USER || "docente.b";
// La clave de la cuenta de prueba va solo por entorno (nunca escrita en el repositorio, que es público).
const TEACHER_PW = process.env.E2E_TEACHER_PW;
if (!TEACHER_PW) { console.error("Falta E2E_TEACHER_PW (clave de la cuenta docente de prueba en la copia)."); process.exit(2); }
const PERSON = Number(sql0(`SELECT PersonId FROM Users WHERE UserName='${TEACHER}'`));

const OUT = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "outputs", "e2e", "cambio_material");
mkdirSync(OUT, { recursive: true });
function sql0(q) {
  // sqlcmd -Q recibe UNA línea: los saltos de línea de las consultas largas se cambian por espacios.
  const oneLine = q.replace(/\s*\r?\n\s*/g, " ").replace(/"/g, '\\"');
  return execSync(`sqlcmd -S localhost -d ${DB} -E -C -W -h -1 -I -Q "SET NOCOUNT ON; ${oneLine}"`, { encoding: "utf8" }).trim();
}
const sql = sql0;
const results = [];
const check = (name, ok, detail = "") => { results.push({ name, ok: !!ok, detail }); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };
const linesOf = (id) => Object.fromEntries(sql(`SELECT CONCAT(ArticleId,'=',CAST(SUM(COALESCE(RequestedQuantity, ExpectedQuantity)) AS decimal(10,2))) FROM PlanningRequirements WHERE PlanningId=${id} GROUP BY ArticleId`)
  .split(/\r?\n/).filter(Boolean).map(s => { const [a, q] = s.split("="); return [Number(a), Number(q)]; }));
const lastAction = (id) => Number(sql(`SELECT TOP 1 Action FROM PlanningReviews WHERE PlanningId=${id} AND Action BETWEEN 8 AND 11 ORDER BY CreatedDate DESC, Id DESC`) || 0);

// Práctica aprobada y futura del docente, con L-3 de al menos 3 artículos y sin salida ni pedidos de cambio.
const candidates = sql(`SELECT TOP 3 p.Id FROM Plannings p WHERE p.TeacherPersonId=${PERSON} AND p.IsDeleted=0 AND p.Status='Aprobada'
  AND p.ScheduledDate >= DATEADD(day, 10, CAST(GETDATE() AS date)) AND p.LaboratoryId IS NOT NULL AND p.StartPeriodId IS NOT NULL
  AND NOT EXISTS(SELECT 1 FROM Departures d WHERE d.PlanningId=p.Id) AND NOT EXISTS(SELECT 1 FROM PlanningReviews r WHERE r.PlanningId=p.Id AND r.Action BETWEEN 8 AND 11)
  AND (SELECT COUNT(DISTINCT ArticleId) FROM PlanningRequirements r WHERE r.PlanningId=p.Id AND r.EquipmentUnitId IS NULL) >= 3
  ORDER BY p.ScheduledDate, p.Id`).split(/\r?\n/).filter(Boolean).map(Number);

const browser = await chromium.launch({ headless: true });
const jsErrors = [];
async function session(user, pw) {
  const ctx = await browser.newContext({ locale: "es-BO", viewport: { width: 1400, height: 950 } });
  const page = await ctx.newPage();
  page.on("pageerror", e => jsErrors.push(`${page.url()} · ${e.message}`));
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', user);
  await page.fill('[name="Input.Password"]', pw);
  await Promise.all([page.waitForLoadState("networkidle"), page.locator('form button[type="submit"]').first().click()]);
  return page;
}
const settle = (page) => page.waitForLoadState("networkidle");
// El layout muestra una vez por sesión el aviso flotante «Tienes N avisos que requieren atención» (SweetAlert, arriba a
// la derecha, 7 s y se PAUSA con el mouse encima): tapa los botones de la cabecera y el puntero del robot lo deja fijo.
// Se cierra antes de interactuar (solo ese aviso de esquina; las confirmaciones propias son modales centrados).
const dismissToast = (page) => page.evaluate(() => {
  if (document.querySelector(".swal2-container.swal2-top-end") && window.Swal) window.Swal.close();
});
const shot = (page, name) => page.screenshot({ path: resolve(OUT, `${name}.png`), fullPage: true });
const swalConfirm = async (page, text) => {
  await page.locator(".swal2-popup").waitFor();
  if (text !== undefined) await page.locator(".swal2-input:visible, .swal2-textarea:visible").first().fill(text);
  await Promise.all([page.waitForLoadState("networkidle").catch(() => {}), page.locator(".swal2-confirm").click()]);
  await settle(page);
};
const badge = async (page) => {
  const t = await page.locator('a[href*="/LaboratoryOperations/Solicitudes"] .badge').first().textContent().catch(() => null);
  return Number((t || "0").trim()) || 0;
};

// Pide un cambio desde la pantalla del docente. edits: { up: articleId, remove: articleId, addTerm: texto }.
async function requestChange(teacher, id, edits, reason, name) {
  await teacher.goto(`${BASE}/Docente/Solicitud?id=${id}`);
  await settle(teacher);
  await dismissToast(teacher);
  const button = teacher.locator("#btnPedirCambio");
  if (await button.count() === 0) return { ok: false, detail: "no aparece «Pedir cambio de material»" };
  // Se sigue el enlace del botón (es un <a>): así el aviso flotante de la esquina no puede interceptar el clic.
  await teacher.goto(new URL(await button.getAttribute("href"), BASE).toString());
  await settle(teacher);
  await dismissToast(teacher);
  await teacher.locator("#cm-form").waitFor();
  if (edits.up) {
    const input = teacher.locator(`[data-article-id="${edits.up}"] input[type="number"]`).first();
    const value = Number(await input.inputValue());
    await input.fill(String(value + 1));
  }
  if (edits.remove) await teacher.locator(`[data-article-id="${edits.remove}"] .cm-remove`).first().click();
  if (edits.addTerm) {
    await teacher.fill("#cm-search", edits.addTerm);
    const option = teacher.locator(".cm-result").first();
    await option.waitFor({ timeout: 8000 });
    await option.click();
    // La fila nueva arranca en 1; el docente escribe la cantidad que necesita.
    const added = teacher.locator(`[data-article-id="${edits.addId}"] .cm-qty`).first();
    await added.waitFor({ timeout: 5000 });
    edits.addedDefault = Number(await added.inputValue());
    await added.fill("2");
  }
  await teacher.fill("#cm-reason", reason);
  // Resumen en vivo que ve el docente antes de enviar (cuenta contra el L-3 vigente de cada fila).
  edits.summary = ((await teacher.locator("#cm-summary").textContent()) || "").trim();
  edits.warningVisible = await teacher.locator("#cm-alert-warning").isVisible();
  await shot(teacher, `${name}_formulario`);
  await teacher.locator("#cm-submit").click();
  await swalConfirm(teacher);
  return { ok: true };
}

try {
  check("Hay una práctica aprobada y futura del docente para probar", candidates.length >= 3, `docente persona ${PERSON} · ${candidates.join(", ")}`);
  const [P1, P2, P3] = candidates;
  const admin = await session("admin.qa", creds["admin.qa"]);
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Index?tab=cambios`);
  const badgeBefore = await badge(admin);
  const teacher = await session(TEACHER, TEACHER_PW);

  // ───── 1. Pedido del docente: +1, quitar y agregar ─────
  const before = linesOf(P1);
  const ids = Object.keys(before).map(Number);
  const up = ids[0], remove = ids[1];
  // Artículo activo (no equipo) que NO está en el L-3: se busca por su código para que el resultado sea único.
  const [addId, addCode] = sql(`SELECT TOP 1 CONCAT(a.Id,'|',a.Code) FROM Articles a WHERE a.Status=0 AND a.EquipmentId IS NULL
    AND a.Id NOT IN (SELECT ArticleId FROM PlanningRequirements WHERE PlanningId=${P1}) ORDER BY a.Id`).split("|");
  const edits1 = { up, remove, addTerm: addCode, addId: Number(addId) };
  const r1 = await requestChange(teacher, P1, edits1, "Somos 4 estudiantes más y cambiamos la técnica de emplatado.", "01_pedido");
  check("Un artículo agregado desde el buscador arranca con cantidad 1", edits1.addedDefault === 1, `arrancó en ${edits1.addedDefault}`);
  check("El resumen en vivo cuenta bien: 1 de cantidad y 2 de artículos, sin llamada de atención",
    /^1 cambio\(s\) de cantidad · 2 artículo\(s\)/.test(edits1.summary || "") && edits1.warningVisible === false,
    `«${edits1.summary}» · alerta ${edits1.warningVisible ? "visible" : "oculta"}`);
  check("El docente envía el pedido de cambio desde su práctica aprobada", r1.ok && lastAction(P1) === 8, r1.detail || `práctica ${P1}`);
  const lines8 = Number(sql(`SELECT COUNT(*) FROM PlanningReviewLines l JOIN PlanningReviews r ON r.Id=l.PlanningReviewId WHERE r.PlanningId=${P1} AND r.Action=8`));
  check("El pedido guarda una línea por artículo cambiado (aumento, quitado y agregado)", lines8 === 3, `${lines8} líneas`);
  check("La práctica sigue «Aprobada» y su L-3 no cambia mientras espera", sql(`SELECT Status FROM Plannings WHERE Id=${P1}`) === "Aprobada"
    && JSON.stringify(linesOf(P1)) === JSON.stringify(before));
  await teacher.goto(`${BASE}/Docente/Solicitud?id=${P1}`);
  check("El docente ve su pedido «en revisión» con el botón para retirarlo", (await teacher.locator("#btnWithdrawChange").count()) === 1);
  await shot(teacher, "02_docente_pedido_en_revision");

  // ───── 2. La Inge lo ve en la bandeja y lo aprueba ─────
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Index?tab=cambios`);
  await settle(admin);
  await dismissToast(admin);
  check("El número del menú cuenta el pedido de cambio", (await badge(admin)) === badgeBefore + 1, `${badgeBefore} → ${await badge(admin)}`);
  const row = admin.locator(`a[href*="RevisarCambio?id=${P1}"]`).first();
  check("La pestaña «Cambios de material» lista el pedido", await row.count() === 1);
  await shot(admin, "03_bandeja_cambios");
  await row.click();
  await admin.waitForURL(/RevisarCambio/);
  await settle(admin);
  await dismissToast(admin);
  await admin.locator("#rc-approve").waitFor();
  await admin.locator(".preloader").waitFor({ state: "hidden", timeout: 5000 }).catch(() => {});
  await shot(admin, "04_revisar_cambio");
  const shortage = await admin.locator("#rc-allow-shortage").count() > 0;
  if (shortage) {
    // Casilla con estilo de Bootstrap (el input real está oculto): se marca con clic en su etiqueta, como una persona.
    const label = admin.locator('label[for="rc-allow-shortage"]');
    if (await label.count() > 0) await label.click(); else await admin.locator("#rc-allow-shortage").check({ force: true });
    check("Con faltantes, la casilla «Aprobar igual con faltantes» se puede marcar", await admin.locator("#rc-allow-shortage").isChecked());
  }
  await admin.locator("#rc-approve").click();
  await swalConfirm(admin);
  check("La revisión muestra el material y, si falta, ofrece «Aprobar igual con faltantes»", true, shortage ? "con faltantes" : "alcanza");
  const after = linesOf(P1);
  check("Aprobado: queda la acción 9 en el historial", lastAction(P1) === 9);
  check("Aprobado: el artículo aumentado sube en 1", after[up] === before[up] + 1, `${before[up]} → ${after[up]}`);
  check("Aprobado: el artículo quitado sale del L-3", !(remove in after));
  check("Aprobado: el artículo agregado entra al L-3 con la cantidad pedida", after[Number(addId)] === 2, `${addCode} → ${after[Number(addId)]}`);
  const notice = sql(`SELECT COUNT(*) FROM Notifications n JOIN Users u ON u.Id=n.UserId WHERE u.UserName='${TEACHER}' AND n.Title='Cambio de material aprobado'`);
  check("El docente recibe el aviso «Cambio de material aprobado»", Number(notice) >= 1);

  // ───── 3. Rechazo y retiro ─────
  const before2 = linesOf(P2);
  const r2 = await requestChange(teacher, P2, { up: Object.keys(before2).map(Number)[0] }, "Necesito una unidad más para la demostración.", "05_pedido_a_rechazar");
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/RevisarCambio?id=${P2}`);
  await settle(admin);
  await dismissToast(admin);
  await admin.locator("#rc-reject").click();
  await swalConfirm(admin, "No hay stock suficiente para esa fecha.");
  check("Rechazado: acción 10 y el L-3 queda igual", r2.ok && lastAction(P2) === 10 && JSON.stringify(linesOf(P2)) === JSON.stringify(before2));

  const r3 = await requestChange(teacher, P3, { up: Object.keys(linesOf(P3)).map(Number)[0] }, "Pedido de prueba que el docente retira.", "06_pedido_a_retirar");
  await teacher.goto(`${BASE}/Docente/Solicitud?id=${P3}`);
  await settle(teacher);
  await dismissToast(teacher);
  await teacher.locator("#btnWithdrawChange").click();
  await swalConfirm(teacher);
  check("Retirado por el docente: acción 11 y vuelve a poder pedir", r3.ok && lastAction(P3) === 11 && (await teacher.locator("#btnPedirCambio").count()) === 1);

  // ───── 4. Carrera: aprobar y rechazar a la vez sobre el mismo pedido ─────
  const r4 = await requestChange(teacher, P3, { up: Object.keys(linesOf(P3)).map(Number)[0] }, "Pedido para probar la carrera aprobar/rechazar.", "07_pedido_carrera");
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/RevisarCambio?id=${P3}`);
  await settle(admin);
  await admin.evaluate(async (id) => {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const post = (handler, extra) => fetch(`/LaboratoryOperations/Solicitudes/RevisarCambio?handler=${handler}&id=${id}`, {
      method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({ __RequestVerificationToken: token, id: String(id), allowShortage: "true", ...extra }) });
    await Promise.all([post("Approve", {}), post("Reject", { comment: "Rechazo simultáneo de prueba" })]);
  }, P3);
  const decisions = Number(sql(`SELECT COUNT(*) FROM PlanningReviews WHERE PlanningId=${P3} AND Action IN (9,10)
    AND CreatedDate > (SELECT MAX(CreatedDate) FROM PlanningReviews WHERE PlanningId=${P3} AND Action=8)`));
  check("Carrera aprobar/rechazar: una sola decisión", r4.ok && decisions === 1, `${decisions} decisiones`);

  // ───── 5. Reporte de cambios de material de la gestión de la práctica ─────
  const management = sql(`SELECT ManagementId FROM Plannings WHERE Id=${P1}`);
  const report = await admin.request.get(`${BASE}/api/reports/download/cambios-material?managementId=${management}`);
  const body = await report.body();
  writeFileSync(resolve(OUT, "Cambios_material.xlsx"), body);
  check("El reporte de cambios de material se descarga (Excel)", report.status() === 200 && body.length > 3000
    && (report.headers()["content-type"] || "").includes("spreadsheetml"), `${report.status()} · ${body.length} bytes`);
} catch (e) {
  check("Recorrido sin excepciones", false, e.message.split("\n")[0]);
} finally {
  check("Sin errores de JavaScript en las páginas", jsErrors.length === 0, jsErrors.slice(0, 3).join(" | "));
  writeFileSync(resolve(OUT, "resultado.json"), JSON.stringify({ base: BASE, db: DB, results, jsErrors }, null, 2));
  await browser.close();
  const fails = results.filter(r => !r.ok).length;
  console.log(`\n${results.length - fails}/${results.length} comprobaciones correctas. Capturas en ${OUT}`);
  process.exit(fails ? 1 : 0);
}
