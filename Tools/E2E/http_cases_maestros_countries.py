"""Países: Edit / Delete / Details + 404 + sin sesión + sin token (Create ya cubierto en http_cases_maestros_paises.py). Solo contra QA."""
import re

import http_cases_maestros_common as K
from http_cases_maestros_common import R, sub, post, check, expect

c = K.client()
A = K.uniq("Pais Edicion")
B = K.uniq("Pais Estado")
C3 = K.uniq("Pais Token")


def name_of(i):
    return K.dbv("select Name from Countries where Id=?", i)


def status_of(i):
    return K.dbv("select Status from Countries where Id=?", i)


# ---- preparación ----
expect(R, "Países/Create (preparación A)", sub(c, "crear A", "/Countries/Create", {"Input.Name": A}), status=302, redirect="/Countries")
idA = K.id_by("Countries", "Name", A)
expect(R, "Países/Create (preparación B)", sub(c, "crear B", "/Countries/Create", {"Input.Name": B}), status=302, redirect="/Countries")
idB = K.id_by("Countries", "Name", B)
expect(R, "Países/Create (preparación C3)", sub(c, "crear C3", "/Countries/Create", {"Input.Name": C3}), status=302, redirect="/Countries")
idC3 = K.id_by("Countries", "Name", C3)
check(R, "Países: ids de preparación obtenidos de la BD QA", idA and idB and idC3, f"A={idA} B={idB} C3={idC3}")

# ---- Details / Edit / Delete (GET) ----
st, page = c.get(f"/Countries/Details/{idA}")
check(R, "Países/Details: 200 y 'Expediente de País: <nombre>'", st == 200 and "Expediente de" in page and A in page, f"status={st}")
st, page = c.get(f"/Countries/Edit/{idA}")
check(R, "Países/Edit GET: 200 con el nombre actual", st == 200 and A in page, f"status={st}")
st, page = c.get(f"/Countries/Delete/{idA}")
check(R, "Países/Delete GET: 200 y pregunta de confirmación '¿Retirar este país?'", st == 200 and "Retirar este pa" in page, f"status={st}")

# ---- Edit: validaciones del servidor (se juzgan por la respuesta) ----
expect(R, "Países/Edit: nombre vacío", sub(c, "edit vacío", f"/Countries/Edit/{idA}", {"Country.Name": ""}), status=200, error="obligatorio")
expect(R, "Países/Edit: solo espacios", sub(c, "edit espacios", f"/Countries/Edit/{idA}", {"Country.Name": "     "}), status=200, error="obligatorio")
expect(R, "Países/Edit: 101 caracteres (mensaje por defecto, sin traducir)", sub(c, "edit 101", f"/Countries/Edit/{idA}", {"Country.Name": "x" * 101}), status=200, error="100")
expect(R, "Países/Edit: duplicado ' BOLIVIA ' (otra capitalización y espacios)", sub(c, "edit dup", f"/Countries/Edit/{idA}", {"Country.Name": " BOLIVIA "}), status=200, error="Ya existe un país")
res = sub(c, "edit estado 7", f"/Countries/Edit/{idA}", {"Country.Status": "7"})
check(R, "Países/Edit: estado inválido 7 por POST no se guarda (200, sin redirect)", res.status == 200, f"status={res.status}")
check(R, "Países/Edit: el rechazo del estado inválido 7 muestra algún mensaje al usuario (menor: no alcanzable desde la UI)", bool(res.errors), f"errores visibles={res.errors[:2]} (Edit.cshtml no tiene validation-for en Country.Status)")
expect(R, "Países/Edit: guardar sin cambios (el propio nombre no es duplicado)", sub(c, "edit self", f"/Countries/Edit/{idA}", {"Country.Name": A}), status=302, redirect="/Countries", toast=f"País '{A}' actualizado correctamente.", toast_kind="success")
expect(R, "Países/Edit: nombre de otro país eliminado ('Peru QA') no bloquea", sub(c, "edit dup eliminado", f"/Countries/Edit/{idB}", {"Country.Name": "Peru QA"}), status=302, redirect="/Countries")
nuevo = K.uniq("Pais Renombrado")
res = sub(c, "edit id inexistente en el cuerpo", f"/Countries/Edit/{idA}", {"Country.Id": "99999", "Country.Name": nuevo})
check(R, "Países/Edit: POST con Country.Id inexistente en el cuerpo -> 404", res.status == 404, f"status={res.status}")
for nom in ["<b>QAedit</b>", 'Pais "comillas" O\'Brien', "Perú Ñandú Árbol", "Z"]:
    res = sub(c, f"edit especial {nom}", f"/Countries/Edit/{idB}", {"Country.Name": nom})
    check(R, f"Países/Edit: nombre {nom!r} se acepta o rechaza sin excepción (nunca 500)", res.status in (200, 302), f"status={res.status} errores={res.errors[:1]}")

# ---- PERSISTENCIA: ¿Edit guarda de verdad lo que dice que guardó? ----
nombre_nuevo = K.uniq("Pais Persistido")
res = sub(c, "edit persistencia", f"/Countries/Edit/{idB}", {"Country.Name": nombre_nuevo, "Country.Status": "1"})
expect(R, "Países/Edit: válido -> redirige a /Countries con toast de éxito", res, status=302, redirect="/Countries", toast=f"País '{nombre_nuevo}' actualizado correctamente.", toast_kind="success")
PERSISTS = name_of(idB) == nombre_nuevo
check(R, "Países/Edit: el cambio de nombre y estado SE GUARDA en la BD (tras mostrar el toast de éxito)", PERSISTS and status_of(idB) == 1,
      f"BD: nombre={name_of(idB)!r} (enviado {nombre_nuevo!r}), Status={status_of(idB)} (enviado 1)")
if PERSISTS:
    st, pg = c.get("/Cities/Create")
    check(R, "Ciudades/Create: el país Inactivo no aparece en el desplegable", st == 200 and nombre_nuevo not in pg, "")
    sub(c, "reactivar B", f"/Countries/Edit/{idB}", {"Country.Status": "0"})
    st, pg = c.get("/Cities/Create")
    check(R, "Ciudades/Create: el país reactivado aparece", st == 200 and nombre_nuevo in pg, "")
    sub(c, "edit upper", f"/Countries/Edit/{idB}", {"Country.Name": nombre_nuevo.upper()})
    check(R, "Países/Edit: mismo nombre en mayúsculas (self) se guarda", name_of(idB) == nombre_nuevo.upper(), name_of(idB))
    sub(c, "edit 1 char", f"/Countries/Edit/{idB}", {"Country.Name": "Z"})
    check(R, "Países/Edit: 1 carácter se rechaza como en Create (mínimo 2)", name_of(idB) == nombre_nuevo.upper(), f"BD={name_of(idB)!r}")
    # forzar Status=2 por Edit
    sub(c, "edit estado 2", f"/Countries/Edit/{idB}", {"Country.Name": B, "Country.Status": "2"})
    check(R, "Países/Edit: forzar Status=2 por POST no debe permitirse (la baja se hace en Delete)", status_of(idB) != 2, f"Status BD={status_of(idB)}")
else:
    for n in ("Ciudades/Create: país Inactivo/reactivado en el desplegable", "Países/Edit: mayúsculas, 1 carácter, 100 caracteres (persistencia del nombre)", "Países/Edit: forzar Status=2 por POST"):
        K.skip(n, "Países/Edit no persiste los cambios (FindAsync sin tracking, sin Attach)")
    # la respuesta de Status=2 sí es observable:
    res = sub(c, "edit estado 2", f"/Countries/Edit/{idC3}", {"Country.Status": "2"})
    check(R, "Países/Edit: Status=2 por POST responde sin excepción (302 con toast de éxito si se aceptara)", res.status in (200, 302), f"status={res.status} toasts={res.toasts[:1]}")

# ---- Delete ----
city = K.uniq("Ciudad Huerfana")
expect(R, "Ciudades/Create (ciudad bajo A, para probar baja con dependientes)", sub(c, "crear ciudad", "/Cities/Create", {"Input.CountryId": str(idA), "Input.Name": city}), status=302, redirect="/Cities")
idCity = K.id_by("Cities", "Name", city)

expect(R, "Países/Delete sin token: 400", K.post_notoken(c, "delete sin token", f"/Countries/Delete/{idC3}", []), status=400)
check(R, "Países/Delete sin token: el país no cambió de estado", status_of(idC3) == 0, f"Status={status_of(idC3)}")
expect(R, "Países/Edit sin token: 400", K.post_notoken(c, "edit sin token", f"/Countries/Edit/{idC3}", [("Country.Id", str(idC3)), ("Country.Name", "Hack"), ("Country.Status", "0")]), status=400)
check(R, "Países/Edit sin token: el nombre no cambió", name_of(idC3) == C3, "")

# Decisión del usuario 2026-10-18: un país con ciudades activas no se da de baja (antes se ocultaban en cascada).
res = post(c, "delete A con ciudad activa", f"/Countries/Delete/{idA}", [("Country.Id", str(idA))], f"/Countries/Delete/{idA}")
check(R, "Países/Delete: con ciudades activas se bloquea con aviso y el país sigue Activo", res.has_toast("ciudad(es) activa(s)", "warning") and status_of(idA) == 0, f"toasts={res.toasts[:1]} Status={status_of(idA)}")
post(c, "baja de la ciudad de A", f"/Cities/Delete/{idCity}", [], f"/Cities/Delete/{idCity}")
check(R, "Ciudades/Delete: la ciudad dependiente se da de baja primero", K.dbv("select Status from Cities where Id=?", idCity) == 2, "")
cur = name_of(idA)
res = post(c, "delete A", f"/Countries/Delete/{idA}", [("Country.Id", str(idA))], f"/Countries/Delete/{idA}")
expect(R, "Países/Delete: baja lógica con toast", res, status=302, redirect="/Countries", toast=f"El país '{cur}' ha sido dado de baja correctamente.", toast_kind="success")
check(R, "Países/Delete: BD Status=2 (la fila sigue existiendo)", status_of(idA) == 2, f"Status={status_of(idA)}")
st, lst = c.get(f"/Countries?SearchTerm={A.split()[-1]}")
check(R, "Países/Index: el país dado de baja desaparece del listado", st == 200 and f"/Countries/Details/{idA}" not in lst, "")
for p in (f"/Countries/Edit/{idA}", f"/Countries/Details/{idA}", f"/Countries/Delete/{idA}"):
    st, _ = c.get(p)
    check(R, f"Países: GET {p} del país eliminado -> 404", st == 404, f"status={st}")
res = post(c, "segunda baja de A", f"/Countries/Delete/{idA}", [("Country.Id", str(idA))], "/Countries/Create")
check(R, "Países/Delete: segunda baja -> 404 (no 500 ni segundo mensaje de éxito)", res.status == 404, f"status={res.status} toasts={res.toasts[:1]}")
res2 = post(c, "POST edit país eliminado", f"/Countries/Edit/{idA}", [("Country.Id", str(idA)), ("Country.Name", "Revivir " + K.rnd()), ("Country.Status", "0")], "/Countries/Create")
check(R, "Países/Edit: POST sobre un país eliminado -> 404 y no lo revive", res2.status == 404 and status_of(idA) == 2, f"status={res2.status} Status BD={status_of(idA)}")

# ciudad dependiente: ya se dio de baja antes que el país (no quedan ciudades activas de países eliminados)
st, pg = c.get(f"/Cities/Details/{idCity}")
check(R, "Ciudades/Details de la ciudad dada de baja -> 404", st == 404, f"status={st}")

# ---- id inexistente / negativo / no numérico ----
K.guard_404(c, "Países", ["/Countries/Edit/{id}", "/Countries/Delete/{id}", "/Countries/Details/{id}"])
for p in ("Edit", "Delete"):
    for bid in ("99999", "-1", "abc"):
        res = post(c, f"POST {p} {bid}", f"/Countries/{p}/{bid}", [("Country.Id", bid if bid.lstrip('-').isdigit() else "0"), ("Country.Name", "Zzz Fantasma"), ("Country.Status", "0")], "/Countries/Create")
        check(R, f"Países: POST /Countries/{p}/{bid} -> 404", res.status == 404, f"status={res.status}")

# ---- sin sesión / sin token ----
K.guard_anon("Países", ["/Countries", "/Countries/Create", f"/Countries/Edit/{idC3}", f"/Countries/Delete/{idC3}", f"/Countries/Details/{idC3}"],
             ["/Countries/Create", f"/Countries/Edit/{idC3}", f"/Countries/Delete/{idC3}"])
K.guard_token(c, "Países", ["/Countries/Create", f"/Countries/Edit/{idC3}", f"/Countries/Delete/{idC3}"])
check(R, "Países: tras los POST sin token el país C3 sigue Activo y con su nombre", status_of(idC3) == 0 and name_of(idC3) == C3, "")

# ---- duplicado con espacios dobles internos (Create) ----
dbase = K.uniq("Pais Doble")
sub(c, "crear base doble", "/Countries/Create", {"Input.Name": dbase})
res = sub(c, "dup espacio doble", "/Countries/Create", {"Input.Name": dbase.replace(" ", "  ")})
check(R, "Países/Create: duplicado con espacios dobles internos debe detectarse (se guarda normalizado a un espacio)", res.has_error("Ya existe un país"),
      f"status={res.status} (302 = se creó un duplicado) cuenta en BD de '{dbase}'={K.dbv('select count(*) from Countries where Name=?', dbase)}")

raise SystemExit(K.finish())
