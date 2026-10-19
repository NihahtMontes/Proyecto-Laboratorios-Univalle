"""Laboratorios: Create / Edit / Delete / Details + 404 + sin sesión + sin token. Solo contra QA."""
import re

import http_cases_maestros_common as K
from http_cases_maestros_common import R, sub, post, check, expect

c = K.client()
tag = K.rnd(7)
NOTES = []
FAC1, FAC2 = 1, 2                          # facultades Activas del seed


def lname(i):
    return K.dbv("select Name from Laboratories where Id=?", i)


def lstatus(i):
    return K.dbv("select Status from Laboratories where Id=?", i)


def in_listing(i, term):
    st, lst = c.get(f"/Laboratories?SearchTerm={term}")
    return st == 200 and f"/Laboratories/Details/{i}" in lst


# facultad dada de baja para probar FacultyId eliminado
fdn = "Facultad ParaLab " + tag
sub(c, "crear facultad baja", "/Faculties/Create", {"Input.Name": fdn})
FD = K.id_by("Faculties", "Name", fdn)
post(c, "baja facultad", f"/Faculties/Delete/{FD}", [("Faculty.Id", str(FD))], f"/Faculties/Delete/{FD}")
check(R, "Laboratorios/prep: facultad eliminada creada", FD and K.dbv("select Status from Faculties where Id=?", FD) == 2, f"FD={FD}")

# ---- pantallas ----
st, pg = c.get("/Laboratories")
check(R, "Laboratorios/Index: 200 con las 3 pestañas", st == 200 and "Centros de Laboratorio" in pg and "Unidades Acad" in pg and "Carreras" in pg, f"status={st}")
st, pg = c.get("/Laboratories/Create")
check(R, "Laboratorios/Create GET: 200 y no ofrece facultades eliminadas", st == 200 and fdn not in pg, f"status={st}")

# ---- Create: validaciones ----
expect(R, "Laboratorios/Create: facultad vacía", sub(c, "facultad vacía", "/Laboratories/Create", {"Input.FacultyId": "", "Input.Name": "Lab " + tag}), status=200, error="obligatori")
res = sub(c, "facultad abc", "/Laboratories/Create", {"Input.FacultyId": "abc", "Input.Name": "Lab " + tag})
check(R, "Laboratorios/Create: facultad no numérica 'abc' -> 200 con mensaje", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
res = sub(c, "facultad inexistente", "/Laboratories/Create", {"Input.FacultyId": "99999", "Input.Name": "Fantasma " + tag})
check(R, "Laboratorios/Create: facultad inexistente 99999 se rechaza con un mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]} creado={K.dbv('select count(*) from Laboratories where Name=?', 'Fantasma ' + tag)}")
res = sub(c, "facultad eliminada", "/Laboratories/Create", {"Input.FacultyId": str(FD), "Input.Name": "DeBaja " + tag})
check(R, "Laboratorios/Create: facultad dada de baja se rechaza (no se crea el laboratorio)", K.dbv("select count(*) from Laboratories where Name=?", "DeBaja " + tag) == 0, f"status={res.status} errores={res.errors[:2]}")
check(R, "Laboratorios/Create: facultad dada de baja se rechaza con un mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
expect(R, "Laboratorios/Create: nombre vacío", sub(c, "nombre vacío", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": ""}), status=200, error="El nombre del laboratorio es obligatorio")
expect(R, "Laboratorios/Create: nombre solo espacios", sub(c, "nombre espacios", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": "    "}), status=200, error="El nombre del laboratorio es obligatorio")
expect(R, "Laboratorios/Create: nombre 101 caracteres", sub(c, "nombre 101", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": "x" * 101}), status=200, error="no puede superar los 100")
res = sub(c, "piso 51", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": "Piso51 " + tag, "Input.Floor": "p" * 51})
check(R, "Laboratorios/Create: Piso de 51 caracteres se rechaza con un mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]} creado={K.dbv('select count(*) from Laboratories where Name=?', 'Piso51 ' + tag)}")
res = sub(c, "descripción 1001", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": "Desc1001 " + tag, "Input.Description": "d" * 1001})
check(R, "Laboratorios/Create: Descripción de 1001 caracteres se rechaza con un mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]} creado={K.dbv('select count(*) from Laboratories where Name=?', 'Desc1001 ' + tag)}")

# ---- Create: válidos y límites ----
n1 = "Lab QA " + tag
res = sub(c, "crear n1", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": n1, "Input.Floor": "Planta 2", "Input.Description": "Ambiente de prueba"})
expect(R, "Laboratorios/Create: válido -> /Laboratories", res, status=302, redirect="/Laboratories")
id1 = K.id_by("Laboratories", "Name", n1)
code1 = K.dbv("select Code from Laboratories where Id=?", id1)
check(R, "Laboratorios/Create: toast 'Laboratorio 'LAB-…' (nombre) registrado exitosamente en el catálogo.'", res.has_toast(f"({n1}) registrado exitosamente en el catálogo", "success") and res.has_toast(f"'{code1}'"), f"toasts={res.toasts[:1]} code={code1}")
check(R, "Laboratorios/Create: código automático LAB-<n>, Activo, piso y descripción guardados", bool(re.fullmatch(r"LAB-\d+", code1 or "")) and lstatus(id1) == 0 and K.dbv("select Floor from Laboratories where Id=?", id1) == "Planta 2", f"Code={code1}")
check(R, "Laboratorios/Index: el laboratorio creado aparece en el listado", in_listing(id1, tag), "")
expect(R, "Laboratorios/Create: Piso de 50 y Descripción de 1000 (límites exactos)", sub(c, "límites", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": "Limites " + tag, "Input.Floor": "p" * 50, "Input.Description": "d" * 1000}), status=302, redirect="/Laboratories")
n100 = ("Lab" + tag + "x" * 100)[:100]
expect(R, "Laboratorios/Create: nombre de 100 caracteres (límite exacto)", sub(c, "crear 100", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": n100}), status=302, redirect="/Laboratories")
expect(R, "Laboratorios/Create: espacios se normalizan", sub(c, "espacios", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": f"  Lab   Esp {tag}  "}), status=302)
check(R, "Laboratorios/Create: nombre guardado sin espacios sobrantes", K.dbv("select count(*) from Laboratories where Name=?", f"Lab Esp {tag}") == 1, "")
for etiqueta, nom in (("html", f"<b>QAl{tag}</b>"), ("comillas", f"Lab \"Q\" O'Brien {tag}"), ("tildes", f"Laboratorio Ñandú Cocción {tag}")):
    res = sub(c, f"crear {etiqueta}", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": nom, "Input.Description": "<script>x</script>"})
    check(R, f"Laboratorios/Create: caracteres especiales ({etiqueta}) sin excepción", res.status in (200, 302), f"status={res.status} errores={res.errors[:1]}")
st, lst = c.get(f"/Laboratories?SearchTerm={tag}")
check(R, "Laboratorios/Index: el HTML hostil se muestra escapado", st == 200 and f"<b>QAl{tag}</b>" not in lst and "<script>x</script>" not in lst, "")

# ---- Create: duplicados ----
expect(R, "Laboratorios/Create: duplicado exacto en la misma facultad", sub(c, "dup exacto", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": n1}), status=200, error="Ya existe un ambiente con este nombre en la facultad seleccionada")
expect(R, "Laboratorios/Create: duplicado con otra capitalización y espacios laterales", sub(c, "dup caps", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": f"  {n1.upper()}  "}), status=200, error="Ya existe un ambiente con este nombre en la facultad seleccionada")
base_d = "Lab Doble " + tag
sub(c, "crear base doble", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": base_d})
res = sub(c, "dup espacio doble", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": base_d.replace(" ", "  ")})
check(R, "Laboratorios/Create: duplicado con espacios dobles internos debe detectarse (se guarda normalizado a un espacio)", res.has_error("Ya existe un ambiente"),
      f"status={res.status} (302 = se creó un duplicado) cuenta en BD de '{base_d}'={K.dbv('select count(*) from Laboratories where Name=?', base_d)}")
res = sub(c, "mismo nombre otra facultad", "/Laboratories/Create", {"Input.FacultyId": str(FAC2), "Input.Name": n1})
check(R, "Laboratorios/Create: el mismo nombre en OTRA facultad se permite", res.status == 302, f"status={res.status} errores={res.errors[:1]}")
id1b = K.dbv("select top 1 Id from Laboratories where Name=? and FacultyId=? order by Id desc", n1, FAC2)

# ---- GET de pantallas ----
st, pg = c.get(f"/Laboratories/Details/{id1}")
check(R, "Laboratorios/Details: 200 'Hoja Técnica del Laboratorio' con nombre y código", st == 200 and "Hoja T" in pg and n1 in pg and code1 in pg, f"status={st}")
st, pg = c.get(f"/Laboratories/Edit/{id1}")
check(R, "Laboratorios/Edit GET: 200 con el nombre actual", st == 200 and n1 in pg, f"status={st}")
st, pg = c.get(f"/Laboratories/Delete/{id1}")
check(R, "Laboratorios/Delete GET: 200 y '¿Retirar Laboratorio?'", st == 200 and "Retirar Laboratorio" in pg, f"status={st}")

# ---- Edit: validaciones por la respuesta ----
expect(R, "Laboratorios/Edit: nombre vacío (el mensaje de Edit es 'El nombre descriptivo es obligatorio')", sub(c, "edit vacío", f"/Laboratories/Edit/{id1}", {"Input.Name": ""}), status=200, error="El nombre descriptivo es obligatorio")
expect(R, "Laboratorios/Edit: nombre 101 caracteres", sub(c, "edit 101", f"/Laboratories/Edit/{id1}", {"Input.Name": "x" * 101}), status=200, error="no puede superar los 100")
expect(R, "Laboratorios/Edit: facultad vacía", sub(c, "edit facultad vacía", f"/Laboratories/Edit/{id1}", {"Input.FacultyId": ""}), status=200, error="obligatori")
res = sub(c, "edit piso 51", f"/Laboratories/Edit/{id1}", {"Input.Floor": "p" * 51})
check(R, "Laboratorios/Edit: Piso de 51 caracteres se rechaza con mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
res = sub(c, "edit descripción 1001", f"/Laboratories/Edit/{id1}", {"Input.Description": "d" * 1001})
check(R, "Laboratorios/Edit: Descripción de 1001 caracteres se rechaza con mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
res = sub(c, "edit facultad inexistente", f"/Laboratories/Edit/{id1}", {"Input.FacultyId": "99999"})
check(R, "Laboratorios/Edit: facultad inexistente 99999 se rechaza con mensaje (no excepción 500)", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
other = "Lab Hermano " + tag
sub(c, "crear hermano", "/Laboratories/Create", {"Input.FacultyId": str(FAC1), "Input.Name": other})
id_h = K.id_by("Laboratories", "Name", other)
expect(R, "Laboratorios/Edit: nombre duplicado en la misma facultad", sub(c, "edit dup", f"/Laboratories/Edit/{id1}", {"Input.Name": " " + other.upper() + " "}), status=200, error="Ya existe otro ambiente con este nombre en la facultad seleccionada")
expect(R, "Laboratorios/Edit: guardar sin cambios (no es duplicado de sí mismo)", sub(c, "edit self", f"/Laboratories/Edit/{id1}", {}), status=302, redirect="/Laboratories",
       toast=f"Datos del Laboratorio '{code1}' ({n1}) actualizados correctamente.", toast_kind="success")
res = sub(c, "edit id inexistente", f"/Laboratories/Edit/{id1}", {"Input.Id": "99999", "Input.Name": "Nuevo " + K.rnd()})
check(R, "Laboratorios/Edit: POST con Input.Id inexistente en el cuerpo -> 404", res.status == 404, f"status={res.status}")
res = sub(c, "edit estado 7", f"/Laboratories/Edit/{id1}", {"Input.Status": "7"})
check(R, "Laboratorios/Edit: estado inválido 7 no se guarda (200, sin redirect)", res.status == 200, f"status={res.status} errores={res.errors[:1]}")

# ---- PERSISTENCIA de Edit + código inmutable ----
nuevo = "Lab Cambiado " + tag
res = sub(c, "edit persistencia", f"/Laboratories/Edit/{id1}", {"Input.Name": nuevo, "Input.Floor": "Planta 9", "Input.Description": "Otra", "Input.Status": "1", "Input.Code": "HACK-001"})
expect(R, "Laboratorios/Edit: válido -> redirige a /Laboratories con toast", res, status=302, redirect="/Laboratories", toast=f"({nuevo}) actualizados correctamente", toast_kind="success")
PERSISTS = lname(id1) == nuevo
check(R, "Laboratorios/Edit: el cambio de nombre, piso, descripción y estado SE GUARDA en la BD (tras mostrar el toast de éxito)",
      PERSISTS and K.dbv("select Floor from Laboratories where Id=?", id1) == "Planta 9" and lstatus(id1) == 1,
      f"BD: nombre={lname(id1)!r} (enviado {nuevo!r}) Piso={K.dbv('select Floor from Laboratories where Id=?', id1)!r} Status={lstatus(id1)} (enviado 1)")
check(R, "Laboratorios/Edit: el código LAB-n es inmutable (no se puede sobrescribir por POST)", K.dbv("select Code from Laboratories where Id=?", id1) == code1, f"Code={K.dbv('select Code from Laboratories where Id=?', id1)}")
if PERSISTS:
    sub(c, "reactivar", f"/Laboratories/Edit/{id1}", {"Input.Status": "0"})
    sub(c, "mover facultad", f"/Laboratories/Edit/{id1}", {"Input.FacultyId": str(FAC2), "Input.Name": nuevo})
    check(R, "Laboratorios/Edit: cambio de facultad se guarda", K.dbv("select FacultyId from Laboratories where Id=?", id1) == FAC2, "")
else:
    K.skip("Laboratorios/Edit: reactivar, cambio de facultad", "Laboratories/Edit no persiste los cambios (FindAsync sin tracking, sin Attach)")

# ---- Delete ----
cur = lname(id1b)
expect(R, "Laboratorios/Delete sin token: 400", K.post_notoken(c, "delete sin token", f"/Laboratories/Delete/{id_h}", []), status=400)
check(R, "Laboratorios/Delete sin token: el laboratorio sigue Activo", lstatus(id_h) == 0, "")
expect(R, "Laboratorios/Edit sin token: 400", K.post_notoken(c, "edit sin token", f"/Laboratories/Edit/{id_h}", [("Input.Id", str(id_h)), ("Input.Name", "Hack"), ("Input.FacultyId", "1"), ("Input.Status", "0")]), status=400)
res = post(c, "delete id1b", f"/Laboratories/Delete/{id1b}", [("Laboratory.Id", str(id1b))], f"/Laboratories/Delete/{id1b}")
expect(R, "Laboratorios/Delete: baja lógica con toast", res, status=302, redirect="/Laboratories", toast=f"El laboratorio '{cur}' ha sido dado de baja del sistema institucional.", toast_kind="success")
check(R, "Laboratorios/Delete: BD Status=2 (la fila sigue existiendo)", lstatus(id1b) == 2, f"Status={lstatus(id1b)}")
check(R, "Laboratorios/Index: el laboratorio dado de baja desaparece del listado", not in_listing(id1b, tag), "")
for p in (f"/Laboratories/Edit/{id1b}", f"/Laboratories/Details/{id1b}", f"/Laboratories/Delete/{id1b}"):
    st, _ = c.get(p)
    check(R, f"Laboratorios: GET {p} del laboratorio eliminado -> 404", st == 404, f"status={st}")
res = post(c, "segunda baja", f"/Laboratories/Delete/{id1b}", [("Laboratory.Id", str(id1b))], "/Laboratories/Create")
check(R, "Laboratorios/Delete: segunda baja -> 404", res.status == 404, f"status={res.status} toasts={res.toasts[:1]}")
res = post(c, "POST edit eliminado", f"/Laboratories/Edit/{id1b}", [("Input.Id", str(id1b)), ("Input.FacultyId", str(FAC2)), ("Input.Name", "Revivir " + K.rnd()), ("Input.Status", "0")], "/Laboratories/Create")
check(R, "Laboratorios/Edit: POST sobre laboratorio eliminado -> 404 y no lo revive", res.status == 404 and lstatus(id1b) == 2, f"status={res.status} Status={lstatus(id1b)}")
res = sub(c, "recrear tras baja", "/Laboratories/Create", {"Input.FacultyId": str(FAC2), "Input.Name": n1})
check(R, "Laboratorios/Create: recrear con el nombre de un laboratorio dado de baja se permite", res.status == 302, f"status={res.status} errores={res.errors[:1]}")

# ---- id inexistente / negativo / no numérico ----
K.guard_404(c, "Laboratorios", ["/Laboratories/Edit/{id}", "/Laboratories/Delete/{id}", "/Laboratories/Details/{id}"])
for p in ("Edit", "Delete"):
    for bid in ("99999", "-1", "abc"):
        res = post(c, f"POST {p} {bid}", f"/Laboratories/{p}/{bid}", [("Input.Id", bid if bid.lstrip('-').isdigit() else "0"), ("Input.FacultyId", "1"), ("Input.Name", "Zzz Fantasma"), ("Input.Status", "0")], "/Laboratories/Create")
        check(R, f"Laboratorios: POST /Laboratories/{p}/{bid} -> 404", res.status == 404, f"status={res.status}")

# ---- sin sesión / sin token ----
K.guard_anon("Laboratorios", ["/Laboratories", "/Laboratories/Create", f"/Laboratories/Edit/{id_h}", f"/Laboratories/Delete/{id_h}", f"/Laboratories/Details/{id_h}"],
             ["/Laboratories/Create", f"/Laboratories/Edit/{id_h}", f"/Laboratories/Delete/{id_h}"])
K.guard_token(c, "Laboratorios", ["/Laboratories/Create", f"/Laboratories/Edit/{id_h}", f"/Laboratories/Delete/{id_h}"])
check(R, "Laboratorios: tras los POST sin token el laboratorio hermano sigue Activo y con su nombre", lstatus(id_h) == 0 and lname(id_h) == other, "")

raise SystemExit(K.finish(NOTES))
