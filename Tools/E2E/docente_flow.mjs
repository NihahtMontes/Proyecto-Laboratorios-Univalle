// Recorrido de punta a punta del flujo docente (Sprint 3) con navegador real (Chromium), sobre una COPIA de la base.
// Alta del docente → cambio de contraseña → solicitud → revisión con reparto por origen → salida L-3 desde varios
// orígenes (con un consumible) → «Finalizar sin novedades» → el docente ve la práctica realizada.
//
// Uso (la app debe estar corriendo contra la copia):
//   E2E_BASE=http://localhost:5099 E2E_DB=DB_Laboratorios_Univalle_EXP E2E_CRED_FILE=<archivo privado de Tools/QaTestUsers> \
//   E2E_SCENARIO='{"personId":41,"groupId":128,"practiceId":85,"date":"2026-11-17","startPeriodId":3,"labId":1,
//                  "lines":[{"articleId":522,"quantity":7},{"articleId":1270,"quantity":2}]}' node docente_flow.mjs
// Escribe en la base (crea un usuario, una solicitud y su salida): por eso se niega a correr contra REAL u oficial.
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
const credFile = process.env.E2E_CRED_FILE;
if (!credFile) { console.error("Falta E2E_CRED_FILE"); process.exit(2); }
const creds = Object.fromEntries([...readFileSync(credFile, "utf8").matchAll(/\|\s*`([^`]+)`\s*\|[^|]*\|\s*`([^`]+)`\s*\|/g)].map(m => [m[1], m[2]]));
const S = JSON.parse(process.env.E2E_SCENARIO || "{}");
const TEACHER = `docente.e2e.${Date.now().toString(36)}`;
const TEMP_PW = "Temporal#2026xY";
const NEW_PW = "Docente#2026Seguro";

const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "e2e", "docente");
mkdirSync(OUT, { recursive: true });

const sql = (q) => execSync(`sqlcmd -S localhost -d ${DB} -E -C -W -h -1 -I -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`, { encoding: "utf8" }).trim();
const results = [];
const check = (name, ok, detail = "") => { results.push({ name, ok: !!ok, detail }); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };

const browser = await chromium.launch({ headless: true });
const errors = [];
async function newPage() {
  const ctx = await browser.newContext({ locale: "es-BO", viewport: { width: 1400, height: 900 } });
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
const shot = (page, name) => page.screenshot({ path: resolve(OUT, `${name}.png`), fullPage: true });
const confirmSwal = async (page) => { await page.locator(".swal2-confirm").waitFor(); await page.locator(".swal2-confirm").click(); };

try {
  // 0) Corridas anteriores en la copia: se libera la persona docente (la cuenta vieja queda eliminada lógicamente).
  sql(`UPDATE Users SET PersonId=NULL, Role=99, Status=2 WHERE PersonId=${S.personId} AND UserName LIKE 'docente.%'`);

  // 1) El administrador crea la cuenta del docente vinculada a su persona del catálogo.
  const admin = await newPage();
  await login(admin, "admin.qa", creds["admin.qa"]);
  await admin.goto(`${BASE}/Users/Create?role=4&personId=${S.personId}`);
  await admin.fill('[name="Input.UserName"]', TEACHER);
  await admin.fill('[name="Input.Email"]', `${TEACHER}@qa.univalle.test`);
  await admin.fill('[name="Input.FirstName"]', "Docente");
  await admin.fill('[name="Input.LastName"]', "Prueba");
  await admin.fill('[name="Input.IdentityCard"]', `Q${Date.now().toString().slice(-8)}`);
  await admin.fill('[name="Input.PhoneNumber"]', "70000004");
  await admin.fill('[name="Input.Password"]', TEMP_PW);
  await shot(admin, "01_alta_docente");
  await Promise.all([admin.waitForLoadState("networkidle"), admin.locator('form button[type="submit"]').last().click()]);
  const teacherRow = sql(`SELECT CONCAT(Role,'|',PersonId,'|',MustChangePassword) FROM Users WHERE UserName='${TEACHER}'`);
  check("Alta del docente con persona vinculada y cambio de clave obligatorio", teacherRow === `4|${S.personId}|1`, teacherRow);

  // 2) Primer ingreso del docente: debe cambiar la contraseña.
  const teacher = await newPage();
  await login(teacher, TEACHER, TEMP_PW);
  check("Primer ingreso redirige a Cambiar contraseña", teacher.url().includes("/Account/CambiarClave"), teacher.url());
  await teacher.fill('[name="Input.CurrentPassword"]', TEMP_PW);
  await teacher.fill('[name="Input.NewPassword"]', NEW_PW);
  await teacher.fill('[name="Input.ConfirmPassword"]', NEW_PW);
  await Promise.all([teacher.waitForLoadState("networkidle"), teacher.locator('button:has-text("Guardar contraseña")').click()]);
  await teacher.goto(`${BASE}/Docente`);
  await shot(teacher, "02_portal_docente");
  check("Tras cambiar la clave entra a su portal", teacher.url().includes("/Docente"), teacher.url());
  await teacher.goto(`${BASE}/Users`);
  check("El docente no puede abrir pantallas de gestión", !teacher.url().includes("/Users"), teacher.url());

  // 3) Solicitud: se abre el asistente y se envía el formulario final (la revalidación completa es del servidor).
  await teacher.goto(`${BASE}/Docente/NuevaSolicitud`);
  await shot(teacher, "03_asistente");
  await teacher.evaluate((s) => {
    const set = (id, v) => { document.getElementById(id).value = v; };
    set("f_group", s.groupId); set("f_practice", s.practiceId); set("f_students", 20); set("f_date", s.date);
    set("f_start", s.startPeriodId); set("f_count", 2); set("f_lab", s.labId); set("f_obs", "Solicitud E2E");
    set("f_lines", JSON.stringify(s.lines));
    HTMLFormElement.prototype.submit.call(document.getElementById("submitForm"));
  }, S);
  await teacher.waitForLoadState("networkidle");
  const planId = Number(new URL(teacher.url()).searchParams.get("id"));
  await shot(teacher, "04_solicitud_enviada");
  check("La solicitud queda En revisión", sql(`SELECT Status FROM Plannings WHERE Id=${planId || 0}`) === "Solicitada", `id ${planId}`);

  // 4) El revisor ve el reparto sugerido por origen y aprueba.
  await admin.goto(`${BASE}/LaboratoryOperations/Solicitudes/Revisar?id=${planId}`);
  await shot(admin, "05_revision_origenes");
  const originInputs = await admin.locator(".dc-origin-qty").count();
  check("La revisión muestra el reparto por origen", originInputs >= 2, `${originInputs} campos de origen`);
  await admin.locator("#btnApprove").click();
  await confirmSwal(admin);
  await admin.waitForLoadState("networkidle");
  const status = sql(`SELECT Status FROM Plannings WHERE Id=${planId}`);
  const assigned = sql(`SELECT STRING_AGG(CONCAT(l.Code,':',a.Quantity), ' ') FROM PlanningAssignments a JOIN PlanningRequirements r ON r.Id=a.PlanningRequirementId JOIN Laboratories l ON l.Id=a.LaboratoryId WHERE r.PlanningId=${planId}`);
  check("Aprobada con asignaciones por origen", status === "Aprobada" && assigned.length > 0, `${status} · ${assigned}`);

  // 5) Salida L-3 desde varios orígenes; el consumible descuenta su existencia.
  const consumable = S.lines.find(l => l.consumable) || S.lines[S.lines.length - 1];
  const stockBefore = Number(sql(`SELECT SUM(AvailableQuantity) FROM ArticleStocks WHERE ArticleId=${consumable.articleId}`));
  await admin.goto(`${BASE}/LaboratoryOperations/L3?id=${planId}`);
  await shot(admin, "06_entrega");
  await admin.locator(".js-departure-confirm").click();
  await confirmSwal(admin);
  await admin.waitForLoadState("networkidle");
  const items = sql(`SELECT STRING_AGG(CONCAT(i.ArticleId,'@',ISNULL(o.Code,'-'),':',i.Quantity), ' ') FROM DepartureItems i JOIN Departures d ON d.Id=i.DepartureId LEFT JOIN Laboratories o ON o.Id=i.OriginLaboratoryId WHERE d.PlanningId=${planId}`);
  const stockAfter = Number(sql(`SELECT SUM(AvailableQuantity) FROM ArticleStocks WHERE ArticleId=${consumable.articleId}`));
  check("Salida con ítems por origen", items.split(" ").length >= 3, items);
  check("El consumible descuenta su existencia al entregar", stockAfter === stockBefore - consumable.quantity, `${stockBefore} → ${stockAfter}`);
  await shot(admin, "07_salida_registrada");

  // 6) Finalizar sin novedades: devuelve lo pendiente reutilizable y finaliza.
  await admin.locator("#btnFinalizeClean").click();
  await admin.locator(".swal2-select").waitFor();
  await admin.selectOption(".swal2-select", { index: 1 });
  await admin.locator(".swal2-confirm").click();
  await admin.waitForLoadState("networkidle");
  const closed = sql(`SELECT CONCAT(p.Status,'|',d.Status) FROM Plannings p JOIN Departures d ON d.PlanningId=p.Id WHERE p.Id=${planId}`);
  check("Finalizada sin novedades y salida devuelta", closed === "Finalizada|1", closed);
  await shot(admin, "08_finalizada");

  // 7) El docente ve su práctica realizada.
  await teacher.goto(`${BASE}/Docente/Solicitud?id=${planId}`);
  await shot(teacher, "09_docente_realizada");
  check("El docente ve la práctica Realizada", (await teacher.content()).includes("Realizada"));
} catch (e) {
  check("Recorrido sin excepciones", false, e.message.split("\n")[0]);
} finally {
  check("Sin errores de JavaScript en las páginas", errors.length === 0, errors.slice(0, 3).join(" | "));
  writeFileSync(resolve(OUT, "resultado.json"), JSON.stringify({ base: BASE, db: DB, results, errors }, null, 2));
  await browser.close();
  const fails = results.filter(r => !r.ok).length;
  console.log(`\n${results.length - fails}/${results.length} comprobaciones correctas. Capturas en ${OUT}`);
  process.exit(fails ? 1 : 0);
}
