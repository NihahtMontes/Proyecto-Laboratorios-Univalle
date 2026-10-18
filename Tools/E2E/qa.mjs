// Cliente del navegador de QA persistente (ver qa_driver.mjs).
// Cada argumento es una acción en una línea; los valores con espacios van entre comillas dobles:
//   node Tools/E2E/qa.mjs 'goto /Countries' 'click "text=Nuevo País"' 'fill "input[name=\"Input.Name\"]" Bolivia' 'shot paises_nuevo'
// Acciones: goto u | reload | back | click s [n] | dblclick s | hover s | fill s v | type s v | select s v | check s | uncheck s | upload s ruta
//           press k | wait ms | waitfor s [ms] | url | count s | text [s] [max] | content [max] | html s | links [s] | rows [s] | value s | valid
//           swal | confirm | cancel | shot nombre [full] | viewport w h | eval js | errors | lastdownload | clearcookies | login [usuario] [clave]
import http from "node:http";

const PORT = Number(process.env.QA_DRIVER_PORT || 9333);
const SPEC = {
  goto: ["u"], click: ["s", "n"], dblclick: ["s"], hover: ["s"], fill: ["s", "v"], type: ["s", "v"], select: ["s", "v"], check: ["s"], uncheck: ["s"],
  upload: ["s", "v"], press: ["k"], wait: ["ms"], waitfor: ["s", "ms"], count: ["s"], html: ["s", "max"], links: ["s"], rows: ["s"], value: ["s"],
  shot: ["name", "full"], viewport: ["w", "h"], eval: ["js"], login: ["u", "p"],
};

function split(line) {
  const out = []; let cur = ""; let q = false; let esc = false; let had = false;
  for (const ch of line.trim()) {
    if (esc) { cur += ch; esc = false; continue; }
    if (ch === "\\") { esc = true; continue; }
    if (ch === '"') { q = !q; had = true; continue; }
    if (!q && /\s/.test(ch)) { if (cur !== "" || had) { out.push(cur); cur = ""; had = false; } continue; }
    cur += ch;
  }
  if (cur !== "" || had) out.push(cur);
  return out;
}

const actions = process.argv.slice(2).map((line) => {
  const [name, ...rest] = split(line);
  const a = { a: name };
  if (name === "text") { if (rest[0] !== undefined && /^\d+$/.test(rest[0])) a.max = rest[0]; else { a.s = rest[0]; a.max = rest[1]; } }
  else if (name === "content") { a.max = rest[0]; }
  else if (name === "eval") { a.js = rest.join(" "); }
  else (SPEC[name] || []).forEach((k, i) => { if (rest[i] !== undefined) a[k] = rest[i]; });
  return a;
});

const body = JSON.stringify({ actions });
const req = http.request({ host: "127.0.0.1", port: PORT, path: "/run", method: "POST", headers: { "content-type": "application/json", "content-length": Buffer.byteLength(body) } }, (res) => {
  let data = ""; res.on("data", (c) => (data += c));
  res.on("end", () => {
    try {
      for (const r of JSON.parse(data)) {
        const { a, ms, error, ...rest } = r;
        if (error) { console.log(`✘ ${a}: ${error}`); continue; }
        const keys = Object.keys(rest);
        if (!keys.length) { console.log(`✔ ${a} (${ms} ms)`); continue; }
        console.log(`✔ ${a} (${ms} ms) ${JSON.stringify(rest, null, 0)}`);
      }
    } catch { console.log(data); }
  });
});
req.on("error", () => { console.error("qa_driver no responde: arránquelo con node Tools/E2E/qa_driver.mjs"); process.exit(2); });
req.write(body); req.end();
