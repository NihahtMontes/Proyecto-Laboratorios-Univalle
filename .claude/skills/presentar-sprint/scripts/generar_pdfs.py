"""Genera los PDF de presentación de un sprint a partir de sus Markdown.

Convierte el subconjunto de Markdown que usan esos documentos (títulos, párrafos, tablas, listas anidadas,
citas, bloques de código, imágenes y énfasis) a HTML con estilo de impresión A4, incrusta las imágenes en
base64 y lo imprime con Chrome (o Edge) en modo headless. Solo usa la biblioteca estándar.

Uso (desde la raíz del repositorio):
  python .claude/skills/presentar-sprint/scripts/generar_pdfs.py --sprint 2 --fecha "26 de octubre de 2026" \
      [--src docs/sprint] [--out outputs/presentacion-sprint2] [--proyecto "..."] \
      [--doc FUENTE.md=SALIDA.pdf ...]

Sin --doc usa los 4 documentos estándar (backlog, pruebas, arquitectura, base de datos).
El guion de defensa es privado: nunca se pasa a PDF.
"""
from __future__ import annotations

import argparse
import base64
import html
import mimetypes
import re
import shutil
import subprocess
import tempfile
from pathlib import Path


def find_root() -> Path:
    """Raíz del repositorio: el primer ancestro del directorio actual que contiene .git."""
    here = Path.cwd().resolve()
    for candidate in (here, *here.parents):
        if (candidate / ".git").exists():
            return candidate
    return here


ROOT = find_root()


def default_documents(sprint: str) -> list[tuple[str, str]]:
    return [
        (f"BACKLOG_SPRINT{sprint}.md", f"01_Backlog_Sprint{sprint}.pdf"),
        (f"INFORME_PRUEBAS_SPRINT{sprint}.md", "02_Informe_Pruebas.pdf"),
        ("ARQUITECTURA_SOLID_PATRONES.md", "03_Arquitectura_SOLID_Patrones.pdf"),
        ("ESTADO_BASE_DATOS.md", "04_Estado_Base_Datos.pdf"),
    ]


# Se completan en main() a partir de los argumentos.
SPRINT_LABEL = "Sprint"
PROJECT_TEXT = "Laboratorios Univalle — Gastronomía"
DATE_TEXT = ""

BROWSERS = [
    Path(r"C:\Program Files\Google\Chrome\Application\chrome.exe"),
    Path(r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"),
    Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
    Path(r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
]

STATUS = {
    "✅": ("ok", "Hecho"),
    "🟡": ("partial", "Parcial"),
    "⚪": ("pending", "Pendiente"),
    "❌": ("discarded", "Descartado"),
}

CSS = """
@page { size: A4; margin: 16mm 15mm 16mm 15mm; }
* { box-sizing: border-box; }
body { font-family: 'Segoe UI', Aptos, Arial, sans-serif; font-size: 10pt; color: #1f2933; line-height: 1.45; margin: 0; }
.cover { height: 255mm; display: flex; flex-direction: column; justify-content: center; page-break-after: always;
         border-left: 10px solid #17365D; padding-left: 14mm; }
.cover .kicker { color: #2E6DA4; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; font-size: 10pt; }
.cover h1 { font-size: 28pt; color: #17365D; margin: 6mm 0 8mm 0; line-height: 1.15; }
.cover .meta { font-size: 11pt; color: #33475b; }
.cover .meta div { margin: 1.5mm 0; }
.cover .foot { margin-top: 18mm; font-size: 9.5pt; color: #6b7c93; }
h1 { font-size: 20pt; color: #17365D; }
h2 { font-size: 15pt; color: #17365D; border-bottom: 2px solid #D5DFEA; padding-bottom: 2mm; margin: 8mm 0 3mm 0;
     break-after: avoid; page-break-after: avoid; }
h3 { font-size: 12pt; color: #17365D; margin: 6mm 0 2mm 0; break-after: avoid; page-break-after: avoid; }
p { margin: 2mm 0; }
hr { border: none; border-top: 1px solid #D5DFEA; margin: 5mm 0; }
table { width: 100%; border-collapse: collapse; margin: 3mm 0 4mm 0; font-size: 8.8pt; }
th { background: #EAF2F8; color: #17365D; text-align: left; font-weight: 700; }
th, td { border: 1px solid #D5DFEA; padding: 1.6mm 2mm; vertical-align: top; }
tr { break-inside: avoid; page-break-inside: avoid; }
thead { display: table-header-group; }
tr:nth-child(even) td { background: #FAFCFE; }
code { font-family: Consolas, 'Cascadia Mono', monospace; font-size: 8.6pt; background: #F1F5F9; padding: 0 1mm;
       border-radius: 2px; word-break: break-word; }
pre { background: #0F2540; color: #E8F0F8; padding: 3mm 4mm; border-radius: 4px; font-size: 8.4pt; line-height: 1.4;
      white-space: pre-wrap; break-inside: avoid; page-break-inside: avoid; }
pre code { background: none; color: inherit; padding: 0; font-size: inherit; }
blockquote { margin: 3mm 0; padding: 2.5mm 4mm; border-left: 4px solid #2E6DA4; background: #EAF2F8; color: #17365D; }
ul, ol { margin: 1.5mm 0 1.5mm 6mm; padding-left: 4mm; }
li { margin: 0.8mm 0; }
figure { margin: 4mm 0 5mm 0; text-align: center; break-inside: avoid; page-break-inside: avoid; }
figure img { max-width: 100%; max-height: 150mm; border: 1px solid #D5DFEA; border-radius: 3px; }
figure.diagram img { border: none; max-height: 190mm; }
figcaption { font-size: 8.8pt; color: #52606d; font-style: italic; margin-top: 1.5mm; }
.badge { display: inline-block; padding: 0.3mm 2mm; border-radius: 8px; font-size: 8pt; font-weight: 700; white-space: nowrap; }
.badge.ok { background: #DCF2E3; color: #1E6B3A; }
.badge.partial { background: #FFF1CC; color: #8A5A00; }
.badge.pending { background: #E8ECF1; color: #4A5868; }
.badge.discarded { background: #FBE0E0; color: #9B1C1C; }
.dot { font-size: 9pt; }
.dot.ok { color: #2E9E5B; } .dot.partial { color: #D9A100; } .dot.pending { color: #8A99AA; } .dot.discarded { color: #C0392B; }
.dagger { color: #9B1C1C; font-weight: 700; }
.nowrap { white-space: nowrap; font-weight: 600; color: #17365D; }
"""


# ---------------------------------------------------------------- inline

def _status_dots(text: str) -> str:
    for emoji, (cls, _) in STATUS.items():
        text = text.replace(emoji, f'<span class="dot {cls}">●</span>')
    return text


def inline(text: str) -> str:
    text = html.escape(text, quote=False)
    codes: list[str] = []

    def keep_code(m: re.Match) -> str:
        codes.append(m.group(1))
        return f"\x00{len(codes) - 1}\x00"

    text = re.sub(r"`([^`]+)`", keep_code, text)
    text = text.replace("\\*", "\x01")
    text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])", r"<em>\1</em>", text)
    text = text.replace("\x01", "*")
    text = text.replace("†", '<span class="dagger">†</span>')
    text = _status_dots(text)
    return re.sub(r"\x00(\d+)\x00", lambda m: f"<code>{codes[int(m.group(1))]}</code>", text)


def table_cell(text: str) -> str:
    raw = text.strip()
    core = raw.replace("†", "").strip()
    if core in STATUS:
        cls, label = STATUS[core]
        dagger = ' <span class="dagger">†</span>' if "†" in raw else ""
        return f'<span class="badge {cls}">{label}</span>{dagger}'
    if re.fullmatch(r"[A-Z]{2,3}-\d{2,3}", raw):  # identificadores de historia (BD-01, QA-04…)
        return f'<span class="nowrap">{raw}</span>'
    return inline(raw)


# ---------------------------------------------------------------- bloques

def image_tag(alt: str, src: str, base: Path) -> str:
    path = (base / src).resolve()
    if not path.exists():
        raise FileNotFoundError(f"Imagen no encontrada: {path}")
    mime = mimetypes.guess_type(path.name)[0] or ("image/svg+xml" if path.suffix == ".svg" else "image/png")
    data = base64.b64encode(path.read_bytes()).decode("ascii")
    cls = ' class="diagram"' if path.suffix == ".svg" else ""
    return (f'<figure{cls}><img src="data:{mime};base64,{data}" alt="{html.escape(alt)}">'
            f"<figcaption>{inline(alt)}</figcaption></figure>")


def render_table(rows: list[str]) -> str:
    def cells(line: str) -> list[str]:
        line = line.strip()
        if line.startswith("|"):
            line = line[1:]
        if line.endswith("|"):
            line = line[:-1]
        return line.split("|")

    header = cells(rows[0])
    body = [cells(r) for r in rows[2:]]
    out = ["<table><thead><tr>", *(f"<th>{inline(c.strip())}</th>" for c in header), "</tr></thead><tbody>"]
    for r in body:
        out.append("<tr>" + "".join(f"<td>{table_cell(c)}</td>" for c in r) + "</tr>")
    out.append("</tbody></table>")
    return "".join(out)


LIST_RE = re.compile(r"^(\s*)([-*]|\d+\.)\s+(.*)$")


def render_list(lines: list[str]) -> str:
    """Listas anidadas por sangría; las líneas sin viñeta continúan el ítem anterior."""
    out: list[str] = []
    stack: list[tuple[int, str]] = []  # (sangría, etiqueta)
    for line in lines:
        m = LIST_RE.match(line)
        if not m:
            out.append("<br>" + inline(line.strip()))
            continue
        indent, marker, content = len(m.group(1)), m.group(2), m.group(3)
        tag = "ol" if marker[0].isdigit() else "ul"
        while stack and indent < stack[-1][0]:
            out.append(f"</li></{stack.pop()[1]}>")
        if not stack or indent > stack[-1][0]:
            out.append(f"<{tag}><li>")
            stack.append((indent, tag))
        else:
            out.append("</li><li>")
        out.append(inline(content))
    while stack:
        out.append(f"</li></{stack.pop()[1]}>")
    return "".join(out)


def render_blocks(lines: list[str], base: Path) -> str:
    out: list[str] = []
    i, n = 0, len(lines)
    while i < n:
        line = lines[i]
        stripped = line.strip()
        if not stripped:
            i += 1
            continue
        if stripped.startswith("```"):
            j = i + 1
            while j < n and not lines[j].strip().startswith("```"):
                j += 1
            code = "\n".join(lines[i + 1:j])
            out.append(f"<pre><code>{html.escape(code)}</code></pre>")
            i = j + 1
            continue
        if stripped == "---":
            out.append("<hr>")
            i += 1
            continue
        m = re.match(r"^(#{1,3})\s+(.*)$", stripped)
        if m:
            level = len(m.group(1))
            out.append(f"<h{level}>{inline(m.group(2))}</h{level}>")
            i += 1
            continue
        m = re.match(r"^!\[(.*)\]\((.+)\)$", stripped)
        if m:
            out.append(image_tag(m.group(1), m.group(2), base))
            i += 1
            continue
        if stripped.startswith("|"):
            j = i
            while j < n and lines[j].strip().startswith("|"):
                j += 1
            out.append(render_table(lines[i:j]))
            i = j
            continue
        if stripped.startswith(">"):
            j = i
            quote = []
            while j < n and lines[j].strip().startswith(">"):
                quote.append(lines[j].strip()[1:].strip())
                j += 1
            out.append("<blockquote>" + "<br>".join(inline(q) for q in quote) + "</blockquote>")
            i = j
            continue
        if LIST_RE.match(line):
            j = i
            block = []
            while j < n and lines[j].strip() and (LIST_RE.match(lines[j]) or lines[j].startswith("   ")):
                block.append(lines[j])
                j += 1
            out.append(render_list(block))
            i = j
            continue
        j = i
        para = []
        while j < n and lines[j].strip() and not re.match(r"^(#{1,3}\s|\||>|```|!\[|---$)", lines[j].strip()) \
                and not LIST_RE.match(lines[j]):
            para.append(inline(lines[j].strip()))
            j += 1
        out.append("<p>" + "<br>".join(para) + "</p>")
        i = j
    return "\n".join(out)


def render_document(md_path: Path, number: int, total: int) -> str:
    lines = md_path.read_text(encoding="utf-8").splitlines()
    # Portada: el H1 y las líneas de metadatos hasta el primer separador.
    sep = next(k for k, l in enumerate(lines) if l.strip() == "---")
    head, body = lines[:sep], lines[sep + 1:]
    title = next(l[2:].strip() for l in head if l.startswith("# "))
    meta = [inline(l.strip()) for l in head if l.strip() and not l.startswith("# ")]
    cover = (
        '<section class="cover">'
        f'<div class="kicker">{html.escape(SPRINT_LABEL)} · Documento {number} de {total}</div>'
        f"<h1>{inline(title)}</h1>"
        '<div class="meta">' + "".join(f"<div>{m}</div>" for m in meta) + "</div>"
        f'<div class="foot">{html.escape(PROJECT_TEXT)} · {html.escape(DATE_TEXT)}</div>'
        "</section>"
    )
    content = render_blocks(body, md_path.parent)
    return (f'<!DOCTYPE html><html lang="es"><head><meta charset="utf-8"><title>{html.escape(title)}</title>'
            f"<style>{CSS}</style></head><body>{cover}{content}</body></html>")


# ---------------------------------------------------------------- PDF

def find_browser() -> Path:
    for candidate in BROWSERS:
        if candidate.exists():
            return candidate
    found = shutil.which("chrome") or shutil.which("msedge")
    if found:
        return Path(found)
    raise SystemExit("No se encontró Chrome ni Edge para imprimir el PDF.")


def print_pdf(browser: Path, html_path: Path, pdf_path: Path) -> None:
    with tempfile.TemporaryDirectory(prefix="pdfprof_") as profile:
        cmd = [
            str(browser), "--headless=new", "--disable-gpu", "--no-first-run", "--no-default-browser-check",
            f"--user-data-dir={profile}", "--no-pdf-header-footer", "--virtual-time-budget=8000",
            f"--print-to-pdf={pdf_path}", html_path.as_uri(),
        ]
        subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=180)
    if not pdf_path.exists() or pdf_path.stat().st_size == 0:
        raise SystemExit(f"El navegador no generó {pdf_path}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Genera los PDF de presentación de un sprint.")
    parser.add_argument("--sprint", required=True, help="Número del sprint, p. ej. 2")
    parser.add_argument("--fecha", required=True, help='Fecha de la portada, p. ej. "26 de octubre de 2026"')
    parser.add_argument("--src", default="docs/sprint", help="Carpeta con los Markdown (relativa a la raíz)")
    parser.add_argument("--out", help="Carpeta de salida (por defecto outputs/presentacion-sprint<N>)")
    parser.add_argument("--proyecto", default=PROJECT_TEXT, help="Texto del pie de la portada")
    parser.add_argument("--doc", action="append", default=[], metavar="FUENTE.md=SALIDA.pdf",
                        help="Documento a generar; se puede repetir. Sin --doc se usan los 4 estándar")
    return parser.parse_args()


def main() -> None:
    global SPRINT_LABEL, PROJECT_TEXT, DATE_TEXT
    args = parse_args()
    SPRINT_LABEL, PROJECT_TEXT, DATE_TEXT = f"Sprint {args.sprint}", args.proyecto, args.fecha
    src = (ROOT / args.src).resolve()
    out_dir = (ROOT / (args.out or f"outputs/presentacion-sprint{args.sprint}")).resolve()
    documents = [tuple(d.split("=", 1)) for d in args.doc] if args.doc else default_documents(args.sprint)
    missing = [s for s, _ in documents if not (src / s).exists()]
    if missing:
        raise SystemExit(f"No existen en {src}: {', '.join(missing)}")
    html_dir = out_dir / "html"
    html_dir.mkdir(parents=True, exist_ok=True)
    browser = find_browser()
    for number, (source, pdf_name) in enumerate(documents, start=1):
        page = render_document(src / source, number, len(documents))
        html_path = html_dir / (Path(pdf_name).stem + ".html")
        html_path.write_text(page, encoding="utf-8")
        pdf_path = out_dir / pdf_name
        print_pdf(browser, html_path, pdf_path)
        print(f"OK  {pdf_path.relative_to(ROOT)}  ({pdf_path.stat().st_size // 1024} KB)")


if __name__ == "__main__":
    main()
