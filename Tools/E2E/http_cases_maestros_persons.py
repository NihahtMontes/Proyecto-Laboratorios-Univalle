"""Personas: Create / Edit / Delete / Details (con ?id=N) + 404 + sin sesión + sin token. Solo contra QA."""
import http_cases_maestros_common as K
from http_cases_maestros_common import R, sub, post, check, expect

c = K.client()
tag = K.rnd(7)
NOTES = []


def pname(i):
    return K.dbv("select coalesce((select Name from Interns where Id=?),(select Name from Externs where Id=?))", i, i)


def pstatus(i):
    return K.dbv("select Status from People where Id=?", i)


def by_email(email):
    return K.dbv("select top 1 Id from People where Email=? order by Id desc", email.lower())


def in_listing(i, term):
    st, lst = c.get(f"/Persons?SearchTerm={term}")
    return st == 200 and f"/Persons/Details?id={i}" in lst


def mail(tag2):
    return f"qa.{tag2}.{tag}@example.test"


INT = {"Input.IsInternal": "true"}
EXT = {"Input.IsInternal": "false"}

# ---- pantallas ----
st, pg = c.get("/Persons")
check(R, "Personas/Index: 200 'Directorio de Personal y Colaboradores'", st == 200 and "Directorio de Personal" in pg, f"status={st}")
st, pg = c.get("/Persons/Create")
check(R, "Personas/Create GET: 200", st == 200, f"status={st}")

# ---- Create: validaciones ----
expect(R, "Personas/Create: nombre vacío", sub(c, "vacío", "/Persons/Create", {**INT, "Input.Name": ""}), status=200, error="El nombre / razón social es obligatorio")
expect(R, "Personas/Create: nombre solo espacios", sub(c, "espacios", "/Persons/Create", {**INT, "Input.Name": "     "}), status=200, error="El nombre / razón social es obligatorio")
for nom in ("José Pérez", "Muñoz", "Ñandú", "O'Brien", "Ana_Maria", "<b>x</b>", 'Dr "Q"'):
    res = sub(c, f"nombre {nom}", "/Persons/Create", {**INT, "Input.Name": nom + " " + tag})
    # el regex del servidor solo admite letras ASCII, dígitos, espacio, punto y guion
    esperado_rechazo = any(ch in nom for ch in "éñÑ'_<>\"")
    check(R, f"Personas/Create: nombre {nom!r} -> {'rechazado con ' + chr(39) + 'Formato de nombre inválido' + chr(39) if esperado_rechazo else 'aceptado'}", res.has_error("Formato de nombre inválido") if esperado_rechazo else res.status == 302, f"status={res.status} errores={res.errors[:1]}")
NOTES.append("Personas: el regex de Name (^[a-zA-Z0-9\\s.\\-]*$) rechaza tildes y ñ: nombres reales como 'José', 'Muñoz' o 'Ñandú' no se pueden registrar aunque el JS del navegador los deje escribir.")
res = sub(c, "nombre 201", "/Persons/Create", {**INT, "Input.Name": "x" * 201})
check(R, "Personas/Create: nombre de 201 caracteres -> 200 con error (mensaje por defecto)", res.status == 200 and res.has_error("200"), f"status={res.status} errores={res.errors[:2]}")
expect(R, "Personas/Create: email inválido", sub(c, "email inválido", "/Persons/Create", {**INT, "Input.Name": "Persona Email " + tag, "Input.Email": "no-es-correo"}), status=200, error="Email inválido")
expect(R, "Personas/Create: teléfono inválido", sub(c, "tel inválido", "/Persons/Create", {**INT, "Input.Name": "Persona Tel " + tag, "Input.PhoneNumber": "abc"}), status=200, error="Teléfono inválido")
expect(R, "Personas/Create: externo sin dirección", sub(c, "externo sin dirección", "/Persons/Create", {**EXT, "Input.Name": "Empresa Sin Dir " + tag}), status=200, error="La dirección es obligatoria para externos.")
res = sub(c, "tipo omitido", "/Persons/Create", {"Input.IsInternal": None, "Input.Name": "Sin Tipo " + tag})
check(R, "Personas/Create: sin tipo de registro (IsInternal ausente) se trata como externo y exige dirección", res.has_error("La dirección es obligatoria para externos"), f"status={res.status} errores={res.errors[:2]}")
res = sub(c, "externo dirección espacios", "/Persons/Create", {**EXT, "Input.Name": "Empresa Dir Blanca " + tag, "Input.Address": "     "})
check(R, "Personas/Create: externo con dirección de solo espacios debe rechazarse", res.has_error("La dirección es obligatoria"), f"status={res.status} (302 = se guardó una dirección en blanco) Address en BD={K.dbv('select top 1 Address from Externs order by Id desc')!r}")
# límites de columna (entidad): Name interno 100, Email 100, Teléfono 20 -> el servidor atrapa y muestra modal
res = sub(c, "interno nombre 150", "/Persons/Create", {**INT, "Input.Name": ("N" + tag + "x" * 150)[:150]})
check(R, "Personas/Create: interno con nombre de 101-200 caracteres -> modal 'No se pudo registrar la persona' (límite de columna 100)", res.has_toast("No se pudo registrar la persona", "error"), f"status={res.status} toasts={res.toasts[:2]} errores={res.errors[:1]}")
res = sub(c, "email 101", "/Persons/Create", {**INT, "Input.Name": "Persona Mail Largo " + tag, "Input.Email": "a" * 90 + "@example.test"})
check(R, "Personas/Create: email de 103 caracteres (válido de formato) -> error controlado, sin 500", res.status in (200, 302) and (res.has_toast("No se pudo registrar", "error") or res.has_error("")), f"status={res.status} toasts={res.toasts[:2]} errores={res.errors[:1]}")
res = sub(c, "tel 25", "/Persons/Create", {**INT, "Input.Name": "Persona Tel Largo " + tag, "Input.PhoneNumber": "7" * 25})
check(R, "Personas/Create: teléfono de 25 dígitos -> error controlado, sin 500", res.status in (200, 302) and (res.has_toast("No se pudo registrar", "error") or bool(res.errors)), f"status={res.status} toasts={res.toasts[:2]} errores={res.errors[:1]}")

# ---- Create: válidos ----
e1 = mail("int")
n1 = f"Persona Interna {tag}"
res = sub(c, "crear interno", "/Persons/Create", {**INT, "Input.Name": n1, "Input.Email": e1.upper(), "Input.PhoneNumber": "70012345"})
expect(R, "Personas/Create: interno válido -> /Persons con toast", res, status=302, redirect="/Persons", toast=f"Registro de '{n1}' completado exitosamente.", toast_kind="success")
id1 = by_email(e1)
check(R, "Personas/Create: interno guardado Activo con email en minúsculas", id1 and pstatus(id1) == 0 and K.dbv("select Email from People where Id=?", id1) == e1.lower(), f"id={id1} Email={K.dbv('select Email from People where Id=?', id1)}")
check(R, "Personas/Create: el interno se guardó en la tabla de internos con su nombre", K.dbv("select count(*) from Interns where Id=? and Name=?", id1, n1) == 1, f"id={id1}")
check(R, "Personas/Index: búsqueda por email encuentra a la persona", in_listing(id1, e1), "")
st, lst = c.get(f"/Persons?SearchTerm={n1.replace(' ', '+')}")
check(R, "Personas/Index: la búsqueda por NOMBRE debería encontrarla (el placeholder promete 'nombre, apellido o C.I.')", st == 200 and f"/Persons/Details?id={id1}" in lst, f"status={st} (solo busca por email o id)")
st, lst = c.get(f"/Persons?SearchTerm={id1}")
check(R, "Personas/Index: búsqueda por id encuentra a la persona", st == 200 and f"/Persons/Details?id={id1}" in lst, "")
e2 = mail("ext")
n2 = f"Empresa Externa {tag}"
res = sub(c, "crear externo", "/Persons/Create", {**EXT, "Input.Name": n2, "Input.Address": "Av. Siempre Viva 742", "Input.Email": e2, "Input.IsEntity": ["true", "false"]})
expect(R, "Personas/Create: externo válido (entidad) -> /Persons con toast", res, status=302, redirect="/Persons", toast=f"Registro de '{n2}' completado exitosamente.", toast_kind="success")
id2 = by_email(e2)
check(R, "Personas/Create: externo guardado con dirección e IsEntity=1", K.dbv("select count(*) from Externs where Id=? and Address='Av. Siempre Viva 742' and IsEntity=1", id2) == 1, f"id={id2}")
res = sub(c, "email duplicado", "/Persons/Create", {**INT, "Input.Name": f"Persona Mismo Mail {tag}", "Input.Email": e1})
check(R, "Personas/Create: el mismo email en dos personas se permite (el plan: sin unicidad)", res.status == 302 and K.dbv("select count(*) from People where Email=?", e1.lower()) == 2, f"status={res.status}")
NOTES.append("Personas/Create: dos personas pueden compartir el mismo email (sin control de unicidad); el plan ya lo anticipa.")
n100 = ("P" + tag + "x" * 100)[:100]
expect(R, "Personas/Create: interno con nombre de 100 caracteres (límite exacto)", sub(c, "100", "/Persons/Create", {**INT, "Input.Name": n100}), status=302, redirect="/Persons")
expect(R, "Personas/Create: espacios se normalizan", sub(c, "espacios", "/Persons/Create", {**INT, "Input.Name": f"  Pers   Esp {tag}  "}), status=302)
check(R, "Personas/Create: nombre guardado sin espacios sobrantes", K.dbv("select count(*) from Interns where Name=?", f"Pers Esp {tag}") == 1, "")

# ---- Details / Edit / Delete GET ----
st, pg = c.get(f"/Persons/Details?id={id1}")
check(R, "Personas/Details: 200 con 'Mantenimientos Realizados'", st == 200 and n1 in pg and "Mantenimientos Realizados" in pg, f"status={st}")
st, pg = c.get(f"/Persons/Edit?id={id1}")
check(R, "Personas/Edit GET: 200 con el nombre actual", st == 200 and n1 in pg, f"status={st}")
st, pg = c.get(f"/Persons/Delete?id={id1}")
check(R, "Personas/Delete GET: 200 y '¿Dar de Baja Identidad?'", st == 200 and "Dar de Baja Identidad" in pg, f"status={st}")

# ---- Edit ----
E = f"/Persons/Edit?id={id1}"
expect(R, "Personas/Edit: nombre vacío", sub(c, "edit vacío", E, {"Input.Name": ""}), status=200, error="El nombre / razón social es obligatorio")
expect(R, "Personas/Edit: nombre con tilde 'José'", sub(c, "edit José", E, {"Input.Name": "José " + tag}), status=200, error="Formato de nombre inválido")
res = sub(c, "edit 201", E, {"Input.Name": "x" * 201})
check(R, "Personas/Edit: nombre de 201 caracteres -> 200 con error (mensaje por defecto)", res.status == 200 and res.has_error("200"), f"status={res.status} errores={res.errors[:2]}")
expect(R, "Personas/Edit: email inválido", sub(c, "edit email", E, {"Input.Email": "no-es-correo"}), status=200, error="Email inválido")
expect(R, "Personas/Edit: teléfono inválido", sub(c, "edit tel", E, {"Input.PhoneNumber": "abc"}), status=200, error="Teléfono inválido")
res = sub(c, "edit interno 150", E, {"Input.Name": ("N" + tag + "x" * 150)[:150]})
check(R, "Personas/Edit: interno con nombre de 101-200 caracteres -> modal 'No se pudo actualizar la persona'", res.has_toast("No se pudo actualizar la persona", "error"), f"status={res.status} toasts={res.toasts[:2]} errores={res.errors[:1]}")
res = sub(c, "edit estado 7", E, {"Input.Status": "7"})
check(R, "Personas/Edit: estado inválido 7 no se guarda", pstatus(id1) in (0, 1), f"status={res.status} Status en BD={pstatus(id1)} errores={res.errors[:1]}")
res = sub(c, "edit id inexistente", E, {"Input.Id": "99999", "Input.Name": "Nuevo " + K.rnd()})
check(R, "Personas/Edit: POST con Input.Id inexistente en el cuerpo -> 404", res.status == 404, f"status={res.status}")
expect(R, "Personas/Edit: guardar sin cambios", sub(c, "edit self", E, {}), status=302, redirect="/Persons", toast=f"Datos de '{n1}' actualizados correctamente.", toast_kind="success")
nuevo = f"Persona Cambiada {tag}"
e1b = mail("cambio")
res = sub(c, "edit persistencia", E, {"Input.Name": nuevo, "Input.Email": e1b.upper(), "Input.PhoneNumber": "60011122", "Input.Status": "1", "Input.InternStatus": "1"})
expect(R, "Personas/Edit: válido -> /Persons con toast", res, status=302, redirect="/Persons", toast=f"Datos de '{nuevo}' actualizados correctamente.", toast_kind="success")
check(R, "Personas/Edit: el cambio de nombre, email (a minúsculas), teléfono y estado SE GUARDA en la BD",
      K.dbv("select count(*) from People p join Interns i on i.Id=p.Id where p.Id=? and i.Name=? and p.Email=? and p.PhoneNumber='60011122' and p.Status=1", id1, nuevo, e1b.lower()) == 1,
      f"BD: {K.db('select i.Name,p.Email,p.PhoneNumber,p.Status from People p join Interns i on i.Id=p.Id where p.Id=?', id1)}")
sub(c, "edit reactivar", E, {"Input.Status": "0", "Input.InternStatus": "0"})
# externo: la dirección es obligatoria al crear; en Edit
E2 = f"/Persons/Edit?id={id2}"
res = sub(c, "edit externo dirección vacía", E2, {"Input.Address": ""})
check(R, "Personas/Edit: externo sin dirección debe rechazarse como en Create ('La dirección es obligatoria para externos.')", res.has_error("La dirección es obligatoria"), f"status={res.status} Address en BD={K.dbv('select Address from Externs where Id=?', id2)!r}")
sub(c, "edit externo restaurar", E2, {"Input.Address": "Av. Siempre Viva 742"})

# ---- Delete ----
other = mail("hermana")
nh = f"Persona Hermana {tag}"
sub(c, "crear hermana", "/Persons/Create", {**INT, "Input.Name": nh, "Input.Email": other})
id_h = by_email(other)
expect(R, "Personas/Delete sin token: 400", K.post_notoken(c, "delete sin token", f"/Persons/Delete?id={id_h}", []), status=400)
check(R, "Personas/Delete sin token: la persona sigue Activa", pstatus(id_h) == 0, "")
expect(R, "Personas/Edit sin token: 400", K.post_notoken(c, "edit sin token", f"/Persons/Edit?id={id_h}", [("Input.Id", str(id_h)), ("Input.Name", "Hack"), ("Input.Status", "0")]), status=400)

cur = pname(id2)
res = post(c, "delete id2", f"/Persons/Delete?id={id2}", [("Person.Id", str(id2))], f"/Persons/Delete?id={id2}")
expect(R, "Personas/Delete: baja lógica con toast", res, status=302, redirect="/Persons", toast=f"La identidad de '{cur}' ha sido dada de baja correctamente.", toast_kind="success")
check(R, "Personas/Delete: BD Status=2 (la fila sigue existiendo)", pstatus(id2) == 2, f"Status={pstatus(id2)}")
check(R, "Personas/Index: la persona dada de baja desaparece del listado", not in_listing(id2, e2), "")
for p in (f"/Persons/Edit?id={id2}", f"/Persons/Details?id={id2}", f"/Persons/Delete?id={id2}"):
    st, _ = c.get(p)
    check(R, f"Personas: GET {p} de la persona eliminada -> 404", st == 404, f"status={st}")
res = post(c, "segunda baja", f"/Persons/Delete?id={id2}", [("Person.Id", str(id2))], "/Persons/Create")
check(R, "Personas/Delete: segunda baja (plan: [W] 'ya se encuentra dada de baja'; el código da 404 porque FindAsync respeta el filtro)", res.status == 404 or res.has_toast("ya se encuentra dada de baja", "warning"), f"status={res.status} toasts={res.toasts[:1]}")
NOTES.append(f"Personas/Delete: la segunda baja responde {res.status}; el aviso 'La identidad de ... ya se encuentra dada de baja.' del plan es inalcanzable (FindAsync respeta el filtro de eliminados).")
res = post(c, "POST edit eliminada", f"/Persons/Edit?id={id2}", [("Input.Id", str(id2)), ("Input.Name", "Revivir " + K.rnd()), ("Input.Status", "0")], "/Persons/Create")
check(R, "Personas/Edit: POST sobre persona eliminada -> 404 y no la revive", res.status == 404 and pstatus(id2) == 2, f"status={res.status} Status={pstatus(id2)}")

# ---- id inexistente / negativo / no numérico ----
bad = ("99999", "-1", "0", "abc", "1.5", "99999999999", "2147483648", "")
for ruta in ("Edit", "Delete", "Details"):
    for bid in bad:
        st, _ = c.get(f"/Persons/{ruta}?id={bid}")
        check(R, f"Personas: GET /Persons/{ruta}?id={bid!r} -> 404", st == 404, f"status={st}")
    st, _ = c.get(f"/Persons/{ruta}")
    check(R, f"Personas: GET /Persons/{ruta} (sin id) -> 404", st == 404, f"status={st}")
for ruta in ("Edit", "Delete"):
    for bid in ("99999", "-1", "abc"):
        res = post(c, f"POST {ruta} {bid}", f"/Persons/{ruta}?id={bid}", [("Input.Id", bid if bid.lstrip('-').isdigit() else "0"), ("Input.Name", "Zzz Fantasma"), ("Input.Status", "0")], "/Persons/Create")
        check(R, f"Personas: POST /Persons/{ruta}?id={bid} -> 404", res.status == 404, f"status={res.status}")

# ---- sin sesión / sin token ----
K.guard_anon("Personas", ["/Persons", "/Persons/Create", f"/Persons/Edit?id={id_h}", f"/Persons/Delete?id={id_h}", f"/Persons/Details?id={id_h}"],
             ["/Persons/Create", f"/Persons/Edit?id={id_h}", f"/Persons/Delete?id={id_h}"])
K.guard_token(c, "Personas", ["/Persons/Create", f"/Persons/Edit?id={id_h}", f"/Persons/Delete?id={id_h}"])
check(R, "Personas: tras los POST sin token la persona hermana sigue Activa y con su nombre", pstatus(id_h) == 0 and pname(id_h) == nh, "")

raise SystemExit(K.finish(NOTES))
