"""
Utilidades compartidas de las pruebas HTTP de los catálogos académicos (http_cases_catalogos_*.py).
Solo contra la copia desechable de QA (http_forms ya se niega a correr con otra base).

- QClient: Client con detección de anomalías (500, página de error, texto en inglés) en cada respuesta.
- tag()/uniq(): sufijos únicos SOLO con letras (re-ejecutable).
- anon_get/anon_post, tokenless_post: acceso sin sesión (302 a /Login) y POST sin token antifalsificación (400).
- multipart_post: subida multipart con urllib (el motor solo envía urlencoded).
- sql()/scalar(): lecturas a la base QA para comprobar el estado real (no se escribe en la base por SQL).
- finish(): imprime la tabla, anomalías y escribe outputs/qa-usuario/http_cases_catalogos_<modulo>.txt
"""
import contextlib
import io
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

import e2e_lib
from http_forms import Client, Result as HttpResult, _NoRedirect, check, expect, parse_messages  # noqa: F401

BASE = e2e_lib.BASE
OUT_DIR = Path(__file__).resolve().parents[2] / "outputs" / "qa-usuario"

ANOMALIES: list[str] = []
_EN = re.compile(r"\b(The|is invalid|is not valid|field is required|must be|must not|A value is required|Failed to|Unhandled|An error)\b")
_ERR_PAGE = re.compile(r"Codigo tecnico de seguimiento|An unhandled exception|NullReferenceException|InvalidOperationException|SqlException|Stack trace|DbUpdateException", re.I)


def note(where: str, status: int, text: str, errors=None, toasts=None, location=None):
    """Registra 500, páginas de error y textos de validación/toast en inglés."""
    errors = errors or []
    toasts = toasts or []
    if status >= 500:
        ANOMALIES.append(f"HTTP {status} en {where}")
    if location and "/Error" in location:
        ANOMALIES.append(f"redirect a /Error desde {where} -> {location}")
    if text and _ERR_PAGE.search(re.sub(r"<script.*?</script>", "", text, flags=re.S)):
        ANOMALIES.append(f"página de error/excepción visible en {where} (status {status})")
    for e in errors:
        if _EN.search(e):
            ANOMALIES.append(f"texto en inglés en validación de {where}: {e!r}")
    for k, t in toasts:
        if _EN.search(t):
            ANOMALIES.append(f"texto en inglés en toast ({k}) de {where}: {t!r}")


class QClient(Client):
    def get(self, path):
        status, text = super().get(path)
        note(f"GET {path[:90]}", status, text)
        return status, text

    def _post(self, action, fields, follow, name):
        res = super()._post(action, fields, follow, name)
        note(f"POST {action.replace(BASE, '')[:90]} [{name}]", res.status, res.text, res.errors, res.toasts, res.location)
        return res


def client() -> QClient:
    return QClient(os.environ.get("E2E_USER", "qa_super"), os.environ["E2E_PW"])


_counter = [0]


def tag() -> str:
    """Sufijo único de solo letras (a-z)."""
    _counter[0] += 1
    n = (int(time.time()) % (26 ** 6)) * 100 + _counter[0]
    s = ""
    while n:
        n, r = divmod(n, 26)
        s += chr(97 + r)
    return s


def uniq(prefix: str) -> str:
    return f"{prefix} {tag()}"


# ---------------------------------------------------------------- lectura de la base QA
def sql(q, *params):
    return e2e_lib.db_rows(q, *params)


def scalar(q, *params):
    return e2e_lib.db_scalar(q, *params)


# ---------------------------------------------------------------- acceso sin sesión / sin token
def anon(path, fields=None):
    """GET (o POST urlencoded si hay fields) sin cookies ni redirects -> (status, location)."""
    op = urllib.request.build_opener(_NoRedirect)
    data = urllib.parse.urlencode(fields).encode() if fields is not None else None
    req = urllib.request.Request(BASE + path, data=data)
    try:
        r = op.open(req, timeout=60)
        return r.status, None
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Location")


def check_anon(R, label, path, post=False):
    status, loc = anon(path, {"x": "1"} if post else None)
    ok = status == 302 and "/login" in (loc or "").lower()
    check(R, f"{label}: sin sesión {'POST' if post else 'GET'} {path} -> 302 /Login", ok, f"status={status} location={loc}")


def tokenless(R, c, label, action, fields):
    """POST con sesión pero SIN token antifalsificación: debe ser 400."""
    res = c.raw_post(action, fields, follow=False, name=label)
    check(R, f"{label}: POST sin token antifalsificación -> 400", res.status == 400, f"status={res.status}")
    return res


def get_nofollow(c, path):
    """GET con la sesión de c sin seguir redirects -> (status, location)."""
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(c.jar), _NoRedirect)
    try:
        r = op.open(BASE + path, timeout=120)
        return r.status, None
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Location")


def check_404(R, c, label, path):
    status, _ = c.get(path)
    check(R, f"{label}: GET {path} -> 404", status == 404, f"status={status}")


def post_404(R, c, label, action, fields, token_from):
    res = c.raw_post(action, fields, follow=False, name=label, token_from=token_from)
    check(R, f"{label}: POST {action} -> 404", res.status == 404, f"status={res.status} loc={res.location}")
    return res


# ---------------------------------------------------------------- multipart
def multipart_post(c, action, fields, files, token_from=None, follow=True, name=""):
    """POST multipart/form-data con cookies de c. files: [(campo, nombre_archivo, bytes, content_type)]."""
    fields = list(fields)
    if token_from:
        _, page = c.get(token_from)
        tok = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
        fields = [("__RequestVerificationToken", tok)] + fields
    boundary = "----qac" + uuid.uuid4().hex
    body = bytearray()
    for k, v in fields:
        body += f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode()
    for field, filename, content, ctype in files:
        body += (f'--{boundary}\r\nContent-Disposition: form-data; name="{field}"; filename="{filename}"\r\n'
                 f'Content-Type: {ctype}\r\n\r\n').encode()
        body += content + b"\r\n"
    body += f"--{boundary}--\r\n".encode()
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(c.jar), _NoRedirect)
    req = urllib.request.Request(urllib.parse.urljoin(BASE, action), data=bytes(body),
                                 headers={"Content-Type": f"multipart/form-data; boundary={boundary}"})
    status, location, text = 0, None, ""
    try:
        r = opener.open(req, timeout=180)
        status, text = r.status, r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        status, location = e.code, e.headers.get("Location")
        text = e.read().decode("utf-8", "replace")
    if status in (301, 302, 303) and location and follow:
        _, text = c.get(location)
    res = HttpResult(status, location, text=text, name=name)
    if res.text:
        try:
            res.errors, res.toasts = parse_messages(res.text)
        except Exception:
            pass
    note(f"POST(multipart) {action[:90]} [{name}]", res.status, res.text, res.errors, res.toasts, res.location)
    return res


# ---------------------------------------------------------------- utilidades varias
def toast_text(res, kind=None):
    return " | ".join(t for k, t in res.toasts if kind is None or k == kind)


def has_link(html, fragment):
    return fragment in html


def listing_has_id(c, listing, term, item_id, kind="Edit", extra=""):
    """True si el listado filtrado por SearchTerm=term muestra el enlace /{kind}/{id}."""
    q = urllib.parse.urlencode({"SearchTerm": term})
    _, page = c.get(f"{listing}?{q}{extra}")
    return bool(re.search(rf'href="[^"]*/{kind}/{item_id}["?#]', page)), page


def finish(R, module):
    """Imprime la tabla de resultados, anomalías y escribe el archivo en outputs/qa-usuario."""
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        fails = e2e_lib.run_all([lambda: R])
    out = buf.getvalue()
    anomalies = list(dict.fromkeys(ANOMALIES))
    out += f"-- ANOMALIAS ({len(anomalies)}): 500 / páginas de error / texto en inglés detectados en las respuestas\n"
    for a in anomalies:
        out += f"   * {a}\n"
    print(out)
    try:
        OUT_DIR.mkdir(parents=True, exist_ok=True)
        (OUT_DIR / f"http_cases_catalogos_{module}.txt").write_text(out, encoding="utf-8")
    except Exception as exc:  # no impide imprimir
        print("No se pudo escribir el resultado:", exc)
    return fails


def run_module(module, func):
    """Ejecuta func(R, c) capturando excepciones inesperadas como una comprobación fallida."""
    R = []
    c = client()
    try:
        func(R, c)
    except Exception as exc:  # una excepción no debe ocultar lo ya comprobado
        import traceback
        traceback.print_exc()
        R.append(e2e_lib.Result(f"EXCEPCIÓN de la prueba en {module}", "sin excepción", f"{type(exc).__name__}: {exc}"))
    sys.exit(1 if finish(R, module) else 0)
