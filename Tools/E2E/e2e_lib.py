"""
Biblioteca de la prueba de punta a punta (solo lectura) contra la app local y la BD REAL.

- La app debe estar corriendo (la levanta Claude): http://localhost:5000
- La contraseña viene de la variable de entorno E2E_PW (nunca se escribe en archivos). Usuario: E2E_USER (por defecto 'nihaht').
- Cada check devuelve un Result(nombre, esperado, observado, detalle); `run_all` imprime la tabla y devuelve el número de fallas.
"""
import html as _html
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import http.cookiejar
from dataclasses import dataclass

import pyodbc

BASE = os.environ.get("E2E_BASE", "http://localhost:5000")
DB_NAME = os.environ.get("E2E_DB", "DB_Laboratorios_Univalle_REAL")  # E2E_DB permite apuntar a una copia desechable
CONN = (f"Driver={{ODBC Driver 17 for SQL Server}};Server=localhost;Database={DB_NAME};"
        "Trusted_Connection=yes;TrustServerCertificate=yes")
ERROR_MARKERS = re.compile(r"NullReferenceException|InvalidOperationException|SqlException|An unhandled exception|Stack trace", re.I)


@dataclass
class Result:
    name: str
    expected: object
    observed: object
    detail: str = ""

    @property
    def ok(self):
        return self.expected == self.observed


class Session:
    def __init__(self):
        self.jar = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar))
        self.login()

    def get(self, path):
        """GET -> (status, html). Nunca lanza por códigos HTTP."""
        url = path if path.startswith("http") else BASE + path
        try:
            r = self.opener.open(url, timeout=180)
            return r.status, r.read().decode("utf-8", "replace")
        except urllib.error.HTTPError as e:
            return e.code, e.read().decode("utf-8", "replace")

    def login(self):
        pw = os.environ.get("E2E_PW")
        if not pw:
            sys.exit("Falta la variable de entorno E2E_PW")
        _, html = self.get("/Login")
        token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', html).group(1)
        data = urllib.parse.urlencode({"__RequestVerificationToken": token,
                                       "Input.UserName": os.environ.get("E2E_USER", "nihaht"),
                                       "Input.Password": pw}).encode()
        self.opener.open(urllib.request.Request(BASE + "/Login", data=data), timeout=60)
        status, home = self.get("/")
        if status != 200 or not re.search(r"Cerrar ses(?:i|ió)n", home, re.I):
            sys.exit("No se pudo iniciar sesión")

    def text(self, path):
        """Texto visible del <body> (sin scripts/estilos/etiquetas), espacios colapsados."""
        status, html = self.get(path)
        body = re.search(r"<body.*", html, re.S)
        body = body.group(0) if body else html
        body = re.sub(r"<script.*?</script>|<style.*?</style>|<[^>]+>", " ", body, flags=re.S)
        return status, re.sub(r"\s+", " ", _html.unescape(body))

    def healthy(self, path):
        """True si responde 200 y no muestra una excepción."""
        status, html = self.get(path)
        return status == 200 and not ERROR_MARKERS.search(re.sub(r"<script.*?</script>", "", html, flags=re.S))

    def collect(self, path, link_regex, page_param="pageIndex", max_pages=200):
        """Recorre todas las páginas de un listado y devuelve el conjunto de coincidencias únicas de link_regex (grupo 1 o 0)."""
        found, sep = set(), ("&" if "?" in path else "?")
        for page in range(1, max_pages + 1):
            status, html = self.get(f"{path}{sep}{page_param}={page}")
            if status != 200:
                break
            new = {m.group(1) if m.groups() else m.group(0) for m in re.finditer(link_regex, html)}
            if not new or new <= found:
                break
            found |= new
        return found


def db_scalar(sql, *params):
    with pyodbc.connect(CONN) as cn:
        return cn.cursor().execute(sql, *params).fetchval()


def db_rows(sql, *params):
    with pyodbc.connect(CONN) as cn:
        return cn.cursor().execute(sql, *params).fetchall()


def run_all(checks):
    """checks: lista de callables sin argumentos que devuelven Result o lista de Result."""
    results = []
    for check in checks:
        try:
            out = check()
        except Exception as exc:  # una falla de un check no detiene los demás
            out = Result(getattr(check, "__name__", "check"), "sin excepción", f"{type(exc).__name__}: {exc}")
        results.extend(out if isinstance(out, list) else [out])
    fails = 0
    for r in results:
        mark = "OK   " if r.ok else "FALLA"
        fails += 0 if r.ok else 1
        print(f"[{mark}] {r.name}: esperado={r.expected} observado={r.observed} {r.detail}".rstrip())
    print(f"== {len(results)} comprobaciones, {fails} fallas")
    return fails
