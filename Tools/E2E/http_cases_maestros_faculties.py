"""Facultades: Create / Edit / Delete / Details + 404 + sin sesión + sin token. Solo contra QA."""
import re

import http_cases_maestros_common as K
from http_cases_maestros_common import R, sub, post, check, expect

c = K.client()
tag = K.rnd(7)
NOTES = []


def fname(i):
    return K.dbv("select Name from Faculties where Id=?", i)


def fstatus(i):
    return K.dbv("select Status from Faculties where Id=?", i)


def in_listing(i, term):
    st, lst = c.get(f"/Laboratories?SearchTerm={term}")
    return st == 200 and f"/Faculties/Details/{i}" in lst


# ---- Index redirige ----
st, loc = K.anon("GET", "/Faculties")
check(R, "Facultades/Index sin sesión -> 302 /Login", st == 302 and "/login" in (loc or "").lower(), f"status={st} location={loc}")
r = c.raw_post("/Faculties", [], follow=False, token_from="/Faculties/Create")
st2, page = c.get("/Faculties")
check(R, "Facultades/Index con sesión termina en /Laboratories (pestaña facultades) con 200", st2 == 200 and "Unidades Acad" in page, f"status={st2}")

# ---- Create: validaciones ----
expect(R, "Facultades/Create: nombre vacío", sub(c, "vacío", "/Faculties/Create", {"Input.Name": ""}), status=200, error="El nombre de la facultad es obligatorio")
expect(R, "Facultades/Create: solo espacios", sub(c, "espacios", "/Faculties/Create", {"Input.Name": "    "}), status=200, error="El nombre de la facultad es obligatorio")
expect(R, "Facultades/Create: nombre 151 caracteres", sub(c, "151", "/Faculties/Create", {"Input.Name": "x" * 151}), status=200, error="no puede superar los 150")
expect(R, "Facultades/Create: descripción 501 caracteres", sub(c, "desc 501", "/Faculties/Create", {"Input.Name": "Fac " + tag, "Input.Description": "d" * 501}), status=200, error="no puede superar los 500")

# ---- Create: válidos ----
f1 = "Facultad QA " + tag
res = sub(c, "crear f1", "/Faculties/Create", {"Input.Name": f1, "Input.Description": "Descripción de prueba"})
expect(R, "Facultades/Create: válida -> /Laboratories#faculties", res, status=302, redirect="/Laboratories#faculties")
id1 = K.id_by("Faculties", "Name", f1)
code1 = K.dbv("select Code from Faculties where Id=?", id1)
check(R, "Facultades/Create: toast 'Unidad Académica 'FAC-…' (nombre) registrada exitosamente.'", res.has_toast(f"({f1}) registrada exitosamente", "success") and res.has_toast(f"'{code1}'"), f"toasts={res.toasts[:1]} code={code1}")
check(R, "Facultades/Create: código automático con formato FAC-<n> y estado Activo", bool(re.fullmatch(r"FAC-\d+", code1 or "")) and fstatus(id1) == 0, f"Code={code1} Status={fstatus(id1)}")
check(R, "Facultades/Index (pestaña): la facultad creada aparece en el listado", in_listing(id1, tag), "")
n150 = ("Fac" + tag + "x" * 150)[:150]
expect(R, "Facultades/Create: nombre de 150 caracteres y descripción de 500 (límite exacto)", sub(c, "crear 150", "/Faculties/Create", {"Input.Name": n150, "Input.Description": "d" * 500}), status=302, redirect="/Laboratories#faculties")
expect(R, "Facultades/Create: espacios se normalizan ('  Fac   Esp  ')", sub(c, "espacios", "/Faculties/Create", {"Input.Name": f"  Fac   Esp {tag}  "}), status=302)
check(R, "Facultades/Create: nombre guardado sin espacios sobrantes", K.dbv("select count(*) from Faculties where Name=?", f"Fac Esp {tag}") == 1, "")
for etiqueta, nom in (("html", f"<b>QAf{tag}</b>"), ("comillas", f"Fac \"Q\" O'Brien {tag}"), ("tildes", f"Facultad Ñandú Educación {tag}")):
    res = sub(c, f"crear {etiqueta}", "/Faculties/Create", {"Input.Name": nom, "Input.Description": "<script>x</script> \"q\" 'q'"})
    check(R, f"Facultades/Create: caracteres especiales ({etiqueta}) sin excepción", res.status in (200, 302), f"status={res.status} errores={res.errors[:1]}")
st, lst = c.get(f"/Laboratories?SearchTerm={tag}")
check(R, "Facultades/Index: el HTML hostil se muestra escapado", st == 200 and f"<b>QAf{tag}</b>" not in lst and "<script>x</script>" not in lst, "")

# ---- Create: duplicados ----
expect(R, "Facultades/Create: duplicado exacto", sub(c, "dup exacto", "/Faculties/Create", {"Input.Name": f1}), status=200, error="Ya existe una facultad con este nombre en el sistema institucional")
expect(R, "Facultades/Create: duplicado con otra capitalización y espacios laterales", sub(c, "dup caps", "/Faculties/Create", {"Input.Name": f"  {f1.upper()}  "}), status=200, error="Ya existe una facultad con este nombre en el sistema institucional")
base_d = "Facultad Doble " + tag
sub(c, "crear base doble", "/Faculties/Create", {"Input.Name": base_d})
res = sub(c, "dup espacio doble", "/Faculties/Create", {"Input.Name": base_d.replace(" ", "  ")})
check(R, "Facultades/Create: duplicado con espacios dobles internos debe detectarse (se guarda normalizado a un espacio)", res.has_error("Ya existe una facultad"),
      f"status={res.status} (302 = se creó un duplicado) cuenta en BD de '{base_d}'={K.dbv('select count(*) from Faculties where Name=?', base_d)}")

# ---- GET de pantallas ----
st, pg = c.get(f"/Faculties/Details/{id1}")
check(R, "Facultades/Details: 200 con nombre y código", st == 200 and f1 in pg and code1 in pg, f"status={st}")
st, pg = c.get(f"/Faculties/Edit/{id1}")
check(R, "Facultades/Edit GET: 200 con el nombre actual", st == 200 and f1 in pg, f"status={st}")
st, pg = c.get(f"/Faculties/Delete/{id1}")
check(R, "Facultades/Delete GET: 200 y '¿Retirar Unidad Académica?'", st == 200 and "Retirar Unidad Acad" in pg, f"status={st}")

# ---- Edit: validaciones por la respuesta ----
expect(R, "Facultades/Edit: nombre vacío", sub(c, "edit vacío", f"/Faculties/Edit/{id1}", {"Faculty.Name": ""}), status=200, error="El nombre de la facultad es obligatorio")
expect(R, "Facultades/Edit: nombre 201 caracteres (mensaje por defecto)", sub(c, "edit 201", f"/Faculties/Edit/{id1}", {"Faculty.Name": "x" * 201}), status=200, error="200")
res = sub(c, "edit 151", f"/Faculties/Edit/{id1}", {"Faculty.Name": "Fac" + tag + "y" * 150})
NOTES.append(f"Facultades/Edit con nombre de 153 caracteres: status={res.status} (Create rechaza >150; Edit usa el límite del modelo, 200) -> {'inconsistencia de límites' if res.status == 302 else 'rechazado'}")
check(R, "Facultades/Edit: nombre de 151-200 caracteres se trata igual que en Create (rechazado, límite 150)", res.status == 200, f"status={res.status} errores={res.errors[:1]}")
expect(R, "Facultades/Edit: descripción 501 caracteres (mensaje por defecto)", sub(c, "edit desc 501", f"/Faculties/Edit/{id1}", {"Faculty.Description": "d" * 501}), status=200, error="500")
other = "Facultad Hermana " + tag
sub(c, "crear hermana", "/Faculties/Create", {"Input.Name": other})
id_h = K.id_by("Faculties", "Name", other)
expect(R, "Facultades/Edit: nombre duplicado de otra facultad", sub(c, "edit dup", f"/Faculties/Edit/{id1}", {"Faculty.Name": " " + other.upper() + " "}), status=200, error="Ya existe otra facultad con este nombre en el sistema institucional")
expect(R, "Facultades/Edit: guardar sin cambios (no es duplicado de sí misma)", sub(c, "edit self", f"/Faculties/Edit/{id1}", {}), status=302, redirect="/Laboratories#faculties",
       toast=f"Datos de la Facultad '{code1}' ({f1}) actualizados correctamente.", toast_kind="success")
res = sub(c, "edit id inexistente", f"/Faculties/Edit/{id1}", {"Faculty.Id": "99999", "Faculty.Name": "Nueva " + K.rnd()})
check(R, "Facultades/Edit: POST con Faculty.Id inexistente en el cuerpo -> 404", res.status == 404, f"status={res.status}")
res = sub(c, "edit estado 7", f"/Faculties/Edit/{id1}", {"Faculty.Status": "7"})
check(R, "Facultades/Edit: estado inválido 7 no se guarda (200, sin redirect)", res.status == 200, f"status={res.status} errores={res.errors[:1]}")

# ---- PERSISTENCIA de Edit + código inmutable ----
nuevo = "Facultad Cambiada " + tag
res = sub(c, "edit persistencia", f"/Faculties/Edit/{id1}", {"Faculty.Name": nuevo, "Faculty.Description": "Otra descripción", "Faculty.Status": "1", "Faculty.Code": "HACK-001"})
expect(R, "Facultades/Edit: válido -> redirige a /Laboratories#faculties con toast", res, status=302, redirect="/Laboratories#faculties", toast=f"({nuevo}) actualizados correctamente", toast_kind="success")
PERSISTS = fname(id1) == nuevo
check(R, "Facultades/Edit: el cambio de nombre, descripción y estado SE GUARDA en la BD (tras mostrar el toast de éxito)",
      PERSISTS and K.dbv("select Description from Faculties where Id=?", id1) == "Otra descripción" and fstatus(id1) == 1,
      f"BD: nombre={fname(id1)!r} (enviado {nuevo!r}) Status={fstatus(id1)} (enviado 1) descripción={K.dbv('select Description from Faculties where Id=?', id1)!r}")
check(R, "Facultades/Edit: el código FAC-n es inmutable (no se puede sobrescribir por POST)", K.dbv("select Code from Faculties where Id=?", id1) == code1, f"Code={K.dbv('select Code from Faculties where Id=?', id1)}")
if PERSISTS:
    sub(c, "reactivar", f"/Faculties/Edit/{id1}", {"Faculty.Status": "0"})
    st, pg = c.get("/Laboratories/Create")
    check(R, "Laboratorios/Create: la facultad reactivada aparece en el desplegable", st == 200 and nuevo in pg, "")
else:
    K.skip("Facultades/Edit: estado Inactivo oculta la facultad en Laboratorios/Create y reactivación", "Faculties/Edit no persiste los cambios (FindAsync sin tracking, sin Attach)")

# ---- Delete con dependientes (laboratorio y carrera) ----
fd = "Facultad ConHijos " + tag
sub(c, "crear fd", "/Faculties/Create", {"Input.Name": fd})
idd = K.id_by("Faculties", "Name", fd)
labn = "Lab Hijo " + tag
career_n = "Carrera Hija " + tag
r1 = sub(c, "crear lab hijo", "/Laboratories/Create", {"Input.FacultyId": str(idd), "Input.Name": labn})
r2 = sub(c, "crear carrera hija", "/Career/Create", {"Input.Name": career_n, "Input.FacultadId": str(idd)})
id_lab = K.id_by("Laboratories", "Name", labn)
id_car = K.id_by("Careers", "Name", career_n)
check(R, "Facultades/prep: laboratorio y carrera hijos creados", id_lab and id_car, f"lab={id_lab} carrera={id_car}")

expect(R, "Facultades/Delete sin token: 400", K.post_notoken(c, "delete sin token", f"/Faculties/Delete/{id_h}", []), status=400)
check(R, "Facultades/Delete sin token: la facultad sigue Activa", fstatus(id_h) == 0, "")
expect(R, "Facultades/Edit sin token: 400", K.post_notoken(c, "edit sin token", f"/Faculties/Edit/{id_h}", [("Faculty.Id", str(id_h)), ("Faculty.Name", "Hack"), ("Faculty.Status", "0")]), status=400)

cur = fname(idd)
res = post(c, "delete fd", f"/Faculties/Delete/{idd}", [("Faculty.Id", str(idd))], f"/Faculties/Delete/{idd}")
expect(R, "Facultades/Delete: baja lógica con toast", res, status=302, redirect="/Laboratories#faculties", toast=f"La Facultad académica '{cur}' ha sido dada de baja del sistema institucional.", toast_kind="success")
check(R, "Facultades/Delete: BD Status=2 (la fila sigue existiendo)", fstatus(idd) == 2, f"Status={fstatus(idd)}")
check(R, "Facultades/Index: la facultad dada de baja desaparece del listado", not in_listing(idd, tag), "")
for p in (f"/Faculties/Edit/{idd}", f"/Faculties/Details/{idd}", f"/Faculties/Delete/{idd}"):
    st, _ = c.get(p)
    check(R, f"Facultades: GET {p} de la facultad eliminada -> 404", st == 404, f"status={st}")
res = post(c, "segunda baja", f"/Faculties/Delete/{idd}", [("Faculty.Id", str(idd))], "/Faculties/Create")
check(R, "Facultades/Delete: segunda baja -> 404", res.status == 404, f"status={res.status} toasts={res.toasts[:1]}")
res = post(c, "POST edit eliminada", f"/Faculties/Edit/{idd}", [("Faculty.Id", str(idd)), ("Faculty.Name", "Revivir " + K.rnd()), ("Faculty.Status", "0")], "/Faculties/Create")
check(R, "Facultades/Edit: POST sobre facultad eliminada -> 404 y no la revive", res.status == 404 and fstatus(idd) == 2, f"status={res.status} Status={fstatus(idd)}")
res = sub(c, "recrear tras baja", "/Faculties/Create", {"Input.Name": fd})
check(R, "Facultades/Create: recrear con el nombre de una facultad dada de baja se permite", res.status == 302, f"status={res.status} errores={res.errors[:1]}")

# dependientes tras la baja de la facultad
check(R, "Facultades/Delete: no valida dependientes (laboratorio y carrera siguen Activos en la BD)",
      K.dbv("select Status from Laboratories where Id=?", id_lab) == 0 and K.dbv("select Status from Careers where Id=?", id_car) == 0, "")
st, _ = c.get(f"/Laboratories/Details/{id_lab}")
check(R, "Laboratorios/Details de un laboratorio Activo cuya facultad fue dada de baja -> 200 (el plan dice que pasan a 'Área General')", st == 200, f"status={st}")
st, _ = c.get(f"/Laboratories/Edit/{id_lab}")
check(R, "Laboratorios/Edit de un laboratorio Activo cuya facultad fue dada de baja -> 200", st == 200, f"status={st}")
st, lst = c.get(f"/Laboratories?SearchTerm={tag}")
check(R, "Laboratorios/Index: el laboratorio Activo cuya facultad fue dada de baja sigue en el listado", st == 200 and f"/Laboratories/Details/{id_lab}" in lst, f"status={st}")
st, _ = c.get(f"/Career/Details?id={id_car}")
check(R, "Carreras/Details de una carrera Activa cuya facultad fue dada de baja -> 200", st == 200, f"status={st}")
st, _ = c.get(f"/Career/Edit?id={id_car}")
check(R, "Carreras/Edit de una carrera Activa cuya facultad fue dada de baja -> 200", st == 200, f"status={st}")

# ---- id inexistente / negativo / no numérico ----
K.guard_404(c, "Facultades", ["/Faculties/Edit/{id}", "/Faculties/Delete/{id}", "/Faculties/Details/{id}"])
for p in ("Edit", "Delete"):
    for bid in ("99999", "-1", "abc"):
        res = post(c, f"POST {p} {bid}", f"/Faculties/{p}/{bid}", [("Faculty.Id", bid if bid.lstrip('-').isdigit() else "0"), ("Faculty.Name", "Zzz Fantasma"), ("Faculty.Status", "0")], "/Faculties/Create")
        check(R, f"Facultades: POST /Faculties/{p}/{bid} -> 404", res.status == 404, f"status={res.status}")

# ---- sin sesión / sin token ----
K.guard_anon("Facultades", ["/Faculties/Create", f"/Faculties/Edit/{id_h}", f"/Faculties/Delete/{id_h}", f"/Faculties/Details/{id_h}"],
             ["/Faculties/Create", f"/Faculties/Edit/{id_h}", f"/Faculties/Delete/{id_h}"])
K.guard_token(c, "Facultades", ["/Faculties/Create", f"/Faculties/Edit/{id_h}", f"/Faculties/Delete/{id_h}"])
check(R, "Facultades: tras los POST sin token la facultad hermana sigue Activa y con su nombre", fstatus(id_h) == 0 and fname(id_h) == other, "")

raise SystemExit(K.finish(NOTES))
