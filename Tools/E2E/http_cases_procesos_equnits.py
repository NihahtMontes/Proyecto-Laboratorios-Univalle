"""EquipmentUnits (unidades fisicas): Create / Edit (+ lecturas Details/Kardex). Solo contra la copia QA."""
import os
import re

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, unidad, post_form, post_raw

ENUM_EN = re.compile(r"\b(Operational|UnderMaintenance|OutOfService|InRepair|Decommissioned|Dismantled|Broken|OnLoan|Deleted|Excellent|Good|Regular|Bad|PorConfirmar)\b")


def uid_por_inv(inv):
    v = scalar("select top 1 Id from EquipmentUnits where InventoryNumber=? order by Id desc", inv)
    return int(v) if v else None


def historias(uid):
    return sql("select Status,EndDate,Reason from EquipmentStateHistories where EquipmentUnitId=? order by Id", uid)


def base(inv, lab, **extra):
    ov = {"Input.EquipmentId": str(P.EQUIP), "Input.InventoryNumber": inv, "Input.CareerId": str(P.CAREER),
          "Input.FacultyId": str(P.FAC), "Input.LaboratoryId": str(lab)}
    ov.update(extra)
    return ov


def rechazado(r):
    return r.status == 200 and not r.location


def run(R, c):
    lab = P.lab_propio(c, "unidades")
    pw = os.environ["E2E_PW"]
    I = lambda s: f"{P.PREFIX}{s}"

    # ============================================================ Create: GET y validaciones de campos
    st, _ = c.get("/EquipmentUnits/Create")
    check(R, "Create: GET 200", st == 200, f"status={st}")
    st, _ = c.get(f"/EquipmentUnits/Create?equipmentId={P.EQUIP}")
    check(R, "Create: GET con equipmentId 200", st == 200, f"status={st}")

    r = post_form(c, "/EquipmentUnits/Create", {"Input.EquipmentId": None, "Input.InventoryNumber": None, "Input.CareerId": None, "Input.FacultyId": None, "Input.LaboratoryId": None}, name="vacio")
    expect(R, "Create: vacio -> facultad obligatoria", r, status=200, error="La facultad es obligatoria")
    for msg in ("El laboratorio es obligatorio", "El modelo de equipo es obligatorio", "La carrera es obligatoria", "El número de inventario es obligatorio"):
        check(R, f"Create: vacio -> '{msg}'", r.has_error(msg), P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base("   ", lab), name="inventario en blanco")
    check(R, "Create: inventario solo espacios rechazado (no se guarda una unidad sin inventario)", rechazado(r) and not scalar("select count(*) from EquipmentUnits where LTRIM(RTRIM(InventoryNumber))='' and CreatedDate>=DATEADD(minute,-5,GETUTCDATE())"), P.dump(r))

    # ---- longitudes y rangos que el formulario HTML no impide
    import random, string
    def libre(n):
        while True:
            v = "".join(random.choices(string.ascii_uppercase, k=n))
            if not scalar("select count(*) from EquipmentUnits where InventoryNumber=?", v):
                return v
    for lbl, inv in (("inventario de 1 caracter", libre(1)), ("inventario de 2 caracteres", libre(2))):
        r = post_form(c, "/EquipmentUnits/Create", base(inv, lab), name=lbl, follow=False)
        check(R, f"Create: {lbl} (Edit exige minimo 3) -> debe rechazarse", rechazado(r), P.dump(r) + f" creada={uid_por_inv(inv)}")
    r = post_form(c, "/EquipmentUnits/Create", base(I("L") + "x" * 60, lab), name="inventario 64+", follow=False)
    check(R, "Create: inventario de mas de 50 caracteres no produce 500", r.status < 500, P.dump(r))
    check(R, "Create: inventario de mas de 50 caracteres se rechaza con mensaje", rechazado(r) and bool(r.errors), P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("sr"), lab, **{"Input.SerialNumber": "s" * 101}), name="serie 101", follow=False)
    check(R, "Create: numero de serie de 101 caracteres no produce 500", r.status < 500, P.dump(r))
    check(R, "Create: numero de serie de 101 caracteres se rechaza con mensaje", rechazado(r) and bool(r.errors), P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("nt"), lab, **{"Input.Notes": "n" * 2001}), name="notas 2001", follow=False)
    check(R, "Create: notas de 2001 caracteres no produce 500", r.status < 500, P.dump(r))
    check(R, "Create: notas de 2001 caracteres se rechazan con mensaje", rechazado(r) and bool(r.errors), P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("vn"), lab, **{"Input.AcquisitionValue": "-50"}), name="valor negativo", follow=False)
    check(R, "Create: precio de adquisicion negativo se rechaza (el modelo declara Range(0..))", rechazado(r) and not uid_por_inv(I("vn")), P.dump(r) + f" creada={uid_por_inv(I('vn'))}")
    r = post_form(c, "/EquipmentUnits/Create", base(I("vh"), lab, **{"Input.AcquisitionValue": "99999999999999999999"}), name="valor enorme", follow=False)
    check(R, "Create: precio de adquisicion fuera de decimal(18,2) no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("fc"), lab, **{"Input.AcquisitionDate": "2030-01-01", "Input.ManufacturingDate": "2031-01-01"}), name="fechas incoherentes", follow=False)
    check(R, "Create: fabricacion posterior a adquisicion se rechaza", rechazado(r) and not uid_por_inv(I("fc")), P.dump(r) + f" creada={uid_por_inv(I('fc'))}")

    # ---- referencias inexistentes (el servidor no las valida: se espera error controlado, no 500)
    r = post_form(c, "/EquipmentUnits/Create", base(I("eq"), lab, **{"Input.EquipmentId": "99999"}), name="equipo inexistente", follow=False)
    check(R, "Create: definicion de equipo inexistente no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("lb"), 99999), name="laboratorio inexistente", follow=False)
    check(R, "Create: laboratorio inexistente no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("cr"), lab, **{"Input.CareerId": "99999"}), name="carrera inexistente", follow=False)
    check(R, "Create: carrera inexistente no produce 500", r.status < 500, P.dump(r))
    otra_fac = scalar("select top 1 Id from Faculties where Id<>? and Status=0 order by Id", P.FAC)
    r = post_form(c, "/EquipmentUnits/Create", base(I("fl"), lab, **{"Input.FacultyId": str(otra_fac)}), name="laboratorio de otra facultad", follow=False)
    check(R, "Create: laboratorio que no pertenece a la facultad elegida se rechaza", rechazado(r) and not uid_por_inv(I("fl")), P.dump(r) + f" creada={uid_por_inv(I('fl'))}")
    eq_baja = scalar("select top 1 Id from Equipments where Status=2 order by Id desc")
    if eq_baja:
        r = post_form(c, "/EquipmentUnits/Create", base(I("eb"), lab, **{"Input.EquipmentId": str(eq_baja)}), name="equipo dado de baja", follow=False)
        check(R, "Create: no se pueden crear unidades de una definicion de equipo eliminada", rechazado(r) and not uid_por_inv(I("eb")), P.dump(r) + f" creada={uid_por_inv(I('eb'))}")

    # ---- enums fuera de rango
    r = post_form(c, "/EquipmentUnits/Create", base(I("e1"), lab, **{"Input.CurrentStatus": "77"}), name="estado 77", follow=False)
    check(R, "Create: estado operativo inexistente (77) se rechaza", rechazado(r) and not uid_por_inv(I("e1")), P.dump(r) + f" creada={uid_por_inv(I('e1'))}")
    r = post_form(c, "/EquipmentUnits/Create", base(I("e2"), lab, **{"Input.CurrentStatus": "99"}), name="estado Deleted", follow=False)
    check(R, "Create: no se puede crear una unidad ya 'Eliminada' (99)", rechazado(r) and not uid_por_inv(I("e2")), P.dump(r) + f" creada={uid_por_inv(I('e2'))}")
    r = post_form(c, "/EquipmentUnits/Create", base(I("e3"), lab, **{"Input.PhysicalCondition": "77"}), name="condicion 77", follow=False)
    check(R, "Create: condicion fisica inexistente (77) se rechaza", rechazado(r) and not uid_por_inv(I("e3")), P.dump(r) + f" creada={uid_por_inv(I('e3'))}")
    r = post_form(c, "/EquipmentUnits/Create", base(I("e4"), lab, **{"Input.PhysicalCondition": ""}), name="condicion vacia", follow=False)
    check(R, "Create: condicion fisica vacia -> rechazo con mensaje en espanol", rechazado(r) and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("e5"), lab, **{"Input.AcquisitionDate": "no-es-fecha"}), name="fecha invalida", follow=False)
    check(R, "Create: fecha de adquisicion invalida -> rechazo con mensaje en espanol", rechazado(r) and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))

    # ============================================================ Create valido
    inv1 = I("u1")
    nombre = scalar("select Name from Equipments where Id=?", P.EQUIP)
    r = post_form(c, "/EquipmentUnits/Create", base(inv1, lab, **{"Input.SerialNumber": "SER-QAP", "Input.Notes": "Nota <b>qap</b>", "Input.AcquisitionValue": "1500.50",
                                                                   "Input.AcquisitionDate": "2025-05-01", "Input.ManufacturingDate": "2024-01-01"}), name="valido")
    u1 = uid_por_inv(inv1)
    expect(R, "Create: unidad valida", r, status=302, redirect=f"/Equipment/Details/{P.EQUIP}", toast=f"Equipo {nombre} ({inv1}) fue creado exitosamente.", toast_kind="success")
    row = unidad(u1) if u1 else None
    check(R, "Create valido: estado Operativo, condicion Excelente, laboratorio y carrera guardados", bool(row) and row["status"] == 0 and row["cond"] == 5 and row["lab"] == lab and row["career"] == P.CAREER, f"{row}")
    check(R, "Create valido: queda en la gestion activa y con ubicacion Confirmada (1)", bool(u1) and scalar("select ManagementId from EquipmentUnits where Id=?", u1) == P.MGMT and row["locres"] == 1, f"{row}")
    h = historias(u1) if u1 else []
    check(R, "Create valido: historial inicial abierto 'Alta inicial en sistema'", len(h) == 1 and h[0][1] is None and "Alta inicial" in (h[0][2] or ""), f"{h}")
    check(R, "Create valido: el motivo del historial inicial no muestra enums en ingles", not ENUM_EN.search(h[0][2] or "") if h else False, f"{h}")
    check(R, "Create valido: CreatedById registrado (auditoria)", u1 and scalar("select CreatedById from EquipmentUnits where Id=?", u1) is not None)
    if u1:
        _, pg = c.get(f"/EquipmentUnits/Details?id={u1}")
        check(R, "Details: la unidad creada se muestra y la nota con HTML sale escapada", "&lt;b&gt;qap&lt;/b&gt;" in pg and "<b>qap</b>" not in pg)

    # duplicado exacto, con espacios, en otra capitalizacion, doble envio
    r = post_form(c, "/EquipmentUnits/Create", base(inv1, lab), name="duplicado")
    expect(R, "Create: inventario duplicado", r, status=200, error="Este número de inventario ya está registrado.")
    r = post_form(c, "/EquipmentUnits/Create", base(inv1.lower(), lab), name="duplicado minusculas")
    expect(R, "Create: inventario duplicado en minusculas (el motor compara sin distinguir mayusculas)", r, status=200, error="Este número de inventario ya está registrado.")
    r = post_form(c, "/EquipmentUnits/Create", base(" " + inv1, lab), name="duplicado espacio inicial", follow=False)
    check(R, "Create: inventario duplicado con espacio inicial se rechaza (el servidor recorta DESPUES de comprobar)", rechazado(r) and scalar("select count(*) from EquipmentUnits where InventoryNumber=? and CurrentStatus<>99", inv1) == 1,
          P.dump(r) + f" copias={scalar('select count(*) from EquipmentUnits where InventoryNumber=? and CurrentStatus<>99', inv1)}")
    r = post_form(c, "/EquipmentUnits/Create", base(inv1 + " ", lab), name="duplicado espacio final", follow=False)
    check(R, "Create: inventario duplicado con espacio final se rechaza", rechazado(r) and scalar("select count(*) from EquipmentUnits where InventoryNumber=? and CurrentStatus<>99", inv1) == 1, P.dump(r))
    inv_ws = I("ws")
    r = post_form(c, "/EquipmentUnits/Create", base("  " + inv_ws + "  ", lab), name="espacios sobrantes", follow=False)
    check(R, "Create: el inventario se guarda sin espacios sobrantes", scalar("select count(*) from EquipmentUnits where InventoryNumber=?", inv_ws) == 1, P.dump(r))

    # ReturnUrl
    inv_ru = I("ru")
    r = post_form(c, "/EquipmentUnits/Create", base(inv_ru, lab, ReturnUrl="https://evil.example.com/robo"), name="ReturnUrl externa", follow=False)
    check(R, "Create: ReturnUrl externa se ignora (redirige a Equipment/Details)", r.status == 302 and (r.location or "").startswith(f"/Equipment/Details/{P.EQUIP}"), P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("r2"), lab, ReturnUrl="//evil.example.com/robo"), name="ReturnUrl //", follow=False)
    check(R, "Create: ReturnUrl '//host' se ignora", r.status == 302 and "evil" not in (r.location or ""), P.dump(r))
    r = post_form(c, "/EquipmentUnits/Create", base(I("r3"), lab, ReturnUrl="/Equipment"), name="ReturnUrl local", follow=False)
    check(R, "Create: ReturnUrl local se respeta", r.status == 302 and (r.location or "") == "/Equipment", P.dump(r))

    # estados iniciales distintos al por defecto
    r = post_form(c, "/EquipmentUnits/Create", base(I("os"), lab, **{"Input.CurrentStatus": "2", "Input.PhysicalCondition": "2"}), name="fuera de servicio")
    uo = uid_por_inv(I("os"))
    check(R, "Create: estado inicial Fuera de servicio / condicion Malo se guardan", uo and unidad(uo)["status"] == 2 and unidad(uo)["cond"] == 2, f"{unidad(uo) if uo else None}")

    # ============================================================ Edit
    st, _ = c.get("/EquipmentUnits/Edit")
    check(R, "Edit: GET sin id -> 404", st == 404, f"status={st}")
    st, _ = c.get("/EquipmentUnits/Edit?id=99999")
    check(R, "Edit: GET id inexistente -> 404", st == 404, f"status={st}")
    u2, inv2 = P.nueva_unidad(c, lab, "u2")
    u3, inv3 = P.nueva_unidad(c, lab, "u3")
    ep = f"/EquipmentUnits/Edit?id={u2}"
    st, _ = c.get(ep)
    check(R, "Edit: GET 200", st == 200, f"status={st}")
    O = lambda **kw: {f"EquipmentUnit.{k}": v for k, v in kw.items()}

    r = post_form(c, ep, O(InventoryNumber=""), name="edit sin inventario")
    expect(R, "Edit: inventario vacio", r, status=200, error="El número de inventario es obligatorio")
    r = post_form(c, ep, O(InventoryNumber="ab"), name="edit inventario 2")
    expect(R, "Edit: inventario de 2 caracteres", r, status=200, error="El inventario debe tener al menos 3 caracteres")
    r = post_form(c, ep, O(InventoryNumber="x" * 51), name="edit inventario 51")
    expect(R, "Edit: inventario de 51 caracteres rechazado", r, status=200)
    check(R, "Edit: inventario de 51 caracteres -> el mensaje menciona el maximo de 50 (el actual dice 'al menos 3')", r.has_error("50"), P.dump(r))
    r = post_form(c, ep, O(SerialNumber="s" * 101), name="edit serie 101")
    check(R, "Edit: serie de 101 caracteres rechazada con mensaje en espanol", rechazado(r) and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep, O(Notes="n" * 2001), name="edit notas 2001")
    check(R, "Edit: notas de 2001 caracteres rechazadas con mensaje en espanol", rechazado(r) and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep, O(EquipmentId=""), name="edit sin equipo")
    expect(R, "Edit: sin definicion de equipo", r, status=200, error="El modelo de equipo es obligatorio")
    r = post_form(c, ep, O(InventoryNumber=inv3), name="edit duplicado")
    expect(R, "Edit: inventario de OTRA unidad", r, status=200, error="Este número de inventario ya está registrado en otra unidad.")
    r = post_form(c, ep, O(InventoryNumber=" " + inv3), name="edit duplicado espacio", follow=False)
    check(R, "Edit: inventario de otra unidad con espacio inicial se rechaza", rechazado(r) and scalar("select InventoryNumber from EquipmentUnits where Id=?", u2) == inv2, P.dump(r) + f" inv={scalar('select InventoryNumber from EquipmentUnits where Id=?', u2)}")
    r = post_form(c, ep, O(EquipmentId="99999"), name="edit equipo inexistente", follow=False)
    check(R, "Edit: definicion de equipo inexistente no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, ep, O(LaboratoryId="99999"), name="edit laboratorio inexistente", follow=False)
    check(R, "Edit: laboratorio inexistente no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, ep, O(CurrentStatus="77"), name="edit estado 77", follow=False)
    check(R, "Edit: estado inexistente (77) se rechaza", rechazado(r) and unidad(u2)["status"] == 0, P.dump(r) + f" estado={unidad(u2)['status']}")
    r = post_form(c, ep, O(AcquisitionValue="-5"), name="edit valor negativo", follow=False)
    check(R, "Edit: precio negativo se rechaza", rechazado(r), P.dump(r))
    r = post_form(c, ep, O(Id="99999", InventoryNumber=I("nx")), name="edit Id inexistente", follow=False)
    check(R, "Edit: POST con Id inexistente -> 404", r.status == 404, P.dump(r))
    check(R, "Edit: ningun rechazo modifico la unidad", unidad(u2)["inv"] == inv2 and unidad(u2)["status"] == 0 and unidad(u2)["cond"] == 5, f"{unidad(u2)}")

    # edicion valida con cambio de estado y condicion
    hist_antes = len(historias(u2))
    r = post_form(c, ep, O(CurrentStatus="1", PhysicalCondition="3", Notes="mantenimiento QAP", SerialNumber="SER-ED"), name="edit estado")
    esp_msg = "Unidad "
    check(R, "Edit: cambio de estado y condicion -> 302 a Equipment/Details con mensaje de exito",
          r.status == 302 and (r.location or "").startswith("/Equipment/Details/") and r.has_toast("cambios guardados", "success"), P.dump(r))
    check(R, "Edit: el mensaje de exito no muestra nombres de enum en ingles", not any(ENUM_EN.search(t) for k, t in r.toasts), P.dump(r))
    h = historias(u2)
    check(R, "Edit: cierra el historial abierto y abre otro 'Actualizacion manual'", len(h) == hist_antes + 1 and h[-2][1] is not None and h[-1][1] is None and h[-1][0] == 1, f"{h}")
    check(R, "Edit: el motivo del nuevo historial no muestra enums en ingles", not ENUM_EN.search(h[-1][2] or ""), f"{h[-1]}")
    check(R, "Edit: estado y condicion guardados", unidad(u2)["status"] == 1 and unidad(u2)["cond"] == 3 and unidad(u2)["serial"] == "SER-ED")
    check(R, "Edit: registra quien modifico y cuando (auditoria ModifiedById/LastModifiedDate)", scalar("select count(*) from EquipmentUnits where Id=? and ModifiedById is not null and LastModifiedDate is not null", u2) == 1,
          f"ModifiedById={scalar('select ModifiedById from EquipmentUnits where Id=?', u2)}")
    # sin cambios de estado / doble envio
    n_h = len(historias(u2))
    r = post_form(c, ep, O(Notes="mantenimiento QAP"), name="edit sin cambios")
    check(R, "Edit: reenvio sin cambios de estado no crea historial y avisa 'sin alteracion de estados clave'", len(historias(u2)) == n_h and r.has_toast("sin alteración de estados clave", "success"), P.dump(r))
    # laboratorio nulo
    r = post_form(c, ep, O(LaboratoryId=""), name="edit lab nulo")
    row = unidad(u2)
    check(R, "Edit: laboratorio vacio deja la ubicacion en Pendiente (LocationResolutionStatus=0)", r.status == 302 and row["lab"] is None and row["locres"] == 0, f"{row} {P.dump(r)}")
    r = post_form(c, ep, O(LaboratoryId=str(lab)), name="edit lab restaurado")
    check(R, "Edit: laboratorio restaurado -> ubicacion Confirmada", unidad(u2)["lab"] == lab and unidad(u2)["locres"] == 1)
    # inventario propio sin cambios y cambio valido de inventario
    r = post_form(c, ep, O(InventoryNumber=inv2), name="edit mismo inventario")
    check(R, "Edit: conservar el propio inventario no es duplicado", r.status == 302, P.dump(r))
    nuevo_inv = I("u2n")
    r = post_form(c, ep, O(InventoryNumber="  " + nuevo_inv + " "), name="edit inventario nuevo")
    check(R, "Edit: cambio de inventario con espacios sobrantes -> guardado recortado", r.status == 302 and unidad(u2)["inv"] == nuevo_inv, f"{unidad(u2)['inv']!r}")
    # estado Prestamo / Baja / Eliminada desde Edit (observaciones de negocio)
    r = post_form(c, ep, O(CurrentStatus="10"), name="edit OnLoan")
    check(R, "Edit: cambiar a En Prestamo sin salida L-3 abierta (el plan lo documenta como permitido; observacion)", r.status in (200, 302), f"{P.dump(r)} estado={unidad(u2)['status']}")
    r = post_form(c, ep, O(CurrentStatus="99"), name="edit Deleted", follow=False)
    check(R, "Edit: el estado 99 (Eliminado) NO debe poder fijarse por Edit (la baja se hace con DeleteUnit)", rechazado(r) and unidad(u2)["status"] != 99, P.dump(r) + f" estado={unidad(u2)['status']}")

    # unidad con plan activo: editar estado no rompe el plan
    uP, invP = P.nueva_unidad(c, lab, "up")
    pidP = P.planificar(c, [uP])[uP]
    r = post_form(c, f"/EquipmentUnits/Edit?id={uP}", O(CurrentStatus="2", CareerId=""), name="edit unidad planificada")
    check(R, "Edit: unidad planificada -> estado cambia y el plan no se altera", r.status == 302 and P.plan(pidP)["phase"] == 1 and P.plan(pidP)["state"] == 1, f"{P.plan(pidP)} {P.dump(r)}")
    r = post_form(c, f"/EquipmentUnits/Edit?id={uP}", O(CareerId=""), name="edit carrera vacia")
    check(R, "Edit: carrera vacia permitida (carrera opcional en Edit; obligatoria en Create)", r.status == 302 and unidad(uP)["career"] is None, P.dump(r))

    # unidad dada de baja
    uD, invD = P.nueva_unidad(c, lab, "ud")
    P.post_raw(c, f"/Equipment/Details/{uD}?handler=DeleteUnit", [], f"/Equipment/Details/{P.EQUIP}", name="baja unidad")
    st, _ = c.get(f"/EquipmentUnits/Edit?id={uD}")
    check(R, "Edit: GET de una unidad dada de baja -> 404", st == 404, f"status={st}")
    r = post_form(c, ep, O(Id=str(uD), InventoryNumber=invD), name="edit sobre unidad baja", follow=False)
    check(R, "Edit: POST sobre el Id de una unidad dada de baja -> 404 y la baja sigue", r.status == 404 and unidad(uD)["status"] == 99, P.dump(r) + f" estado={unidad(uD)['status']}")
    r = post_form(c, "/EquipmentUnits/Create", base(invD, lab), name="reutilizar inventario de baja")
    check(R, "Create: el inventario de una unidad dada de baja puede reutilizarse", r.status == 302, P.dump(r))

    # ============================================================ lecturas
    for path in ("/EquipmentUnits", f"/EquipmentUnits?SearchTerm={P.PREFIX}", f"/EquipmentUnits/Details?id={u1}", f"/EquipmentUnits/Kardex/{u1}",
                 f"/EquipmentUnits/Details?id={u1}&isWizard=true", "/EquipmentUnits/Details?id=99999", "/EquipmentUnits/Details", "/EquipmentUnits/Kardex/99999"):
        st, pg = c.get(path)
        esperado = (404,) if path in ("/EquipmentUnits/Details?id=99999", "/EquipmentUnits/Details", "/EquipmentUnits/Kardex/99999") else (200,)
        check(R, f"GET {path}: {'/'.join(map(str, esperado))} sin pagina de error", st in esperado, f"status={st}")

    # ============================================================ acceso
    P.check_anon(R, "EquipmentUnits", "/EquipmentUnits/Create")
    P.check_anon(R, "EquipmentUnits", ep)
    P.check_anon(R, "EquipmentUnits", "/EquipmentUnits/Create", post=True)
    P.tokenless(R, c, "EquipmentUnits/Create", "/EquipmentUnits/Create", list(base(I("sintok"), lab).items()))
    P.tokenless(R, c, "EquipmentUnits/Edit", ep, [("EquipmentUnit.Id", str(u2)), ("EquipmentUnit.InventoryNumber", "XXXX")])
    check(R, "Sin token: no se creo ni modifico nada", not uid_por_inv(I("sintok")) and unidad(u2)["inv"] == nuevo_inv)
    cl = P.client("qa_cierre", pw)
    r = post_raw(cl, "/EquipmentUnits/Create", list(base(I("rol1"), lab).items()), "/", follow=False, name="create como PracticeClosure")
    check(R, "Rol PracticeClosure: POST Create denegado y sin alta", r.status in (302, 403) and not uid_por_inv(I("rol1")), P.dump(r))
    r = post_raw(cl, ep, [("EquipmentUnit.Id", u2), ("EquipmentUnit.InventoryNumber", "HACK1"), ("EquipmentUnit.EquipmentId", P.EQUIP)], "/", follow=False, name="edit como PracticeClosure")
    check(R, "Rol PracticeClosure: POST Edit denegado", r.status in (302, 403) and unidad(u2)["inv"] == nuevo_inv, P.dump(r))
    cs = P.client("qa_sup", pw)
    st, _ = cs.get("/EquipmentUnits/Create")
    check(R, "Rol Supervisor: Create accesible (ManagementRoles)", st == 200, f"status={st}")
    r = post_form(cs, "/EquipmentUnits/Create", base(I("sup"), lab), name="create como Supervisor", follow=False)
    check(R, "Rol Supervisor: puede crear una unidad", r.status == 302 and uid_por_inv(I("sup")), P.dump(r))
    st, loc = cs.probe("/EquipmentUnits")
    check(R, "Rol Supervisor: listado /EquipmentUnits (AdminRoles) denegado (302 a /Error o 403)", st in (302, 403), f"status={st} loc={loc}")


if __name__ == "__main__":
    P.run_module("equnits", run)
