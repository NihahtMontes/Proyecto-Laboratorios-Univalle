"""
Motor de pruebas de formularios por HTTP (con escritura) contra la copia desechable de QA.

Hace lo que hace el navegador pero sin JavaScript: GET de la página, toma el formulario, rellena campos, POST con el token antifalsificación
y lee el resultado (errores de validación, mensajes TempData que el layout convierte en toast/modal y el destino del redirect).
Sirve para probar a escala las reglas del SERVIDOR (lo que el JS del navegador impide escribir) en cada módulo.

Seguridad: se niega a ejecutar si la base de datos objetivo no es DB_Laboratorios_Univalle_QA_E2E (variable E2E_DB).
Uso:  E2E_PW=... E2E_USER=qa_super E2E_DB=DB_Laboratorios_Univalle_QA_E2E python Tools/E2E/http_cases_<modulo>.py
"""
import json
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass, field

from lxml import html as LH

import e2e_lib
from e2e_lib import BASE, DB_NAME, Session

if DB_NAME != "DB_Laboratorios_Univalle_QA_E2E":
    sys.exit(f"SEGURIDAD: http_forms solo corre contra la copia de QA (E2E_DB={DB_NAME!r}).")


class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k):
        return None


_TOAST = re.compile(r"(Toast|Swal)\.fire\(\{\s*icon:\s*'(\w+)',\s*type:\s*'\w+',\s*(?:title:\s*(?:escapeHtml\()?(\"(?:\\.|[^\"\\])*\"|'[^']*')\)?,?\s*)?(?:text:\s*(\"(?:\\.|[^\"\\])*\"|'[^']*'),?\s*)?", re.S)


@dataclass
class Result:
    status: int
    location: str | None = None
    errors: list = field(default_factory=list)     # mensajes de validación junto a los campos y resumen
    toasts: list = field(default_factory=list)     # (tipo, texto) de TempData mostrados por el layout tras el redirect
    text: str = ""                                 # HTML de la respuesta final
    name: str = ""

    def has_error(self, fragment):
        return any(fragment.lower() in e.lower() for e in self.errors)

    def has_toast(self, fragment, kind=None):
        return any(fragment.lower() in t.lower() and (kind is None or kind == k) for k, t in self.toasts)


def _json_str(token):
    if token is None:
        return ""
    return json.loads(token) if token.startswith('"') else token.strip("'")


def parse_messages(html_text):
    doc = LH.fromstring(html_text)
    errors = [re.sub(r"\s+", " ", e.text_content()).strip() for e in doc.xpath("//*[contains(@class,'field-validation-error')]")]
    errors += [re.sub(r"\s+", " ", li.text_content()).strip() for li in doc.xpath("//*[contains(@class,'validation-summary-errors')]//li")]
    errors += [re.sub(r"\s+", " ", li.text_content()).strip() for li in doc.xpath("//*[@data-valmsg-summary]//li")]
    errors = [e for e in dict.fromkeys(errors) if e and e != "*"]
    toasts = []
    for script in doc.xpath("//script[not(@src)]"):
        for m in _TOAST.finditer(script.text or ""):
            text = _json_str(m.group(4)) or _json_str(m.group(3))
            if text and text not in ("¡Ups!", "Atención"):
                toasts.append((m.group(2), text))
            elif m.group(4):
                toasts.append((m.group(2), _json_str(m.group(4))))
    return errors, toasts


class Client(Session):
    """Sesión autenticada que además puede leer formularios y enviarlos sin seguir redirects."""

    def __init__(self, user=None, password=None):
        super().__init__() if user is None else self._login_as(user, password)

    def _login_as(self, user, password):
        import http.cookiejar
        self.jar = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar))
        status, page = self.get("/Login")
        token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
        data = urllib.parse.urlencode({"__RequestVerificationToken": token, "Input.UserName": user, "Input.Password": password}).encode()
        self.opener.open(urllib.request.Request(BASE + "/Login", data=data), timeout=60)

    # ---- lectura de formularios ----
    def form(self, path, action_contains=None, index=0):
        """GET de `path` y devuelve (action_url, campos[(nombre, valor)]) del formulario POST pedido (por defecto el primero que no es Logout)."""
        status, page = self.get(path)
        if status != 200:
            raise RuntimeError(f"GET {path} -> {status}")
        doc = LH.fromstring(page)
        forms = [f for f in doc.xpath("//form[translate(@method,'POST','post')='post']")
                 if not (f.get("action") or "").endswith("/Logout") and f.xpath(".//input[@name='__RequestVerificationToken']")]
        if action_contains:
            forms = [f for f in forms if action_contains in (f.get("action") or "")]
        if not forms:
            raise RuntimeError(f"Sin formulario POST en {path} ({action_contains or 'cualquiera'})")
        f = forms[index]
        action = f.get("action") or path
        action = urllib.parse.urljoin(BASE + path, action)
        fields = []
        for el in f.xpath(".//input|.//select|.//textarea"):
            name = el.get("name")
            if not name or el.get("disabled") is not None:
                continue
            tag = el.tag
            if tag == "input":
                typ = (el.get("type") or "text").lower()
                if typ in ("submit", "button", "image", "file", "reset"):
                    continue
                if typ in ("checkbox", "radio"):
                    if el.get("checked") is not None:
                        fields.append((name, el.get("value") or "on"))
                    continue
                fields.append((name, el.get("value") or ""))
            elif tag == "textarea":
                fields.append((name, el.text_content()))
            elif tag == "select":
                opts = el.xpath(".//option")
                chosen = [o for o in opts if o.get("selected") is not None] or opts[:1]
                for o in chosen:
                    fields.append((name, o.get("value") if o.get("value") is not None else o.text_content().strip()))
        return action, fields

    def probe(self, path, method="GET"):
        """Petición sin seguir redirects: devuelve (status, Location). Útil para matrices de roles."""
        opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar), _NoRedirect)
        req = urllib.request.Request(BASE + path, method=method, data=(b"" if method == "POST" else None))
        try:
            r = opener.open(req, timeout=180)
            r.read()
            return r.status, None
        except urllib.error.HTTPError as e:
            return e.code, e.headers.get("Location")

    # ---- envío ----
    def submit(self, path, overrides=None, action_contains=None, index=0, follow=True, name=""):
        """Rellena y envía el formulario. overrides: {campo: valor | None (quitar) | [valores]}."""
        action, fields = self.form(path, action_contains, index)
        overrides = overrides or {}
        out, seen = [], set()
        for k, v in fields:
            if k in overrides:
                if k in seen:
                    continue
                seen.add(k)
                val = overrides[k]
                if val is None:
                    continue
                for item in (val if isinstance(val, list) else [val]):
                    out.append((k, str(item)))
            else:
                out.append((k, v))
        for k, val in overrides.items():
            if k not in seen and not any(k == f[0] for f in fields) and val is not None:
                for item in (val if isinstance(val, list) else [val]):
                    out.append((k, str(item)))
        return self._post(action, out, follow, name)

    def raw_post(self, action, fields, follow=True, name="", token_from=None):
        """POST de campos arbitrarios (para forzar valores que el HTML impediría). token_from: ruta de una página con token."""
        if token_from:
            _, page = self.get(token_from)
            tok = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
            fields = [("__RequestVerificationToken", tok)] + list(fields)
        return self._post(urllib.parse.urljoin(BASE, action), list(fields), follow, name)

    def _post(self, action, fields, follow, name):
        body = urllib.parse.urlencode(fields).encode("utf-8")
        opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar), _NoRedirect)
        req = urllib.request.Request(action, data=body, headers={"Content-Type": "application/x-www-form-urlencoded"})
        status, location, text = 0, None, ""
        try:
            r = opener.open(req, timeout=180)
            status, text = r.status, r.read().decode("utf-8", "replace")
        except urllib.error.HTTPError as e:
            status, location = e.code, e.headers.get("Location")
            text = e.read().decode("utf-8", "replace")
        if status in (301, 302, 303) and location and follow:
            st2, text = self.get(location if location.startswith("http") else location)
            res = Result(status, location, text=text, name=name)
        else:
            res = Result(status, location, text=text, name=name)
        if res.text:
            try:
                res.errors, res.toasts = parse_messages(res.text)
            except Exception:  # cuerpo no HTML (JSON, vacío)
                pass
        return res


def check(results, name, ok, detail=""):
    results.append(e2e_lib.Result(name, True, bool(ok), detail))


def expect(results, name, res, *, status=None, redirect=None, error=None, toast=None, toast_kind=None, no_error=None):
    """Registra una comprobación. error/toast: fragmento esperado (insensible a mayúsculas)."""
    problems = []
    if status is not None and res.status != status:
        problems.append(f"status={res.status} (esperado {status})")
    if redirect is not None and not ((res.location or "").split("?")[0].rstrip("/").lower() == redirect.rstrip("/").lower()):
        problems.append(f"redirect={res.location!r} (esperado {redirect!r})")
    if error is not None and not res.has_error(error):
        problems.append(f"error '{error}' no aparece; errores={res.errors[:3]}")
    if toast is not None and not res.has_toast(toast, toast_kind):
        problems.append(f"mensaje '{toast}' no aparece; toasts={res.toasts[:3]}")
    if no_error is not None and res.has_error(no_error):
        problems.append(f"no debía aparecer '{no_error}'")
    results.append(e2e_lib.Result(name, "OK", "OK" if not problems else "; ".join(problems)))
    return not problems
