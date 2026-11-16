// Recorrido en navegador real (Chromium) de la planificación del semestre, sobre una COPIA de la base:
//  A) bandeja de solicitudes de docentes: pestañas, contadores y filtros,
//  B) reprogramar una clase: banda azul, resumen antes → después, aviso de motivo corto, bloqueo con material entregado,
//  C) suspender un día con categoría y reponerlo: banda naranja, filtro por categoría,
//  D) cargar la planificación desde Excel: plantilla, errores, repetidas, ya cargadas (sobrescribir / omitir) y carga,
//  E) docente: disponibilidad por semana académica, solicitud desde una celda libre y aviso recibido,
//  F) pantallas nuevas en tamaño teléfono.
// Al terminar deja la copia como estaba (restaura lo que movió y da de baja lo que creó).
// Requiere la cuenta super.qa (SuperAdmin, misma contraseña que admin.qa) y docente.a (la crea docente_casos.mjs).
// Uso: E2E_BASE=http://localhost:5099 E2E_DB=DB_Laboratorios_Univalle_EXP E2E_CRED_FILE=<archivo privado> node planificacion_semestre.mjs
import { chromium, devices } from "playwright";
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
const TEACHER = { user: "docente.a", pw: "DocenteA#2026Seguro" + "x", person: 41 };

// Datos de la copia EXP (gestión 2026-2).
const MGMT = 4;
const MOVE = { id: 836, candidates: [["2026-12-14", 1], ["2026-12-14", 7], ["2026-12-15", 1], ["2026-12-16", 7], ["2026-12-18", 1]] }; // clase del docente A
const DELIVERED = 1052;                         // aprobada con material entregado
const SUSPEND = { date: "2026-12-17", planning: 887, candidates: [["2026-12-16", 3], ["2026-12-18", 3], ["2026-12-16", 7]] };
const IMPORT = { subject: "IGA3G8", group: "C", lab: "H-07", teacher: "Pedro guereca", existing: 839, existingPractice: "PRA-IGA3G8-03" };

const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "e2e", "planificacion_semestre");
mkdirSync(OUT, { recursive: true });
const sql = (q) => execSync(`sqlcmd -S localhost -d ${DB} -E -C -W -h -1 -I -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`, { encoding: "utf8" }).trim();
const results = [];
const check = (name, ok, detail = "") => { results.push({ name, ok: !!ok, detail }); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };
const settle = (page) => page.waitForLoadState("networkidle");
const shot = (page, name) => page.screenshot({ path: resolve(OUT, `${name}.png`), fullPage: true });
/** Espera el SweetAlert cuyo título contiene el texto y pulsa su botón de confirmar. */
async function swal(page, titleFragment, button = ".swal2-confirm") {
  await page.locator(".swal2-title", { hasText: titleFragment }).waitFor({ state: "visible", timeout: 15000 });
  await page.locator(button).click();
  await page.locator(".swal2-container").waitFor({ state: "detached", timeout: 15000 }).catch(() => {});
}

const browser = await chromium.launch({ headless: true });
const jsErrors = [];
async function session(user, pw, options = { viewport: { width: 1400, height: 900 } }) {
  const ctx = await browser.newContext({ locale: "es-BO", acceptDownloads: true, ...options });
  const page = await ctx.newPage();
  page.on("pageerror", e => jsErrors.push(`${page.url()} · ${e.message}`));
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', user);
  await page.fill('[name="Input.Password"]', pw);
  await Promise.all([settle(page), page.locator('form button[type="submit"]').first().click()]);
  return page;
}
async function phoneCheck(page, path, name) {
  await page.goto(`${BASE}${path}`);
  await settle(page);
  const overflow = await page.evaluate(() => {
    const wide = [...document.querySelectorAll("body *")]
      .filter(el => { const r = el.getBoundingClientRect(); return r.width > 0 && r.right > window.innerWidth + 2 && !el.closest(".table-responsive, .dataTables_wrapper, .select2-container, [style*='overflow']"); })
      .slice(0, 3).map(el => `${el.tagName.toLowerCase()}${el.id ? "#" + el.id : ""}${el.className && typeof el.className === "string" ? "." + el.className.trim().split(/\s+/).slice(0, 2).join(".") : ""}`);
    return { scroll: document.documentElement.scrollWidth, width: window.innerWidth, wide };
  });
  await shot(page, name);
  check(`Teléfono ${name.replace(/^[A-Z]\d+_/, "")} sin desborde ni elementos cortados`, overflow.scroll <= overflow.width + 2 && overflow.wide.length === 0,
    `${overflow.scroll}px de ${overflow.width}px${overflow.wide.length ? " · " + overflow.wide.join(", ") : ""}`);
}
const phoneOptions = () => { const o = { ...devices["iPhone 13"], locale: "es-BO" }; delete o.defaultBrowserType; return o; };

// ───── Restauración: la copia vuelve a quedar como estaba ─────
const PLAN_COLS = "Status,StatusBeforeRecovery,ScheduledDate,ScheduledEndDate,StartTime,EndTime,StartPeriodId,PeriodCount,LaboratoryId,CampusId,TeacherPersonId,AcademicWeekId,AcademicWeek,StudentCount,Observations";
const SCHED_COLS = "ExecutionDate,StartTime,EndTime,LaboratoryId,TeacherPersonId,AcademicWeekId,WeekNumber,Status,StudentCount,ScalingFactor,ImportBatchId";
const setList = (cols, alias) => cols.split(",").map(c => `t.${c}=${alias}.${c}`).join(",");
function restore() {
  if (sql("SELECT ISNULL(OBJECT_ID('dbo.E2E_Marks'),0)") === "0") return;
  const [plan, sched, notif, rec, batch] = sql("SELECT CONCAT(MaxPlan,'|',MaxSched,'|',MaxNotif,'|',MaxRec,'|',MaxBatch) FROM dbo.E2E_Marks").split("|").map(Number);
  sql(`DELETE FROM Reschedulings WHERE DestinationPlanningId > ${plan} OR DayRecoveryId > ${rec} OR OriginPlanningId IN (SELECT Id FROM dbo.E2E_Plan)`);
  sql(`UPDATE AcademicDays SET Status=0, Reason=NULL, DayRecoveryId=NULL WHERE DayRecoveryId > ${rec}`);
  sql(`DELETE FROM DayRecoveries WHERE Id > ${rec}`);
  sql(`UPDATE t SET ${setList(PLAN_COLS, "b")} FROM Plannings t JOIN dbo.E2E_Plan b ON b.Id=t.Id`);
  sql(`UPDATE t SET ${setList(SCHED_COLS, "b")} FROM PracticeSchedules t JOIN dbo.E2E_Sched b ON b.Id=t.Id`);
  sql(`UPDATE Plannings SET IsDeleted=1, DeletedDate=GETUTCDATE() WHERE Id > ${plan}`);
  sql(`UPDATE PracticeSchedules SET IsDeleted=1, DeletedDate=GETUTCDATE(), ImportBatchId=NULL WHERE Id > ${sched}`);
  sql(`DELETE FROM ImportBatches WHERE Id > ${batch}`);
  sql(`DELETE FROM Notifications WHERE Id > ${notif}`);
  sql("DROP TABLE dbo.E2E_Marks; DROP TABLE dbo.E2E_Plan; DROP TABLE dbo.E2E_Sched");
}
restore();   // por si una corrida anterior se cortó
const touched = [MOVE.id, SUSPEND.planning, IMPORT.existing].join(",");
sql(`SELECT (SELECT ISNULL(MAX(Id),0) FROM Plannings) MaxPlan, (SELECT ISNULL(MAX(Id),0) FROM PracticeSchedules) MaxSched, (SELECT ISNULL(MAX(Id),0) FROM Notifications) MaxNotif, (SELECT ISNULL(MAX(Id),0) FROM DayRecoveries) MaxRec, (SELECT ISNULL(MAX(Id),0) FROM ImportBatches) MaxBatch INTO dbo.E2E_Marks`);
sql(`SELECT * INTO dbo.E2E_Plan FROM Plannings WHERE Id IN (${touched})`);
sql(`SELECT * INTO dbo.E2E_Sched FROM PracticeSchedules WHERE Id IN (SELECT PracticeScheduleId FROM Plannings WHERE Id IN (${touched}))`);
const periodId = (order) => sql(`SELECT Id FROM ClassPeriods WHERE [Order]=${order}`);

/** En el modo reprogramar/reponer: prueba destinos hasta que uno queda fijado sin bloqueos. Devuelve el destino usado. */
async function pickDestination(page, candidates) {
  for (const [date, order] of candidates) {
    await page.fill("#gr-rs-date", date);
    await page.selectOption("#gr-rs-period", periodId(order));
    await page.click("#gr-rs-preview-btn");
    try {
      await page.locator("#gr-rs-confirm").waitFor({ state: "visible", timeout: 8000 });
      return [date, order];
    } catch { /* destino con bloqueo: se prueba el siguiente */ }
  }
  return null;
}

try {
  const admin = await session("super.qa", creds["admin.qa"]);
  check("Ingresa el SuperAdmin de prueba", !admin.url().includes("/Login"), admin.url());

  // ───── A. Bandeja de solicitudes de docentes ─────
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes`);
  await settle(admin);
  const tabs = await admin.locator("#sol-tabs .nav-link").evaluateAll(els => els.map(e => e.dataset.tab));
  check("La bandeja tiene las seis pestañas", ["revisar", "observadas", "aprobadas", "rechazadas", "retiradas", "todas"].every(t => tabs.includes(t)), tabs.join(", "));
  await shot(admin, "A1_bandeja_por_revisar");
  const rejectedDb = Number(sql("SELECT COUNT(*) FROM Plannings p WHERE p.IsDeleted=0 AND p.Status='Rechazada' AND EXISTS (SELECT 1 FROM PlanningReviews r WHERE r.PlanningId=p.Id)"));
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes?tab=rechazadas`);
  await settle(admin);
  const rejectedBadge = Number(await admin.locator('#sol-tabs .nav-link[data-tab="rechazadas"] .badge').innerText());
  const rejectedRows = await admin.locator("tr.dc-inbox-row").count();
  check("Rechazadas: el contador y las filas coinciden con la base", rejectedBadge === rejectedDb && rejectedRows === Math.min(rejectedDb, 150), `contador ${rejectedBadge}, filas ${rejectedRows}, base ${rejectedDb}`);
  await shot(admin, "A2_bandeja_rechazadas");
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes?tab=aprobadas`);
  await settle(admin);
  const approvedBadge = Number(await admin.locator('#sol-tabs .nav-link[data-tab="aprobadas"] .badge').innerText());
  check("Aprobadas se ve separada de Rechazadas", approvedBadge >= 0 && !(await admin.locator("tr.dc-inbox-row .badge", { hasText: "Rechazada" }).count()), `${approvedBadge} aprobadas`);
  await shot(admin, "A3_bandeja_aprobadas");
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes?tab=todas`);
  await settle(admin);
  const allBadge = Number(await admin.locator('#sol-tabs .nav-link[data-tab="todas"] .badge').innerText());
  const teacherOption = await admin.locator("#f-teacher option").nth(1).getAttribute("value").catch(() => null);
  if (teacherOption) {
    await admin.selectOption("#f-teacher", teacherOption);
    await Promise.all([settle(admin), admin.click('#sol-filters button[type="submit"]')]);
    const filteredBadge = Number(await admin.locator('#sol-tabs .nav-link[data-tab="todas"] .badge').innerText());
    const filteredDb = Number(sql(`SELECT COUNT(*) FROM Plannings p WHERE p.IsDeleted=0 AND p.TeacherPersonId=${teacherOption} AND EXISTS (SELECT 1 FROM PlanningReviews r WHERE r.PlanningId=p.Id)`));
    check("Filtrar por docente acota la lista y los contadores", filteredBadge === filteredDb && filteredBadge <= allBadge && admin.url().includes("teacherId="), `${filteredBadge} de ${allBadge}`);
    await admin.fill("#f-q", "zzz-no-existe");
    await Promise.all([settle(admin), admin.click('#sol-filters button[type="submit"]')]);
    check("Buscar algo que no existe muestra el mensaje de sin coincidencias", await admin.locator(".dc-empty", { hasText: "Ninguna solicitud coincide" }).count() === 1);
    await shot(admin, "A4_bandeja_filtros");
  } else check("Filtrar por docente acota la lista y los contadores", false, "no hay docentes en el filtro");

  // ───── B. Reprogramar una clase ─────
  await admin.goto(`${BASE}/LaboratoryOperations/Grilla?reprogramar=${DELIVERED}`);
  await settle(admin);
  const blockedText = await admin.locator("body").innerText();
  check("Con material entregado no deja entrar a reprogramar", !/Estás reprogramando/i.test(blockedText) && admin.url().indexOf("reprogramar=") === -1, admin.url().replace(BASE, ""));

  await admin.goto(`${BASE}/LaboratoryOperations/Grilla?reprogramar=${MOVE.id}`);
  await settle(admin);
  const moveBand = admin.locator("#gr-rs-band");
  check("Reprogramar: banda azul «Estás reprogramando una clase»", await moveBand.evaluate(el => el.classList.contains("gr-rs-band--move")).catch(() => false)
    && /Estás reprogramando una clase/i.test(await moveBand.innerText().catch(() => "")));
  const moveDest = await pickDestination(admin, MOVE.candidates);
  check("Reprogramar: se fija un destino libre", !!moveDest, moveDest ? moveDest.join(" P") : "ningún candidato quedó libre");
  if (moveDest) {
    const summary = await admin.locator("#gr-rs-summary").innerText();
    check("Reprogramar: resumen antes → después", /Antes/i.test(summary) && /Después/i.test(summary), summary.replace(/\s+/g, " ").slice(0, 110));
    await shot(admin, "B1_reprogramar_resumen");
    if (await admin.locator("#gr-rs-accept-wrap").isVisible()) await admin.locator('label[for="gr-rs-accept"]').click();
    // Motivo vacío: no pasa.
    await admin.fill("#gr-rs-reason", "   ");
    await admin.click(".gr-rs-submit");
    await swal(admin, "Falta el motivo");
    check("Reprogramar: el motivo vacío no se acepta", Number(sql(`SELECT COUNT(*) FROM Reschedulings WHERE OriginPlanningId=${MOVE.id}`)) === 0);
    // Motivo de una palabra: recomienda 5, pero deja guardar igual.
    await admin.fill("#gr-rs-reason", "0");
    await admin.click(".gr-rs-submit");
    await admin.locator(".swal2-title", { hasText: "Motivo muy corto" }).waitFor({ state: "visible" });
    const shortText = await admin.locator(".swal2-content").innerText();
    await shot(admin, "B2_motivo_corto");
    check("Reprogramar: con una palabra recomienda al menos 5", /al menos 5 palabras/i.test(shortText), shortText.slice(0, 90));
    await admin.locator(".swal2-confirm").click();
    await admin.locator(".swal2-title", { hasText: /reprogramaci|completada/i }).waitFor({ state: "visible" });
    await Promise.all([admin.waitForNavigation(), admin.locator(".swal2-confirm").click()]);
    await settle(admin);
    const moved = sql(`SELECT CONCAT(r.Reason,'|',CONVERT(varchar(10),r.DestinationDate,23),'|',o.Status,'|',d.Status) FROM Reschedulings r JOIN Plannings o ON o.Id=r.OriginPlanningId JOIN Plannings d ON d.Id=r.DestinationPlanningId WHERE r.OriginPlanningId=${MOVE.id}`);
    check("Reprogramar: se guarda igual con el motivo «0»", moved === `0|${moveDest[0]}|Reprogramada|Aprobada`, moved);
    await shot(admin, "B3_reprogramada");
  }

  // ───── C. Suspender un día (con categoría) y reponerlo ─────
  await admin.goto(`${BASE}/LaboratoryOperations?managementId=${MGMT}`);
  await settle(admin);
  // El panel abre con un aviso emergente de conflictos del día que tapa el botón unos segundos: se cierra primero.
  await admin.evaluate(() => { if (window.Swal && Swal.isVisible && Swal.isVisible()) Swal.close(); });
  await admin.locator(".swal2-container").waitFor({ state: "detached", timeout: 10000 }).catch(() => {});
  await admin.click('[data-target="#modal-reponer-dia"]');
  await admin.locator("#modal-reponer-dia").waitFor({ state: "visible" });
  await admin.fill("#recovery-date", SUSPEND.date);
  await admin.dispatchEvent("#recovery-date", "change");
  await admin.fill("#recovery-reason", "Bloqueo de la avenida por la marcha");
  await admin.locator("#recovery-confirm:not([disabled])").waitFor({ timeout: 15000 });
  const categoryRequired = await admin.locator("#recovery-category").evaluate(el => el.required && !el.checkValidity());
  check("Suspender: la categoría del motivo es obligatoria", categoryRequired);
  await admin.selectOption("#recovery-category", "Paro o movilización");
  await shot(admin, "C1_suspender_dia");
  await admin.click("#recovery-confirm");
  await admin.locator(".swal2-title", { hasText: "Suspender el día" }).waitFor({ state: "visible" });
  await Promise.all([admin.waitForNavigation(), admin.locator(".swal2-confirm").click()]);
  await settle(admin);
  const recovery = sql(`SELECT TOP 1 CONCAT(Id,'|',ReasonCategory,'|',Reason) FROM DayRecoveries WHERE Date='${SUSPEND.date}' ORDER BY Id DESC`).split("|");
  check("Suspender: la reposición guarda categoría y motivo", /^Paro o movilizaci.n$/.test(recovery[1]) && recovery[2] === "Bloqueo de la avenida por la marcha", recovery.join(" · "));
  const recoveryId = recovery[0];
  await shot(admin, "C2_grilla_reposicion_en_curso");

  await admin.goto(`${BASE}/LaboratoryOperations/Reposiciones?managementId=${MGMT}`);
  await settle(admin);
  check("Reposiciones: filtro por categoría con su cantidad", await admin.locator("#rp-categories a", { hasText: "Paro o movilización (" }).count() === 1);
  await admin.locator("#rp-categories a", { hasText: "Paro o movilización (" }).click();
  await settle(admin);
  check("Reposiciones: al filtrar queda el día suspendido", admin.url().includes("category=") && (await admin.locator("body").innerText()).includes("17/12/2026"));
  await shot(admin, "C3_reposiciones_filtradas");

  await admin.goto(`${BASE}/LaboratoryOperations/Grilla?reponer=${SUSPEND.planning}&recoveryId=${recoveryId}`);
  await settle(admin);
  const recBand = admin.locator("#gr-rs-band");
  const recBandText = await recBand.innerText().catch(() => "");
  check("Reponer: banda naranja con día, categoría y pendientes", await recBand.evaluate(el => el.classList.contains("gr-rs-band--recovery")).catch(() => false)
    && /Estás reponiendo un día suspendido/i.test(recBandText) && /Paro o movilización/.test(recBandText) && /quedan\s+1\s+de\s+1/i.test(recBandText), recBandText.replace(/\s+/g, " ").slice(0, 140));
  const recDest = await pickDestination(admin, SUSPEND.candidates);
  check("Reponer: se fija un destino libre", !!recDest, recDest ? recDest.join(" P") : "ningún candidato quedó libre");
  if (recDest) {
    if (await admin.locator("#gr-rs-accept-wrap").isVisible()) await admin.locator('label[for="gr-rs-accept"]').click();
    await admin.fill("#gr-rs-reason", "Se repone el miércoles porque el laboratorio está libre");
    await shot(admin, "C4_reponer_resumen");
    await admin.click(".gr-rs-submit");
    // Motivo de más de 5 palabras: va directo a la confirmación, sin el aviso de motivo corto.
    await admin.locator(".swal2-title").waitFor({ state: "visible" });
    const directTitle = await admin.locator(".swal2-title").innerText();
    check("Reponer: con motivo largo no aparece el aviso de motivo corto", !/Motivo muy corto/i.test(directTitle), directTitle);
    await Promise.all([admin.waitForNavigation(), admin.locator(".swal2-confirm").click()]);
    await settle(admin);
    const rec = sql(`SELECT CONCAT(o.Status,'|',d.Status,'|',CONVERT(varchar(10),r.DestinationDate,23),'|',(SELECT Status FROM DayRecoveries WHERE Id=${recoveryId})) FROM Reschedulings r JOIN Plannings o ON o.Id=r.OriginPlanningId JOIN Plannings d ON d.Id=r.DestinationPlanningId WHERE r.DayRecoveryId=${recoveryId}`);
    check("Reponer: el origen queda Repuesta y la reposición Completada", rec === `Repuesta|Aprobada|${recDest[0]}|1`, rec);
    await shot(admin, "C5_repuesta");
  }

  // ───── D. Cargar la planificación desde Excel ─────
  await admin.goto(`${BASE}/LaboratoryOperations/Importar?managementId=${MGMT}`);
  await settle(admin);
  check("Importar: aparece en el menú y abre en el paso 1", await admin.locator('a[href*="/LaboratoryOperations/Importar"]').count() > 0 && await admin.locator("#im-steps").getAttribute("data-step") === "1");
  await shot(admin, "D1_importar_paso1");
  const [download] = await Promise.all([admin.waitForEvent("download"), admin.click("#im-template")]);
  const templatePath = resolve(OUT, "plantilla.xlsx");
  await download.saveAs(templatePath);
  check("Importar: la plantilla se descarga con el código de la gestión", /Planificacion_2026-2_plantilla\.xlsx/.test(download.suggestedFilename()), download.suggestedFilename());
  const base = { subject: IMPORT.subject, group: IMPORT.group, lab: IMPORT.lab, teacher: IMPORT.teacher, count: 2 };
  const rows = [
    { ...base, practice: "PRA-IGA3G8-01", date: "2026-12-01", start: 3 },                 // fila 2: nueva, repetida con la 6
    { ...base, practice: "PRA-IGA3G8-02", date: "2026-12-02", start: 3 },                 // fila 3: nueva y limpia
    { ...base, practice: IMPORT.existingPractice, date: "2026-12-01", start: 8 },         // fila 4: ya cargada → decidir
    { ...base, practice: "PRACTICA-INVENTADA", date: "2026-12-02", start: 8, teacher: "Pedro" }, // fila 5: práctica y docente con error
    { ...base, practice: "PRA-IGA3G8-01", date: "2026-12-03", start: 9 }                  // fila 6: repetida con la 2
  ];
  const rowsFile = resolve(OUT, "filas.json");
  const filledPath = resolve(OUT, "planificacion_prueba.xlsx");
  writeFileSync(rowsFile, JSON.stringify(rows));
  execSync(`python "${resolve(here, "planificacion_semestre_fill.py")}" "${templatePath}" "${filledPath}" "${rowsFile}"`, { encoding: "utf8" });
  await admin.setInputFiles("#im-file", filledPath);
  await Promise.all([admin.waitForNavigation(), admin.click('#im-upload-form button[type="submit"]')]);
  await settle(admin);
  const kpi = async (key) => Number(await admin.locator(`.im-kpi[data-ver="${key}"] .im-kpi__value`).innerText());
  const rowText = async (n) => (await admin.locator(`tr[data-row="${n}"]`).innerText().catch(() => "")).replace(/\s+/g, " ");
  const firstKpis = [await kpi("problemas"), await kpi("decidir"), await kpi("cargar"), await kpi("todas")];
  check("Importar: la revisión separa problemas (3), ya cargadas (1) y listas (1)", firstKpis.join(",") === "3,1,1,5", firstKpis.join(","));
  await admin.goto(admin.url() + "&ver=todas");
  await settle(admin);
  check("Importar: la fila repetida señala a la otra fila", /Fila repetida/.test(await rowText(2)) && /fila 6/.test(await rowText(2)) && /fila 2/.test(await rowText(6)));
  const errorRow = await rowText(5);
  check("Importar: el error dice la columna y sugiere el docente", /Práctica:/.test(errorRow) && /no existe/.test(errorRow) && /Docente:/.test(errorRow) && /Quiso decir/.test(errorRow), errorRow.slice(0, 160));
  const existingRow = await rowText(4);
  check("Importar: la ya cargada muestra «Aquí ya está» con enlace a la grilla", /Aquí ya está/.test(existingRow) && await admin.locator('tr[data-row="4"] a[href*="resaltar="]').count() === 1, existingRow.slice(0, 150));
  check("Importar: sin resolver no se puede confirmar", await admin.locator("#im-apply").count() === 0 && await admin.locator("#im-pending").count() === 1);
  await shot(admin, "D2_importar_revision");
  const [resultDownload] = await Promise.all([admin.waitForEvent("download"), admin.click("#im-result")]);
  check("Importar: se descarga el Excel con el resultado", /_revisado\.xlsx$/.test(resultDownload.suggestedFilename()), resultDownload.suggestedFilename());

  const decide = async (rowNumber, value) => {
    await Promise.all([admin.waitForNavigation(), admin.selectOption(`select[name="d[${rowNumber}]"]`, value)]);
    await settle(admin);
  };
  await decide(6, "skip");
  check("Importar: al omitir una de las repetidas, la otra queda lista", /Se cargará/.test(await rowText(2)) && /Omitida/.test(await rowText(6)));
  await decide(5, "skip");
  await decide(4, "overwrite");
  check("Importar: la ya cargada pasa a «Se sobrescribirá»", /Se sobrescribirá/.test(await rowText(4)));
  check("Importar: con todo resuelto aparece el paso 3", await admin.locator("#im-steps").getAttribute("data-step") === "3" && await admin.locator("#im-ready").count() === 1);
  await shot(admin, "D3_importar_listo");
  await admin.click("#im-apply");
  await admin.locator(".swal2-title", { hasText: "Cargar la planificación" }).waitFor({ state: "visible" });
  await Promise.all([admin.waitForNavigation(), admin.locator(".swal2-confirm").click()]);
  await settle(admin);
  const marks = Number(sql("SELECT MaxPlan FROM dbo.E2E_Marks"));
  const created = sql(`SELECT STRING_AGG(CONCAT(p.Status,' ',CONVERT(varchar(10),p.ScheduledDate,23),' P',cp.[Order],'x',p.PeriodCount,' ',l.Code,' req',q.n), '; ') WITHIN GROUP (ORDER BY p.ScheduledDate) FROM Plannings p JOIN ClassPeriods cp ON cp.Id=p.StartPeriodId JOIN Laboratories l ON l.Id=p.LaboratoryId JOIN PracticeSchedules s ON s.Id=p.PracticeScheduleId CROSS APPLY (SELECT COUNT(*) n FROM PlanningRequirements r WHERE r.PlanningId=p.Id) q WHERE p.Id > ${marks} AND s.ImportBatchId > (SELECT MaxBatch FROM dbo.E2E_Marks)`);
  check("Importar: se crean las 2 prácticas nuevas, aprobadas y con código", /^Aprobada 2026-12-01 P3x2 H-07 req\d+; Aprobada 2026-12-02 P3x2 H-07 req\d+$/.test(created)
    && Number(sql(`SELECT COUNT(*) FROM Plannings WHERE Id > ${marks} AND Code LIKE 'PRA-%' AND PracticeScheduleId IN (SELECT Id FROM PracticeSchedules WHERE ImportBatchId > (SELECT MaxBatch FROM dbo.E2E_Marks))`)) === 2, created);
  const overwritten = sql(`SELECT CONCAT(CONVERT(varchar(10),p.ScheduledDate,23),' P',cp.[Order],'x',p.PeriodCount,' ',p.Status) FROM Plannings p JOIN ClassPeriods cp ON cp.Id=p.StartPeriodId WHERE p.Id=${IMPORT.existing}`);
  check("Importar: la sobrescrita se mueve y conserva su estado", overwritten === "2026-12-01 P8x2 Aprobada", overwritten);
  check("Importar: al terminar muestra la grilla con el mensaje de éxito", admin.url().includes("/Grilla") && /Planificación cargada/.test(await admin.locator("body").innerText()), admin.url().replace(BASE, ""));
  await shot(admin, "D4_importar_cargada_grilla");

  // ───── E. Docente: disponibilidad por semana y aviso ─────
  const teacher = await session(TEACHER.user, TEACHER.pw);
  check("Ingresa el docente", teacher.url().includes("/Docente"), teacher.url());
  const notice = sql(`SELECT TOP 1 CONCAT(n.Title,' · ',n.Message) FROM Notifications n JOIN Users u ON u.Id=n.UserId WHERE u.UserName='${TEACHER.user}' AND n.Id > (SELECT MaxNotif FROM dbo.E2E_Marks) ORDER BY n.Id`);
  check("Docente: recibió el aviso de su clase reprogramada, con el motivo", /Tu pr.ctica fue reprogramada/.test(notice) && /Motivo: 0$/.test(notice), notice.slice(0, 150));
  await teacher.goto(`${BASE}/Docente/Disponibilidad?view=semana&date=2026-12-01`);
  await settle(teacher);
  const weekLabel = await teacher.locator("#week option:checked").innerText();
  const labs = await teacher.locator(".dv-lab").count();
  check("Docente: vista por semana académica con todos los laboratorios", /Semana \d+/.test(weekLabel) && labs > 1, `${weekLabel.trim()} · ${labs} laboratorios`);
  const busy = await teacher.locator('.dv-lab[data-lab="7"] .dc-cell.is-busy').count();
  check("Docente: lo cargado por Excel se ve ocupado, sin datos del otro docente", busy >= 4 && !/Pedro/i.test(await teacher.locator('.dv-lab[data-lab="7"]').innerText()), `${busy} celdas ocupadas en H-07`);
  await shot(teacher, "E1_docente_semana");
  await teacher.goto(`${BASE}/Docente/Disponibilidad?view=semana&date=${moveDest ? moveDest[0] : "2026-12-14"}&labId=6`);
  await settle(teacher);
  check("Docente: su clase reprogramada aparece como «Tuya»", await teacher.locator(".dc-cell.is-mine a.dv-link").count() >= 1, `${await teacher.locator(".dc-cell.is-mine").count()} celdas`);
  await shot(teacher, "E2_docente_semana_tuya");
  const freeLink = teacher.locator(".dc-cell.is-free a.dv-link").first();
  const freeHref = await freeLink.getAttribute("href");
  await Promise.all([teacher.waitForNavigation(), freeLink.click()]);
  await settle(teacher);
  check("Docente: tocar una celda libre abre la solicitud con día, periodo y laboratorio", /NuevaSolicitud/.test(teacher.url()) && /date=/.test(freeHref) && /startPeriodId=/.test(freeHref) && /labId=6/.test(freeHref), freeHref);
  await shot(teacher, "E3_docente_solicitud_precargada");

  // ───── F. Teléfono ─────
  const adminPhone = await session("super.qa", creds["admin.qa"], phoneOptions());
  await phoneCheck(adminPhone, "/LaboratoryOperations/Solicitudes?tab=todas", "F1_bandeja");
  await phoneCheck(adminPhone, `/LaboratoryOperations/Importar?managementId=${MGMT}`, "F2_importar");
  await phoneCheck(adminPhone, `/LaboratoryOperations/Reposiciones?managementId=${MGMT}`, "F3_reposiciones");
  const teacherPhone = await session(TEACHER.user, TEACHER.pw, phoneOptions());
  await phoneCheck(teacherPhone, "/Docente/Disponibilidad?view=semana&date=2026-12-01&labId=7", "F4_docente_semana");

  check("Sin errores de JavaScript en el recorrido", jsErrors.length === 0, jsErrors.slice(0, 3).join(" | "));
} catch (e) {
  check("El recorrido terminó sin excepciones", false, String(e).split("\n")[0]);
} finally {
  await browser.close();
  try { restore(); console.log("Copia restaurada."); } catch (e) { console.error("NO se pudo restaurar la copia: " + String(e).split("\n")[0]); }
}
const failed = results.filter(r => !r.ok);
writeFileSync(resolve(OUT, "resultado.json"), JSON.stringify({ when: new Date().toISOString(), base: BASE, db: DB, results }, null, 2));
console.log(`\n${results.length - failed.length}/${results.length} verificaciones correctas. Capturas en ${OUT}`);
process.exit(failed.length ? 1 : 0);
