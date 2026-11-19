// Recorrido E2E del menú lateral plegable (17/11): orden de secciones, plegado inicial,
// apertura automática de la sección de la página actual, clic para abrir/cerrar, menú mini y teléfono.
// Solo lee. Uso: E2E_BASE=http://localhost:5055 E2E_CRED_FILE=<archivo privado> node menu_plegable.mjs
import { chromium, devices } from "playwright";
import { mkdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE || "http://localhost:5055";
const creds = Object.fromEntries([...readFileSync(process.env.E2E_CRED_FILE, "utf8").matchAll(/\|\s*`([^`]+)`\s*\|[^|]*\|\s*`([^`]+)`\s*\|/g)].map(m => [m[1], m[2]]));
const ADMIN = { user: "admin.qa", pw: creds["admin.qa"] };
const SUPERVISOR = { user: "supervisor.qa", pw: creds["supervisor.qa"] };
const OUT = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "outputs", "e2e", "menu_plegable");
mkdirSync(OUT, { recursive: true });

const results = [];
const check = (name, ok, detail = "") => { results.push(ok); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };
const browser = await chromium.launch({ headless: true });
const jsErrors = [];

async function login(who, options = { viewport: { width: 1400, height: 900 } }) {
  const ctx = await browser.newContext({ locale: "es-BO", ...options });
  const page = await ctx.newPage();
  page.on("pageerror", e => jsErrors.push(`${page.url()} · ${e.message}`));
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', who.user);
  await page.fill('[name="Input.Password"]', who.pw);
  await Promise.all([page.waitForLoadState("networkidle"), page.locator('form button[type="submit"]').first().click()]);
  return page;
}

// Estado del menú: títulos en orden, cuáles están abiertos y cuántos enlaces visibles tiene cada sección.
const menuState = (page) => page.evaluate(() => {
  const out = [];
  let cur = null;
  for (const li of document.querySelectorAll("#sidebarnav > li")) {
    if (li.classList.contains("nav-small-cap")) {
      const btn = li.querySelector(".sidebar-section-toggle");
      cur = { title: li.textContent.replace(/\s+/g, " ").trim(), toggle: !!btn,
              open: btn ? btn.getAttribute("aria-expanded") === "true" : true,
              shown: getComputedStyle(li).display !== "none", visible: 0, total: 0 };
      out.push(cur);
    } else if (cur) {
      cur.total++;
      if (li.offsetParent !== null) cur.visible++;
    }
  }
  return out;
});
const sec = (st, t) => st.find(s => s.title.toLowerCase() === t.toLowerCase());

// ---------- Administrador en Inicio
const page = await login(ADMIN);
await page.goto(`${BASE}/AssetView/Index`); await page.waitForLoadState("networkidle");
let st = await menuState(page);
const order = st.filter(s => s.shown).map(s => s.title);
check("Orden de secciones", JSON.stringify(order) === JSON.stringify(["Inicio", "Planificación académica", "Mantenimiento de equipos", "Equipos e inventario", "Configuración", "Ayuda"]), order.join(" › "));
for (const t of ["Planificación académica", "Mantenimiento de equipos", "Equipos e inventario", "Configuración"]) {
  const s = sec(st, t);
  check(`Inicio: «${t}» plegada`, s && s.toggle && !s.open && s.visible === 0, s ? `${s.visible}/${s.total} visibles` : "no está");
}
check("Inicio y Ayuda siguen visibles", sec(st, "Inicio").visible === 1 && sec(st, "Ayuda").visible === 1);
await page.screenshot({ path: resolve(OUT, "A1_inicio_plegado.png") });

// Clic abre y vuelve a cerrar
await page.locator('.sidebar-section-toggle[data-section="Planificación académica"]').click();
st = await menuState(page);
const plan = sec(st, "Planificación académica");
check("Clic abre Planificación académica", plan.open && plan.visible === plan.total, `${plan.visible}/${plan.total}`);
const rot = (s) => page.locator(`.sidebar-section-toggle[data-section="${s}"] .sidebar-section-chevron`).evaluate(el => getComputedStyle(el).rotate);
await page.waitForTimeout(300);
const rOpen = await rot("Planificación académica"), rClosed = await rot("Mantenimiento de equipos");
check("La flecha apunta hacia abajo al abrir y a la derecha cerrada", (rOpen === "0deg" || rOpen === "none") && rClosed === "-90deg", `abierta=${rOpen} · cerrada=${rClosed}`);
await page.screenshot({ path: resolve(OUT, "A2_planificacion_abierta.png") });
await page.locator('.sidebar-section-toggle[data-section="Planificación académica"]').click();
st = await menuState(page);
check("Segundo clic la cierra", !sec(st, "Planificación académica").open && sec(st, "Planificación académica").visible === 0);

// Teclado: Enter sobre el título
await page.locator('.sidebar-section-toggle[data-section="Configuración"]').focus();
await page.keyboard.press("Enter");
st = await menuState(page);
check("Enter abre Configuración", sec(st, "Configuración").open);

// Sección de la página actual se abre sola
for (const [path, title, label] of [
  ["/Equipment/Index", "Equipos e inventario", "Equipos"],
  ["/LaboratoryOperations/Planificaciones", "Planificación académica", "Planificaciones"],
  ["/Managements/Index?type=Preventive", "Mantenimiento de equipos", "Gestiones"],
  ["/Users/Index", "Configuración", "Usuarios"],
]) {
  await page.goto(`${BASE}${path}`); await page.waitForLoadState("networkidle");
  st = await menuState(page);
  const others = st.filter(s => s.toggle && s.title !== title && s.open).map(s => s.title);
  check(`${label}: abre «${title}» y deja las demás plegadas`, sec(st, title).open && others.length === 0, others.join(", "));
}
await page.screenshot({ path: resolve(OUT, "A3_configuracion_abierta.png") });

// Registros sigue funcionando dentro de Mantenimiento
await page.goto(`${BASE}/Managements/Index?type=Preventive`); await page.waitForLoadState("networkidle");
await page.locator("#sidebarnav a.has-arrow", { hasText: "Registros" }).click();
await page.waitForTimeout(400);
const registrosVisible = await page.locator("#sidebarnav a", { hasText: "Servicio técnico" }).isVisible();
check("Registros se despliega dentro de Mantenimiento", registrosVisible);
await page.screenshot({ path: resolve(OUT, "A4_registros.png") });

// Menú mini: todos los íconos visibles
await page.goto(`${BASE}/AssetView/Index`); await page.waitForLoadState("networkidle");
await page.locator(".sidebartoggler").first().click();
await page.mouse.move(900, 500);
await page.waitForTimeout(500);
st = await menuState(page);
const hiddenInMini = st.filter(s => s.toggle && s.shown && s.visible < s.total).map(s => s.title);
check("Menú mini muestra todos los íconos", hiddenInMini.length === 0, hiddenInMini.join(", "));
await page.screenshot({ path: resolve(OUT, "A5_menu_mini.png") });
await page.locator(".sidebartoggler").first().click();

// ---------- Supervisor
const sup = await login(SUPERVISOR);
await sup.goto(`${BASE}/AssetView/Index`); await sup.waitForLoadState("networkidle");
st = await menuState(sup);
const supOrder = st.filter(s => s.shown).map(s => s.title);
check("Supervisor: secciones sin enlaces no se muestran", !supOrder.includes("Configuración") && st.every(s => !s.shown || !s.toggle || s.total > 0), supOrder.join(" › "));
await sup.screenshot({ path: resolve(OUT, "B1_supervisor.png") });

// ---------- Teléfono
const phoneOpts = { ...devices["iPhone 13"], locale: "es-BO" }; delete phoneOpts.defaultBrowserType;
const tel = await login(ADMIN, phoneOpts);
await tel.goto(`${BASE}/AssetView/Index`); await tel.waitForLoadState("networkidle");
await tel.locator(".nav-toggler").first().click();
await tel.waitForTimeout(500);
st = await menuState(tel);
check("Teléfono: secciones plegadas al abrir el menú", sec(st, "Mantenimiento de equipos").visible === 0);
await tel.locator('.sidebar-section-toggle[data-section="Mantenimiento de equipos"]').click();
st = await menuState(tel);
check("Teléfono: tocar el título abre la sección", sec(st, "Mantenimiento de equipos").visible > 0);
const wide = await tel.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 2);
check("Teléfono sin desborde horizontal", wide);
await tel.screenshot({ path: resolve(OUT, "C1_telefono.png") });

check("Sin errores de JavaScript", jsErrors.length === 0, jsErrors.slice(0, 3).join(" | "));
await browser.close();
const fails = results.filter(r => !r).length;
console.log(`\n${results.length - fails}/${results.length} OK`);
process.exit(fails ? 1 : 0);
