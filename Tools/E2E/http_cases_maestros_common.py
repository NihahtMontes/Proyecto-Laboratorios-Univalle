"""
Utilidades comunes de los casos HTTP de los maestros (Países, Ciudades, Facultades, Laboratorios, Carreras, Personas y Usuarios).
Solo contra la copia desechable de QA (http_forms ya se niega a correr con otra base). No es un caso de prueba por sí mismo.
"""
import os
import random
import re
import string
import urllib.error
import urllib.parse
import urllib.request

import e2e_lib
import http_forms
from http_forms import Client, expect, check

BASE = e2e_lib.BASE
R = []                 # resultados del módulo en ejecución
SEEN = []              # (etiqueta, status, errores, toasts) de cada respuesta observada
LOG500 = []            # respuestas con status >= 500 o con la página de error
SKIPPED = []           # comprobaciones no evaluables (con el motivo)
ENGLISH = []           # textos que parecen estar en inglés
_EN = re.compile(r"\b(The|must|Passwords?|is invalid|not valid|field|characters?|required|Invalid|already|exceed|maximum|minimum)\b")


def rnd(n=6):
    """Sufijo solo de letras (sirve para campos que no admiten dígitos) para poder re-ejecutar."""
    return "".join(random.choices(string.ascii_lowercase, k=n))


def uniq(prefix, n=6):
    return f"{prefix} {rnd(n)}"


def digits(n=8):
    return "".join(random.choices(string.digits, k=n))


# ---- base de datos (solo QA: e2e_lib toma E2E_DB, que http_forms obliga a ser la copia QA) ----
def db(sql, *p):
    return e2e_lib.db_rows(sql, *p)


def dbv(sql, *p):
    rows = e2e_lib.db_rows(sql, *p)
    return rows[0][0] if rows else None


def id_by(table, col, val):
    return dbv(f"select top 1 Id from {table} where {col}=? order by Id desc", val)


# ---- cliente ----
def client(user=None, pw=None):
    return Client(user or os.environ.get("E2E_USER", "qa_super"), pw or os.environ["E2E_PW"])


def logged_in(c):
    st, page = c.get("/")
    return st == 200 and "Cerrar Sesion" in page


def track(label, res):
    """Registra la respuesta para el resumen final (500, inglés)."""
    SEEN.append((label, res.status, list(res.errors), list(res.toasts)))
    body = res.text or ""
    if res.status >= 500 or "Codigo tecnico de seguimiento" in body or "Stack trace" in body or "An unhandled exception" in body:
        LOG500.append(f"{label}: status={res.status} location={res.location} errores={res.errors[:2]} toasts={res.toasts[:2]}")
    for e in res.errors + [t for _, t in res.toasts]:
        if _EN.search(e):
            ENGLISH.append(f"{label}: {e}")
    return res


def sub(c, label, path, ov=None, **kw):
    """c.submit() con seguimiento. Si el GET del formulario falla devuelve un Result con ese status."""
    try:
        res = c.submit(path, ov, name=label, **kw)
    except RuntimeError as exc:
        m = re.search(r"-> (\d+)", str(exc))
        res = http_forms.Result(int(m.group(1)) if m else 0, name=label)
    return track(label, res)


def post(c, label, action, fields, token_from, follow=True):
    """POST arbitrario con token tomado de `token_from`."""
    return track(label, c.raw_post(action, fields, follow=follow, name=label, token_from=token_from))


def post_notoken(c, label, action, fields):
    return track(label, c.raw_post(action, fields, follow=False, name=label))


# ---- anónimo ----
class _NR(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k):
        return None


def anon(method, path, fields=None):
    """(status, location) de una petición sin sesión y sin seguir redirects."""
    opener = urllib.request.build_opener(_NR)
    data = urllib.parse.urlencode(fields or []).encode() if method == "POST" else None
    req = urllib.request.Request(BASE + path, data=data, method=method,
                                 headers={"Content-Type": "application/x-www-form-urlencoded"} if data is not None else {})
    try:
        r = opener.open(req, timeout=60)
        return r.status, r.headers.get("Location")
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Location")


def guard_anon(label, gets, posts):
    """Sin sesión: GET y POST deben dar 302 a /Login."""
    for p in gets:
        st, loc = anon("GET", p)
        ok = st == 302 and (loc or "").lower().startswith(("/login", BASE.lower() + "/login"))
        check(R, f"{label}: sin sesión GET {p} -> 302 /Login", ok, f"status={st} location={loc}")
    for p in posts:
        st, loc = anon("POST", p, [("x", "y")])
        ok = st == 302 and (loc or "").lower().startswith(("/login", BASE.lower() + "/login"))
        check(R, f"{label}: sin sesión POST {p} -> 302 /Login", ok, f"status={st} location={loc}")


def guard_token(c, label, posts):
    """Con sesión pero sin token antifalsificación: 400."""
    for p in posts:
        res = post_notoken(c, f"{label}: POST sin token {p}", p, [("Input.Name", "sin token")])
        check(R, f"{label}: POST sin token {p} -> 400", res.status == 400, f"status={res.status}")


def guard_404(c, label, route_templates, query_templates=(), bad_ids=("99999", "-1", "0", "abc", "1.5", "99999999999", "2147483648")):
    """GET con ids inexistentes / negativos / no numéricos -> 404. route_templates usan {id}."""
    for tpl in list(route_templates) + list(query_templates):
        for bid in bad_ids:
            path = tpl.format(id=bid)
            st, page = c.get(path)
            check(R, f"{label}: GET {path} -> 404", st == 404, f"status={st}")
        if tpl in query_templates:
            path = tpl.split("?")[0]
            st, page = c.get(path)
            check(R, f"{label}: GET {path} (sin id) -> 404", st == 404, f"status={st}")


def finish(extra_notes=None):
    fails = e2e_lib.run_all([lambda: R])
    print()
    print("---- RESPUESTAS 500 / PÁGINA DE ERROR ----")
    for x in (LOG500 or ["(ninguna)"]):
        print(x)
    print("---- TEXTOS QUE PARECEN INGLÉS ----")
    seen = set()
    for x in (ENGLISH or ["(ninguno)"]):
        if x not in seen:
            seen.add(x)
            print(x)
    print("---- NO EVALUABLES (dependen de otro defecto) ----")
    for x in (SKIPPED or ["(ninguna)"]):
        print(x)
    if extra_notes:
        print("---- NOTAS ----")
        for n in extra_notes:
            print(n)
    return fails


def skip(name, reason):
    SKIPPED.append(f"{name}: {reason}")


def listing_has(c, path, text):
    """True si el HTML del listado contiene `text` (ya escapado como lo devuelve Razor)."""
    st, page = c.get(path)
    return st == 200 and text in page


def esc(s):
    import html
    return html.escape(s, quote=True)
