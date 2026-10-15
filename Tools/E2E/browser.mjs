// Recorrido con navegador real (Chromium) de las pantallas de la app. Solo lectura.
// Uso: E2E_PW=... node browser.mjs   (la app debe estar en http://localhost:5000)
import { chromium } from "playwright";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE || "http://localhost:5000";
const USER = process.env.E2E_USER || "nihaht";
const PW = process.env.E2E_PW;
if (!PW) { console.error("Falta E2E_PW"); process.exit(2); }

const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "e2e");
mkdirSync(OUT, { recursive: true });

const PAGES = [
  "/", "/Equipment", "/EquipmentUnits", "/AssetView", "/AssetView?ActiveTab=Inventario", "/Kardex/Create",
  "/Laboratories", "/Cities", "/Countries", "/Persons", "/Users", "/Faculties",
  "/Catalogs/Articles", "/Catalogs/ArticleCategories", "/Catalogs/MeasurementUnits", "/Catalogs/Subjects",
  "/Catalogs/Practices", "/Catalogs/Stock", "/Catalogs/StudentGroups", "/Catalogs/CourseGroups",
  "/Verifications?filterLaboratoryId=1", "/Requests", "/Maintenances", "/Maintenances/Plan", "/Managements?Type=PorConfirmar",
  "/Acquisitions", "/Departures", "/Schedules", "/Schedules/Practices", "/Reports", "/Notifications",
  "/LaboratoryOperations", "/LaboratoryOperations/Planificaciones", "/LaboratoryOperations/Grilla",
  "/LaboratoryOperations/CalendarioAcademico", "/LaboratoryOperations/Demanda", "/LaboratoryOperations/Reposiciones",
  "/LaboratoryOperations/Conflictos", "/Cierre",
  "/Equipment/Details/1", "/EquipmentUnits/Details?id=1", "/EquipmentUnits/Kardex/1", "/Requests/Details/158",
  "/Maintenances/Details/1001", "/Catalogs/Practices/Details/5", "/Managements/Details?id=1", "/Persons/Details?id=1",
  "/Laboratories/Details/1", "/Users/Details/1",
];

const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
const rows = [];
let current = "login";
const issues = { console: [], http: [], page: [] };
page.on("console", (m) => { if (m.type() === "error") issues.console.push([current, m.text().slice(0, 200)]); });
page.on("pageerror", (e) => issues.page.push([current, String(e).slice(0, 200)]));
page.on("response", (r) => {
  const s = r.status();
  if (s >= 400 && !r.url().includes("favicon")) issues.http.push([current, s, r.url().replace(BASE, "").slice(0, 120)]);
});

// Login por la interfaz
await page.goto(`${BASE}/Login`);
await page.fill('input[name="Input.UserName"]', USER);
await page.fill('input[name="Input.Password"]', PW);
await Promise.all([page.waitForNavigation({ waitUntil: "networkidle", timeout: 60000 }), page.click('button[type="submit"], input[type="submit"]')]);
const loggedIn = !page.url().includes("/Login");
console.log(`Login por interfaz: ${loggedIn ? "OK" : "FALLÓ"} -> ${page.url()}`);
if (!loggedIn) { await page.screenshot({ path: `${OUT}/00-login-falla.png` }); await browser.close(); process.exit(1); }

let n = 0;
for (const path of PAGES) {
  current = path;
  n += 1;
  let status = 0, title = "", bodyErr = false, ms = 0;
  const t0 = Date.now();
  try {
    const resp = await page.goto(BASE + path, { waitUntil: "networkidle", timeout: 90000 });
    status = resp ? resp.status() : 0;
    title = (await page.title()).slice(0, 60);
    const text = await page.evaluate(() => document.body.innerText || "");
    bodyErr = /NullReferenceException|InvalidOperationException|SqlException|An unhandled exception|Stack trace/i.test(text);
    ms = Date.now() - t0;
    const name = String(n).padStart(2, "0") + "-" + path.replace(/[^a-zA-Z0-9]+/g, "_").slice(0, 60);
    await page.screenshot({ path: `${OUT}/${name}.png`, fullPage: false });
  } catch (e) {
    title = "ERROR: " + String(e).slice(0, 80);
  }
  const ok = status === 200 && !bodyErr && !page.url().includes("/Login") && !title.startsWith("ERROR");
  rows.push({ path, status, ok, ms, title });
  console.log(`${ok ? "OK   " : "FALLA"} ${status} ${String(ms).padStart(5)}ms ${path}  [${title}]`);
}

await browser.close();
const fails = rows.filter((r) => !r.ok);
writeFileSync(`${OUT}/reporte_navegador.json`, JSON.stringify({ rows, issues }, null, 2));
console.log(`== ${rows.length} pantallas, ${fails.length} fallas, ${issues.console.length} errores de consola, ${issues.page.length} errores JS, ${issues.http.length} respuestas HTTP>=400`);
for (const [p, m] of issues.console.slice(0, 15)) console.log("  consola:", p, m);
for (const [p, m] of issues.page.slice(0, 15)) console.log("  js:", p, m);
for (const [p, s, u] of issues.http.slice(0, 20)) console.log("  http:", p, s, u);
process.exit(fails.length ? 1 : 0);
