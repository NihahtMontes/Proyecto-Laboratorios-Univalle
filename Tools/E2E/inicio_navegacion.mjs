// Recorrido E2E de las tareas del 16/11: planes de gestión reconstruidos, nuevo Inicio, menú renombrado,
// panel del preventivo, Reportes con equipos, paginador común (Usuarios, Personas, Gestiones) y teléfono.
// Escribe en la copia: la parte A reconstruye los planes (es lo que se quiere dejar hecho en EXP); el resto solo lee.
// Uso: E2E_BASE=http://localhost:5099 E2E_DB=DB_Laboratorios_Univalle_EXP E2E_CRED_FILE=<archivo privado> node inicio_navegacion.mjs
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
const SUPER = { user: "super.qa", pw: creds["admin.qa"] };      // misma contraseña que admin.qa en la copia
const ADMIN = { user: "admin.qa", pw: creds["admin.qa"] };
const SUPERVISOR = { user: "supervisor.qa", pw: creds["supervisor.qa"] };
const TEACHER = { user: "docente.a", pw: "DocenteA#2026Seguro" + "x" };
const MGMT = 4;            // 2026-2 preventivo (activa)
const LAB = 9;             // Aula demostrativa h-102: 11 equipos con verificación

const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "e2e", "inicio_navegacion");
mkdirSync(OUT, { recursive: true });
const sql = (q) => execSync(`sqlcmd -S localhost -d ${DB} -E -C -W -h -1 -I -Q "SET NOCOUNT ON; ${q.replace(/"/g, '\\"')}"`, { encoding: "utf8" }).trim();
const results = [];
const check = (name, ok, detail = "") => { results.push({ name, ok: !!ok, detail }); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };
const settle = (page) => page.waitForLoadState("networkidle");
const shot = (page, name) => page.screenshot({ path: resolve(OUT, `${name}.png`), fullPage: true });

const browser = await chromium.launch({ headless: true });
const jsErrors = [];
async function session(who, options = { viewport: { width: 1400, height: 900 } }) {
  const ctx = await browser.newContext({ locale: "es-BO", ...options });
  const page = await ctx.newPage();
  page.on("pageerror", e => jsErrors.push(`${page.url()} · ${e.message}`));
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', who.user);
  await page.fill('[name="Input.Password"]', who.pw);
  await Promise.all([settle(page), page.locator('form button[type="submit"]').first().click()]);
  return page;
}
async function noOverflow(page, name) {
  await settle(page);
  const o = await page.evaluate(() => {
    const wide = [...document.querySelectorAll("body *")]
      .filter(el => { const r = el.getBoundingClientRect(); return r.width > 0 && r.right > window.innerWidth + 2 && !el.closest(".table-responsive, .dataTables_wrapper, .select2-container, [style*='overflow']"); })
      .slice(0, 3).map(el => `${el.tagName.toLowerCase()}${el.id ? "#" + el.id : ""}${typeof el.className === "string" && el.className ? "." + el.className.trim().split(/\s+/).slice(0, 2).join(".") : ""}`);
    return { scroll: document.documentElement.scrollWidth, width: window.innerWidth, wide };
  });
  await shot(page, name);
  check(`Teléfono ${name.replace(/^[A-Z]\d+_/, "")} sin desborde`, o.scroll <= o.width + 2 && o.wide.length === 0,
    `${o.scroll}px de ${o.width}px${o.wide.length ? " · " + o.wide.join(", ") : ""}`);
}
const phone = () => { const o = { ...devices["iPhone 13"], locale: "es-BO" }; delete o.defaultBrowserType; return o; };
// textContent: incluye el submenú «Registros» aunque esté plegado (innerText omite lo oculto).
const menuText = async (page) => (await page.locator("aside.left-sidebar").textContent()).replace(/\s+/g, " ");

// ───── A. Planes de gestión reconstruidos (SuperAdmin) ─────
let page = await session(SUPER);
const plansBefore = Number(sql("SELECT COUNT(*) FROM ManagementPlans"));
await page.goto(`${BASE}/Managements/ReconstruirPlanes`);
await settle(page);
await shot(page, "A1_reconstruir_vista_previa");
const toCreateText = await page.locator("#rp-table").innerText().catch(() => "");
check("A1 Vista previa muestra la gestión 2026-2 con planes por crear", /2026-2/.test(toCreateText), `${plansBefore} planes antes`);
if (await page.locator("#rp-apply").isEnabled()) {
  await page.locator("#rp-apply").click();
  await page.locator(".swal2-title", { hasText: "Reconstruir" }).waitFor({ state: "visible", timeout: 15000 });
  await Promise.all([page.waitForNavigation({ timeout: 300000 }), page.locator(".swal2-confirm").click()]);
  await settle(page);
}
await shot(page, "A2_reconstruir_resultado");
const plansAfter = Number(sql("SELECT COUNT(*) FROM ManagementPlans"));
const dupPairs = Number(sql("SELECT COUNT(*) FROM (SELECT ManagementId, EquipmentUnitId FROM ManagementPlans WHERE EquipmentUnitId IS NOT NULL GROUP BY ManagementId, EquipmentUnitId HAVING COUNT(*) > 1) d"));
const lastBatch = sql("SELECT TOP 1 CONCAT(Code,'|',SourceType) FROM ImportBatches ORDER BY Id DESC");
check("A2 Se crearon los planes (1 → miles)", plansAfter > 1000, `${plansBefore} → ${plansAfter}`);
check("A2 Sin pares gestión-equipo duplicados", dupPairs === 0, `${dupPairs} duplicados`);
check("A2 Lote de trazabilidad PLANES-…", /^PLANES-\d{14}\|ReconstruccionPlanes$/.test(lastBatch), lastBatch);
check("A2 Practicas académicas no generan planes", Number(sql("SELECT COUNT(*) FROM ManagementPlans p JOIN Departures d ON d.Id=p.DepartureId WHERE d.PlanningId IS NOT NULL")) === 0);
await page.goto(`${BASE}/Managements/ReconstruirPlanes`);
await settle(page);
check("A3 Segunda vez: nada por crear (botón deshabilitado)", !(await page.locator("#rp-apply").isEnabled().catch(() => false)));

// ───── B. Inicio y menú (SuperAdmin) ─────
await page.goto(`${BASE}/`);
await settle(page);
check("B1 «/» lleva al nuevo Inicio", /\/(AssetView(\/Index)?|Inicio)$/i.test(new URL(page.url()).pathname), page.url());
const resumen = await page.locator("#resumen").innerText();
check("B1 Saludo según la hora", /Buen(os días|as tardes|as noches),/.test(resumen), resumen.split("\n")[0]);
for (const label of ["Reportar una falla", "Reportes", "Panel de planificación", "Adquisición"]) {
  check(`B1 Atajo «${label}»`, resumen.includes(label));
}
await shot(page, "B1_inicio_resumen");
const menu = await menuText(page);
check("B2 Menú sin «Inventario general»", !/Inventario general/.test(menu));
for (const label of ["L6 · Verificaciones", "L7 · Servicio técnico", "L8 · Kardex", "L3 · Salidas", "Adquisición"]) {
  check(`B2 Menú con «${label}»`, menu.includes(label));
}
await page.locator('a[href="#inventario"]').click();
await page.locator("#inventario").waitFor({ state: "visible" });
await shot(page, "B3_inicio_inventario");
check("B3 Pestaña Inventario con sus accesos", /Equipos|Utensilios/.test(await page.locator("#inventario").innerText()));
await page.locator('a[href="#indicadores"]').click();
await page.locator("#indicadores-content").waitFor({ state: "visible", timeout: 30000 });
await page.waitForTimeout(800);
const canvases = await page.locator("#indicadores-content canvas").count();
const conclusions = await page.locator("#indicadores-content").innerText();
check("B4 Indicadores: gráficos cargados al abrir la pestaña", canvases >= 5, `${canvases} gráficos`);
check("B4 Indicadores: frases de conclusión", /%|más/.test(conclusions));
await shot(page, "B4_inicio_indicadores");
await page.goto(`${BASE}/Inicio?ActiveTab=Indicadores`);
await settle(page);
check("B5 Ruta /Inicio responde y abre la pestaña pedida", await page.locator("#indicadores").isVisible());

// ───── C. Mantenimiento preventivo → panel de la gestión ─────
await page.goto(`${BASE}/Managements?handler=CurrentPreventive`);
await settle(page);
const panelUrl = page.url();
check("C1 «Mantenimiento preventivo» abre el panel de la gestión activa", /Managements\/Details\?id=\d+&ActiveTab=dashboard/i.test(panelUrl), panelUrl);
const panel = await page.locator("body").innerText();
const totalMatch = panel.match(/(\d[\d.,]*)\s*\n?\s*Activos Totales/i);
check("C2 Panel 2026-2 ya no está en cero", totalMatch && Number(totalMatch[1].replace(/\D/g, "")) > 0, totalMatch ? `${totalMatch[1]} activos` : "sin dato");
check("C2 Tarjetas del panel renombradas", ["L6 · Verificación", "L7 · Servicio técnico", "L8 · Kardex", "L3 · Salidas", "Adquisición"].every(t => panel.includes(t)));
await shot(page, "C2_panel_preventivo");

// ───── D. Reportes con equipos ─────
await page.goto(`${BASE}/Reports?ManagementId=${MGMT}&ReportFilter=l6&SelectedLabId=${LAB}`);
await settle(page);
const rows = await page.locator("table tbody tr").filter({ hasNotText: "No se encontraron" }).count();
check("D1 Reportes «Con verificación» + Aula demostrativa h-102 lista equipos", rows >= 11, `${rows} filas`);
await shot(page, "D1_reportes_lab");

// ───── E. Paginador común y renombres de títulos ─────
for (const [path, title] of [["/Verifications", "L6 · Verificaciones"], ["/Requests", "L7 · Servicio técnico"], ["/Maintenances", "L8 · Kardex"], ["/Departures", "L3 · Salidas"], ["/Acquisitions", "Adquisición"]]) {
  await page.goto(`${BASE}${path}`);
  await settle(page);
  check(`E1 Título de ${path}: «${title}»`, (await page.title()).includes(title) && (await page.locator(".page-wrapper").innerText()).includes(title));
}
await page.goto(`${BASE}/Users`);
await settle(page);
check("E2 Usuarios: paginador «Mostrando…»", /Mostrando/.test(await page.locator("body").innerText()));
await shot(page, "E2_usuarios");
await page.goto(`${BASE}/Users?tab=personal&staffPageIndex=2`);
await settle(page);
const staffActive = await page.locator('a.nav-link.active').allInnerTexts();
check("E3 Usuarios: la pestaña Personal se conserva al paginar", staffActive.some(t => /Personal/i.test(t)), staffActive.join(" | "));
await shot(page, "E3_usuarios_personal_p2");
for (const path of ["/Persons", "/Managements?type=Preventive", "/Equipment"]) {
  await page.goto(`${BASE}${path}`);
  await settle(page);
  check(`E4 ${path}: paginador común`, (await page.locator(".pg-pager, [class*='pg-']").count()) > 0 || /Mostrando/.test(await page.locator("body").innerText()));
}
await shot(page, "E4_equipos");
await page.goto(`${BASE}/Managements?type=Preventive`);
await settle(page);
const mg = await page.locator("body").innerText();
check("E5 Gestiones: estados claros, sin «Eliminar (Lógico)»", /Activa/.test(mg) && !/Eliminar \(Lógico\)/.test(mg));
await shot(page, "E5_gestiones");
await page.context().close();

// ───── F. Otros roles ─────
page = await session(SUPERVISOR);
check("F1 Supervisor entra al Inicio", /\/(AssetView(\/Index)?|Inicio)$/i.test(new URL(page.url()).pathname), page.url());
const supMenu = await menuText(page);
check("F1 Supervisor no ve «Mantenimiento preventivo» ni «Inventario general»", !/Mantenimiento preventivo/.test(supMenu) && !/Inventario general/.test(supMenu));
await shot(page, "F1_supervisor_inicio");
await page.context().close();
page = await session(TEACHER);
check("F2 Docente sigue entrando a su portal", /\/Docente/i.test(page.url()), page.url());
await page.context().close();

// ───── G. Teléfono ─────
page = await session(ADMIN, phone());
await page.goto(`${BASE}/Inicio`);
await noOverflow(page, "G1_tel_inicio_resumen");
await page.goto(`${BASE}/Inicio?ActiveTab=Indicadores`);
await page.locator("#indicadores-content").waitFor({ state: "visible", timeout: 30000 });
await page.waitForTimeout(800);
await noOverflow(page, "G2_tel_inicio_indicadores");
await page.goto(`${BASE}/Users`);
await noOverflow(page, "G3_tel_usuarios");
await page.goto(`${BASE}/Managements?type=Preventive`);
await noOverflow(page, "G4_tel_gestiones");
await page.context().close();

await browser.close();
check("Sin errores de JavaScript", jsErrors.length === 0, jsErrors.slice(0, 3).join(" ‖ "));
const fails = results.filter(r => !r.ok).length;
writeFileSync(resolve(OUT, "resultado.txt"), results.map(r => `[${r.ok ? "OK" : "FALLA"}] ${r.name}${r.detail ? " · " + r.detail : ""}`).join("\n") + `\n== ${results.length} comprobaciones, ${fails} fallas\n`);
console.log(`== ${results.length} comprobaciones, ${fails} fallas`);
process.exit(fails ? 1 : 0);
