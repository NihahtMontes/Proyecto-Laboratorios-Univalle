"""
Catálogos / Categorías de artículos (y, por parametrización, Unidades de medida): reglas del servidor por HTTP.
Solo contra la copia QA. Nombres con prefijo 'QAC'. La suite genérica `catalog_suite` la reutiliza http_cases_catalogos_units.py.
"""
import re
import urllib.parse

import e2e_lib
from http_cases_catalogos_common import (anon, check, check_404, check_anon, expect, post_404, run_module, scalar, sql, tag,
                                         tokenless, uniq)

CATEGORIES = dict(
    key="ArticleCategories", table="ArticleCategories", base="/Catalogs/ArticleCategories", noun="categoría", prefix="QAC Cat",
    maxlen=100, req_msg="El nombre de la categoría es obligatorio", max_msg="no puede superar los 100",
    dup_create="Ya existe una categoría con este nombre", dup_edit="Ya existe otra categoría",
    created_toast="registrada exitosamente", updated_toast="actualizada correctamente",
    blocked_msg="No se puede retirar la categoría", deleted_msg="ha sido dada de baja",
    recreate_msg="ya existe un registro con el mismo nombre", article_field="Input.ArticleCategoryId",
    other_field="Input.MeasurementUnitId", other_table="MeasurementUnits",
)


def _row(cfg, item_id):
    r = sql(f"select Id, Name, Code, Status from {cfg['table']} where Id=?", item_id)
    return tuple(r[0]) if r else None


def _id_by_name(cfg, name):
    return scalar(f"select top 1 Id from {cfg['table']} where Name=? order by Id desc", name)


def catalog_suite(R, c, cfg):
    base, label = cfg["base"], cfg["key"]
    L = lambda s: f"{label}/{s}"  # noqa: E731
    mk = lambda txt: f"{cfg['prefix']} {txt}"  # noqa: E731

    # ------------------------------------------------------------ acceso sin sesión
    for p in (base, f"{base}/Create", f"{base}/Edit/1", f"{base}/Delete/1"):
        check_anon(R, label, p)
    check_anon(R, label, f"{base}/Create", post=True)
    check_anon(R, label, f"{base}/Delete/1", post=True)

    # ------------------------------------------------------------ crear: validaciones
    expect(R, L("Create: vacío"), c.submit(f"{base}/Create", {"Input.Name": ""}), status=200, error=cfg["req_msg"])
    expect(R, L("Create: solo espacios"), c.submit(f"{base}/Create", {"Input.Name": "    "}), status=200, error=cfg["req_msg"])
    expect(R, L(f"Create: {cfg['maxlen']+1} caracteres"), c.submit(f"{base}/Create", {"Input.Name": "x" * (cfg["maxlen"] + 1)}),
           status=200, error=cfg["max_msg"])

    # longitud máxima exacta: válida
    exact = (mk(tag()) + " " + "y" * cfg["maxlen"])[:cfg["maxlen"]]
    r = c.submit(f"{base}/Create", {"Input.Name": exact})
    expect(R, L(f"Create: {cfg['maxlen']} caracteres exactos (válido)"), r, status=302, redirect=base, toast=cfg["created_toast"], toast_kind="success")
    exact_id = _id_by_name(cfg, exact.strip())

    # válido
    NAME = uniq(cfg["prefix"])
    r = c.submit(f"{base}/Create", {"Input.Name": NAME})
    expect(R, L("Create: válido"), r, status=302, redirect=base, toast=cfg["created_toast"], toast_kind="success")
    cid = _id_by_name(cfg, NAME)
    row = _row(cfg, cid) if cid else None
    check(R, L("Create: fila en BD con nombre, código y Activo"), row and row[1] == NAME and row[2] and row[3] == 0, str(row))
    m = re.search(r"'([A-Z]+-\d+)'", r.toasts[0][1]) if r.toasts else None
    check(R, L("Create: el toast cita el código asignado por el sistema"), bool(m) and row and m.group(1) == row[2], f"toast={r.toasts[:1]} codigo={row and row[2]}")

    # duplicados
    expect(R, L("Create: duplicado con otra capitalización y espacios"), c.submit(f"{base}/Create", {"Input.Name": f"  {NAME.upper()}  "}),
           status=200, error=cfg["dup_create"])
    expect(R, L("Create: duplicado con espacios internos dobles (mismo nombre tras limpiar)"),
           c.submit(f"{base}/Create", {"Input.Name": NAME.replace(" ", "  ")}), status=200, error="ya existe")
    n_same = scalar(f"select count(*) from {cfg['table']} where Name=?", NAME)
    check(R, L("Create: tras los duplicados sigue habiendo una sola fila con el nombre"), n_same == 1, f"filas={n_same}")

    # espacios sobrantes se limpian
    messy = f"  {cfg['prefix']}   {tag()}    x  "
    r = c.submit(f"{base}/Create", {"Input.Name": messy})
    expect(R, L("Create: espacios sobrantes (se limpian)"), r, status=302, redirect=base, toast=cfg["created_toast"])
    cleaned = re.sub(r"\s+", " ", messy.strip())
    messy_id = _id_by_name(cfg, cleaned)
    check(R, L("Create: el nombre se guarda sin espacios sobrantes"), messy_id is not None, f"buscado={cleaned!r}")

    # caracteres especiales
    html_name = f"<b>{cfg['prefix']}</b> {tag()}"
    r = c.submit(f"{base}/Create", {"Input.Name": html_name})
    html_ok = r.status == 302
    expect(R, L("Create: nombre con HTML se acepta sin excepción"), r, status=302, redirect=base)
    html_id = _id_by_name(cfg, html_name)
    if html_id:
        q = urllib.parse.urlencode({"SearchTerm": html_name.split("</b> ")[1]})
        _, page = c.get(f"{base}?{q}")
        check(R, L("Index: el HTML hostil se muestra escapado (no se ejecuta)"), html_name not in page and "&lt;b&gt;" in page,
              "contiene <b> crudo" if html_name in page else "")
    acc = f"{tag()} Pañuelo ñandú áéíóú «{cfg['prefix']}» \"comillas\" 'simples'"
    acc = acc[:cfg["maxlen"]]
    r = c.submit(f"{base}/Create", {"Input.Name": acc})
    expect(R, L("Create: tildes, ñ, comillas y símbolos"), r, status=302, redirect=base)
    acc_id = _id_by_name(cfg, acc)
    check(R, L("Create: el nombre con tildes/ñ/comillas se guarda idéntico (UTF-8)"), acc_id is not None, f"buscado={acc!r}")

    # ------------------------------------------------------------ listado
    q = urllib.parse.urlencode({"SearchTerm": NAME})
    _, page = c.get(f"{base}?{q}")
    check(R, L("Index: la búsqueda por nombre encuentra el registro"), f"{base}/Edit/{cid}" in page)
    _, page = c.get(f"{base}?{urllib.parse.urlencode({'SearchTerm': NAME.upper()})}")
    check(R, L("Index: la búsqueda no distingue mayúsculas"), f"{base}/Edit/{cid}" in page)
    for odd in ("%", "'", "<script>alert(1)</script>", "x" * 500, "[a-z]"):
        st, _ = c.get(f"{base}?{urllib.parse.urlencode({'SearchTerm': odd})}")
        check(R, L(f"Index: SearchTerm {odd[:20]!r} responde 200"), st == 200, f"status={st}")
    st, _ = c.get(f"{base}?pageIndex=99999")
    check(R, L("Index: pageIndex=99999 responde 200"), st == 200, f"status={st}")
    st, _ = c.get(f"{base}?pageIndex=-3")
    check(R, L("Index: pageIndex=-3 responde 200"), st == 200, f"status={st}")
    st, _ = c.get(f"{base}?StatusFilter=abc")
    check(R, L("Index: StatusFilter=abc no rompe (200)"), st == 200, f"status={st}")

    # ------------------------------------------------------------ editar
    action, fields = c.form(f"{base}/Edit/{cid}")
    print(f"[{label}] campos Edit:", [k for k, _ in fields if k != "__RequestVerificationToken"])
    NEW = uniq(cfg["prefix"])
    r = c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW})
    expect(R, L("Edit: renombrar (válido)"), r, status=302, redirect=base, toast=cfg["updated_toast"], toast_kind="success")
    check(R, L("Edit: nombre nuevo en BD"), (_row(cfg, cid) or (0, 0))[1] == NEW)
    expect(R, L("Edit: guardar sin cambios (no choca consigo mismo)"), c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW}), status=302, redirect=base)
    expect(R, L("Edit: solo cambia la capitalización de su propio nombre"), c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW.upper()}), status=302, redirect=base)
    expect(R, L("Edit: vacío"), c.submit(f"{base}/Edit/{cid}", {"Input.Name": ""}), status=200, error=cfg["req_msg"])
    expect(R, L(f"Edit: {cfg['maxlen']+1} caracteres"), c.submit(f"{base}/Edit/{cid}", {"Input.Name": "x" * (cfg["maxlen"] + 1)}), status=200, error=cfg["max_msg"])
    other = _row(cfg, messy_id)[1] if messy_id else None
    if other:
        expect(R, L("Edit: nombre de otro registro (otra capitalización y espacios)"),
               c.submit(f"{base}/Edit/{cid}", {"Input.Name": f" {other.upper()} "}), status=200, error=cfg["dup_edit"])
    # estados
    expect(R, L("Edit: Inactivo (válido)"), c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW, "Input.Status": "1"}), status=302, redirect=base)
    check(R, L("Edit: Inactivo queda en BD"), (_row(cfg, cid) or (0, 0, 0, -1))[3] == 1)
    expect(R, L("Edit: volver a Activo"), c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW, "Input.Status": "0"}), status=302, redirect=base)
    r = c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW, "Input.Status": "2"})
    expect(R, L("Edit: Estado Eliminado forzado por POST"), r, status=200, error="El estado Eliminado sólo se gestiona desde la opción Dar de Baja.")
    check(R, L("Edit: tras forzar Eliminado la fila sigue Activa"), (_row(cfg, cid) or (0, 0, 0, -1))[3] == 0)
    r = c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW, "Input.Status": "7"})
    check(R, L("Edit: Estado=7 (fuera del enum) se rechaza sin 500"), r.status == 200 and bool(r.errors) and (_row(cfg, cid) or (0, 0, 0, -1))[3] == 0,
          f"status={r.status} errores={r.errors[:2]}")
    r = c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW, "Input.Status": "abc"})
    check(R, L("Edit: Estado='abc' se rechaza sin 500"), r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
    # ids inexistentes
    for bad in ("99999", "0", "-1", "abc"):
        check_404(R, c, label, f"{base}/Edit/{bad}")
        check_404(R, c, label, f"{base}/Delete/{bad}")
    post_404(R, c, L("Edit POST id inexistente"), f"{base}/Edit/99999", [("Input.Id", "99999"), ("Input.Name", "QAC fantasma"), ("Input.Status", "0")], token_from=f"{base}/Create")
    post_404(R, c, L("Delete POST id inexistente"), f"{base}/Delete/99999", [], token_from=f"{base}/Create")
    # sin token
    tokenless(R, c, L("Create"), f"{base}/Create", [("Input.Name", uniq("QAC sintoken"))])
    tokenless(R, c, L("Edit"), f"{base}/Edit/{cid}", [("Input.Id", cid), ("Input.Name", "QAC sintoken"), ("Input.Status", "0")])
    tokenless(R, c, L("Delete"), f"{base}/Delete/{cid}", [])
    check(R, L("POST sin token no modificó nada"), (_row(cfg, cid) or (0, 0))[1] == NEW and (_row(cfg, cid) or (0,) * 4)[3] == 0)

    # ------------------------------------------------------------ baja: bloqueo con artículos activos
    other_id = scalar(f"select top 1 Id from {cfg['other_table']} where Status=0 order by Id")
    art_name = uniq("QAC Art dep")
    art_fields = [("Input.Name", art_name), (cfg["article_field"], cid), (cfg["other_field"], other_id)]
    r = c.raw_post("/Catalogs/Articles/Create", art_fields, token_from="/Catalogs/Articles/Create", name="artículo dependiente")
    check(R, L("Preparación: artículo activo que usa el registro"), r.status == 302, f"status={r.status} errores={r.errors[:2]}")
    art_id = scalar("select top 1 Id from Articles where Name=? order by Id desc", art_name)
    r = c.submit(f"{base}/Delete/{cid}", {})
    check(R, L("Delete: bloqueado si tiene artículos activos"),
          r.status == 302 and r.has_toast(cfg["blocked_msg"], "error") and (r.location or "").lower().endswith(f"{base}/Delete/{cid}".lower()),
          f"status={r.status} loc={r.location} toasts={r.toasts[:2]}")
    check(R, L("Delete: tras el bloqueo la fila sigue Activa y sin sufijo -DEL-"), (_row(cfg, cid) or (0, 0, "", -1))[3] == 0 and "-DEL-" not in ((_row(cfg, cid) or (0, 0, ""))[2]))
    # estado forzado: aunque tenga artículos no se puede "Eliminar" por Edit (cubierto arriba); ahora retirar el artículo y dar de baja
    if art_id:
        c.submit(f"/Catalogs/Articles/Delete/{art_id}", {})
    old_code = (_row(cfg, cid) or (0, 0, ""))[2]
    r = c.submit(f"{base}/Delete/{cid}", {})
    expect(R, L("Delete: baja válida (sin artículos activos)"), r, status=302, redirect=base, toast=cfg["deleted_msg"], toast_kind="success")
    row = _row(cfg, cid)
    check(R, L("Delete: Status=Eliminado y código renombrado {Code}-DEL-yyyyMMddHHmmss"),
          row and row[3] == 2 and re.fullmatch(re.escape(old_code) + r"-DEL-\d{14}", row[2] or "") is not None, str(row))
    ok, page = False, ""
    _, page = c.get(f"{base}?{urllib.parse.urlencode({'SearchTerm': NEW})}")
    check(R, L("Index: el registro dado de baja desaparece del listado"), f"{base}/Edit/{cid}" not in page)
    code_after_first = row[2] if row else None
    r = c.submit(f"{base}/Delete/{cid}", {})
    row2 = _row(cfg, cid)
    check(R, L("Delete: una segunda baja no altera el código (idempotente)"), row2 and row2[2] == code_after_first,
          f"codigo antes={code_after_first} despues={row2 and row2[2]} toasts={r.toasts[:1]}")
    # recrear un nombre dado de baja
    r = c.submit(f"{base}/Create", {"Input.Name": NEW})
    expect(R, L("Create: recrear un nombre dado de baja (sospecha de índice único sin filtro)"), r, status=200, error=cfg["recreate_msg"])
    # editar un registro dado de baja
    st, _ = c.get(f"{base}/Edit/{cid}")
    r = c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW + " rev", "Input.Status": "0"}) if st == 200 else None
    reactivated = (_row(cfg, cid) or (0, 0, 0, -1))[3] == 0
    check(R, L("Edit: un registro dado de baja no se puede reactivar editándolo por URL (hallazgo extra)"), not reactivated,
          f"GET Edit={st}; Status final={(_row(cfg, cid) or (0, 0, 0, -1))[3]}")

    # ------------------------------------------------------------ limpieza (bajas lógicas de lo creado)
    if reactivated:
        r = c.submit(f"{base}/Delete/{cid}", {})
        if (_row(cfg, cid) or (0, 0, 0, -1))[3] == 0:   # la baja falla (código demasiado largo tras la doble baja): se inactiva por Edit
            c.submit(f"{base}/Edit/{cid}", {"Input.Name": NEW + " rev", "Input.Status": "1"})
            check(R, L("Delete: tras reactivar un registro con código '-DEL-' la nueva baja falla (código excede la longitud)"),
                  False, f"toasts={r.toasts[:1]}; el registro {cid} queda Inactivo por Edit")
    for rid in (exact_id, messy_id, html_id, acc_id):
        if rid:
            c.submit(f"{base}/Delete/{rid}", {})
    left = scalar(f"select count(*) from {cfg['table']} where Name like ? and Status=0", cfg["prefix"] + "%")
    check(R, L("Limpieza: no quedan registros QAC Activos"), left == 0, f"quedan={left}")


def run(R, c):
    catalog_suite(R, c, CATEGORIES)


if __name__ == "__main__":
    run_module("categories", run)
