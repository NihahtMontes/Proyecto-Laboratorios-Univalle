"""Usuarios: Create / Edit / Delete / Details + reglas especiales (autobaja, SuperAdmin, contraseñas, fotos) + 404 + sin sesión + sin token. Solo contra QA.
Crea sus propias cuentas (admin, supervisor, cierre, superadmin 2, objetivos) con contraseña conocida; NO modifica ni elimina qa_super ni nihaht."""
import re
import urllib.error
import urllib.parse
import urllib.request

import http_cases_maestros_common as K
import http_forms
from http_cases_maestros_common import R, sub, post, check, expect

c = K.client()
tag = K.rnd(7)
NOTES = []
PW = f"Qa!{tag}9Z"          # >= 12, mayúscula, minúscula, dígito, símbolo, >= 4 únicos
PW2 = f"Nw!{tag}8Y"
N = [0]


def defaults(**ov):
    N[0] += 1
    n = N[0]
    d = {"Input.UserName": f"qa{tag}{n}", "Input.Email": f"qa.{tag}.{n}@example.test", "Input.FirstName": "Prueba", "Input.LastName": f"Usuario{n}",
         "Input.SecondLastName": "", "Input.IdentityCard": "Q" + K.digits(8), "Input.PhoneNumber": "70012345", "Input.Role": "1", "Input.Password": PW,
         "Input.Position": "Tester", "Input.Department": "QA"}
    for k, v in ov.items():
        d["Input." + k] = v
    return d


def ucreate(label, **ov):
    return sub(c, label, "/Users/Create", defaults(**ov))


def uid(username):
    return K.dbv("select top 1 Id from Users where UserName=? order by Id desc", username.lower())


def ustatus(i):
    return K.dbv("select Status from Users where Id=?", i)


def urole(i):
    return K.dbv("select Role from Users where Id=?", i)


def full(i):
    r = K.db("select FirstName,LastName,SecondLastName from Users where Id=?", i)[0]
    return f"{r[0]} {r[1]}" + (f" {r[2]}" if r[2] else "")


def make(label, role="1", **ov):
    d = defaults(Role=role, **ov)
    res = sub(c, label, "/Users/Create", d)
    return res, d["Input.UserName"], d["Input.Email"], d["Input.IdentityCard"], uid(d["Input.UserName"])


def can_login(user, pw):
    cl = K.client(user, pw)
    return cl, K.logged_in(cl)


def nr_get(cl, path):
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cl.jar), K._NR)
    try:
        r = op.open(K.BASE + path, timeout=60)
        return r.status, r.headers.get("Location")
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Location")


def mp_post(cl, label, action, fields, files, token_from):
    """POST multipart/form-data con archivo (para probar la foto de perfil). Solo se envían archivos que el servidor RECHAZA."""
    _, page = cl.get(token_from)
    tok = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
    b = "----qa" + K.rnd(14)
    body = b""
    for k, v in [("__RequestVerificationToken", tok)] + list(fields):
        body += f'--{b}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode("utf-8")
    for name, fname, ctype, data in files:
        body += f'--{b}\r\nContent-Disposition: form-data; name="{name}"; filename="{fname}"\r\nContent-Type: {ctype}\r\n\r\n'.encode() + data + b"\r\n"
    body += f"--{b}--\r\n".encode()
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cl.jar), K._NR)
    req = urllib.request.Request(K.BASE + action, data=body, headers={"Content-Type": f"multipart/form-data; boundary={b}"})
    status, loc, text = 0, None, ""
    try:
        r = op.open(req, timeout=120)
        status, text = r.status, r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        status, loc, text = e.code, e.headers.get("Location"), e.read().decode("utf-8", "replace")
    res = http_forms.Result(status, loc, text=text, name=label)
    try:
        res.errors, res.toasts = http_forms.parse_messages(text)
    except Exception:
        pass
    return K.track(label, res)


# =====================================================================================
# Pantallas
# =====================================================================================
st, pg = c.get("/Users")
check(R, "Usuarios/Index: 200 'Control de Usuarios'", st == 200 and "Control de Usuarios" in pg, f"status={st}")
st, pg = c.get("/Users/Create")
check(R, "Usuarios/Create GET: 200", st == 200, f"status={st}")

# =====================================================================================
# Create: campos obligatorios vacíos
# =====================================================================================
for campo, msg in (("UserName", "El nombre de usuario es obligatorio"), ("Email", "El correo institucional es obligatorio"), ("FirstName", "Los nombres son obligatorios"),
                   ("LastName", "El primer apellido es obligatorio"), ("IdentityCard", "El documento de identidad es obligatorio"), ("Role", "obligatorio"),
                   ("PhoneNumber", "El teléfono es obligatorio"), ("Password", "La contraseña es obligatoria")):
    expect(R, f"Usuarios/Create: {campo} vacío", ucreate(f"vacío {campo}", **{campo: ""}), status=200, error=msg)
expect(R, "Usuarios/Create: nombres solo espacios", ucreate("nombres espacios", FirstName="    "), status=200, error="Los nombres son obligatorios")

# formatos
expect(R, "Usuarios/Create: cédula en minúsculas 'abc123' (el regex solo admite mayúsculas)", ucreate("ci minúsculas", IdentityCard="abc123"), status=200, error="Formato de cédula inválido")
expect(R, "Usuarios/Create: cédula con espacio y símbolo '12 3!'", ucreate("ci símbolo", IdentityCard="12 3!"), status=200, error="Formato de cédula inválido")
res = ucreate("ci 11", IdentityCard="Q1234567890")
check(R, "Usuarios/Create: cédula de 11 caracteres -> 200 con error (mensaje por defecto)", res.status == 200 and res.has_error("10"), f"status={res.status} errores={res.errors[:2]}")
expect(R, "Usuarios/Create: correo inválido", ucreate("email inválido", Email="no-es-correo"), status=200, error="Correo electrónico inválido")
expect(R, "Usuarios/Create: teléfono inválido", ucreate("tel inválido", PhoneNumber="abc"), status=200, error="Formato de teléfono inválido")
expect(R, "Usuarios/Create: rol inválido 7", ucreate("rol 7", Role="7"), status=200, error="no es válido")
res = ucreate("rol abc", Role="abc")
check(R, "Usuarios/Create: rol no numérico -> 200 con mensaje", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")

# longitudes
for campo, largo in (("UserName", 257), ("FirstName", 101), ("LastName", 101), ("SecondLastName", 101), ("Position", 101), ("Department", 101)):
    res = ucreate(f"largo {campo}", **{campo: "x" * largo})
    check(R, f"Usuarios/Create: {campo} de {largo} caracteres -> 200 con error (mensaje por defecto)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
res = ucreate("largo Email", Email="a" * 250 + "@example.test")
check(R, "Usuarios/Create: correo de 263 caracteres -> 200 con error", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")

# contraseñas
expect(R, "Usuarios/Create: contraseña de 4 caracteres", ucreate("pw corta", Password="Ab1!"), status=200, error="La contraseña debe tener al menos 12 caracteres")
res = ucreate("pw 101", Password="Aa1!" * 26)
check(R, "Usuarios/Create: contraseña de 104 caracteres -> 200 con error", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
NOTES.append(f"Usuarios/Create: una contraseña de 104 caracteres muestra: {res.errors[:1]} (el mensaje de 'mínimo 8' aparece también para exceso de largo)." if res.errors else "Usuarios/Create: contraseña larga sin mensaje visible.")
res = ucreate("pw 8 chars", Password="Abcdef1!")
check(R, "Usuarios/Create: contraseña de 8 caracteres válida para el atributo pero Identity exige 12 -> error del servidor en español", res.status == 200 and bool(res.errors) and not any("must" in e or "Passwords" in e for e in res.errors),
      f"status={res.status} errores={res.errors[:3]} (la ayuda del formulario dice 8 caracteres)")
for etiqueta, pw in (("sin mayúscula", "qa!abcdefg9z"), ("sin minúscula", "QA!ABCDEFG9Z"), ("sin dígito", "Qa!abcdefghZ"), ("sin símbolo", "Qaabcdefg9Zx")):
    res = ucreate(f"pw {etiqueta}", Password=pw)
    check(R, f"Usuarios/Create: contraseña {etiqueta} se rechaza con mensaje en español", res.status == 200 and bool(res.errors) and not any(re.search(r"\b(Passwords?|must|have at least)\b", e) for e in res.errors), f"status={res.status} errores={res.errors[:3]}")

# =====================================================================================
# Create: válidos
# =====================================================================================
res, u_adm, e_adm, ci_adm, id_adm = make("crear admin", "1")
expect(R, "Usuarios/Create: administrador válido -> /Users", res, status=302, redirect="/Users")
check(R, "Usuarios/Create: toast 'Cuenta de usuario para '<nombre>' creada exitosamente.'", res.has_toast(f"Cuenta de usuario para '{full(id_adm)}' creada exitosamente.", "success"), f"toasts={res.toasts[:1]}")
check(R, "Usuarios/Create: guardado Activo, rol 1, correo y usuario en minúsculas, fecha de alta puesta, correo confirmado",
      K.dbv("select count(*) from Users where Id=? and Status=0 and Role=1 and UserName=? and Email=? and HireDate is not null and EmailConfirmed=1", id_adm, u_adm.lower(), e_adm.lower()) == 1, f"id={id_adm}")
adm, ok = can_login(u_adm, PW)
check(R, "Usuarios/Create: el administrador creado puede iniciar sesión con la contraseña indicada", ok, "")
check(R, "Usuarios/Index: el usuario creado aparece en el listado (búsqueda por usuario)", f"/Users/Details/{id_adm}" in c.get(f"/Users?SearchTerm={u_adm}")[1], "")

res, u_sup, e_sup, ci_sup, id_sup = make("crear supervisor", "2")
res2, u_pc, e_pc, ci_pc, id_pc = make("crear cierre", "3")
res3, u_sa2, e_sa2, ci_sa2, id_sa2 = make("crear superadmin 2", "99")
expect(R, "Usuarios/Create: supervisor válido", res, status=302, redirect="/Users")
expect(R, "Usuarios/Create: encargado de cierre válido", res2, status=302, redirect="/Users")
expect(R, "Usuarios/Create: superadministrador válido creado por un superadministrador", res3, status=302, redirect="/Users")
check(R, "Usuarios/Create: roles guardados 2, 3 y 99", urole(id_sup) == 2 and urole(id_pc) == 3 and urole(id_sa2) == 99, f"roles={urole(id_sup)},{urole(id_pc)},{urole(id_sa2)}")
sa2, ok_sa2 = can_login(u_sa2, PW)
check(R, "Usuarios/Create: el superadministrador creado puede iniciar sesión", ok_sa2, "")
sup, ok_sup = can_login(u_sup, PW)
pcu, ok_pc = can_login(u_pc, PW)
check(R, "Usuarios/Create: supervisor y encargado de cierre pueden iniciar sesión", ok_sup and ok_pc, f"sup={ok_sup} pc={ok_pc}")

# caracteres especiales
for etiqueta, kw in (("html", {"FirstName": f"<b>QAu{tag}</b>", "Position": "<script>x</script>"}), ("comillas", {"FirstName": "O'Brien \"Q\""}), ("tildes", {"FirstName": "José Ñandú", "LastName": "Pérez Muñoz"})):
    r = ucreate(f"crear {etiqueta}", **kw)
    check(R, f"Usuarios/Create: caracteres especiales ({etiqueta}) sin excepción", r.status in (200, 302), f"status={r.status} errores={r.errors[:1]}")
st, lst = c.get("/Users?SearchTerm=qa" + tag)
check(R, "Usuarios/Index: el HTML hostil se muestra escapado", st == 200 and f"<b>QAu{tag}</b>" not in lst and "<script>x</script>" not in lst, f"status={st}")

# espacios normalizados
r = ucreate("espacios", FirstName="  Ana   Maria  ", LastName="  Lopez   Soto ")
check(R, "Usuarios/Create: nombres con espacios se normalizan", K.dbv("select count(*) from Users where FirstName='Ana Maria' and LastName='Lopez Soto'") >= 1, f"status={r.status}")

# fechas (HireDate no está en el formulario HTML pero el servidor la acepta si llega)
r = ucreate("hire inválida", HireDate="no-es-fecha")
check(R, "Usuarios/Create: fecha de contratación inválida 'no-es-fecha' -> 200 con mensaje", r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
r, u_h, e_h, ci_h, id_h2 = make("crear con fecha", "1", HireDate="2020-01-31")
check(R, "Usuarios/Create: fecha de contratación válida 2020-01-31 se guarda", r.status == 302 and str(K.dbv("select HireDate from Users where Id=?", id_h2))[:10] == "2020-01-31", f"status={r.status} HireDate={K.dbv('select HireDate from Users where Id=?', id_h2)}")
r, u_h3, e_h3, ci_h3, id_h3 = make("crear con fecha futura", "1", HireDate="2999-12-31")
NOTES.append(f"Usuarios/Create: una fecha de contratación en el año 2999 se {'acepta' if r.status == 302 else 'rechaza'} (status={r.status}); no hay validación de rango.")
r = ucreate("hire año 0001", HireDate="0001-01-01")
check(R, "Usuarios/Create: fecha 0001-01-01 no provoca excepción (200 o 302)", r.status in (200, 302), f"status={r.status} errores={r.errors[:1]}")

# duplicados
expect(R, "Usuarios/Create: documento de identidad duplicado (9900001)", ucreate("ci dup", IdentityCard="9900001"), status=200, error="Este documento de identidad ya está vinculado a otra cuenta.")
expect(R, "Usuarios/Create: usuario duplicado 'QA_SUPER ' (otra capitalización y espacios)", ucreate("user dup", UserName=" QA_SUPER "), status=200, error="El nombre de usuario ya está en uso.")
expect(R, "Usuarios/Create: correo duplicado 'QA_SUPER@QA.LOCAL'", ucreate("email dup", Email="QA_SUPER@QA.LOCAL"), status=200, error="Este correo electrónico ya se encuentra registrado.")
expect(R, "Usuarios/Create: usuario duplicado de la cuenta creada", ucreate("user dup 2", UserName=u_adm.upper()), status=200, error="El nombre de usuario ya está en uso.")

# fotos inválidas (multipart). Solo archivos que el servidor rechaza: no se escribe nada en wwwroot.
d = defaults()
fields = [(k, v) for k, v in d.items()]
for etiqueta, fname, ctype, data, msg in (
        ("vacía", "foto.png", "image/png", b"", "El archivo de imagen está vacío."),
        ("más de 5 MB", "foto.png", "image/png", b"\x89PNG\r\n\x1a\n" + b"0" * (5 * 1024 * 1024 + 1), "La imagen no puede superar los 5 MB."),
        ("extensión .txt", "foto.txt", "text/plain", b"hola", "La imagen debe ser JPG, PNG o WEBP."),
        ("extensión .php", "evil.php", "image/png", b"<?php ?>", "La imagen debe ser JPG, PNG o WEBP."),
        ("tipo de contenido que no coincide", "foto.png", "text/plain", b"\x89PNG\r\n\x1a\n0000", "El tipo de contenido del archivo no coincide con una imagen permitida."),
        ("contenido que no es imagen", "foto.png", "image/png", b"esto no es un png de verdad", "El contenido del archivo no corresponde a una imagen válida."),
):
    r = mp_post(c, f"foto {etiqueta}", "/Users/Create", fields, [("Input.ProfilePictureUpload", fname, ctype, data)], "/Users/Create")
    check(R, f"Usuarios/Create: foto {etiqueta} -> '{msg}'", r.status == 200 and r.has_error(msg), f"status={r.status} errores={r.errors[:2]}")
check(R, "Usuarios/Create: ninguna foto inválida creó la cuenta", K.dbv("select count(*) from Users where UserName=?", d["Input.UserName"].lower()) == 0, "")

# =====================================================================================
# Rol SuperAdmin asignado por un Administrador (sesión del admin creado)
# =====================================================================================
st, pg = adm.get("/Users/Create")
check(R, "Usuarios/Create (como Administrador): el desplegable de roles NO ofrece 'Super Administrador'", st == 200 and "Super Administrador" not in pg, f"status={st}")
d = defaults(Role="99")
r = sub(adm, "admin crea superadmin", "/Users/Create", d)
check(R, "Usuarios/Create (como Administrador): forzar rol 99 por POST -> 'Solo un superadministrador puede asignar este rol.'", r.has_error("Solo un superadministrador puede asignar este rol."), f"status={r.status} errores={r.errors[:2]}")
check(R, "Usuarios/Create (como Administrador): no se creó el superadministrador", K.dbv("select count(*) from Users where UserName=?", d["Input.UserName"].lower()) == 0, "")

# =====================================================================================
# Details / Edit / Delete: pantallas
# =====================================================================================
res, u_t, e_t, ci_t, id_t = make("crear objetivo", "1")
st, pg = c.get(f"/Users/Details/{id_t}")
check(R, "Usuarios/Details: 200 con el nombre completo", st == 200 and "Prueba" in pg, f"status={st}")
st, pg = c.get(f"/Users/Edit/{id_t}")
check(R, "Usuarios/Edit GET: 200", st == 200 and u_t.lower() in pg.lower(), f"status={st}")
st, pg = c.get(f"/Users/Delete/{id_t}")
check(R, "Usuarios/Delete GET: 200", st == 200, f"status={st}")
# Admin ante una cuenta SuperAdmin
for p in (f"/Users/Edit/{id_sa2}", f"/Users/Delete/{id_sa2}"):
    st, loc = nr_get(adm, p)
    check(R, f"Usuarios (como Administrador): GET {p} de un SuperAdmin -> acceso denegado (302 a /Error)", st == 302 and "/error" in (loc or "").lower(), f"status={st} location={loc}")

# =====================================================================================
# Edit
# =====================================================================================
E = f"/Users/Edit/{id_t}"
for campo, msg in (("FirstName", "Los nombres son obligatorios"), ("LastName", "El primer apellido es obligatorio"), ("IdentityCard", "El documento de identidad es obligatorio"), ("Email", "El correo institucional es obligatorio")):
    expect(R, f"Usuarios/Edit: {campo} vacío", sub(c, f"edit vacío {campo}", E, {f"Input.{campo}": ""}), status=200, error=msg)
r = sub(c, "edit rol vacío", E, {"Input.Role": ""})
check(R, "Usuarios/Edit: rol vacío -> 200 con 'El rol es obligatorio'", r.status == 200 and r.has_error("obligatorio"), f"status={r.status} errores={r.errors[:2]}")
expect(R, "Usuarios/Edit: rol inválido 7", sub(c, "edit rol 7", E, {"Input.Role": "7"}), status=200, error="no es válido")
r = sub(c, "edit estado 7", E, {"Input.Status": "7"})
check(R, "Usuarios/Edit: estado inválido 7 -> 200 con mensaje", r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
expect(R, "Usuarios/Edit: cédula en minúsculas", sub(c, "edit ci", E, {"Input.IdentityCard": "abc123"}), status=200, error="Formato de cédula inválido")
expect(R, "Usuarios/Edit: correo inválido", sub(c, "edit email", E, {"Input.Email": "no-es-correo"}), status=200, error="Correo electrónico inválido")
expect(R, "Usuarios/Edit: teléfono inválido", sub(c, "edit tel", E, {"Input.PhoneNumber": "abc"}), status=200, error="Formato de teléfono inválido")
expect(R, "Usuarios/Edit: cédula duplicada de otra cuenta", sub(c, "edit ci dup", E, {"Input.IdentityCard": ci_adm}), status=200, error="Este C.I. ya está asignado a otro usuario.")
expect(R, "Usuarios/Edit: correo duplicado de otra cuenta", sub(c, "edit email dup", E, {"Input.Email": e_adm.upper()}), status=200, error="Este correo electrónico ya se encuentra registrado.")
expect(R, "Usuarios/Edit: clave nueva de 4 caracteres", sub(c, "edit pw corta", E, {"Input.NewPassword": "Ab1!"}), status=200, error="La contraseña debe tener al menos 12 caracteres")
r = sub(c, "edit pw 8", E, {"Input.NewPassword": "Abcdef1!"})
check(R, "Usuarios/Edit: clave nueva de 8 caracteres (Identity exige 12) -> error en español", r.status == 200 and bool(r.errors) and not any(re.search(r"\b(Passwords?|must|have at least)\b", e) for e in r.errors), f"status={r.status} errores={r.errors[:3]}")
r = post(c, "edit POST id 99999", "/Users/Edit/99999", [("Input.FirstName", "Zz"), ("Input.LastName", "Zz"), ("Input.IdentityCard", "ZZ" + K.digits(6)), ("Input.Email", f"zz.{tag}@example.test"), ("Input.Role", "1"), ("Input.PhoneNumber", "70012345")], "/Users/Create")
check(R, "Usuarios/Edit: POST sobre un usuario inexistente -> 404", r.status == 404, f"status={r.status}")

# edición válida
hire_before = K.dbv("select HireDate from Users where Id=?", id_t)
r = sub(c, "edit válido", E, {"Input.Position": "Cargo Nuevo " + tag, "Input.Department": "Depto Nuevo"})
expect(R, "Usuarios/Edit: válido -> /Users con toast", r, status=302, redirect="/Users", toast=f"Datos de la cuenta '{full(id_t)}' actualizados correctamente.", toast_kind="success")
check(R, "Usuarios/Edit: el cambio de cargo y departamento SE GUARDA", K.dbv("select count(*) from Users where Id=? and Position=? and Department='Depto Nuevo'", id_t, "Cargo Nuevo " + tag) == 1, "")
hire_after = K.dbv("select HireDate from Users where Id=?", id_t)
check(R, "Usuarios/Edit: guardar sin tocar la fecha de alta NO debe borrarla (el formulario no envía HireDate)", hire_after is not None, f"HireDate antes={hire_before} después={hire_after}")
# clave: vacía no cambia; nueva sí
tc, ok_old = can_login(u_t, PW)
check(R, "Usuarios/Edit: clave nueva vacía conserva la contraseña anterior", ok_old, "")
r = sub(c, "edit pw nueva", E, {"Input.NewPassword": PW2})
expect(R, "Usuarios/Edit: clave nueva válida de 12+ caracteres", r, status=302, redirect="/Users")
tc2, ok_new = can_login(u_t, PW2)
check(R, "Usuarios/Edit: con la clave nueva ya puede iniciar sesión", ok_new, "")
tc3, ok_old2 = can_login(u_t, PW)
check(R, "Usuarios/Edit: la clave anterior ya no sirve", not ok_old2, "")
# estado inactivo forzado (el formulario no tiene campo Status)
r = sub(c, "edit inactivar", E, {"Input.Status": "1"})
check(R, "Usuarios/Edit: Status=1 por POST inactiva la cuenta (BD Status=1, bloqueo máximo)", ustatus(id_t) == 1 and K.dbv("select count(*) from Users where Id=? and LockoutEnd is not null", id_t) == 1, f"status={r.status} Status={ustatus(id_t)}")
tc4, ok_inact = can_login(u_t, PW2)
check(R, "Usuarios/Edit: una cuenta Inactiva no puede iniciar sesión", not ok_inact, "")
r = sub(c, "edit guardar cuenta inactiva", E, {})
check(R, "Usuarios/Edit: guardar el formulario de una cuenta INACTIVA (sin tocar el estado) no debe reactivarla", ustatus(id_t) == 1, f"status={r.status} Status después={ustatus(id_t)} (el formulario no envía Status; el modelo usa Activo por defecto)")
NOTES.append(f"Usuarios/Edit: el formulario no incluye Status ni HireDate; guardar una cuenta inactiva dejó Status={ustatus(id_t)} y la fecha de alta quedó {hire_after!r}.")

# =====================================================================================
# Reglas propias del superadmin 2 (autoedición / autobaja)
# =====================================================================================
E_sa2 = f"/Users/Edit/{id_sa2}"
r = sub(sa2, "sa2 cambia su rol", E_sa2, {"Input.Role": "1"})
check(R, "Usuarios/Edit (SA2 sobre sí mismo): cambiar su rol -> 'No puede cambiar su propio rol ni desactivar su propia cuenta.'", r.has_error("No puede cambiar su propio rol ni desactivar su propia cuenta."), f"status={r.status} errores={r.errors[:2]}")
r = sub(sa2, "sa2 se inactiva", E_sa2, {"Input.Status": "1"})
check(R, "Usuarios/Edit (SA2 sobre sí mismo): desactivar su cuenta -> mismo mensaje", r.has_error("No puede cambiar su propio rol ni desactivar su propia cuenta."), f"status={r.status} errores={r.errors[:2]}")
check(R, "Usuarios/Edit (SA2 sobre sí mismo): sigue SuperAdmin y Activo", urole(id_sa2) == 99 and ustatus(id_sa2) == 0, f"rol={urole(id_sa2)} Status={ustatus(id_sa2)}")
r = post(sa2, "sa2 autobaja", f"/Users/Delete/{id_sa2}", [("AppUser.Id", str(id_sa2))], f"/Users/Delete/{id_sa2}")
expect(R, "Usuarios/Delete (SA2 sobre sí mismo): autobaja", r, status=302, redirect="/Users", toast="No puede dar de baja su propia cuenta.", toast_kind="error")
check(R, "Usuarios/Delete (SA2 sobre sí mismo): la cuenta sigue Activa", ustatus(id_sa2) == 0, f"Status={ustatus(id_sa2)}")
# Administrador sobre sí mismo y sobre un SuperAdmin
E_adm = f"/Users/Edit/{id_adm}"
r = sub(adm, "adm cambia su rol", E_adm, {"Input.Role": "2"})
check(R, "Usuarios/Edit (Administrador sobre sí mismo): cambiar su rol -> bloqueado", r.has_error("No puede cambiar su propio rol ni desactivar su propia cuenta."), f"status={r.status} errores={r.errors[:2]}")
r = sub(adm, "adm se asigna SA", E_adm, {"Input.Role": "99"})
check(R, "Usuarios/Edit (Administrador): asignarse el rol SuperAdmin -> 'Solo un superadministrador puede asignar este rol.'", r.has_error("Solo un superadministrador puede asignar este rol."), f"status={r.status} errores={r.errors[:2]}")
check(R, "Usuarios/Edit (Administrador): su rol sigue siendo Administrador", urole(id_adm) == 1, f"rol={urole(id_adm)}")
r = post(adm, "adm edita SA2", E_sa2, [("Input.FirstName", "Hack"), ("Input.LastName", "Hack"), ("Input.IdentityCard", ci_sa2), ("Input.Email", e_sa2), ("Input.Role", "1"), ("Input.PhoneNumber", "70012345")], "/Users/Create", follow=False)
check(R, "Usuarios/Edit (Administrador): POST directo sobre un SuperAdmin -> acceso denegado y sin cambios", r.status == 302 and "/error" in (r.location or "").lower() and urole(id_sa2) == 99, f"status={r.status} location={r.location} rol SA2={urole(id_sa2)}")
r = post(adm, "adm elimina SA2", f"/Users/Delete/{id_sa2}", [("AppUser.Id", str(id_sa2))], "/Users/Create", follow=False)
check(R, "Usuarios/Delete (Administrador): POST directo sobre un SuperAdmin -> acceso denegado y sin cambios", r.status == 302 and "/error" in (r.location or "").lower() and ustatus(id_sa2) == 0, f"status={r.status} location={r.location} Status SA2={ustatus(id_sa2)}")
# SA edita a otro SA (permitido porque qa_super sigue activo)
r = sub(c, "qa_super degrada a SA2", E_sa2, {"Input.Role": "1"})
check(R, "Usuarios/Edit (qa_super sobre SA2): degradar a otro SuperAdmin es válido mientras exista otro SuperAdmin activo (qa_super)", r.status == 302 and urole(id_sa2) == 1, f"status={r.status} rol SA2={urole(id_sa2)} errores={r.errors[:1]}")
sub(c, "restaurar SA2", E_sa2, {"Input.Role": "99"})
NOTES.append("Usuarios: la regla 'Debe existir al menos un superadministrador activo.' (Edit/Delete) no es alcanzable por HTTP: quien actúa es siempre un SuperAdmin activo distinto del objetivo, o es el propio objetivo y lo frena antes la regla de autoedición/autobaja. No se probó la autobaja con qa_super para no dejar la copia QA sin su único SuperAdmin.")

# =====================================================================================
# Delete
# =====================================================================================
res, u_d, e_d, ci_d, id_d = make("crear a eliminar", "1")
res, u_r, e_r, ci_r, id_r = make("crear a revivir", "2")
dd, ok_dd = can_login(u_d, PW)
check(R, "Usuarios/prep: la cuenta a eliminar inicia sesión antes de la baja", ok_dd, "")
expect(R, "Usuarios/Delete sin token: 400", K.post_notoken(c, "delete sin token", f"/Users/Delete/{id_d}", []), status=400)
check(R, "Usuarios/Delete sin token: la cuenta sigue Activa", ustatus(id_d) == 0, "")
nm = full(id_d)
r = post(c, "delete", f"/Users/Delete/{id_d}", [("AppUser.Id", str(id_d))], f"/Users/Delete/{id_d}")
expect(R, "Usuarios/Delete: baja con toast 'El acceso para '<nombre>' ha sido revocado correctamente.'", r, status=302, redirect="/Users", toast=f"El acceso para '{nm}' ha sido revocado correctamente.", toast_kind="success")
check(R, "Usuarios/Delete: BD Status=2 y bloqueo máximo (la fila sigue existiendo)", ustatus(id_d) == 2 and K.dbv("select count(*) from Users where Id=? and LockoutEnd is not null", id_d) == 1, f"Status={ustatus(id_d)}")
check(R, "Usuarios/Index: el usuario dado de baja desaparece del listado", f"/Users/Details/{id_d}" not in c.get(f"/Users?SearchTerm={u_d}")[1], "")
st, lst = c.get(f"/Users?StatusFilter=2&SearchTerm={u_d}")
check(R, "Usuarios/Index?StatusFilter=2: muestra las cuentas eliminadas (según el plan)", st == 200 and f"/Users/Details/{id_d}" in lst, f"status={st}")
dd2, ok_after = can_login(u_d, PW)
check(R, "Usuarios/Delete: la cuenta dada de baja ya no puede iniciar sesión", not ok_after, "")
for p in (f"/Users/Edit/{id_d}", f"/Users/Delete/{id_d}"):
    st, _ = c.get(p)
    check(R, f"Usuarios: GET {p} del usuario eliminado -> 404", st == 404, f"status={st}")
st, pg = c.get(f"/Users/Details/{id_d}")
check(R, "Usuarios/Details de un usuario eliminado -> 200 (por diseño: Details.cshtml.cs usa IgnoreQueryFilters para consultar el historial)", st == 200, f"status={st}")
NOTES.append("Usuarios/Details muestra las cuentas eliminadas (IgnoreQueryFilters); Edit/Delete en cambio dan 404 para ellas.")
r = post(c, "segunda baja", f"/Users/Delete/{id_d}", [("AppUser.Id", str(id_d))], "/Users/Create")
check(R, "Usuarios/Delete: segunda baja (plan: [W] 'ya se encuentra dado de baja'; el código da 404 porque FindAsync respeta el filtro)", r.status == 404 or r.has_toast("ya se encuentra dado de baja", "warning"), f"status={r.status} toasts={r.toasts[:1]}")
NOTES.append(f"Usuarios/Delete: la segunda baja responde {r.status}; el aviso 'El usuario ... ya se encuentra dado de baja.' del plan es inalcanzable.")
# revivir un usuario eliminado haciendo POST directo a Edit (el POST usa IgnoreQueryFilters; el formulario no envía Status)
post(c, "baja revivir", f"/Users/Delete/{id_r}", [("AppUser.Id", str(id_r))], f"/Users/Delete/{id_r}")
check(R, "Usuarios/prep: cuenta eliminada para la prueba de reactivación", ustatus(id_r) == 2, f"Status={ustatus(id_r)}")
r = post(c, "POST edit sobre eliminado", f"/Users/Edit/{id_r}", [("Input.FirstName", "Revivido"), ("Input.LastName", "Zz"), ("Input.IdentityCard", ci_r), ("Input.Email", e_r), ("Input.Role", "2"), ("Input.PhoneNumber", "70012345")], "/Users/Create")
check(R, "Usuarios/Edit: POST directo sobre una cuenta ELIMINADA -> 404 y no la reactiva", r.status == 404 and ustatus(id_r) == 2, f"status={r.status} Status={ustatus(id_r)} (si es 0, el POST revirtió la baja)")
# baja de un SuperAdmin por otro SuperAdmin
r = post(c, "qa_super elimina SA2", f"/Users/Delete/{id_sa2}", [("AppUser.Id", str(id_sa2))], f"/Users/Delete/{id_sa2}")
expect(R, "Usuarios/Delete (qa_super sobre SA2): baja de un SuperAdmin permitida mientras haya otro activo", r, status=302, redirect="/Users", toast_kind="success")
check(R, "Usuarios/Delete: SA2 quedó eliminado y qa_super sigue SuperAdmin Activo", ustatus(id_sa2) == 2 and K.dbv("select count(*) from Users where UserName='qa_super' and Role=99 and Status=0") == 1, f"Status SA2={ustatus(id_sa2)}")

# roles bajos
for nombre, cl in (("Supervisor", sup), ("Encargado de cierre", pcu)):
    for p in ("/Users", "/Users/Create"):
        st, loc = nr_get(cl, p)
        check(R, f"Usuarios ({nombre}): GET {p} -> no accede (302)", st == 302, f"status={st} location={loc}")

# =====================================================================================
# 404 / sin sesión / sin token
# =====================================================================================
K.guard_404(c, "Usuarios", ["/Users/Edit/{id}", "/Users/Delete/{id}"])
for bid in ("99999", "-1", "0"):
    st, loc = nr_get(c, f"/Users/Details/{bid}")
    check(R, f"Usuarios: GET /Users/Details/{bid} -> 302 a /Error (por diseño del código; NO es 404 como en los demás maestros)", st == 302 and "/error" in (loc or "").lower(), f"status={st} location={loc}")
for bid in ("abc", "1.5", "99999999999", "2147483648"):
    st, _ = c.get(f"/Users/Details/{bid}")
    check(R, f"Usuarios: GET /Users/Details/{bid} -> 404", st == 404, f"status={st}")
NOTES.append("Usuarios/Details con id inexistente (99999, -1, 0) responde 302 a /Error ('No se encontro el usuario solicitado.') en lugar de 404 (Details.cshtml.cs, rama user == null).")
for p in ("Edit", "Delete"):
    for bid in ("99999", "-1", "abc"):
        campos = [("Input.FirstName", "Zz"), ("Input.LastName", "Zz"), ("Input.IdentityCard", "ZZ" + K.digits(6)), ("Input.Email", f"zz.{K.rnd()}@example.test"), ("Input.Role", "1"), ("Input.PhoneNumber", "70012345")] if p == "Edit" else [("AppUser.Id", "0")]
        r = post(c, f"POST {p} {bid}", f"/Users/{p}/{bid}", campos, "/Users/Create")
        check(R, f"Usuarios: POST /Users/{p}/{bid} -> 404", r.status == 404, f"status={r.status}")
K.guard_anon("Usuarios", ["/Users", "/Users/Create", f"/Users/Edit/{id_adm}", f"/Users/Delete/{id_adm}", f"/Users/Details/{id_adm}"],
             ["/Users/Create", f"/Users/Edit/{id_adm}", f"/Users/Delete/{id_adm}"])
K.guard_token(c, "Usuarios", ["/Users/Create", f"/Users/Edit/{id_adm}", f"/Users/Delete/{id_adm}"])
check(R, "Usuarios: tras los POST sin token el administrador sigue Activo y con su correo", ustatus(id_adm) == 0 and K.dbv("select Email from Users where Id=?", id_adm) == e_adm.lower(), "")
check(R, "Usuarios: qa_super y nihaht no fueron tocados (Activos, mismo rol)", K.dbv("select count(*) from Users where (UserName='qa_super' and Role=99 and Status=0) or (UserName='nihaht' and Role=1 and Status=0)") == 2, "")

raise SystemExit(K.finish(NOTES))
