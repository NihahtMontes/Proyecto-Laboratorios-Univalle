// Recorrido E2E del Inicio con Resumen + pestañas y bandeja clasificada. Solo lee: «Posponer» queda en el navegador
// de la prueba y «Listo» no se usa. Uso: E2E_BASE=http://localhost:5055 E2E_CRED_FILE=<archivo privado> node inicio_panel.mjs
import { chromium, devices } from "playwright";
import { mkdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE || "http://localhost:5055";
const creds = Object.fromEntries([...readFileSync(process.env.E2E_CRED_FILE, "utf8").matchAll(/\|\s*`([^`]+)`\s*\|[^|]*\|\s*`([^`]+)`\s*\|/g)].map(m => [m[1], m[2]]));
const ADMIN = { user: "admin.qa", pw: creds["admin.qa"] };
const SUPERVISOR = { user: "supervisor.qa", pw: creds["supervisor.qa"] };
const OUT = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "outputs", "e2e", "inicio_panel");
mkdirSync(OUT, { recursive: true });

const results = [];
const check = (name, ok, detail = "") => { results.push(!!ok); console.log(`${ok ? "OK  " : "FALLA"} ${name}${detail ? " · " + detail : ""}`); };
const browser = await chromium.launch({ headless: true });
const jsErrors = [];
const settle = (page) => page.waitForLoadState("networkidle");

async function login(who, options = { viewport: { width: 1440, height: 900 } }) {
  const ctx = await browser.newContext({ locale: "es-BO", ...options });
  const page = await ctx.newPage();
  page.on("pageerror", e => jsErrors.push(`${page.url()} · ${e.message}`));
  page.on("console", m => { if (m.type() === "error") jsErrors.push(`${page.url()} · console · ${m.text()}`); });
  await page.goto(`${BASE}/Login`);
  await page.fill('[name="Input.UserName"]', who.user);
  await page.fill('[name="Input.Password"]', who.pw);
  await Promise.all([settle(page), page.locator('form button[type="submit"]').first().click()]);
  return page;
}
const overflow = (page) => page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
const visiblePanel = (page) => page.evaluate(() => [...document.querySelectorAll('[role="tabpanel"]')].filter(p => !p.hidden).map(p => p.dataset.phKey).join(","));
const shot = (page, name, fullPage = true) => page.screenshot({ path: resolve(OUT, `${name}.png`), fullPage });
const open = async (page, query = "") => { await page.goto(`${BASE}/AssetView/Index${query}`); await settle(page); await page.waitForTimeout(500); };

// ---------- Administrador, escritorio
const page = await login(ADMIN);
await open(page);
const keys = await page.locator('[role="tab"]').evaluateAll(t => t.map(x => x.dataset.phKey).join(","));
check("Pestañas Resumen, Prácticas, Bandeja, Gráficos e Inventario", keys === "resumen,semana,bandeja,graficos,inventario", keys);
check("Abre en Resumen", (await visiblePanel(page)) === "resumen");
check("Sin franja oscura", await page.locator(".ip-kpis").count() === 0);
check("4 indicadores", await page.locator(".ph-kpis .ph-kpi").count() === 4);
const dark = await page.locator(".ph-kpi").evaluateAll(list => list.map(e => getComputedStyle(e).backgroundColor).filter(c => c !== "rgb(255, 255, 255)"));
check("Indicadores con fondo blanco", dark.length === 0, dark.join(" | "));
check("4 tarjetas de sección", await page.locator('[data-ph-key="resumen"] a.ph-section[data-ph-tab]').count() === 4);
const bell = ((await page.locator("#notificationCount").first().textContent().catch(() => "")) || "").trim();
check("Campana solo cuenta lo accionable", Number(bell || 0) <= 3, `campana=${bell || "(sin número)"}`);
await shot(page, "A1_resumen");

await page.locator('[data-ph-key="resumen"] a.ph-section[data-ph-tab="semana"]').click();
await page.waitForTimeout(600);
check("La tarjeta abre Prácticas", (await visiblePanel(page)) === "semana" && page.url().includes("activeTab=semana"), page.url());
const rows = await page.locator("tr.ip-row").count();
check("Tabla de la semana con prácticas", rows >= 20, `${rows} filas`);
const tableWidth = await page.locator('[data-ph-key="semana"] .ip-table').evaluate(e => e.getBoundingClientRect().width);
check("La tabla usa todo el ancho", tableWidth > 1000, `${Math.round(tableWidth)}px`);
await shot(page, "A2_practicas");
await page.goBack(); await page.waitForTimeout(300);
check("Atrás vuelve a Resumen", (await visiblePanel(page)) === "resumen" && !page.url().includes("activeTab"), page.url());
await page.goForward(); await page.waitForTimeout(300);
check("Adelante vuelve a Prácticas", (await visiblePanel(page)) === "semana");

await page.locator("#ph-tab-bandeja").click(); await page.waitForTimeout(300);
const inbox = (await page.locator("#bandeja").innerText()).replace(/\s+/g, " ");
check("Bandeja sin solicitudes muertas", !/Nueva solicitud de práctica/.test(inbox), inbox.slice(0, 120));
check("Bandeja con «Revisar datos»", /Revisar datos/i.test(inbox));
const layout = await page.locator(".ip-feed__item").first().evaluate(item => {
  const main = item.querySelector(".ip-feed__main").getBoundingClientRect();
  const actions = item.querySelector(".ip-feed__actions").getBoundingClientRect();
  return { sameRow: actions.top < main.bottom && actions.left >= main.right - 2, width: item.getBoundingClientRect().width };
});
check("Acciones a la derecha y a ancho completo", layout.sameRow && layout.width > 800, JSON.stringify(layout));
const gap = await page.locator("#bandeja").evaluate(card => {
  const last = [...card.querySelectorAll(".ip-feed__item, .ip-feed__review")].filter(e => e.offsetParent && (e.matches(".ip-feed__review") || !e.closest("details:not([open])"))).pop().getBoundingClientRect();
  return Math.round(card.getBoundingClientRect().bottom - last.bottom);
});
check("Bandeja sin hueco en blanco", gap >= 0 && gap < 130, `${gap}px`);
await shot(page, "A3_bandeja");
const before = Number((await page.locator("#ph-tab-bandeja .ph-tab__count").textContent()).trim());
await page.locator("#bandeja .ip-feed__item button", { hasText: "Posponer" }).first().click();
await page.waitForTimeout(600);
const after = Number((await page.locator("#ph-tab-bandeja .ph-tab__count").textContent().catch(() => "0")).trim() || 0);
check("Posponer actualiza el contador de la pestaña", after === before - 1, `${before} -> ${after}`);
await page.evaluate(() => { try { localStorage.removeItem("postponed_notifications"); } catch { /* sin almacenamiento */ } });

await open(page, "?activeTab=graficos");
check("Enlace directo a Gráficos", (await visiblePanel(page)) === "graficos");
await page.locator('[data-ph-key="graficos"] canvas').first().waitFor({ state: "visible" });
await page.waitForFunction(() => document.querySelectorAll('#indicadores-content canvas').length >= 4 && [...document.querySelectorAll('#indicadores-content canvas')].some(c => c.getBoundingClientRect().height > 40), null, { timeout: 30000 }).catch(() => null);
await page.waitForTimeout(1200);
const charts = await page.locator('[data-ph-key="graficos"] canvas').evaluateAll(list => list.map(c => { const r = c.getBoundingClientRect(); return { v: !!c.offsetParent, w: Math.round(r.width), h: Math.round(r.height) }; }));
const drawn = charts.filter(c => c.v && c.w > 100 && c.h > 80);
check("Gráficos dibujados con tamaño real", drawn.length >= 7, `${drawn.length}/${charts.length}`);
const cards = await page.locator("#indicadores-content .ip-indicator").evaluateAll(l => l.filter(e => getComputedStyle(e).backgroundColor === "rgb(255, 255, 255)").length);
check("Indicadores en tarjetas blancas", cards === 6, `${cards}/6`);
check("«Más indicadores» ya no existe", await page.locator("#ip-mas-indicadores").count() === 0);
await shot(page, "A4_graficos");
await page.reload(); await settle(page);
check("Recargar conserva la pestaña", (await visiblePanel(page)) === "graficos");

await open(page, "?activeTab=Inventario");
check("Enlace antiguo Inventario", (await visiblePanel(page)) === "inventario");
check("Inventario con sus accesos", /Equipos/.test(await page.locator("#inventario").innerText()));
await shot(page, "A5_inventario");
await open(page, "?activeTab=Indicadores");
check("Enlace antiguo Indicadores", (await visiblePanel(page)) === "graficos");

await open(page);
await page.locator("#ph-tab-resumen").focus();
await page.keyboard.press("ArrowRight");
const focused = await page.evaluate(() => document.activeElement.id);
check("Flecha mueve el foco sin cambiar de pestaña", focused === "ph-tab-semana" && (await visiblePanel(page)) === "resumen", focused);
await page.keyboard.press(" "); await page.waitForTimeout(200);
check("Espacio activa la pestaña", (await visiblePanel(page)) === "semana");

for (const width of [1024, 390, 320]) {
  await page.setViewportSize({ width, height: 800 });
  for (const tab of ["resumen", "semana", "bandeja", "graficos", "inventario"]) {
    await open(page, `?activeTab=${tab}`);
    const o = await overflow(page);
    check(`Sin desborde a ${width}px · ${tab}`, o <= 2, `${o}px`);
  }
}
await page.setViewportSize({ width: 1024, height: 800 });
await open(page);
await shot(page, "B1_resumen_1024");

// ---------- Supervisor
const sup = await login(SUPERVISOR);
await open(sup);
check("Supervisor: 3 indicadores, sin preventivo", await sup.locator(".ph-kpis .ph-kpi").count() === 3 && !/Avance del preventivo/i.test(await sup.locator(".ph-kpis").innerText()));
check("Supervisor: bandeja disponible", await sup.locator("#bandeja").count() === 1);
await shot(sup, "C1_supervisor");

// ---------- Teléfono
const phoneOpts = { ...devices["iPhone 13"], locale: "es-BO" }; delete phoneOpts.defaultBrowserType;
const tel = await login(ADMIN, phoneOpts);
await open(tel);
await shot(tel, "D1_telefono_resumen");
await tel.locator("#ph-tab-inventario").tap(); await tel.waitForTimeout(300);
check("Teléfono: la pestaña final se alcanza", (await visiblePanel(tel)) === "inventario");
const revealed = await tel.locator("#ph-tab-inventario").evaluate(t => { const r = t.getBoundingClientRect(); return r.left >= 0 && r.right <= window.innerWidth; });
check("Teléfono: la pestaña activa queda a la vista", revealed);

check("Sin errores de JavaScript", jsErrors.length === 0, jsErrors.slice(0, 3).join(" | "));
await browser.close();
const fails = results.filter(r => !r).length;
console.log(`\n${results.length - fails}/${results.length} OK`);
process.exit(fails ? 1 : 0);
