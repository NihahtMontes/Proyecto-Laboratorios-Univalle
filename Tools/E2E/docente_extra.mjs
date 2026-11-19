// QA complementaria del flujo docente en navegador real (Chromium) sobre una COPIA de la base:
//  A) solicitar desde el cronograma con un número de estudiantes propio (Planning.StudentCount),
//  B) incidencia L-5 de un artículo entregado desde dos orígenes (descuento por origen de cada ítem),
//  C) pantallas del docente y del revisor en tamaño teléfono (sin desborde horizontal).
// Requiere haber corrido antes docente_casos.mjs en la misma copia (crea la cuenta docente.a).
// Uso: E2E_BASE=http://localhost:5099 E2E_DB=DB_Laboratorios_Univalle_EXP E2E_CRED_FILE=<archivo privado> node docente_extra.mjs
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
const A = { person: 41, group: 128, user: "docente.a", pw: "DocenteA#2026Seguro" + "x" };
const PALA = 522, LAB = 1, DAY = process.env.E2E_DAY || "2026-11-23";

const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "e2e", "docente_extra");
mkdirSync(OUT, { recursive: true });
const sql = (q) => execSync(`sqlcmd -S localhost -d ${DB} -E -C -W -h -1 -I -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`, { encoding: "utf8" }).trim();
const results = [];
const check = (name, ok, detail = "") => { results.push({ name, ok: !!ok, detail }); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };
const settle = (page) => page.waitForLoadState("networkidle");
const shot = (page, name) => page.screenshot({ path: resolve(OUT, `${name}.png`), fullPage: true });
const swalConfirm = async (page) => { await page.locator(".swal2-confirm").waitFor(); await page.locator(".swal2-confirm").click(); };

const browser = await chromium.launch({ headless: true });
const jsErrors = [];
async function session(user, pw, options = { viewport: { width: 1400, height: 900 } }) {
  const ctx = await browser.newContext({ locale: "es-BO", ...options });
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
      .filter(el => { const r = el.getBoundingClientRect(); return r.width > 0 && r.right > window.innerWidth + 2 && !el.closest(".ph-tabs, .table-responsive, .dataTables_wrapper, .select2-container, [style*='overflow']"); })
      .slice(0, 3).map(el => `${el.tagName.toLowerCase()}${el.id ? "#" + el.id : ""}${el.className && typeof el.className === "string" ? "." + el.className.trim().split(/\s+/).slice(0, 2).join(".") : ""}`);
    return { scroll: document.documentElement.scrollWidth, width: window.innerWidth, wide };
  });
  await shot(page, name);
  check(`Teléfono ${name.replace(/^[A-Z]\d+b?_/, "")} sin desborde ni elementos cortados`, overflow.scroll <= overflow.width + 2 && overflow.wide.length === 0,
    `${overflow.scroll}px de ${overflow.width}px${overflow.wide.length ? " · " + overflow.wide.join(", ") : ""}`);
}
const phoneOptions = () => { const o = { ...devices["iPhone 13"], locale: "es-BO" }; delete o.defaultBrowserType; return o; };
const stockAt = (lab) => Number(sql(`SELECT ISNULL(SUM(AvailableQuantity),0) FROM ArticleStocks WHERE ArticleId=${PALA} AND LaboratoryId=${lab}`));

// Las palas rotas en la parte B se restauran al final para no afectar a los demás recorridos sobre la copia.
const palaSnapshot = sql(`SELECT STRING_AGG(CONCAT(Id,':',AvailableQuantity), ',') FROM ArticleStocks WHERE ArticleId=${PALA}`);
try {
  const admin = await session("admin.qa", creds["admin.qa"]);
  const teacher = await session(A.user, A.pw);
  check("Ingresan el revisor y el docente", teacher.url().includes("/Docente") && !admin.url().includes("/Login"), teacher.url());

  // ───── A. Solicitar desde el cronograma con estudiantes propios ─────
  await teacher.goto(`${BASE}/Docente?tab=solicitar`);
  const solicitar = teacher.locator('a:has-text("Solicitar")[href*="scheduleId"]').first();
  if (await solicitar.count() === 0) throw new Error("No hay prácticas programadas por solicitar para el docente A");
  const scheduleId = Number(new URL(await solicitar.getAttribute("href"), BASE).searchParams.get("scheduleId"));
  const scheduleStudentsBefore = sql(`SELECT ISNULL(CAST(StudentCount AS varchar),'NULL') FROM PracticeSchedules WHERE Id=${scheduleId}`);
  await solicitar.click();
  await settle(teacher);
  await teacher.fill("#students", "13");
  await teacher.locator("#students").dispatchEvent("change");
  await teacher.locator(".swal2-cancel").click({ timeout: 1500 }).catch(() => {});
  await teacher.locator("#btnNext").click();
  // Paso 2: se elige un periodo libre en la grilla del laboratorio.
  await teacher.selectOption("#labSelect", String(LAB));
  await teacher.fill("#weekDate", DAY);
  await teacher.locator("#weekDate").dispatchEvent("change");
  const free = teacher.locator(`#weekGrid td[data-date="${DAY}"][data-state="free"]`).first();
  await free.waitFor({ timeout: 15000 });
  await free.click();
  await teacher.waitForTimeout(1200);
  for (let i = 0; i < 6 && !(await teacher.locator("#btnSubmit").isVisible()); i++) {
    await teacher.locator("#btnNext").click();
    await teacher.waitForTimeout(900);
  }
  await shot(teacher, "A1_resumen_desde_cronograma");
  await teacher.locator("#btnSubmit").click();
  await swalConfirm(teacher);
  await settle(teacher);
  const ra = Number(new URL(teacher.url()).searchParams.get("id"));
  const savedA = sql(`SELECT CONCAT(p.Status,'|',ISNULL(CAST(p.StudentCount AS varchar),'NULL'),'|',p.PracticeScheduleId) FROM Plannings p WHERE p.Id=${ra || 0}`);
  check("La solicitud desde el cronograma guarda sus 13 estudiantes", savedA === `Solicitada|13|${scheduleId}`, `id ${ra} · ${savedA}`);
  const scheduleStudentsAfter = sql(`SELECT ISNULL(CAST(StudentCount AS varchar),'NULL') FROM PracticeSchedules WHERE Id=${scheduleId}`);
  check("La programación compartida conserva su número", scheduleStudentsAfter === scheduleStudentsBefore, `${scheduleStudentsBefore} → ${scheduleStudentsAfter}`);
  await teacher.goto(`${BASE}/Docente/Solicitud?id=${ra}`);
  const teacherText = await teacher.locator("body").innerText();
  check("El docente ve 13 estudiantes en su solicitud", /Estudiantes[\s\S]{0,40}13/i.test(teacherText));
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${ra}`);
  await shot(admin, "A2_revisar_estudiantes");
  check("El revisor ve 13 estudiantes", /Estudiantes[\s\S]{0,40}13/i.test(await admin.locator("body").innerText()));
  // Se rechaza para no ocupar el horario de la parte B.
  await admin.locator("#btnReject").click();
  await admin.locator(".swal2-popup").waitFor();
  await admin.locator(".swal2-input:visible, .swal2-textarea:visible").first().fill("QA: solo se verificaba el número de estudiantes.");
  await admin.locator(".swal2-confirm").click();
  await settle(admin);

  // ───── B. Incidencia de un artículo entregado desde dos orígenes ─────
  // Corridas anteriores rompen palas: en la copia se repone el Economato a 5 para que 7 obliguen a usar dos orígenes.
  sql(`UPDATE s SET AvailableQuantity=5 FROM ArticleStocks s JOIN Laboratories l ON l.Id=s.LaboratoryId WHERE s.ArticleId=${PALA} AND l.Code='ECONO'`);
  await teacher.goto(`${BASE}/Docente/NuevaSolicitud`);
  const practice = Number(sql(`SELECT TOP 1 p.Id FROM Practices p JOIN CourseGroups g ON g.SubjectId=p.SubjectId WHERE g.Id=${A.group} AND p.Status=0 ORDER BY p.Id DESC`));
  await teacher.evaluate(([g, p, d, l, lns]) => {
    const set = (id, v) => { document.getElementById(id).value = v; };
    set("f_group", g); set("f_practice", p); set("f_students", 18); set("f_date", d); set("f_start", 3); set("f_count", 2);
    set("f_lab", l); set("f_lines", JSON.stringify(lns));
    HTMLFormElement.prototype.submit.call(document.getElementById("submitForm"));
  }, [A.group, practice, DAY, LAB, [{ articleId: PALA, quantity: 7 }]]);
  await settle(teacher);
  const rb = Number(new URL(teacher.url()).searchParams.get("id"));
  check("Solicitud de 7 palas enviada", sql(`SELECT Status FROM Plannings WHERE Id=${rb || 0}`) === "Solicitada", `id ${rb}`);
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${rb}`);
  await admin.locator("#btnApprove").click();
  await swalConfirm(admin);
  await settle(admin);
  const assigned = sql(`SELECT STRING_AGG(CONCAT(a.LaboratoryId,':',CAST(a.Quantity AS int)), ' ') WITHIN GROUP (ORDER BY a.Quantity DESC) FROM PlanningAssignments a JOIN PlanningRequirements r ON r.Id=a.PlanningRequirementId WHERE r.PlanningId=${rb}`);
  check("Aprobada con reparto en dos orígenes", assigned.split(" ").length === 2, assigned);
  await admin.goto(`${BASE}/LaboratoryOperations/L3?id=${rb}`);
  await admin.locator(".js-departure-confirm").click();
  await swalConfirm(admin);
  await settle(admin);
  const delivered = sql(`SELECT STRING_AGG(CONCAT(ISNULL(i.OriginLaboratoryId,d.OriginLaboratoryId),':',CAST(i.Quantity AS int)), ' ') WITHIN GROUP (ORDER BY i.Quantity DESC) FROM DepartureItems i JOIN Departures d ON d.Id=i.DepartureId WHERE d.PlanningId=${rb} AND i.ArticleId=${PALA}`);
  check("La salida entrega desde los dos orígenes", delivered.split(" ").length === 2, delivered);
  const [[mainLab, mainQty], [otherLab]] = delivered.split(" ").map(s => s.split(":").map(Number));
  const before = { main: stockAt(mainLab), other: stockAt(otherLab) };
  // Se rompen todas las del origen principal y una más: la última debe salir del segundo origen.
  const broken = mainQty + 1;
  await admin.goto(`${BASE}/LaboratoryOperations/L5?planId=${rb}`);
  await admin.selectOption('[name="Input.ArticleId"]', String(PALA));
  await admin.selectOption('[name="Input.IncidentType"]', { index: 1 });
  await admin.fill('[name="Input.AffectedQuantity"]', String(broken));
  await admin.selectOption('[name="Input.ResponsibleType"]', { index: 1 });
  await admin.fill('[name="Input.ResponsibleName"]', "Estudiante QA");
  await admin.fill('[name="Input.DamageDescription"]', "QA: palas rotas durante la práctica.");
  await shot(admin, "B1_incidencia");
  await Promise.all([settle(admin), admin.locator('form button[type="submit"]').last().click()]);
  await admin.locator(".swal2-confirm").click({ timeout: 2000 }).catch(() => {});
  await settle(admin);
  const incidents = sql(`SELECT COUNT(*) FROM L5Incidents WHERE PlanningId=${rb} AND ArticleId=${PALA} AND AffectedQuantity=${broken}`);
  const after = { main: stockAt(mainLab), other: stockAt(otherLab) };
  check("La incidencia queda registrada", incidents === "1", `${broken} palas`);
  check("Descuenta de cada origen lo que entregó", after.main === Math.max(0, before.main - mainQty) && after.other === Math.max(0, before.other - 1),
    `lab ${mainLab}: ${before.main} → ${after.main} · lab ${otherLab}: ${before.other} → ${after.other}`);
  await shot(admin, "B2_tras_incidencia");
  const phoneAdmin = await session("admin.qa", creds["admin.qa"], phoneOptions());
  await phoneCheck(phoneAdmin, `/LaboratoryOperations/L3?id=${rb}`, "B2b_entrega_abierta");
  check("En el teléfono se ven los dos botones de cierre", await phoneAdmin.locator("#btnFinalizeClean").isVisible() && await phoneAdmin.locator('#formFinalizeL3 button').isVisible());
  // Cierre: lo que no se rompió vuelve a su origen con «Finalizar sin novedades» y la práctica queda finalizada.
  await admin.goto(`${BASE}/LaboratoryOperations/L3?id=${rb}`);
  await admin.locator("#btnFinalizeClean").click();
  await admin.locator(".swal2-select").waitFor();
  await admin.selectOption(".swal2-select", { index: 1 });
  await admin.locator(".swal2-confirm").click();
  await settle(admin);
  const closedB = sql(`SELECT CONCAT(p.Status,'|',d.Status,'|',CAST(SUM(ISNULL(i.ReturnedQuantity,0)) AS int)) FROM Plannings p JOIN Departures d ON d.PlanningId=p.Id JOIN DepartureItems i ON i.DepartureId=d.Id AND i.ArticleId=${PALA} WHERE p.Id=${rb} GROUP BY p.Status,d.Status`);
  check("Tras la incidencia, «Finalizar sin novedades» devuelve el resto y cierra", closedB === `Finalizada|1|${7 - broken}`, closedB);
  await shot(admin, "B3_finalizada");

  // ───── C. Tamaño teléfono ─────
  const mt = await session(A.user, A.pw, phoneOptions());
  const ma = await session("admin.qa", creds["admin.qa"], phoneOptions());
  const pages = [
    [mt, "/Docente", "C1_docente_inicio"], [mt, "/Docente/NuevaSolicitud", "C2_docente_asistente"],
    [mt, "/Docente/Solicitudes", "C3_docente_solicitudes"], [mt, `/Docente/Solicitud?id=${rb}`, "C4_docente_detalle"],
    [ma, "/LaboratoryOperations/Solicitudes", "C5_bandeja"], [ma, `/LaboratoryOperations/Solicitudes/Revisar?id=${rb}`, "C6_revisar"],
    [ma, `/LaboratoryOperations/L3?id=${rb}`, "C7_entrega"], [ma, `/LaboratoryOperations/L5?planId=${rb}`, "C8_incidencia"],
  ];
  for (const [page, path, name] of pages) await phoneCheck(page, path, name);
} catch (e) {
  check("Recorrido sin excepciones", false, e.message.split("\n")[0]);
} finally {
  for (const pair of palaSnapshot.split(",").filter(Boolean)) {
    const [id, qty] = pair.split(":");
    sql(`UPDATE ArticleStocks SET AvailableQuantity=${Number(qty)} WHERE Id=${Number(id)}`);
  }
  check("Sin errores de JavaScript en las páginas", jsErrors.length === 0, jsErrors.slice(0, 3).join(" | "));
  writeFileSync(resolve(OUT, "resultado.json"), JSON.stringify({ base: BASE, db: DB, results, jsErrors }, null, 2));
  await browser.close();
  const fails = results.filter(r => !r.ok).length;
  console.log(`\n${results.length - fails}/${results.length} comprobaciones correctas. Capturas en ${OUT}`);
  process.exit(fails ? 1 : 0);
}
