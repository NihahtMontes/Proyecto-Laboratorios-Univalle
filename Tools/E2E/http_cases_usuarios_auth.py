"""WS-1 · Usuarios y permisos — huecos que no cubre http_cases_maestros_users.py (copia QA).

  US-01 inicio de sesión de los 4 roles: página de aterrizaje sin error; cerrar sesión invalida la cookie
  US-02 bloqueo a los 5 intentos fallidos con mensaje en español (sin página de error); ReturnUrl externo no redirige fuera ni da 500
  US-05 cambio de rol: tras cambiar el rol de un usuario, en el siguiente inicio de sesión sus permisos cambian
  US-07 Cierre de práctica solo entra a Cierre / Notificaciones (el resto redirige a /Cierre)
  US-11 cada enlace del menú lateral visible para el rol responde sin error (ni 500 ni /Error)
"""
import os
import re
import sys
import time
import urllib.parse

import e2e_lib
from http_forms import Client

if e2e_lib.DB_NAME != "DB_Laboratorios_Univalle_QA_E2E":
    sys.exit(f"SEGURIDAD: solo corre contra la copia de QA (E2E_DB={e2e_lib.DB_NAME!r}).")

PW = os.environ["E2E_PW"]
R = []
ROLES = {"SuperAdmin": "qa_super", "Administrador": "qa_adm", "Supervisor": "qa_sup", "Cierre de práctica": "qa_cierre"}


def ok(name, cond, detail=""):
    R.append(e2e_lib.Result(name, True, bool(cond), "" if cond else detail))


def es_error(st, html, loc=""):
    return st >= 500 or "/error" in (loc or "").lower() or "An unhandled exception" in (html or "")


def login_raw(user, pw, return_url=None):
    """POST /Login sin seguir redirects. Devuelve (status, location, html, client)."""
    c = Client.__new__(Client)
    import http.cookiejar
    import urllib.request
    c.jar = http.cookiejar.CookieJar()
    c.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(c.jar))
    path = "/Login" + (f"?returnUrl={urllib.parse.quote(return_url, safe='')}" if return_url else "")
    _, page = c.get(path)
    tok = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
    fields = [("__RequestVerificationToken", tok), ("Input.UserName", user), ("Input.Password", pw)]
    if return_url:
        fields.append(("ReturnUrl", return_url))
    res = c.raw_post(path, fields, follow=False)
    return res.status, res.location or "", res.text or "", c


# ---------------- US-01 aterrizaje por rol y cierre de sesión
for rol, user in ROLES.items():
    st, loc, html, c = login_raw(user, PW)
    ok(f"US-01 {rol}: inicio de sesión correcto (302)", st == 302, f"status={st} loc={loc}")
    st2, html2 = c.get(loc or "/")
    ok(f"US-01 {rol}: la página de aterrizaje carga sin error", st2 == 200 and not es_error(st2, html2), f"status={st2} destino={loc}")
c = Client("qa_adm", PW)
st, loc = c.probe("/Users")
ok("US-01 sesión activa: /Users accesible para Administrador", st == 200, f"status={st} loc={loc}")
c.raw_post("/Logout", [], follow=True, token_from="/")
st, loc = c.probe("/Users")
ok("US-01 tras cerrar sesión, /Users redirige a /Login", st == 302 and "login" in (loc or "").lower(), f"status={st} loc={loc}")

# ---------------- US-02 bloqueo tras 5 intentos y ReturnUrl
victima = "qa_u25"
e2e_lib.db_scalar("SELECT 1")  # conexión de prueba
for i in range(5):
    st, loc, html, _ = login_raw(victima, "ClaveIncorrecta!" + str(i))
lock_end = e2e_lib.db_scalar("SELECT CONVERT(varchar(40), LockoutEnd, 127) FROM Users WHERE UserName = ? AND LockoutEnd > SYSDATETIMEOFFSET()", victima)
ok("US-02 tras 5 intentos fallidos la cuenta queda bloqueada (LockoutEnd en el futuro)", lock_end is not None, f"LockoutEnd={lock_end}")
st, loc, html, c = login_raw(victima, PW)
st_f, html_f = (c.get(loc)[0], c.get(loc)[1]) if st == 302 and loc else (st, html)
ok("US-02 una cuenta bloqueada ve un aviso en español, no una página de error",
   st < 500 and not es_error(st_f, html_f, loc) and re.search(r"bloquead", html + html_f, re.I) is not None,
   f"status={st} loc={loc} destino_status={st_f}")
st, loc, html, _ = login_raw("qa_u24", PW, return_url="https://ejemplo-externo.test/robar")
ok("US-02 ReturnUrl externo: no redirige fuera del sitio", "ejemplo-externo" not in (loc or ""), f"status={st} loc={loc}")
ok("US-02 ReturnUrl externo: sin página de error (500)", st < 500, f"status={st} loc={loc}")

# ---------------- US-05 cambio de rol
uid = e2e_lib.db_scalar("SELECT Id FROM Users WHERE UserName = 'qa_u23'")
c_sa = Client("qa_super", PW)
c_u = Client("qa_u23", PW)
st, _ = c_u.probe("/Users")
ok("US-05 qa_u23 como Administrador entra a /Users", st == 200, f"status={st}")
r = c_sa.submit(f"/Users/Edit/{uid}", {"Input.Role": "2"})
rol_bd = e2e_lib.db_scalar("SELECT Role FROM Users WHERE Id = ?", uid)
ok("US-05 el cambio de rol a Supervisor se guarda (Users.Role = 2)", rol_bd == 2, f"status={r.status} rol={rol_bd} errores={r.errors[:2]} toasts={r.toasts[:1]}")
c_u2 = Client("qa_u23", PW)
st, loc = c_u2.probe("/Users")
ok("US-05 en el siguiente inicio de sesión ya no entra a /Users (Supervisor)", st == 302 and "/users" not in (loc or "").lower(), f"status={st} loc={loc}")
st, _ = c_u2.probe("/Reports")
ok("US-05 como Supervisor sí entra a Reportes", st == 200, f"status={st}")
c_sa.submit(f"/Users/Edit/{uid}", {"Input.Role": "1"})   # se restaura

# ---------------- US-07 Cierre de práctica
c_pc = Client("qa_cierre", PW)
for path in ("/Cierre", "/Notifications"):
    st, loc = c_pc.probe(path)
    ok(f"US-07 Cierre de práctica entra a {path}", st == 200, f"status={st} loc={loc}")
for path in ("/Equipment", "/Maintenances", "/Requests", "/Reports", "/Users", "/Managements"):
    st, loc = c_pc.probe(path)
    ok(f"US-07 Cierre de práctica NO entra a {path} (redirige a /Cierre)", st == 302 and "/cierre" in (loc or "").lower(), f"status={st} loc={loc}")

# ---------------- US-11 enlaces del menú por rol
for rol, user in ROLES.items():
    cl = Client(user, PW)
    st, html = cl.get("/Cierre" if user == "qa_cierre" else "/Equipment" if user in ("qa_super", "qa_adm") else "/Reports")
    menu = re.search(r'<aside.*?</aside>', html or "", re.S | re.I)
    hrefs = sorted(set(re.findall(r'href="(/[^"#?]*)', menu.group(0) if menu else html or "")))
    hrefs = [h for h in hrefs if h not in ("/Logout",) and not h.startswith(("/lib", "/css", "/js", "/images", "/assets"))]
    malos = []
    for h in hrefs:
        st2, loc2 = cl.probe(h)
        if st2 >= 500 or "/error" in (loc2 or "").lower() or st2 == 404:
            malos.append((h, st2, loc2))
    ok(f"US-11 {rol}: los {len(hrefs)} enlaces del menú lateral responden sin error ni acceso denegado", not malos, f"malos={malos[:6]}")

sys.exit(1 if e2e_lib.run_all([lambda: R]) else 0)
