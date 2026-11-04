"""Resume los resultados de los scripts E2E (Tools/E2E) para el informe de pruebas.

Cada script deja un .txt con líneas «[OK]» / «[FALLA]» y un cierre «== N comprobaciones, M fallas». Este
resumen toma, por script, el resultado MÁS RECIENTE (por fecha de modificación) dentro de las carpetas dadas,
y muestra una tabla con el total. Con --md imprime filas listas para pegar en el Markdown.

Uso (desde la raíz):
  python .claude/skills/presentar-sprint/scripts/resumen_e2e.py outputs/qa-usuario/qa-2026-10-18 [otra_carpeta ...] [--md] [--fallas]
"""
from __future__ import annotations

import argparse
import datetime as dt
import re
from pathlib import Path

SUMMARY = re.compile(r"^== (\d+) comprobaciones, (\d+) fallas", re.M)


def collect(folders: list[Path]) -> dict[str, tuple[int, int, float, Path]]:
    latest: dict[str, tuple[int, int, float, Path]] = {}
    for folder in folders:
        for path in folder.rglob("*.txt"):
            text = path.read_text(encoding="utf-8", errors="replace")
            found = SUMMARY.findall(text)
            if not found:
                continue
            checks, fails = map(int, found[-1])
            mtime = path.stat().st_mtime
            name = path.stem
            if name not in latest or mtime > latest[name][2]:
                latest[name] = (checks, fails, mtime, path)
    return latest


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("folders", nargs="+", type=Path)
    parser.add_argument("--md", action="store_true", help="Imprimir filas de tabla Markdown")
    parser.add_argument("--fallas", action="store_true", help="Mostrar las líneas [FALLA] de cada script")
    args = parser.parse_args()

    rows = collect(args.folders)
    total_checks = sum(r[0] for r in rows.values())
    total_fails = sum(r[1] for r in rows.values())
    for name, (checks, fails, mtime, path) in sorted(rows.items()):
        when = dt.datetime.fromtimestamp(mtime).strftime("%d/%m %H:%M")
        if args.md:
            print(f"| | `{name}` | {checks} | {fails} |")
        else:
            print(f"{name:42s} {checks:5d} {fails:4d}  {when}  {path}")
        if args.fallas and fails:
            for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
                if line.startswith("[FALLA]"):
                    print("      " + line[:220])
    print(f"\n{len(rows)} scripts · {total_checks} comprobaciones · {total_fails} fallas")


if __name__ == "__main__":
    main()
