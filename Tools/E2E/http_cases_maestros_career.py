"""Carreras: Create / Edit / Delete / Details (con ?id=N) + 404 + sin sesión + sin token. Solo contra QA."""
import http_cases_maestros_common as K
from http_cases_maestros_common import R, sub, post, check, expect

c = K.client()
tag = K.rnd(7)
NOTES = []
FAC1, FAC2 = 1, 2


def cname(i):
    return K.dbv("select Name from Careers where Id=?", i)


def cstatus(i):
    return K.dbv("select Status from Careers where Id=?", i)


def in_listing(i, term):
    st, lst = c.get(f"/Laboratories?SearchTerm={term}")
    return st == 200 and f"/Career/Details?id={i}" in lst


# facultad dada de baja
fdn = "Facultad ParaCarrera " + tag
sub(c, "crear facultad baja", "/Faculties/Create", {"Input.Name": fdn})
FD = K.id_by("Faculties", "Name", fdn)
post(c, "baja facultad", f"/Faculties/Delete/{FD}", [("Faculty.Id", str(FD))], f"/Faculties/Delete/{FD}")

# ---- pantallas ----
st, pg = c.get("/Career")
check(R, "Carreras/Index: 200 'Carreras y Programas'", st == 200 and "Carreras y Programas" in pg, f"status={st}")
st, pg = c.get("/Career/Create")
check(R, "Carreras/Create GET: 200", st == 200, f"status={st}")

# ---- Create: validaciones ----
expect(R, "Carreras/Create: nombre vacío", sub(c, "vacío", "/Career/Create", {"Input.Name": ""}), status=200, error="El nombre de la carrera es obligatorio")
expect(R, "Carreras/Create: solo espacios", sub(c, "espacios", "/Career/Create", {"Input.Name": "    "}), status=200, error="El nombre de la carrera es obligatorio")
expect(R, "Carreras/Create: nombre 201 caracteres (mensaje por defecto)", sub(c, "201", "/Career/Create", {"Input.Name": "x" * 201}), status=200, error="200")
res = sub(c, "facultad abc", "/Career/Create", {"Input.Name": "Carrera abc " + tag, "Input.FacultadId": "abc"})
check(R, "Carreras/Create: facultad no numérica 'abc' -> 200 con mensaje", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
res = sub(c, "facultad inexistente", "/Career/Create", {"Input.Name": "Carrera Fantasma " + tag, "Input.FacultadId": "99999"})
check(R, "Carreras/Create: facultad inexistente 99999 se rechaza con mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]} creada={K.dbv('select count(*) from Careers where Name=?', 'Carrera Fantasma ' + tag)}")
res = sub(c, "facultad eliminada", "/Career/Create", {"Input.Name": "Carrera DeBaja " + tag, "Input.FacultadId": str(FD)})
check(R, "Carreras/Create: facultad dada de baja se rechaza (no se crea la carrera)", K.dbv("select count(*) from Careers where Name=?", "Carrera DeBaja " + tag) == 0, f"status={res.status} errores={res.errors[:2]}")
res = sub(c, "estado 5", "/Career/Create", {"Input.Name": "Carrera Estado5 " + tag, "Input.Status": "5"})
check(R, "Carreras/Create: estado inválido 5 por POST no crea la carrera", K.dbv("select count(*) from Careers where Name=?", "Carrera Estado5 " + tag) == 0, f"status={res.status} errores={res.errors[:1]} Status en BD={K.dbv('select Status from Careers where Name=?', 'Carrera Estado5 ' + tag)}")
res = sub(c, "estado 2", "/Career/Create", {"Input.Name": "Carrera Estado2 " + tag, "Input.Status": "2"})
check(R, "Carreras/Create: estado 2 (Eliminado) por POST no crea una carrera ya eliminada", K.dbv("select count(*) from Careers where Name=?", "Carrera Estado2 " + tag) == 0, f"status={res.status} Status en BD={K.dbv('select Status from Careers where Name=?', 'Carrera Estado2 ' + tag)}")

# ---- Create: válidos ----
n1 = "Carrera QA " + tag
res = sub(c, "crear n1", "/Career/Create", {"Input.Name": n1, "Input.FacultadId": str(FAC1)})
expect(R, "Carreras/Create: válida -> /Career con toast", res, status=302, redirect="/Career", toast="Carrera creada correctamente.", toast_kind="success")
id1 = K.id_by("Careers", "Name", n1)
check(R, "Carreras/Create: quedó Activa en la BD con su facultad", id1 and cstatus(id1) == 0 and K.dbv("select FacultadId from Careers where Id=?", id1) == FAC1, f"id={id1}")
check(R, "Carreras/Index (Laboratorios, pestaña Carreras): la carrera aparece en el listado", in_listing(id1, tag), "")
expect(R, "Carreras/Create: sin facultad (opcional) y estado Inactivo", sub(c, "sin facultad", "/Career/Create", {"Input.Name": "Carrera SinFac " + tag, "Input.Status": "1"}), status=302, redirect="/Career")
check(R, "Carreras/Create: sin facultad -> FacultadId NULL y Status=1", K.dbv("select count(*) from Careers where Name=? and FacultadId is null and Status=1", "Carrera SinFac " + tag) == 1, "")
n200 = ("Car" + tag + "x" * 200)[:200]
expect(R, "Carreras/Create: nombre de 200 caracteres (límite exacto)", sub(c, "crear 200", "/Career/Create", {"Input.Name": n200}), status=302, redirect="/Career")
expect(R, "Carreras/Create: espacios se normalizan", sub(c, "espacios", "/Career/Create", {"Input.Name": f"  Car   Esp {tag}  "}), status=302)
check(R, "Carreras/Create: nombre guardado sin espacios sobrantes", K.dbv("select count(*) from Careers where Name=?", f"Car Esp {tag}") == 1, "")
for etiqueta, nom in (("html", f"<b>QAr{tag}</b>"), ("comillas", f"Car \"Q\" O'Brien {tag}"), ("tildes", f"Ingeniería Ñandú Cocción {tag}")):
    res = sub(c, f"crear {etiqueta}", "/Career/Create", {"Input.Name": nom})
    check(R, f"Carreras/Create: caracteres especiales ({etiqueta}) sin excepción", res.status in (200, 302), f"status={res.status} errores={res.errors[:1]}")
st, lst = c.get(f"/Laboratories?SearchTerm={tag}")
check(R, "Carreras/Index: el HTML hostil se muestra escapado", st == 200 and f"<b>QAr{tag}</b>" not in lst, "")

# ---- duplicados: el plan dice 'sin control de duplicados' ----
res = sub(c, "duplicado exacto", "/Career/Create", {"Input.Name": n1, "Input.FacultadId": str(FAC1)})
cnt = K.dbv("select count(*) from Careers where Name=?", n1)
check(R, "Carreras/Create: nombre duplicado exacto (el plan anticipa que NO hay control de duplicados)", res.status == 302 and cnt == 2, f"status={res.status} filas con ese nombre={cnt}")
NOTES.append(f"Carreras/Create: se pudo crear una carrera duplicada exacta ('{n1}', 2 filas activas); Países, Ciudades, Facultades y Laboratorios sí validan duplicados.")

# ---- Details / Edit / Delete GET ----
st, pg = c.get(f"/Career/Details?id={id1}")
check(R, "Carreras/Details: 200 con el nombre y 'Equipos Vinculados'", st == 200 and n1 in pg and "Equipos Vinculados" in pg, f"status={st}")
st, pg = c.get(f"/Career/Edit?id={id1}")
check(R, "Carreras/Edit GET: 200 con el nombre actual", st == 200 and n1 in pg, f"status={st}")
st, pg = c.get(f"/Career/Delete?id={id1}")
check(R, "Carreras/Delete GET: 200", st == 200, f"status={st}")

# ---- Edit ----
E = f"/Career/Edit?id={id1}"
expect(R, "Carreras/Edit: nombre vacío", sub(c, "edit vacío", E, {"Input.Name": ""}), status=200, error="El nombre de la carrera es obligatorio")
expect(R, "Carreras/Edit: nombre 201 caracteres (mensaje por defecto)", sub(c, "edit 201", E, {"Input.Name": "x" * 201}), status=200, error="200")
res = sub(c, "edit facultad inexistente", E, {"Input.FacultadId": "99999"})
check(R, "Carreras/Edit: facultad inexistente 99999 se rechaza con mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
res = sub(c, "edit estado 7", E, {"Input.Status": "7"})
check(R, "Carreras/Edit: estado inválido 7 no se guarda", cstatus(id1) in (0, 1), f"status={res.status} Status en BD={cstatus(id1)}")
res = sub(c, "edit id inexistente", E, {"Input.Id": "99999", "Input.Name": "Nueva " + K.rnd()})
check(R, "Carreras/Edit: POST con Input.Id inexistente en el cuerpo -> 404", res.status == 404, f"status={res.status}")
expect(R, "Carreras/Edit: guardar sin cambios", sub(c, "edit self", E, {}), status=302, redirect="/Career", toast="Carrera actualizada correctamente.", toast_kind="success")
nuevo = "Carrera Cambiada " + tag
res = sub(c, "edit persistencia", E, {"Input.Name": nuevo, "Input.FacultadId": str(FAC2), "Input.Status": "1"})
expect(R, "Carreras/Edit: válido -> /Career con toast", res, status=302, redirect="/Career", toast="Carrera actualizada correctamente.", toast_kind="success")
check(R, "Carreras/Edit: el cambio de nombre, facultad y estado SE GUARDA en la BD", cname(id1) == nuevo and K.dbv("select FacultadId from Careers where Id=?", id1) == FAC2 and cstatus(id1) == 1,
      f"BD: nombre={cname(id1)!r} facultad={K.dbv('select FacultadId from Careers where Id=?', id1)} Status={cstatus(id1)}")
res = sub(c, "edit quitar facultad", E, {"Input.FacultadId": "", "Input.Status": "0"})
check(R, "Carreras/Edit: quitar la facultad (opcional) y reactivar se guarda", K.dbv("select count(*) from Careers where Id=? and FacultadId is null and Status=0", id1) == 1, f"status={res.status}")

# ---- Delete ----
other = "Carrera Hermana " + tag
sub(c, "crear hermana", "/Career/Create", {"Input.Name": other})
id_h = K.id_by("Careers", "Name", other)
expect(R, "Carreras/Delete sin token: 400", K.post_notoken(c, "delete sin token", f"/Career/Delete?id={id_h}", []), status=400)
check(R, "Carreras/Delete sin token: la carrera sigue Activa", cstatus(id_h) == 0, "")
expect(R, "Carreras/Edit sin token: 400", K.post_notoken(c, "edit sin token", f"/Career/Edit?id={id_h}", [("Input.Id", str(id_h)), ("Input.Name", "Hack"), ("Input.Status", "0")]), status=400)

res = post(c, "delete id1", f"/Career/Delete?id={id1}", [("Career.Id", str(id1))], f"/Career/Delete?id={id1}")
expect(R, "Carreras/Delete: responde 302 a /Career con toast de éxito", res, status=302, redirect="/Career", toast="Carrera eliminada correctamente.", toast_kind="success")
DEL_OK = cstatus(id1) == 2
check(R, "Carreras/Delete: la baja lógica SE GUARDA en la BD (Status=2) tras mostrar el toast de éxito", DEL_OK, f"Status en BD={cstatus(id1)} (esperado 2)")
check(R, "Carreras/Index: la carrera dada de baja desaparece del listado", not in_listing(id1, tag), "")

# comportamiento sobre una carrera YA eliminada: si la baja no funciona se usa la carrera creada con Status=2 (Create la acepta)
id_e = id1 if DEL_OK else K.id_by("Careers", "Name", "Carrera Estado2 " + tag)
if not DEL_OK:
    K.skip("Carreras: comportamiento de GET/POST sobre carrera eliminada usando la baja real", "Career/Delete no persiste (FindAsync sin tracking, sin Attach); se sustituye por una carrera creada con Status=2")
check(R, "Carreras/prep: carrera eliminada disponible para las pruebas de 404", id_e and cstatus(id_e) == 2, f"id={id_e} Status={cstatus(id_e)}")
check(R, "Carreras/Index: la carrera eliminada no aparece en el listado", not in_listing(id_e, tag), "")
for p in (f"/Career/Edit?id={id_e}", f"/Career/Details?id={id_e}", f"/Career/Delete?id={id_e}"):
    st, _ = c.get(p)
    check(R, f"Carreras: GET {p} de la carrera eliminada -> 404", st == 404, f"status={st}")
res = post(c, "baja de eliminada", f"/Career/Delete?id={id_e}", [("Career.Id", str(id_e))], "/Career/Create")
check(R, "Carreras/Delete: baja de una carrera ya eliminada (plan: redirige a Index sin mensaje)", res.status == 302 and not res.toasts, f"status={res.status} location={res.location} toasts={res.toasts[:1]}")
res = post(c, "baja id inexistente", "/Career/Delete?id=99999", [("Career.Id", "99999")], "/Career/Create")
check(R, "Carreras/Delete: id inexistente 99999 (plan: redirige a Index sin mensaje)", res.status == 302 and not res.toasts, f"status={res.status} location={res.location} toasts={res.toasts[:1]}")
NOTES.append("Carreras/Delete: con id inexistente o ya eliminado responde 302 a /Career sin ningún mensaje (los otros maestros responden 404).")
res = post(c, "POST edit eliminada", f"/Career/Edit?id={id_e}", [("Input.Id", str(id_e)), ("Input.Name", "Revivir " + K.rnd()), ("Input.Status", "0")], "/Career/Create")
check(R, "Carreras/Edit: POST sobre carrera eliminada -> 404 y no la revive", res.status == 404 and cstatus(id_e) == 2, f"status={res.status} Status={cstatus(id_e)}")

# ---- id inexistente / negativo / no numérico ----
bad = ("99999", "-1", "0", "abc", "1.5", "99999999999", "2147483648", "")
for ruta in ("Edit", "Delete", "Details"):
    for bid in bad:
        st, _ = c.get(f"/Career/{ruta}?id={bid}")
        check(R, f"Carreras: GET /Career/{ruta}?id={bid!r} -> 404", st == 404, f"status={st}")
    st, _ = c.get(f"/Career/{ruta}")
    check(R, f"Carreras: GET /Career/{ruta} (sin id) -> 404", st == 404, f"status={st}")
for ruta in ("Edit", "Delete"):
    for bid in ("99999", "-1", "abc"):
        res = post(c, f"POST {ruta} {bid}", f"/Career/{ruta}?id={bid}", [("Input.Id", bid if bid.lstrip('-').isdigit() else "0"), ("Input.Name", "Zzz Fantasma"), ("Input.Status", "0")], "/Career/Create")
        esperado = 404 if (ruta == "Edit" or not bid.lstrip('-').isdigit()) else 302
        check(R, f"Carreras: POST /Career/{ruta}?id={bid} -> {esperado}", res.status == esperado, f"status={res.status}")

# ---- sin sesión / sin token ----
K.guard_anon("Carreras", ["/Career", "/Career/Create", f"/Career/Edit?id={id_h}", f"/Career/Delete?id={id_h}", f"/Career/Details?id={id_h}"],
             ["/Career/Create", f"/Career/Edit?id={id_h}", f"/Career/Delete?id={id_h}"])
K.guard_token(c, "Carreras", ["/Career/Create", f"/Career/Edit?id={id_h}", f"/Career/Delete?id={id_h}"])
check(R, "Carreras: tras los POST sin token la carrera hermana sigue Activa y con su nombre", cstatus(id_h) == 0 and cname(id_h) == other, "")

raise SystemExit(K.finish(NOTES))
