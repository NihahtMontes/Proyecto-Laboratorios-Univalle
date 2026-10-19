"""Ciudades: Create / Edit / Delete / Details + 404 + sin sesión + sin token. Solo contra QA."""
import http_cases_maestros_common as K
from http_cases_maestros_common import R, sub, post, check, expect

c = K.client()
BOL = 1                                    # Bolivia (país Activo del seed)
P2n = K.uniq("Pais Ciudades")              # segundo país (misma ciudad en otro país)
PDn = K.uniq("Pais ParaBaja")              # país que se da de baja para probar CountryId eliminado
tag = K.rnd(7)


def cname(i):
    return K.dbv("select Name from Cities where Id=?", i)


def cstatus(i):
    return K.dbv("select Status from Cities where Id=?", i)


def in_listing(i, term):
    st, lst = c.get(f"/Cities?SearchTerm={term}")
    return st == 200 and f"/Cities/Details/{i}" in lst


# ---- preparación ----
expect(R, "Ciudades/prep: país 2", sub(c, "pais2", "/Countries/Create", {"Input.Name": P2n}), status=302)
P2 = K.id_by("Countries", "Name", P2n)
expect(R, "Ciudades/prep: país para baja", sub(c, "paisD", "/Countries/Create", {"Input.Name": PDn}), status=302)
PD = K.id_by("Countries", "Name", PDn)
post(c, "baja PD", f"/Countries/Delete/{PD}", [("Country.Id", str(PD))], f"/Countries/Delete/{PD}")
check(R, "Ciudades/prep: países creados y PD eliminado", P2 and PD and K.dbv("select Status from Countries where Id=?", PD) == 2, f"P2={P2} PD={PD}")

# ---- GET de las pantallas ----
st, pg = c.get("/Cities")
check(R, "Ciudades/Index: 200 'Maestro de Localidades' y botón 'Añadir Ciudad'", st == 200 and "Maestro de Localidades" in pg and "Ciudad" in pg, f"status={st}")
st, pg = c.get("/Cities/Create")
check(R, "Ciudades/Create GET: 200; no ofrece países eliminados en el desplegable", st == 200 and PDn not in pg and "Bolivia" in pg, f"status={st}")

# ---- Create: validaciones ----
expect(R, "Ciudades/Create: país vacío", sub(c, "país vacío", "/Cities/Create", {"Input.CountryId": "", "Input.Name": "Algo " + tag}), status=200, error="obligatorio")
res = sub(c, "país no numérico", "/Cities/Create", {"Input.CountryId": "abc", "Input.Name": "Algo " + tag})
check(R, "Ciudades/Create: país no numérico 'abc' -> 200 con mensaje", res.status == 200 and bool(res.errors), f"status={res.status} errores={res.errors[:2]}")
res = sub(c, "país inexistente", "/Cities/Create", {"Input.CountryId": "99999", "Input.Name": "Fantasma " + tag})
check(R, "Ciudades/Create: país inexistente 99999 -> 200 sin crear nada", res.status == 200 and K.dbv("select count(*) from Cities where Name=?", "Fantasma " + tag) == 0, f"status={res.status}")
check(R, "Ciudades/Create: país inexistente muestra un mensaje al usuario (si no, falla en silencio)", bool(res.errors) or bool(res.toasts), f"errores={res.errors[:2]} toasts={res.toasts[:2]}")
res = sub(c, "país eliminado", "/Cities/Create", {"Input.CountryId": str(PD), "Input.Name": "Baja " + tag})
check(R, "Ciudades/Create: país eliminado no crea la ciudad", K.dbv("select count(*) from Cities where Name=?", "Baja " + tag) == 0, f"status={res.status}")
check(R, "Ciudades/Create: país eliminado muestra un mensaje al usuario (si no, falla en silencio)", bool(res.errors) or bool(res.toasts), f"status={res.status} errores={res.errors[:2]} toasts={res.toasts[:2]}")
expect(R, "Ciudades/Create: nombre vacío", sub(c, "nombre vacío", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": ""}), status=200, error="El nombre de la ciudad es obligatorio")
expect(R, "Ciudades/Create: nombre solo espacios", sub(c, "nombre espacios", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": "    "}), status=200, error="El nombre de la ciudad es obligatorio")
expect(R, "Ciudades/Create: nombre 101 caracteres", sub(c, "nombre 101", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": "x" * 101}), status=200, error="no puede superar los 100")
expect(R, "Ciudades/Create: región 101 caracteres", sub(c, "región 101", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": "Reg " + tag, "Input.Region": "r" * 101}), status=200, error="no puede superar los 100")

# ---- Create: válidos y límites ----
n1 = "Ciudad Unica " + tag
expect(R, "Ciudades/Create: válido (con región)", sub(c, "crear n1", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": n1, "Input.Region": "Valle QA"}), status=302, redirect="/Cities",
       toast=f"Ciudad '{n1}' registrada exitosamente en Bolivia.", toast_kind="success")
id1 = K.id_by("Cities", "Name", n1)
check(R, "Ciudades/Create: quedó Activa en la BD con región y auditoría", id1 and cstatus(id1) == 0 and K.dbv("select Region from Cities where Id=?", id1) == "Valle QA" and K.dbv("select CreatedById from Cities where Id=?", id1) is not None, f"id={id1}")
check(R, "Ciudades/Index: la ciudad creada aparece en el listado (búsqueda por nombre)", in_listing(id1, tag), "")
n100 = ("Cdad" + tag + "x" * 100)[:100]
expect(R, "Ciudades/Create: nombre de 100 caracteres y región de 100 (límite exacto)", sub(c, "crear 100", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": n100, "Input.Region": "r" * 100}), status=302, redirect="/Cities")
expect(R, "Ciudades/Create: espacios alrededor y en medio se normalizan ('  Ciudad   Espacios X  ')", sub(c, "espacios", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": f"  Ciudad   Espacios {tag}  "}), status=302)
check(R, "Ciudades/Create: nombre guardado sin espacios sobrantes", K.dbv("select count(*) from Cities where Name=?", f"Ciudad Espacios {tag}") == 1, "")
for etiqueta, nom in (("html", f"<b>QAc{tag}</b>"), ("comillas", f"Ciudad \"Q\" O'Brien {tag}"), ("tildes", f"Potosí Ñandú Árbol {tag}")):
    res = sub(c, f"crear {etiqueta}", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": nom})
    check(R, f"Ciudades/Create: caracteres especiales ({etiqueta}) sin excepción", res.status in (200, 302), f"status={res.status} errores={res.errors[:1]}")
st, lst = c.get(f"/Cities?SearchTerm={tag}")
check(R, "Ciudades/Index: el HTML hostil se muestra escapado", st == 200 and f"<b>QAc{tag}</b>" not in lst, "")

# ---- Create: duplicados ----
expect(R, "Ciudades/Create: duplicado exacto en el mismo país", sub(c, "dup exacto", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": n1}), status=200, error="Esta ciudad ya se encuentra registrada para el país seleccionado")
expect(R, "Ciudades/Create: duplicado con otra capitalización y espacios laterales", sub(c, "dup caps", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": f"  {n1.upper()}  "}), status=200, error="Esta ciudad ya se encuentra registrada para el país seleccionado")
base_d = "Ciudad Doble " + tag
sub(c, "crear base doble", "/Cities/Create", {"Input.CountryId": str(P2), "Input.Name": base_d})
res = sub(c, "dup espacio doble", "/Cities/Create", {"Input.CountryId": str(P2), "Input.Name": base_d.replace(" ", "  ")})
check(R, "Ciudades/Create: duplicado con espacios dobles internos ('Ciudad  Doble') debe detectarse (se guarda normalizado a un espacio)", res.has_error("Esta ciudad ya se encuentra registrada"),
      f"status={res.status} (302 = se creó un duplicado) cuenta en BD de '{base_d}' en ese país={K.dbv('select count(*) from Cities where Name=? and CountryId=?', base_d, P2)}")
expect(R, "Ciudades/Create: la misma ciudad en OTRO país se permite", sub(c, "otro país", "/Cities/Create", {"Input.CountryId": str(P2), "Input.Name": n1}), status=302, redirect="/Cities",
       toast=f"Ciudad '{n1}' registrada exitosamente en {P2n}.", toast_kind="success")
id1b = K.dbv("select top 1 Id from Cities where Name=? and CountryId=? order by Id desc", n1, P2)

# ---- Details / Edit GET ----
st, pg = c.get(f"/Cities/Details/{id1}")
check(R, "Ciudades/Details: 200 con el nombre y el país", st == 200 and n1 in pg and "Bolivia" in pg, f"status={st}")
st, pg = c.get(f"/Cities/Edit/{id1}")
check(R, "Ciudades/Edit GET: 200 con el nombre actual", st == 200 and n1 in pg, f"status={st}")
st, pg = c.get(f"/Cities/Delete/{id1}")
check(R, "Ciudades/Delete GET: 200 y confirmación '¿Deshabilitar esta localidad?'", st == 200 and "Deshabilitar esta localidad" in pg, f"status={st}")

# ---- Edit: validaciones por la respuesta ----
expect(R, "Ciudades/Edit: nombre vacío", sub(c, "edit vacío", f"/Cities/Edit/{id1}", {"City.Name": ""}), status=200, error="El nombre de la ciudad es obligatorio")
expect(R, "Ciudades/Edit: nombre 101 caracteres (mensaje por defecto)", sub(c, "edit 101", f"/Cities/Edit/{id1}", {"City.Name": "x" * 101}), status=200, error="100")
expect(R, "Ciudades/Edit: región 101 caracteres (mensaje por defecto)", sub(c, "edit región 101", f"/Cities/Edit/{id1}", {"City.Region": "r" * 101}), status=200, error="100")
expect(R, "Ciudades/Edit: país vacío", sub(c, "edit país vacío", f"/Cities/Edit/{id1}", {"City.CountryId": ""}), status=200, error="obligatorio")
other_n = "Ciudad Hermana " + tag
sub(c, "crear hermana", "/Cities/Create", {"Input.CountryId": str(BOL), "Input.Name": other_n})
id_h = K.id_by("Cities", "Name", other_n)
expect(R, "Ciudades/Edit: nombre duplicado en el mismo país (de otra ciudad)", sub(c, "edit dup", f"/Cities/Edit/{id1}", {"City.Name": other_n.upper()}), status=200, error="Esta ciudad ya se encuentra registrada para el país seleccionado")
expect(R, "Ciudades/Edit: mover la ciudad al país donde ya existe una igual -> duplicado", sub(c, "edit mover dup", f"/Cities/Edit/{id1b}", {"City.CountryId": str(BOL)}), status=200, error="Esta ciudad ya se encuentra registrada para el país seleccionado")
expect(R, "Ciudades/Edit: guardar sin cambios (no es duplicado de sí misma)", sub(c, "edit self", f"/Cities/Edit/{id1}", {}), status=302, redirect="/Cities", toast=f"Localidad '{n1}' actualizada exitosamente.", toast_kind="success")
res = sub(c, "edit id inexistente", f"/Cities/Edit/{id1}", {"City.Id": "99999", "City.Name": "Nueva " + K.rnd()})
check(R, "Ciudades/Edit: POST con City.Id inexistente en el cuerpo -> 404", res.status == 404, f"status={res.status}")
res = sub(c, "edit estado 7", f"/Cities/Edit/{id1}", {"City.Status": "7"})
check(R, "Ciudades/Edit: estado inválido 7 no se guarda (200, sin redirect)", res.status == 200, f"status={res.status} errores={res.errors[:1]}")

# ---- PERSISTENCIA de Edit ----
nuevo = "Ciudad Cambiada " + tag
res = sub(c, "edit persistencia", f"/Cities/Edit/{id1}", {"City.Name": nuevo, "City.Region": "Region Nueva", "City.Status": "1"})
expect(R, "Ciudades/Edit: válido -> redirige con toast de éxito", res, status=302, redirect="/Cities", toast=f"Localidad '{nuevo}' actualizada exitosamente.", toast_kind="success")
PERSISTS = cname(id1) == nuevo
check(R, "Ciudades/Edit: el cambio de nombre, región y estado SE GUARDA en la BD (tras mostrar el toast de éxito)",
      PERSISTS and K.dbv("select Region from Cities where Id=?", id1) == "Region Nueva" and cstatus(id1) == 1,
      f"BD: nombre={cname(id1)!r} (enviado {nuevo!r}) región={K.dbv('select Region from Cities where Id=?', id1)!r} Status={cstatus(id1)} (enviado 1)")
if PERSISTS:
    check(R, "Ciudades/Edit: ciudad Inactiva sigue visible en el listado", in_listing(id1, tag), "")
    sub(c, "reactivar", f"/Cities/Edit/{id1}", {"City.Status": "0"})
    sub(c, "mover de país", f"/Cities/Edit/{id1}", {"City.CountryId": str(P2), "City.Name": "Ciudad Movida " + tag})
    check(R, "Ciudades/Edit: cambio de país se guarda", K.dbv("select CountryId from Cities where Id=?", id1) == P2, "")
else:
    K.skip("Ciudades/Edit: reactivar, cambio de país, ciudad Inactiva en listado", "Cities/Edit no persiste los cambios (FindAsync sin tracking, sin Attach)")

# ---- Delete ----
cur = cname(id1b)
expect(R, "Ciudades/Delete sin token: 400", K.post_notoken(c, "delete sin token", f"/Cities/Delete/{id1b}", []), status=400)
check(R, "Ciudades/Delete sin token: la ciudad sigue Activa", cstatus(id1b) == 0, "")
expect(R, "Ciudades/Edit sin token: 400", K.post_notoken(c, "edit sin token", f"/Cities/Edit/{id1b}", [("City.Id", str(id1b)), ("City.Name", "Hack"), ("City.CountryId", str(P2)), ("City.Status", "0")]), status=400)
res = post(c, "delete id1b", f"/Cities/Delete/{id1b}", [("City.Id", str(id1b))], f"/Cities/Delete/{id1b}")
expect(R, "Ciudades/Delete: baja lógica con toast", res, status=302, redirect="/Cities", toast=f"La localidad '{cur}' ha sido dada de baja del sistema.", toast_kind="success")
check(R, "Ciudades/Delete: BD Status=2 (la fila sigue existiendo)", cstatus(id1b) == 2, f"Status={cstatus(id1b)}")
check(R, "Ciudades/Index: la ciudad dada de baja desaparece del listado", not in_listing(id1b, tag), "")
for p in (f"/Cities/Edit/{id1b}", f"/Cities/Details/{id1b}", f"/Cities/Delete/{id1b}"):
    st, _ = c.get(p)
    check(R, f"Ciudades: GET {p} de la ciudad eliminada -> 404", st == 404, f"status={st}")
res = post(c, "segunda baja", f"/Cities/Delete/{id1b}", [("City.Id", str(id1b))], "/Cities/Create")
check(R, "Ciudades/Delete: segunda baja -> 404", res.status == 404, f"status={res.status} toasts={res.toasts[:1]}")
res = post(c, "POST edit eliminada", f"/Cities/Edit/{id1b}", [("City.Id", str(id1b)), ("City.CountryId", str(P2)), ("City.Name", "Revivir " + K.rnd()), ("City.Status", "0")], "/Cities/Create")
check(R, "Ciudades/Edit: POST sobre ciudad eliminada -> 404 y no la revive", res.status == 404 and cstatus(id1b) == 2, f"status={res.status} Status={cstatus(id1b)}")
res = sub(c, "recrear tras baja", "/Cities/Create", {"Input.CountryId": str(P2), "Input.Name": n1})
check(R, "Ciudades/Create: recrear con el nombre de una ciudad dada de baja se permite", res.status == 302, f"status={res.status} errores={res.errors[:1]}")

# ---- id inexistente / negativo / no numérico ----
K.guard_404(c, "Ciudades", ["/Cities/Edit/{id}", "/Cities/Delete/{id}", "/Cities/Details/{id}"])
for p in ("Edit", "Delete"):
    for bid in ("99999", "-1", "abc"):
        res = post(c, f"POST {p} {bid}", f"/Cities/{p}/{bid}", [("City.Id", bid if bid.lstrip('-').isdigit() else "0"), ("City.CountryId", str(BOL)), ("City.Name", "Zzz Fantasma"), ("City.Status", "0")], "/Cities/Create")
        check(R, f"Ciudades: POST /Cities/{p}/{bid} -> 404", res.status == 404, f"status={res.status}")

# ---- sin sesión / sin token ----
K.guard_anon("Ciudades", ["/Cities", "/Cities/Create", f"/Cities/Edit/{id_h}", f"/Cities/Delete/{id_h}", f"/Cities/Details/{id_h}"],
             ["/Cities/Create", f"/Cities/Edit/{id_h}", f"/Cities/Delete/{id_h}"])
K.guard_token(c, "Ciudades", ["/Cities/Create", f"/Cities/Edit/{id_h}", f"/Cities/Delete/{id_h}"])
check(R, "Ciudades: tras los POST sin token la ciudad hermana sigue Activa y con su nombre", cstatus(id_h) == 0 and cname(id_h) == other_n, "")

raise SystemExit(K.finish())
