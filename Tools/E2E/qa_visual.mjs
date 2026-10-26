// QA visual (solo lectura): capturas en escritorio y móvil de las pantallas nuevas, errores de consola/JS/HTTP y desbordes.
// Uso: E2E_PW=... node qa_visual.mjs   (la app debe estar en http://localhost:5000)
import { chromium } from "playwright";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE || "http://localhost:5000";
const USER = process.env.E2E_USER || "nihaht";
const PW = process.env.E2E_PW;
if (!PW) { console.error("Falta E2E_PW"); process.exit(2); }
const here = dirname(fileURLToPath(import.meta.url));
const OUT = resolve(here, "..", "..", "outputs", "qa", "img");
mkdirSync(OUT, { recursive: true });

const PAGES = [
  ["assetview", "/AssetView"],
  ["assetview_inventario", "/AssetView?ActiveTab=Inventario"],
  ["equipos_tipos", "/AssetView/EquipmentClassifications"],
  ["equipos_informatico", "/AssetView/Units?classification=Informatico"],
  ["equipos_otro", "/AssetView/Units?classification=Otro"],
  ["utensilios_tipos", "/AssetView/UtensilClassifications"],
  ["utensilios_metal", "/AssetView/UtensilUnits?utensilType=Metal"],
  ["gestiones_p1", "/Managements?Type=PorConfirmar"],
  ["gestiones_p2", "/Managements?Type=PorConfirmar&pageIndex=2"],
  ["gestion_22_l48", "/Managements/Details?id=22&ActiveTab=l48"],
  ["gestion_1_l48", "/Managements/Details?id=1&ActiveTab=l48"],
  ["gestion_1_dashboard", "/Managements/Details?id=1&ActiveTab=dashboard"],
  ["gestion_3_dashboard", "/Managements/Details?id=3&ActiveTab=dashboard"],
  ["gestion_27_l48", "/Managements/Details?id=27&ActiveTab=l48"],
  ["plan_gestion_22", "/Maintenances/Plan?ManagementId=22"],
];
const VIEWPORTS = [["desktop", { width: 1440, height: 900 }], ["movil", { width: 390, height: 844 }]];

const browser = await chromium.launch();
const report = [];
for (const [vname, viewport] of VIEWPORTS) {
  const ctx = await browser.newContext({ viewport });
  const page = await ctx.newPage();
  let current = "login";
  const issues = [];
  page.on("console", (m) => { if (m.type() === "error") issues.push(`consola@${current}: ${m.text().slice(0, 160)}`); });
  page.on("pageerror", (e) => issues.push(`js@${current}: ${String(e).slice(0, 160)}`));
  page.on("response", (r) => { if (r.status() >= 400 && !r.url().includes("favicon")) issues.push(`http ${r.status()}@${current}: ${r.url().replace(BASE, "").slice(0, 100)}`); });
  await page.goto(`${BASE}/Login`);
  await page.fill('input[name="Input.UserName"]', USER);
  await page.fill('input[name="Input.Password"]', PW);
  await Promise.all([page.waitForNavigation({ waitUntil: "networkidle", timeout: 60000 }), page.click('button[type="submit"], input[type="submit"]')]);
  for (const [name, path] of PAGES) {
    current = path;
    const t0 = Date.now();
    const resp = await page.goto(BASE + path, { waitUntil: "networkidle", timeout: 90000 });
    const ms = Date.now() - t0;
    const m = await page.evaluate(() => ({
      overflowX: document.documentElement.scrollWidth - window.innerWidth,
      height: document.documentElement.scrollHeight,
      brokenImgs: [...document.images].filter((i) => !i.complete || i.naturalWidth === 0).length,
      text: document.body.innerText.length,
    }));
    await page.screenshot({ path: `${OUT}/${vname}_${name}.png`, fullPage: true });
    report.push({ viewport: vname, name, path, status: resp.status(), ms, ...m });
  }
  report.push({ viewport: vname, issues });
  await ctx.close();
}
await browser.close();
writeFileSync(`${OUT}/../qa_visual.json`, JSON.stringify(report, null, 2));
for (const r of report) {
  if (r.issues) { console.log(`[${r.viewport}] incidencias: ${r.issues.length}`); r.issues.slice(0, 12).forEach((i) => console.log("   ", i)); continue; }
  const flag = r.status !== 200 || r.overflowX > 4 || r.brokenImgs > 0 ? "REVISAR" : "OK     ";
  console.log(`${flag} ${r.viewport.padEnd(7)} ${String(r.status)} ${String(r.ms).padStart(5)}ms overflowX=${String(r.overflowX).padStart(4)} imgs_rotas=${r.brokenImgs} alto=${r.height} ${r.path}`);
}
