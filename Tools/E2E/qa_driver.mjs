// Navegador de QA con sesión persistente: abre un Chromium VISIBLE y se queda esperando comandos por HTTP local.
// Sirve para "usar la app como una persona" paso a paso (login, clics, formularios, SweetAlert, descargas) y conservar la sesión entre comandos.
// Uso:  E2E_PW=... E2E_USER=qa_super node Tools/E2E/qa_driver.mjs      (la app debe estar en http://localhost:5000)
// Cliente: node Tools/E2E/qa.mjs 'goto /Countries' 'shot paises'
// Solo escucha en 127.0.0.1. La contraseña nunca se escribe en archivos ni se acepta por la red: viene del entorno del proceso.
import { chromium } from "playwright";
import http from "node:http";
import { mkdirSync, appendFileSync, statSync } from "node:fs";
import { dirname, resolve, join } from "node:path";
import { fileURLToPath } from "node:url";
import os from "node:os";

const BASE = process.env.E2E_BASE || "http://localhost:5000";
const PORT = Number(process.env.QA_DRIVER_PORT || 9333);
const SLOWMO = Number(process.env.QA_SLOWMO || 150);
const HEADLESS = process.env.QA_HEADLESS === "1";
const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "qa-usuario");
const IMG = join(OUT, "img");
const DL = join(OUT, "downloads");
mkdirSync(IMG, { recursive: true });
mkdirSync(DL, { recursive: true });

const ctx = await chromium.launchPersistentContext(join(os.tmpdir(), "qa-chromium-profile"), {
  headless: HEADLESS,
  slowMo: SLOWMO,
  viewport: { width: 1366, height: 850 },
  acceptDownloads: true,
  args: ["--window-position=40,20"],
});
let page = ctx.pages()[0] || (await ctx.newPage());

// ---- vigilancia continua: consola, errores JS, HTTP >= 400, diálogos nativos y descargas ----
let events = [];
let swalParamWarnings = 0;
let lastStatus = 0;
let lastDownload = null;
const stamp = () => new Date().toISOString().slice(11, 19);
function watch(p) {
  p.on("console", (m) => {
    if (!["error", "warning"].includes(m.type())) return;
    // Aviso conocido (hallazgo L-01): el layout pasa parámetros de SweetAlert2 >= v9 a la v7. Se cuenta, no se lista.
    if (m.text().startsWith("SweetAlert2: Unknown parameter")) { swalParamWarnings += 1; return; }
    events.push({ t: stamp(), kind: "console." + m.type(), text: m.text().slice(0, 300), url: p.url().replace(BASE, "") });
  });
  p.on("pageerror", (e) => events.push({ t: stamp(), kind: "pageerror", text: String(e).slice(0, 300), url: p.url().replace(BASE, "") }));
  p.on("requestfailed", (r) => { if (!r.url().includes("favicon")) events.push({ t: stamp(), kind: "requestfailed", text: `${r.method()} ${r.url().replace(BASE, "")} ${r.failure()?.errorText || ""}`.slice(0, 300) }); });
  p.on("response", (r) => {
    const s = r.status();
    if (r.request().isNavigationRequest()) lastStatus = s;
    if (s >= 400 && !r.url().includes("favicon")) events.push({ t: stamp(), kind: "http." + s, text: `${r.request().method()} ${r.url().replace(BASE, "")}`.slice(0, 300) });
  });
  p.on("dialog", async (d) => { events.push({ t: stamp(), kind: "dialog." + d.type(), text: d.message().slice(0, 300) }); await d.accept().catch(() => {}); });
  p.on("download", async (d) => {
    const target = join(DL, `${Date.now()}_${d.suggestedFilename()}`);
    await d.saveAs(target).catch((e) => events.push({ t: stamp(), kind: "download.error", text: String(e) }));
    let size = -1; try { size = statSync(target).size; } catch { /* sin archivo */ }
    lastDownload = { file: d.suggestedFilename(), saved: target, bytes: size };
    events.push({ t: stamp(), kind: "download", text: `${d.suggestedFilename()} (${size} bytes)` });
  });
}
watch(page);
ctx.on("page", (p) => { page = p; watch(p); });

const clean = (s) => String(s ?? "").replace(/\s+/g, " ").trim();
const loc = (s) => page.locator(s);

async function settle() { try { await page.waitForLoadState("networkidle", { timeout: 6000 }); } catch { /* sigue */ } }

async function swalInfo() {
  return page.evaluate(() => {
    const vis = (e) => e && !!(e.offsetWidth || e.offsetHeight || e.getClientRects().length);
    const box = document.querySelector(".swal2-container");
    if (!box || !vis(box)) return { visible: false };
    const q = (s) => { const e = box.querySelector(s); return e && vis(e) ? e.innerText.trim() : null; };
    return {
      visible: true,
      toast: !!box.querySelector(".swal2-toast") || box.classList.contains("swal2-toast"),
      title: q(".swal2-title"),
      text: q("#swal2-content") || q(".swal2-content"),
      confirm: q(".swal2-confirm"),
      cancel: q(".swal2-cancel"),
      icon: [...box.querySelectorAll(".swal2-icon")].filter(vis).map((e) => e.className.replace("swal2-icon", "").trim()).join(","),
    };
  });
}

async function run(a) {
  const t0 = Date.now();
  let out;
  switch (a.a) {
    case "goto": {
      // Una respuesta 404/403 sin cuerpo hace que Playwright lance ERR_HTTP_RESPONSE_CODE_FAILURE: se informa el estado en lugar de abortar.
      try { await page.goto(a.u.startsWith("http") ? a.u : BASE + a.u, { waitUntil: "domcontentloaded", timeout: 60000 }); }
      catch (e) { if (!String(e.message).includes("ERR_HTTP_RESPONSE_CODE_FAILURE")) throw e; }
      await settle(); out = { status: lastStatus, url: page.url().replace(BASE, "") }; break;
    }
    case "reload": await page.reload({ waitUntil: "domcontentloaded" }); await settle(); out = { status: lastStatus }; break;
    case "back": await page.goBack({ waitUntil: "domcontentloaded" }); await settle(); out = { url: page.url().replace(BASE, "") }; break;
    case "click": await loc(a.s).nth(Number(a.n || 0)).click({ timeout: 15000 }); await settle(); out = { url: page.url().replace(BASE, "") }; break;
    case "dblclick": await loc(a.s).first().dblclick({ timeout: 15000 }); await settle(); out = { url: page.url().replace(BASE, "") }; break;
    case "hover": await loc(a.s).first().hover({ timeout: 15000 }); out = {}; break;
    case "fill": await loc(a.s).first().fill(a.v ?? "", { timeout: 15000 }); out = {}; break;
    case "type": await loc(a.s).first().click({ timeout: 15000 }); await page.keyboard.type(a.v ?? "", { delay: 40 }); out = {}; break;
    case "select": { const l = loc(a.s).first(); try { await l.selectOption({ label: a.v }, { timeout: 5000 }); } catch { await l.selectOption(a.v, { timeout: 5000 }); } out = {}; break; }
    case "check": await loc(a.s).first().check({ timeout: 15000 }); out = {}; break;
    case "uncheck": await loc(a.s).first().uncheck({ timeout: 15000 }); out = {}; break;
    case "upload": await loc(a.s).first().setInputFiles(a.v); out = {}; break;
    case "press": await page.keyboard.press(a.k); await settle(); out = { url: page.url().replace(BASE, "") }; break;
    case "wait": await page.waitForTimeout(Number(a.ms || 500)); out = {}; break;
    case "waitfor": await loc(a.s).first().waitFor({ state: "visible", timeout: Number(a.ms || 15000) }); out = {}; break;
    case "url": out = { url: page.url().replace(BASE, ""), title: await page.title() }; break;
    case "count": out = { count: await loc(a.s).count() }; break;
    case "text": {
      const max = Number(a.max || 2500);
      const raw = a.s ? await loc(a.s).first().innerText({ timeout: 10000 }) : await page.evaluate(() => { const b = document.body.cloneNode(true); b.querySelectorAll("script,style,noscript").forEach((n) => n.remove()); return document.body.innerText; });
      const t = clean(raw); out = { len: t.length, text: t.slice(0, max) }; break;
    }
    case "content": { // texto de la zona principal (sin menú lateral ni encabezado)
      const max = Number(a.max || 2500);
      const t = await page.evaluate(() => { const m = document.querySelector(".page-wrapper .container-fluid, .page-wrapper, #main-wrapper .page-wrapper, main") || document.body; return m.innerText; });
      const c = clean(t); out = { len: c.length, text: c.slice(0, max) }; break;
    }
    case "html": out = { html: (await loc(a.s).first().evaluate((e) => e.outerHTML)).slice(0, Number(a.max || 1500)) }; break;
    case "links": out = { links: await page.evaluate((s) => [...document.querySelectorAll((s || "body") + " a[href]")].map((e) => e.innerText.replace(/\s+/g, " ").trim() + " | " + e.getAttribute("href")).slice(0, 80), a.s) }; break;
    case "rows": out = { rows: await page.evaluate((s) => [...document.querySelectorAll((s || "table") + " tbody tr")].slice(0, 25).map((r) => [...r.querySelectorAll("td")].map((c) => c.innerText.replace(/\s+/g, " ").trim()).join(" · ")), a.s), total: await page.locator((a.s || "table") + " tbody tr").count() }; break;
    case "value": out = { value: await loc(a.s).first().inputValue() }; break;
    case "valid": out = { messages: await page.evaluate(() => [...document.querySelectorAll(".field-validation-error, .text-danger, .validation-summary-errors li, .invalid-feedback")].map((e) => e.innerText.replace(/\s+/g, " ").trim()).filter(Boolean)) }; break;
    case "swal": out = await swalInfo(); break;
    case "confirm": { const b = loc(".swal2-confirm"); await b.first().click({ timeout: 8000 }); await settle(); out = { url: page.url().replace(BASE, "") }; break; }
    case "cancel": await loc(".swal2-cancel").first().click({ timeout: 8000 }); out = {}; break;
    case "shot": { const f = join(IMG, `${a.name}.png`); await page.screenshot({ path: f, fullPage: a.full === "1" }); out = { file: f }; break; }
    case "viewport": await page.setViewportSize({ width: Number(a.w), height: Number(a.h) }); out = {}; break;
    case "eval": out = { result: await page.evaluate(a.js) }; break;
    case "errors": out = { events: events, swalParamWarnings }; events = []; swalParamWarnings = 0; break;
    case "lastdownload": out = { download: lastDownload }; break;
    case "clearcookies": await ctx.clearCookies(); await page.evaluate(() => { try { localStorage.clear(); sessionStorage.clear(); } catch {} }).catch(() => {}); out = {}; break;
    case "login": {
      const user = a.u || process.env.E2E_USER;
      const pw = a.p === undefined ? process.env.E2E_PW : a.p;
      await page.goto(BASE + "/Login", { waitUntil: "domcontentloaded" });
      await page.fill('input[name="Input.UserName"]', user);
      await page.fill('input[name="Input.Password"]', pw);
      await page.click('button[type="submit"], input[type="submit"]');
      await settle();
      out = { url: page.url().replace(BASE, ""), loggedIn: !page.url().includes("/Login") };
      break;
    }
    default: throw new Error("acción desconocida: " + a.a);
  }
  return { a: a.a, ms: Date.now() - t0, ...out };
}
function clean2(s) { return String(s ?? "").replace(/\s+/g, " ").trim(); }

const server = http.createServer(async (req, res) => {
  if (req.method !== "POST" || req.url !== "/run") { res.writeHead(404); res.end(); return; }
  let body = ""; for await (const c of req) body += c;
  let actions; try { actions = JSON.parse(body).actions; } catch { res.writeHead(400); res.end("json inválido"); return; }
  const results = [];
  for (const a of actions) {
    try { results.push(await run(a)); }
    catch (e) { results.push({ a: a.a, error: String(e.message || e).split("\n")[0].slice(0, 300) }); break; }
  }
  res.writeHead(200, { "content-type": "application/json; charset=utf-8" });
  res.end(JSON.stringify(results));
});
server.listen(PORT, "127.0.0.1", () => console.log(`qa_driver listo en 127.0.0.1:${PORT} (base ${BASE}, ${HEADLESS ? "headless" : "VISIBLE"}, slowMo ${SLOWMO} ms)`));
process.on("SIGINT", async () => { await ctx.close(); process.exit(0); });
process.on("SIGTERM", async () => { await ctx.close(); process.exit(0); });
