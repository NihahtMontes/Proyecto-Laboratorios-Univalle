// Recorrido E2E de «Mi semana» del docente con Resumen + pestañas. Solo lee.
// Uso: E2E_BASE=http://localhost:5055 E2E_TEACHER_USER=docente.b E2E_TEACHER_PW=<clave> node docente_inicio.mjs
import { chromium, devices } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE || "http://localhost:5055";
const USER = process.env.E2E_TEACHER_USER || "docente.b";
const PW = process.env.E2E_TEACHER_PW;
if (!PW) { console.error("Falta E2E_TEACHER_PW"); process.exit(2); }
const OUT = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "outputs", "e2e", "docente_inicio");
mkdirSync(OUT, { recursive: true });

const results = [];
const check = (name, ok, detail = "") => { results.push(!!ok); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };
const browser = await chromium.launch({ headless: true });
const jsErrors = [];
const settle = (page) => page.waitForLoadState("networkidle");

async function login(options = { viewport: { width: 1440, height: 900 } }) {
  const ctx = await browser.newContext({ locale: "es-BO", ...options });
  const page = await ctx.newPage();
  page.on("pageerror", e => jsErrors.push(`${page.url()} · ${e.message}`));
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', USER);
  await page.fill('[name="Input.Password"]', PW);
  await Promise.all([settle(page), page.locator('form button[type="submit"]').first().click()]);
  return page;
}
const visiblePanel = (page) => page.evaluate(() => [...document.querySelectorAll('[role="tabpanel"]')].filter(p => !p.hidden).map(p => p.dataset.phKey).join(","));
const overflow = (page) => page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
const shot = (page, name, fullPage = true) => page.screenshot({ path: resolve(OUT, `${name}.png`), fullPage });

const page = await login();
await page.goto(`${BASE}/Docente`); await settle(page);
check("Entra al portal docente", page.url().includes("/Docente"), page.url());
const keys = await page.locator('[role="tab"]').evaluateAll(t => t.map(x => x.dataset.phKey).join(","));
check("Pestañas Resumen, Mi semana, Por solicitar y Novedades", keys === "resumen,semana,solicitar,novedades", keys);
check("Abre en Resumen", (await visiblePanel(page)) === "resumen");
check("4 indicadores claros", await page.locator(".ph-kpis .ph-kpi").count() === 4);
const kpiBg = await page.locator(".ph-kpi").first().evaluate(e => getComputedStyle(e).backgroundColor);
check("Indicadores con fondo blanco", kpiBg === "rgb(255, 255, 255)", kpiBg);
const purple = await page.evaluate(() => {
  const isPurple = (c) => /rgb\(116, 96, 238\)|rgb\(90, 70, 214\)/.test(c);
  return [...document.querySelectorAll(".ph-home, .ph-home *")].filter(e => {
    const s = getComputedStyle(e);
    return isPurple(s.color) || isPurple(s.backgroundColor) || isPurple(s.backgroundImage) || isPurple(s.borderTopColor);
  }).slice(0, 4).map(e => e.tagName + "." + e.className);
});
check("Sin morado en la página", purple.length === 0, purple.join(" | "));
check("6 tarjetas de sección", await page.locator('[data-ph-key="resumen"] .ph-sections .ph-section').count() === 6);
if (await page.locator("#countdown").count()) {
  await page.waitForTimeout(600);
  const text = await page.locator("#countdown").innerText();
  check("Cuenta regresiva funcionando", !/–/.test(text), text.replace(/\s+/g, " "));
}
await shot(page, "A1_resumen");

await page.locator('[data-ph-key="resumen"] a[data-ph-tab="semana"]').first().click();
await page.waitForTimeout(500);
check("La tarjeta abre Mi semana", (await visiblePanel(page)) === "semana" && page.url().includes("tab=semana"), page.url());
check("Se ve la semana con sus 6 días", await page.locator('[data-ph-key="semana"] .dc-day').count() === 6);
await shot(page, "A2_semana");
await page.goBack(); await page.waitForTimeout(300);
check("Atrás vuelve a Resumen", (await visiblePanel(page)) === "resumen" && !page.url().includes("tab="), page.url());

await page.locator("#ph-tab-semana").click();
const next = page.locator('[data-ph-key="semana"] a[href*="week="]').last();
await Promise.all([page.waitForNavigation(), next.click()]);
await settle(page);
check("Cambiar de semana conserva la pestaña", (await visiblePanel(page)) === "semana" && /tab=semana/.test(page.url()) && /week=/.test(page.url()), page.url());

await page.goto(`${BASE}/Docente?tab=solicitar`); await settle(page);
check("Enlace directo a Por solicitar", (await visiblePanel(page)) === "solicitar");
await shot(page, "A3_por_solicitar");
await page.goto(`${BASE}/Docente?tab=novedades`); await settle(page);
check("Enlace directo a Novedades", (await visiblePanel(page)) === "novedades");
await shot(page, "A4_novedades");
await page.reload(); await settle(page);
check("Recargar conserva la pestaña", (await visiblePanel(page)) === "novedades");

await page.goto(`${BASE}/Docente`); await settle(page);
await Promise.all([page.waitForNavigation(), page.locator('[data-ph-key="resumen"] a.ph-section[href*="Solicitudes"]').first().click()]);
check("Tarjeta Mis solicitudes abre su página", /\/Docente\/Solicitudes/.test(page.url()), page.url());
await page.goto(`${BASE}/Docente`); await settle(page);
await Promise.all([page.waitForNavigation(), page.locator('[data-ph-key="resumen"] a.ph-section[href*="Calendario"]').first().click()]);
check("Tarjeta Mi calendario abre su página", /\/Docente\/Calendario/.test(page.url()), page.url());

await page.goto(`${BASE}/Docente`); await settle(page);
await page.locator("#ph-tab-resumen").focus();
await page.keyboard.press("ArrowRight");
const focused = await page.evaluate(() => document.activeElement.id);
check("Flecha mueve el foco sin cambiar de pestaña", focused === "ph-tab-semana" && (await visiblePanel(page)) === "resumen", focused);
await page.keyboard.press("Enter"); await page.waitForTimeout(200);
check("Enter activa la pestaña", (await visiblePanel(page)) === "semana");

for (const width of [1024, 390, 320]) {
  await page.setViewportSize({ width, height: 800 });
  for (const tab of ["resumen", "semana", "solicitar", "novedades"]) {
    await page.goto(`${BASE}/Docente?tab=${tab}`); await settle(page);
    const o = await overflow(page);
    check(`Sin desborde a ${width}px · ${tab}`, o <= 2, `${o}px`);
  }
}
const phoneOpts = { ...devices["iPhone 13"], locale: "es-BO" }; delete phoneOpts.defaultBrowserType;
const tel = await login(phoneOpts);
await tel.goto(`${BASE}/Docente`); await settle(tel); await tel.waitForTimeout(500);
await shot(tel, "B1_telefono_resumen");

check("Sin errores de JavaScript", jsErrors.length === 0, jsErrors.slice(0, 3).join(" | "));
await browser.close();
const fails = results.filter(r => !r).length;
console.log(`\n${results.length - fails}/${results.length} OK`);
process.exit(fails ? 1 : 0);
