// Casos del flujo docente en navegador real (Chromium) sobre una COPIA de la base: el asistente de 4 pasos usado
// como lo usa un docente, observar → corregir → reenviar → rechazar, retiro, faltantes y «Aprobar igual», reparto
// que no cuadra, acceso a solicitudes ajenas, carrera aprobar/rechazar contra SQL Server, práctica programada y
// control semanal. Escribe en la base: se niega a correr contra REAL u oficial.
//
// Uso: E2E_BASE=http://localhost:5099 E2E_DB=DB_Laboratorios_Univalle_EXP E2E_CRED_FILE=<archivo de Tools/QaTestUsers> node docente_casos.mjs
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

// Escenario de la copia (2026-2): docente A = persona 41, grupo 128; docente B = persona 46, grupo 129.
const A = { person: 41, group: 128, user: "docente.a", pw: "DocenteA#2026Seguro" };
const B = { person: 46, group: 129, user: "docente.b", pw: "DocenteB#2026Seguro" };
const DAY = "2026-11-17", MONDAY = "2026-11-16", LAB_A = 1, LAB_B = 6;
const PALA = 522, COLADOR = 262;   // reutilizables con stock en dos ambientes (Economato + otro)

const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "e2e", "docente_casos");
mkdirSync(OUT, { recursive: true });
const sql = (q) => execSync(`sqlcmd -S localhost -d ${DB} -E -C -W -h -1 -I -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`, { encoding: "utf8" }).trim();
const results = [];
const check = (name, ok, detail = "") => { results.push({ name, ok: !!ok, detail }); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };
const status = (id) => sql(`SELECT Status FROM Plannings WHERE Id=${id}`);

const browser = await chromium.launch({ headless: true });
const jsErrors = [];
async function session(user, pw) {
  const ctx = await browser.newContext({ locale: "es-BO", viewport: { width: 1400, height: 900 } });
  const page = await ctx.newPage();
  page.on("pageerror", e => jsErrors.push(`${page.url()} · ${e.message}`));
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', user);
  await page.fill('[name="Input.Password"]', pw);
  await Promise.all([page.waitForLoadState("networkidle"), page.locator('form button[type="submit"]').first().click()]);
  return page;
}
const shot = (page, name) => page.screenshot({ path: resolve(OUT, `${name}.png`), fullPage: true });
const swalConfirm = async (page, text) => {
  await page.locator(".swal2-popup").waitFor();
  if (text !== undefined) await page.locator(".swal2-input:visible, .swal2-textarea:visible").first().fill(text);
  await page.locator(".swal2-confirm").click();
};
const settle = (page) => page.waitForLoadState("networkidle");

async function createTeacher(admin, t) {
  await admin.goto(`${BASE}/Users/Create?role=4&personId=${t.person}`);
  await admin.fill('[name="Input.UserName"]', t.user);
  await admin.fill('[name="Input.Email"]', `${t.user}@qa.univalle.test`);
  await admin.fill('[name="Input.FirstName"]', "Docente");
  await admin.fill('[name="Input.LastName"]', t.user);
  await admin.fill('[name="Input.IdentityCard"]', `Q${t.person}${Date.now().toString().slice(-5)}`);
  await admin.fill('[name="Input.PhoneNumber"]', "70000009");
  await admin.fill('[name="Input.Password"]', t.pw + "x");
  await admin.locator('[name="Input.MustChangePassword"]').uncheck().catch(() => {});
  await Promise.all([settle(admin), admin.locator('form button[type="submit"]').last().click()]);
  const id = sql(`SELECT Id FROM Users WHERE UserName='${t.user}'`);
  // La contraseña queda sin cambio obligatorio para estos casos (el cambio obligatorio se prueba en docente_flow.mjs).
  sql(`UPDATE Users SET MustChangePassword=0 WHERE Id=${id}`);
  const page = await session(t.user, t.pw + "x");
  return page;
}

/** Envía una solicitud por el formulario final del asistente (para los casos que no prueban el asistente en sí). */
async function quickRequest(page, t, practiceId, lab, startPeriod, lines) {
  await page.goto(`${BASE}/Docente/NuevaSolicitud`);
  await page.evaluate(([g, p, d, s, l, lns]) => {
    const set = (id, v) => { document.getElementById(id).value = v; };
    set("f_group", g); set("f_practice", p); set("f_students", 20); set("f_date", d); set("f_start", s); set("f_count", 2);
    set("f_lab", l); set("f_lines", JSON.stringify(lns));
    HTMLFormElement.prototype.submit.call(document.getElementById("submitForm"));
  }, [t.group, practiceId, DAY, startPeriod, lab, lines]);
  await settle(page);
  const id = Number(new URL(page.url()).searchParams.get("id"));
  if (!id) {
    const why = await page.locator(".swal2-popup").innerText().catch(() => "(sin mensaje)");
    throw new Error(`El envío de ${t.user} fue rechazado: ${why.replace(/\s+/g, " ").trim()}`);
  }
  return id;
}

try {
  const admin = await session("admin.qa", creds["admin.qa"]);
  // Copias anteriores: liberar a las personas docentes.
  sql(`UPDATE Users SET PersonId=NULL, Role=99, Status=2 WHERE PersonId IN (${A.person},${B.person})`);
  const practiceA = Number(sql(`SELECT TOP 1 p.Id FROM Practices p JOIN CourseGroups g ON g.SubjectId=p.SubjectId WHERE g.Id=${A.group} AND p.Status=0 ORDER BY p.Id`));
  const practiceB = Number(sql(`SELECT TOP 1 p.Id FROM Practices p JOIN CourseGroups g ON g.SubjectId=p.SubjectId WHERE g.Id=${B.group} AND p.Status=0 ORDER BY p.Id`));
  const teacherA = await createTeacher(admin, A);
  const teacherB = await createTeacher(admin, B);
  check("Cuentas de docentes A y B creadas", teacherA.url().includes("/Docente") && teacherB.url().includes("/Docente"), `${teacherA.url()} | ${teacherB.url()}`);

  // ───── 1. Asistente de 4 pasos usado como un docente ─────
  await teacherA.goto(`${BASE}/Docente/NuevaSolicitud`);
  await teacherA.locator(`[data-group="${A.group}"]`).click();
  await teacherA.locator("[data-practice]").first().waitFor();
  await teacherA.locator(`[data-practice="${practiceA}"]`).click();
  await teacherA.fill("#students", "20");
  await teacherA.locator("#students").dispatchEvent("change");
  await teacherA.locator(".swal2-cancel").click({ timeout: 1500 }).catch(() => {});
  await teacherA.locator("#btnNext").click();
  // Paso 2: buscador de horario y elección manual en la grilla.
  const t0 = Date.now();
  await teacherA.locator("#btnSuggest").click();
  await teacherA.locator("[data-sug], #suggestions .text-muted").first().waitFor({ timeout: 60000 });
  const suggestMs = Date.now() - t0;
  check("El buscador de horarios responde en menos de 10 s", suggestMs < 10000, `${suggestMs} ms`);
  await teacherA.selectOption("#labSelect", String(LAB_A));
  await teacherA.fill("#weekDate", MONDAY);
  await teacherA.locator("#weekDate").dispatchEvent("change");
  const cell = teacherA.locator(`#weekGrid td[data-date="${DAY}"][data-period="3"]`);
  await cell.waitFor();
  check("La grilla muestra libre el periodo elegido", (await cell.getAttribute("data-state")) === "free", await cell.getAttribute("data-state"));
  await cell.click();
  await teacherA.waitForTimeout(1200);
  for (let i = 0; i < 5 && (await teacherA.locator('#btnNext').isVisible()) && !(await teacherA.locator("#articleTerm").isVisible()); i++) {
    await teacherA.locator("#btnNext").click();
    await teacherA.waitForTimeout(800);
  }
  check("Paso 2 → 3 sin choques", await teacherA.locator("#articleTerm").isVisible());
  await shot(teacherA, "01_asistente_paso3");
  // Paso 3: kit cargado y se agrega un artículo con el buscador.
  await teacherA.waitForFunction(() => !document.getElementById("linesBody").innerText.includes("Cargando"));
  const kitRows = await teacherA.locator("#linesBody [data-qty]").count();
  await teacherA.fill("#articleTerm", "Pala de hielo");
  await teacherA.locator("[data-add]").first().waitFor();
  await teacherA.locator("[data-add]").first().click();
  const qtyInputs = teacherA.locator("#linesBody [data-qty]");
  await qtyInputs.last().fill("7");
  await qtyInputs.last().dispatchEvent("change");
  check("Kit precargado y artículo agregado", (await qtyInputs.count()) === kitRows + 1, `kit ${kitRows} + 1`);
  await teacherA.waitForTimeout(800);
  await teacherA.locator("#btnNext").click();
  await teacherA.locator("#btnSubmit").waitFor({ state: "visible" });
  await shot(teacherA, "02_asistente_resumen");
  await teacherA.locator("#btnSubmit").click();
  await swalConfirm(teacherA);
  await settle(teacherA);
  const r1 = Number(new URL(teacherA.url()).searchParams.get("id"));
  const palaLine = sql(`SELECT RequestedQuantity FROM PlanningRequirements WHERE PlanningId=${r1 || 0} AND ArticleId=${PALA}`);
  check("Solicitud enviada desde el asistente", status(r1 || 0) === "Solicitada" && palaLine === "7.00", `id ${r1} · pala ${palaLine}`);

  // ───── 2. Observar → corregir y reenviar → rechazar ─────
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r1}`);
  await admin.locator("#btnObserve").click();
  await swalConfirm(admin, "Cambia el horario a la tarde, por favor.");
  await settle(admin);
  check("Observada por el revisor", status(r1) === "Observada");
  await teacherA.goto(`${BASE}/Docente/NuevaSolicitud?id=${r1}`);
  await teacherA.locator(`[data-group="${A.group}"].is-selected`).waitFor();
  for (let i = 0; i < 6 && !(await teacherA.locator("#btnSubmit").isVisible()); i++) {
    await teacherA.waitForTimeout(900);
    await teacherA.locator("#btnNext").click();
  }
  await teacherA.locator("#btnSubmit").click();
  await swalConfirm(teacherA);
  await settle(teacherA);
  const resent = sql(`SELECT COUNT(*) FROM PlanningReviews WHERE PlanningId=${r1} AND Action=2`);
  check("Corregida y reenviada por el docente", status(r1) === "Solicitada" && resent === "1", `reenvíos ${resent}`);
  const scheduleBefore = sql(`SELECT CONCAT(s.Id,'|',ISNULL(s.ImportBatchId,0),'|',s.Status) FROM PracticeSchedules s JOIN Plannings p ON p.PracticeScheduleId=s.Id WHERE p.Id=${r1}`);
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r1}`);
  await admin.locator("#btnReject").click();
  await swalConfirm(admin, "No hay docente de apoyo esa semana.");
  await settle(admin);
  const scheduleAfter = sql(`SELECT CONCAT(s.Id,'|',ISNULL(s.ImportBatchId,0),'|',s.Status) FROM PracticeSchedules s JOIN Plannings p ON p.PracticeScheduleId=s.Id WHERE p.Id=${r1}`);
  const imported = scheduleBefore.split("|")[1] !== "0";
  check("Rechazada; la programación se cancela solo si la creó el docente", status(r1) === "Rechazada"
    && (imported ? scheduleAfter === scheduleBefore : scheduleAfter.endsWith("|3")), `${scheduleBefore} → ${scheduleAfter}`);

  // ───── 3. Acceso a solicitudes ajenas ─────
  await teacherB.goto(`${BASE}/Docente/Solicitud?id=${r1}`);
  check("Otro docente no ve la solicitud ajena", !teacherB.url().includes(`Solicitud?id=${r1}`), teacherB.url());
  await teacherB.goto(`${BASE}/LaboratoryOperations/Solicitudes`);
  check("El docente no entra a la bandeja del revisor", !teacherB.url().includes("/LaboratoryOperations"), teacherB.url());

  // ───── 4. Retiro por el docente ─────
  const r2 = await quickRequest(teacherB, B, practiceB, LAB_B, 7, [{ articleId: PALA, quantity: 1 }]);
  await teacherB.goto(`${BASE}/Docente/Solicitud?id=${r2}`);
  await teacherB.locator("#btnWithdraw").click();
  await swalConfirm(teacherB, "Ya no la necesito");
  await settle(teacherB);
  check("Retirada por el docente", status(r2) === "Cancelada");

  // ───── 5. Faltantes: dos pendientes por el mismo artículo escaso ─────
  const colador = Number(sql(`SELECT SUM(AvailableQuantity) FROM ArticleStocks WHERE ArticleId=${COLADOR}`));
  const r3 = await quickRequest(teacherA, A, practiceA, LAB_A, 8, [{ articleId: COLADOR, quantity: colador }]);
  const r4 = await quickRequest(teacherB, B, practiceB, LAB_B, 8, [{ articleId: COLADOR, quantity: colador }]);
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r3}`);
  await admin.locator("#btnApprove").click();
  await swalConfirm(admin);
  await settle(admin);
  check("La primera se aprueba", status(r3) === "Aprobada", `${colador} coladores`);
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r4}`);
  await shot(admin, "03_revision_con_faltante");
  // Reparto que no cuadra: Aprobar se deshabilita y «Repartir automáticamente» lo restaura.
  const firstOrigin = admin.locator(".dc-origin-qty").first();
  const original = await firstOrigin.inputValue();
  await firstOrigin.fill(String(Number(original) + 1));
  await firstOrigin.dispatchEvent("input");
  const disabledOnMismatch = await admin.locator("#btnApprove").isDisabled();
  await admin.locator("#btnAutoSplit").click();
  check("Un reparto que no suma deshabilita Aprobar", disabledOnMismatch && !(await admin.locator("#btnApprove").isDisabled()));
  await admin.locator("#btnApprove").click();
  await swalConfirm(admin);
  await settle(admin);
  check("La segunda, sin «Aprobar igual», no se aprueba", status(r4) === "Solicitada");
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${r4}`);
  await admin.locator('label[for="allowShortage"]').click();
  await admin.locator("#btnApprove").click();
  await swalConfirm(admin);
  await settle(admin);
  const forcedComment = sql(`SELECT TOP 1 Comment FROM PlanningReviews WHERE PlanningId=${r4} ORDER BY Id DESC`);
  check("Con «Aprobar igual con faltantes» se aprueba y queda en el historial", status(r4) === "Aprobada" && forcedComment.includes("Aprobada con faltantes"), forcedComment.slice(0, 90));

  // ───── 6. Carrera aprobar / rechazar contra SQL Server ─────
  for (let round = 1; round <= 3; round++) {
    const rx = await quickRequest(teacherA, A, practiceA, 7, [1, 11, 5][round - 1], [{ articleId: PALA, quantity: 1 }]);
    await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${rx}`);
    await admin.evaluate(async (id) => {
      const token = document.querySelector('#approveForm [name="__RequestVerificationToken"]').value;
      const post = (handler, extra) => fetch(`/LaboratoryOperations/Solicitudes/Revisar?id=${id}&handler=${handler}`, {
        method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams({ __RequestVerificationToken: token, ...extra }) });
      await Promise.all([post("Approve", {}), post("Reject", { comment: "Rechazo simultáneo de prueba" })]);
    }, rx);
    const st = status(rx);
    const decisions = sql(`SELECT COUNT(*) FROM PlanningReviews WHERE PlanningId=${rx} AND Action IN (4,5,6)`);
    check(`Carrera ${round}: un solo resultado coherente`, (st === "Aprobada" || st === "Rechazada") && decisions === "1", `${st} · decisiones ${decisions}`);
  }

  // ───── 7. Práctica programada y control semanal ─────
  // Práctica futura del cronograma sin solicitud vigente: en la copia se cancela la planificación histórica del 27/11.
  sql(`UPDATE Plannings SET Status='Cancelada' WHERE PracticeScheduleId IN (SELECT Id FROM PracticeSchedules WHERE CourseGroupId=${A.group} AND ExecutionDate='2026-11-27')`);
  await teacherA.goto(`${BASE}/Docente`);
  await shot(teacherA, "04_inicio_programadas");
  const solicitar = teacherA.locator('a:has-text("Solicitar")[href*="scheduleId"]').first();
  const hasProgrammed = await solicitar.count() > 0;
  check("El inicio del docente lista prácticas programadas por solicitar", hasProgrammed);
  if (hasProgrammed) {
    await solicitar.click();
    await settle(teacherA);
    const pre = await teacherA.locator(`[data-group="${A.group}"].is-selected`).count();
    check("«Solicitar» abre el asistente con el grupo y la práctica precargados", pre === 1 && (await teacherA.locator("[data-practice].is-selected").count()) === 1);
  }
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes`);
  await shot(admin, "05_bandeja_previstas");
  check("La bandeja muestra «Previstas vs. recibidas»", (await admin.content()).includes("Previstas vs. recibidas"));
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
