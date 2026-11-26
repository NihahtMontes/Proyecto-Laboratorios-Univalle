// Grilla única (2026-11-24): un laboratorio debe verse IGUAL de ocupado para el administrador (Grilla) y para el
// docente (Disponibilidad, vista semana), incluidos los cupos de práctica libre. Además: sin «Tuya», sin periodos
// completos de «Limpieza» y con la franja de limpieza de 10 minutos con su hora real, incluida la de 13:55–14:05
// de una práctica que termina en P7 (capturas 03 y 04).
// Escribe en la base (abre un cupo de práctica libre), así que solo corre contra una COPIA.
//
// Uso: E2E_BASE=http://localhost:5099 E2E_DB=DB_Laboratorios_Univalle_EXP_QA E2E_CRED_FILE=<archivo de Tools/QaTestUsers> \
//      E2E_TEACHER_USER=docente.a E2E_TEACHER_PW=<clave> [E2E_LAB=4] [E2E_DATE=2026-12-01] node grilla_unica.mjs
// La cuenta docente la crea docente_casos.mjs (correrlo antes en la misma copia).
import { chromium } from "playwright";
import { execSync } from "node:child_process";
import { mkdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE || "http://localhost:5099";
const DB = process.env.E2E_DB || "";
if (!DB || /^(DB_Laboratorios_Univalle|DB_Laboratorios_Univalle_REAL|DB_Laboratorios_Univalle_EXP)$/i.test(DB)) {
  console.error("SEGURIDAD: indique E2E_DB con una COPIA desechable (nunca REAL, EXP ni la oficial).");
  process.exit(2);
}
const creds = Object.fromEntries([...readFileSync(process.env.E2E_CRED_FILE, "utf8").matchAll(/\|\s*`([^`]+)`\s*\|[^|]*\|\s*`([^`]+)`\s*\|/g)].map(m => [m[1], m[2]]));
const TEACHER = process.env.E2E_TEACHER_USER || "docente.a";
const TEACHER_PW = process.env.E2E_TEACHER_PW;
if (!TEACHER_PW) { console.error("Falta E2E_TEACHER_PW"); process.exit(2); }
const LAB = Number(process.env.E2E_LAB || 4);
const DATE = process.env.E2E_DATE || "2026-12-01";

const OUT = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "outputs", "e2e", "grilla_unica");
mkdirSync(OUT, { recursive: true });
const sql = (q) => execSync(`sqlcmd -S localhost -d ${DB} -E -C -W -h -1 -I -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`, { encoding: "utf8" }).trim();
const results = [];
const check = (name, ok, detail = "") => { results.push({ name, ok: !!ok }); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };

const browser = await chromium.launch({ headless: true });
const jsErrors = [];
async function session(user, pw) {
  const ctx = await browser.newContext({ locale: "es-BO", viewport: { width: 1500, height: 950 } });
  const page = await ctx.newPage();
  page.on("pageerror", e => jsErrors.push(`${page.url()} · ${e.message}`));
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', user);
  await page.fill('[name="Input.Password"]', pw);
  await Promise.all([page.waitForLoadState("networkidle"), page.locator('form button[type="submit"]').first().click()]);
  return page;
}

try {
  const labCode = sql(`SELECT Code FROM Laboratories WHERE Id=${LAB}`);
  // Un cupo de práctica libre en la semana, por la pantalla de cupos (miércoles, P7–P8), si ese tramo está libre.
  const wednesday = new Date(`${DATE}T00:00:00`); wednesday.setDate(wednesday.getDate() + ((3 - wednesday.getDay() + 7) % 7));
  const wed = wednesday.toISOString().slice(0, 10);
  const admin = await session("admin.qa", creds["admin.qa"]);
  await admin.goto(`${BASE}/PracticaLibre/Cupos`);
  const p7 = sql(`SELECT Id FROM ClassPeriods WHERE [Order]=7`);
  const before = Number(sql(`SELECT COUNT(*) FROM FreePracticeSlots WHERE LaboratoryId=${LAB} AND CAST(Date AS date)='${wed}'`));
  await admin.evaluate(([lab, date, start]) => {
    const f = [...document.querySelectorAll("form")].find(x => (x.getAttribute("action") || "").includes("handler=Create"));
    f.querySelector('[name="Input.LaboratoryId"]').value = lab;
    f.querySelector('[name="Input.Date"]').value = date;
    f.querySelector('[name="Input.StartPeriodId"]').value = start;
    f.querySelector('[name="Input.PeriodCount"]').value = "2";
    f.querySelector('[name="Input.Capacity"]').value = "6";
    HTMLFormElement.prototype.submit.call(f);
  }, [String(LAB), wed, p7]);
  await admin.waitForLoadState("networkidle");
  const after = Number(sql(`SELECT COUNT(*) FROM FreePracticeSlots WHERE LaboratoryId=${LAB} AND CAST(Date AS date)='${wed}'`));
  check(`Cupo de práctica libre abierto en ${labCode} el ${wed} P7–P8`, after === before + 1 || after > 0, `${before} → ${after}`);

  // ── Grilla del administrador
  await admin.goto(`${BASE}/LaboratoryOperations/Grilla?laboratoryId=${LAB}&date=${DATE}`);
  await admin.waitForLoadState("networkidle");
  await admin.screenshot({ path: resolve(OUT, "01_admin_grilla.png"), fullPage: true });
  const adminBusy = new Set(await admin.$$eval(".gr-cell.gr-cell--busy", els => els.map(e => `${e.dataset.date}#${e.dataset.order}`)));
  const adminDays = new Set(await admin.$$eval(".gr-cell", els => els.map(e => e.dataset.date)));
  const adminText = await admin.locator("body").innerText();
  check("Grilla admin: hay celdas ocupadas en la semana", adminBusy.size > 0, `${adminBusy.size} celdas`);
  check("Grilla admin: dibuja el cupo de práctica libre", /Práctica libre|PRÁC\. LIBRE/i.test(adminText));
  // «Limpieza» y la hora son dos piezas: en bloques angostos la hora baja a otra línea (\s+ acepta el salto).
  check("Grilla admin: franja de limpieza con hora real", /Limpieza\s+\d\d:\d\d–\d\d:\d\d/.test(adminText));
  check("Grilla admin: no marca periodos completos de separación", (await admin.locator(".gr-cell--buffer").count()) === 0);

  // ── Disponibilidad del docente (vista semana del mismo laboratorio)
  const teacher = await session(TEACHER, TEACHER_PW);
  await teacher.goto(`${BASE}/Docente/Disponibilidad?view=semana&date=${DATE}&labId=${LAB}`);
  await teacher.waitForLoadState("networkidle");
  await teacher.screenshot({ path: resolve(OUT, "02_docente_disponibilidad.png"), fullPage: true });
  const year = DATE.slice(0, 4);
  const teacherBusy = await teacher.evaluate(([code, y]) => {
    const busy = [];
    const days = [];
    for (const h of document.querySelectorAll("h5")) {
      if (!h.textContent.includes(code)) continue;
      const table = h.closest("section, .card, div")?.parentElement?.querySelector("table.dc-grid") || h.parentElement.parentElement.querySelector("table.dc-grid");
      if (!table) continue;
      const heads = [...table.querySelectorAll("thead th")].slice(1).map(th => {
        const m = (th.querySelector("small")?.textContent || "").match(/(\d\d)\/(\d\d)/);
        return m ? `${y}-${m[2]}-${m[1]}` : null;
      });
      days.push(...heads.filter(Boolean));
      for (const tr of table.querySelectorAll("tbody tr")) {
        // El encabezado de fila es «P7» seguido de la hora en un <small> («P713:05–13:55» como texto): se lee solo el primer nodo.
        const head = tr.querySelector("th");
        const order = ((head?.firstChild?.textContent || "").match(/P(\d{1,2})/) || (head?.textContent || "").match(/P(\d{1,2})(?=\d{2}:\d{2})/))?.[1];
        [...tr.querySelectorAll("td")].forEach((td, i) => {
          if (/\bis-(busy|slot|mine)\b/.test(td.className) && heads[i]) busy.push(`${heads[i]}#${order}`);
        });
      }
      break;
    }
    return { busy, days };
  }, [labCode, year]);
  const teacherText = await teacher.locator("body").innerText();
  check("Docente: encuentra la tabla del laboratorio", teacherBusy.days.length > 0, `${teacherBusy.days.length} días`);
  check("Docente: no aparece «Tuya»", !/\bTuya\b/.test(teacherText));
  check("Docente: la leyenda no habla de «Limpieza entre prácticas»", !/Limpieza entre prácticas/.test(teacherText));

  // ── Comparación celda por celda en los días que muestran ambas grillas
  const common = new Set(teacherBusy.days.filter(d => adminDays.has(d)));
  const a = [...adminBusy].filter(k => common.has(k.split("#")[0])).sort();
  const t = [...new Set(teacherBusy.busy)].filter(k => common.has(k.split("#")[0])).sort();
  const onlyAdmin = a.filter(k => !t.includes(k));
  const onlyTeacher = t.filter(k => !a.includes(k));
  check(`Mismas celdas ocupadas en ${labCode} para admin y docente (${common.size} días)`, common.size > 0 && onlyAdmin.length === 0 && onlyTeacher.length === 0,
    `admin ${a.length} · docente ${t.length} · solo admin [${onlyAdmin.slice(0, 6).join(" ")}] · solo docente [${onlyTeacher.slice(0, 6).join(" ")}]`);
  check(`El cupo del ${wed} P7–P8 está ocupado en ambas`, a.includes(`${wed}#7`) && t.includes(`${wed}#7`));

  // ── Limpieza de 13:55: una práctica que termina en P7 (13:55) limpia en el receso 13:55–14:05.
  // Se busca en la copia la próxima práctica que ocupa y termina en P7, desde E2E_DATE, en cualquier laboratorio.
  const p7End = sql(`SELECT TOP 1 CONCAT(p.LaboratoryId,'|',CONVERT(varchar(10),p.ScheduledDate,120)) FROM Plannings p JOIN ClassPeriods sp ON sp.Id=p.StartPeriodId WHERE p.IsDeleted=0 AND p.LaboratoryId IS NOT NULL AND sp.[Order]+p.PeriodCount-1=7 AND p.Status NOT IN ('Cancelada','Reprogramada','PendienteReposicion','Repuesta','Solicitada','Observada','Rechazada') AND p.ScheduledDate >= '${DATE}' ORDER BY p.ScheduledDate, p.Id`);
  if (!p7End) {
    check("Hay una práctica que termina en P7 para revisar la limpieza de 13:55", false, `ninguna desde ${DATE} en esta copia`);
  } else {
    const [cleanLab, cleanDate] = p7End.split("|");
    await admin.goto(`${BASE}/LaboratoryOperations/Grilla?laboratoryId=${cleanLab}&date=${cleanDate}`);
    await admin.waitForLoadState("networkidle");
    const strip = admin.locator(".gr-block__clean", { hasText: "13:55–14:05" }).first();
    const found = await strip.count() > 0;
    check(`Práctica que termina en P7 (laboratorio ${cleanLab}, ${cleanDate}): franja «Limpieza 13:55–14:05»`, found);
    // La hora debe VERSE entera, no solo estar en el HTML: su caja no puede quedar cortada por la franja.
    const timeBox = found ? await strip.locator(".gr-block__clean-time").boundingBox() : null;
    const stripBox = found ? await strip.boundingBox() : null;
    check("La hora de la limpieza se ve completa aunque el bloque sea angosto",
      !!timeBox && !!stripBox && timeBox.x + timeBox.width <= stripBox.x + stripBox.width + 0.5,
      timeBox && stripBox ? `hora ${Math.round(timeBox.width)}px · franja ${Math.round(stripBox.width)}px` : "");
    await admin.screenshot({ path: resolve(OUT, "03_limpieza_1355.png"), fullPage: true });
    if (found) await admin.locator(".gr-block", { has: strip }).first().screenshot({ path: resolve(OUT, "04_limpieza_1355_bloque.png") });
  }
  check("Sin errores de JavaScript", jsErrors.length === 0, jsErrors.slice(0, 3).join(" | "));
} catch (e) {
  check("El recorrido terminó sin excepciones", false, e.message);
} finally {
  await browser.close();
}
const ok = results.filter(r => r.ok).length;
console.log(`\n${ok}/${results.length} comprobaciones correctas. Capturas en ${OUT}`);
process.exit(ok === results.length ? 0 : 1);
