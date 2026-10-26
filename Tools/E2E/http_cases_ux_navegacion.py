"""
Recorrido de UX por rol (fases 1-2 de docs/ux/AUDITORIA_UX_2026-10-21.md) contra la copia QA.
Por cada rol (qa_super, qa_adm, qa_sup, qa_cierre) abre el inicio, todas las entradas del menú lateral y,
para los administradores, los pasos del asistente preventivo y correctivo. Comprueba:
  - la página responde 200 y no termina en /Error, /AccesoDenegado ni /Login;
  - la pestaña del navegador (<title>) tiene nombre;
  - no se ven códigos de formato institucional (L-3, L7, L-48…) ni «Wizard»/«Dashboard»/«Kardex»/«¡Ups!» (salvo en /Ayuda);
  - el menú no repite etiquetas;
  - una pantalla sin permiso muestra «No tiene permiso» y «Mi perfil» abre para todos.
Uso: E2E_PW=... E2E_DB=DB_Laboratorios_Univalle_QA_E2E python Tools/E2E/http_cases_ux_navegacion.py
"""
import html as _html
import os
import re
import sys
import urllib.error

import e2e_lib
from http_forms import Client

PW = os.environ["E2E_PW"]
BASE = e2e_lib.BASE
ROLES = {"SuperAdmin": "qa_super", "Administrador": "qa_adm", "Supervisor": "qa_sup", "Cierre": "qa_cierre"}
CODE = re.compile(r"\bL-?(?:3|5|6|7|8|12|48)\b|\bWizard\b|\bDashboard\b|\bKardex\b|¡Ups!", re.I)
LOG, FAILS = [], 0


def ok(case, cond, msg):
    global FAILS
    FAILS += 0 if cond else 1
    line = f"[{'OK' if cond else 'FALLA'}] {case}: {msg}"
    LOG.append(line)
    print(line, flush=True)


def open_page(client, path):
    """GET siguiendo redirects -> (status, url_final_relativa, html)."""
    url = path if path.startswith("http") else BASE + path
    try:
        r = client.opener.open(url, timeout=180)
        return r.status, r.geturl().replace(BASE, ""), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.geturl().replace(BASE, ""), e.read().decode("utf-8", "replace")


def visible_text(page):
    body = re.search(r"<body.*", page, re.S)
    body = body.group(0) if body else page
    body = re.sub(r"<script.*?</script>|<style.*?</style>|<!--.*?-->", " ", body, flags=re.S)
    body = re.sub(r'<[^>]*\b(title|placeholder|aria-label|data-[\w-]+)="[^"]*"', "<", body)  # atributos no visibles
    body = re.sub(r"<[^>]+>", " ", body)
    return re.sub(r"\s+", " ", _html.unescape(body))


def menu(page):
    aside = re.search(r'<aside class="left-sidebar".*?</aside>', page, re.S)
    if not aside:
        return [], []
    html = aside.group(0)
    links, labels = [], []
    for m in re.finditer(r'<li class="sidebar-item[^"]*">\s*<a [^>]*href="([^"]+)"[^>]*>(.*?)</a>', html, re.S):
        href, inner = _html.unescape(m.group(1)), m.group(2)
        label = re.sub(r"\s+", " ", re.sub(r"<[^>]+>", " ", inner)).strip()
        labels.append(label)
        if not href.startswith("javascript"):
            links.append((label, href))
    return links, labels


def check_page(role, client, label, path):
    status, final, page = open_page(client, path)
    case = f"{role} · {label}"
    bad_end = re.match(r"/(Error|AccesoDenegado|Login)\b", final or "")
    ok(case, status == 200 and not bad_end, f"{path} -> {status} {final}")
    title = re.search(r"<title>(.*?)</title>", page, re.S)
    name = _html.unescape(title.group(1)).replace("Laboratorios Univalle", "").strip(" -–|") if title else ""
    ok(case, bool(name), f"pestaña con nombre: «{name}»")
    if not path.lower().startswith("/ayuda"):
        hits = sorted({m.group(0) for m in CODE.finditer(visible_text(page))})
        ok(case, not hits, f"sin códigos ni jerga visibles: {hits[:8]}")
    return page


def wizard_paths():
    prev = e2e_lib.db_scalar("select top 1 Id from Managements where Status=0 and Type=0 order by Id desc")
    corr = e2e_lib.db_scalar("select top 1 Id from Managements where Status=0 and Type=1 order by Id desc")
    paths = [(f"asistente preventivo paso {s}", f"/Index?ShowWizard=true&Step={s}&ManagementId={prev}") for s in range(0, 8)] if prev else []
    paths += [(f"asistente correctivo paso {s}", f"/Index?ShowWizard=true&Step={s}&ManagementId={corr}") for s in (0, 2, 3, 4, 5, 6, 7)] if corr else []
    return paths


for role, user in ROLES.items():
    c = Client(user, PW)
    status, final, home = open_page(c, "/")
    ok(f"{role} · inicio", status == 200 and "/Error" not in final, f"/ -> {final}")
    links, labels = menu(home)
    dup = sorted({l for l in labels if labels.count(l) > 1})
    ok(f"{role} · menú", not dup, f"{len(labels)} entradas, repetidas: {dup}")
    seen = set()
    for label, href in links:
        if href in seen:
            continue
        seen.add(href)
        check_page(role, c, label, href)
    if role in ("SuperAdmin", "Administrador"):
        for label, path in wizard_paths():
            check_page(role, c, label, path)
    page = check_page(role, c, "Mi perfil", "/Account/Profile")
    if role == "Supervisor":
        status, final, page = open_page(c, "/Managements/Index")
        ok(f"{role} · sin permiso", "No tiene permiso" in page, f"/Managements/Index -> {final}")

with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "outputs", "ux-2026-10-21",
                       "ux_navegacion_resultado.txt"), "w", encoding="utf-8") as fh:
    fh.write("\n".join(LOG) + f"\n== {len(LOG)} comprobaciones, {FAILS} fallas\n")
print(f"== {len(LOG)} comprobaciones, {FAILS} fallas")
sys.exit(1 if FAILS else 0)
