"""Verificación estructural de los PDF generados y de su HTML de origen.

Revisa, por documento:
- la cantidad de páginas del PDF, de imágenes y de tablas;
- que no queden restos de Markdown sin convertir (tablas, imágenes, títulos);
- que no aparezca contenido prohibido: códigos de Linear, emoji sueltos o el guion privado.

Sale con código 1 si encuentra problemas. No reemplaza mirar el resultado (ver capturas_html.cjs).

Uso (desde la raíz):
  python .claude/skills/presentar-sprint/scripts/verificar_pdfs.py outputs/presentacion-sprint2 [--prohibido REGEX ...]
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

CHECKS = [
    (r"\|---", "tabla Markdown sin convertir"),
    (r"!\[", "imagen Markdown sin convertir"),
    (r"(?m)^#{1,3} ", "título Markdown sin convertir"),
    (r"PRO-\d+", "código de Linear"),
    (r"[✅🟡⚪❌]", "emoji de estado suelto"),
    (r"[Gg]uion de defensa|[Pp]reguntas difíciles", "contenido del guion privado"),
]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("out_dir", type=Path)
    parser.add_argument("--prohibido", action="append", default=[], help="Regex adicional que no debe aparecer")
    args = parser.parse_args()
    checks = CHECKS + [(p, f"prohibido: {p}") for p in args.prohibido]

    problems = 0
    for pdf in sorted(args.out_dir.glob("*.pdf")):
        data = pdf.read_bytes()
        pages = len(re.findall(rb"/Type\s*/Page[^s]", data))
        html_path = args.out_dir / "html" / (pdf.stem + ".html")
        if not html_path.exists():
            print(f"{pdf.name}: falta {html_path}")
            problems += 1
            continue
        page = html_path.read_text(encoding="utf-8")
        text = re.sub(r"<style>.*?</style>", "", re.sub(r"<img [^>]+>", "<img>", page), flags=re.S)
        found = [name for pattern, name in checks if re.search(pattern, text)]
        problems += len(found)
        images, tables, ids = page.count("<img "), page.count("<table>"), page.count('class="nowrap"')
        print(f"{pdf.name}: páginas={pages} imágenes={images} tablas={tables} ids={ids} "
              f"problemas={found or 'ninguno'}")
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
