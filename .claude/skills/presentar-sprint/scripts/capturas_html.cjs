// Capturas del HTML de los informes en modo impresión y a ancho A4, para revisarlos con la herramienta Read.
// La herramienta Read no puede renderizar PDF (no hay pdftoppm), y el render de PDF por PowerShell
// con «-ExecutionPolicy Bypass» lo bloquea el clasificador. Por eso se revisa el HTML de origen.
// No reproduce los cortes de página exactos del PDF: cada captura es un tramo de alto A4.
//
// Uso (desde la raíz; usa el Playwright de Tools/E2E):
//   node .claude/skills/presentar-sprint/scripts/capturas_html.cjs <carpeta_capturas> outputs/presentacion-sprint2/html/*.html
const path = require('path');
const fs = require('fs');
const { pathToFileURL } = require('url');

function loadPlaywright() {
  const local = path.resolve('Tools/E2E/node_modules/playwright');
  return require(fs.existsSync(local) ? local : 'playwright');
}

(async () => {
  const { chromium } = loadPlaywright();
  const [outDir, ...files] = process.argv.slice(2);
  if (!outDir || files.length === 0) {
    console.error('Uso: node capturas_html.cjs <carpeta_capturas> <archivo.html> [...]');
    process.exit(2);
  }
  fs.mkdirSync(outDir, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 760, height: 1075 } });
  await page.emulateMedia({ media: 'print' });
  for (const f of files) {
    await page.goto(pathToFileURL(path.resolve(f)).href);
    await page.addStyleTag({ content: 'body{padding:0 4px} .cover{height:1060px}' });
    const h = await page.evaluate(() => document.body.scrollHeight);
    const n = Math.ceil(h / 1075);
    const base = path.basename(f, '.html');
    for (let i = 0; i < n; i++) {
      await page.screenshot({
        path: path.join(outDir, `${base}_s${String(i + 1).padStart(2, '0')}.png`),
        clip: { x: 0, y: i * 1075, width: 760, height: Math.min(1075, h - i * 1075) },
        fullPage: true,
      });
    }
    console.log(base, 'tramos=' + n);
  }
  await browser.close();
})();
