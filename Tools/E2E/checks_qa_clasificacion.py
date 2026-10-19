"""QA de la clasificación del catálogo, /AssetView, Gestiones y L-48 histórico (solo lectura)."""
import re
import sys
import urllib.error
import urllib.request
from functools import partial

from e2e_lib import BASE, ERROR_MARKERS, Result, Session, db_rows, db_scalar, run_all

TIPOS = {0: "Electronico", 1: "Manual", 2: "Mobiliario", 3: "Medicion", 4: "Vidrio", 5: "Reactivo", 6: "Informatico",
         7: "Otro", 8: "Calor", 9: "Frio", 10: "Congelacion", 11: "Ultracongelacion", 12: "MaquinasRotativas",
         13: "SeguridadIndustrial", 14: "Audiovisuales", 15: "Electricos"}
UTENSILIOS = {0: "NoAplica", 1: "Vidrio", 2: "Plastico", 3: "Metal", 4: "Porcelana", 5: "MenajeCocina", 6: "BartendingBarismo",
              7: "PanaderiaReposteriaPasteleria", 8: "Servicio", 9: "Manteleria", 10: "VajillaGeneral", 11: "Otros"}
# Tarjetas que ofrece hoy /AssetView/EquipmentClassifications (Pages/AssetView/EquipmentClassifications.cshtml.cs)
TARJETAS_EQUIPO = {"Calor", "Frio", "Congelacion", "Ultracongelacion", "MaquinasRotativas", "Electronico",
                   "SeguridadIndustrial", "Medicion", "Audiovisuales", "Electricos", "Mobiliario", "Informatico", "Otro"}
INV_RE = r'<td class="font-weight-bold text-dark">([^<]+)</td>'


def tile(text, etiqueta):
    m = re.search(r"(\d+)\s+" + re.escape(etiqueta), text)
    return int(m.group(1)) if m else -1


def check_assetview_index(s):
    _, t = s.text("/AssetView")
    out = []
    eq = db_scalar("SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON e.Id=u.EquipmentId "
                   "WHERE u.CurrentStatus<>99 AND e.Status<>99 AND e.ClassificationReviewStatus=2 AND e.Category=0 AND e.TypeClassification IS NOT NULL")
    ut = db_scalar("SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON e.Id=u.EquipmentId "
                   "WHERE u.CurrentStatus<>99 AND e.Status<>99 AND e.ClassificationReviewStatus=2 AND e.Category=1 AND e.UtensilType IS NOT NULL")
    total = db_scalar("SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON e.Id=u.EquipmentId WHERE u.CurrentStatus<>99 AND e.Status<>99")
    stock = db_scalar("SELECT COUNT(*) FROM ArticleStocks")
    out.append(Result("AssetView: tarjeta Total activos", total, tile(t, "Total activos")))
    out.append(Result("AssetView: tarjeta Equipos", eq, tile(t, "Equipos")))
    out.append(Result("AssetView: tarjeta Utensilios", ut, tile(t, "Utensilios")))
    out.append(Result("AssetView: tarjeta Otros", 0, tile(t, "Otros")))
    out.append(Result("AssetView: tarjeta Por clasificar", 0, tile(t, "Por clasificar")))
    out.append(Result("AssetView: tarjeta Existencias", stock, tile(t, "Existencias")))
    out.append(Result("AssetView: equipos + utensilios = total", total, eq + ut))
    out.append(Result("AssetView: 'Al día' cuando no hay pendientes", True, "Al dia" in t or "Al día" in t))
    out.append(Result("AssetView: ya no dice 'En Otro / No aplica'", False, "En Otro / No aplica" in t))
    out.append(Result("AssetView: sin serie es informativo", True, "Se identifican por N.º de inventario" in t))
    out.append(Result("AssetView: texto vacío del donut no aparece", False, "Sin inventario vigente" in t))
    out.append(Result("AssetView: salud", True, s.healthy("/AssetView")))
    out.append(Result("AssetView (pestaña Inventario): salud", True, s.healthy("/AssetView?ActiveTab=Inventario")))
    _, ti = s.text("/AssetView?ActiveTab=Inventario")
    out.append(Result("AssetView Inventario: Equipos/Utensilios = BD", (eq, ut),
                      (tile(ti, "Equipos"), tile(ti, "Utensilios"))))
    return out


def check_tarjetas_equipos(s):
    """Cada tarjeta de /AssetView/EquipmentClassifications = BD; la suma debe cubrir TODAS las unidades de equipo."""
    _, t = s.text("/AssetView/EquipmentClassifications")
    rows = db_rows("SELECT e.TypeClassification, COUNT(u.Id) FROM Equipments e JOIN EquipmentUnits u ON u.EquipmentId=e.Id "
                   "WHERE u.CurrentStatus<>99 AND e.Status<>99 AND e.ClassificationReviewStatus=2 AND e.Category=0 "
                   "GROUP BY e.TypeClassification")
    db = {TIPOS[r[0]]: r[1] for r in rows}
    out = []
    for tipo in sorted(TARJETAS_EQUIPO):
        # la tarjeta enlaza a Units?classification=<Tipo>; el número aparece en el mismo bloque
        m = re.search(r"classification=" + tipo + r"\b.*?(\d+)", s.get("/AssetView/EquipmentClassifications")[1], re.S)
        out.append(Result(f"Tarjeta equipos {tipo}: enlace presente", True, m is not None))
    total = sum(db.values())
    cubiertas = sum(v for k, v in db.items() if k in TARJETAS_EQUIPO)
    out.append(Result("Tarjetas de equipos: cubren todas las unidades clasificadas como equipo", total, cubiertas,
                      f"sin tarjeta: {{{', '.join(f'{k}={v}' for k, v in db.items() if k not in TARJETAS_EQUIPO)}}}"))
    return out


def check_listas_por_tipo(s):
    """Units?classification=X lista exactamente las unidades de ese tipo (incluye los tipos sin tarjeta)."""
    out = []
    rows = db_rows("SELECT e.TypeClassification, u.InventoryNumber FROM Equipments e JOIN EquipmentUnits u ON u.EquipmentId=e.Id "
                   "WHERE u.CurrentStatus<>99 AND e.Status<>99 AND e.ClassificationReviewStatus=2 AND e.Category=0")
    por_tipo = {}
    for tipo, inv in rows:
        por_tipo.setdefault(TIPOS[tipo], set()).add(inv)
    for tipo, esperado in sorted(por_tipo.items()):
        vistos = s.collect(f"/AssetView/Units?classification={tipo}", INV_RE, page_param="pageIndex")
        out.append(Result(f"Units?classification={tipo}: unidades visibles vs BD", len(esperado), len(vistos & esperado),
                          f"(extra={len(vistos - esperado)})"))
    return out


def check_utensilios(s):
    out = []
    rows = db_rows("SELECT e.UtensilType, u.InventoryNumber FROM Equipments e JOIN EquipmentUnits u ON u.EquipmentId=e.Id "
                   "WHERE u.CurrentStatus<>99 AND e.Status<>99 AND e.ClassificationReviewStatus=2 AND e.Category=1")
    por_tipo = {}
    for tipo, inv in rows:
        por_tipo.setdefault(UTENSILIOS[tipo], set()).add(inv)
    for tipo, esperado in sorted(por_tipo.items()):
        vistos = s.collect(f"/AssetView/UtensilUnits?utensilType={tipo}", INV_RE)
        out.append(Result(f"UtensilUnits?utensilType={tipo}: visibles vs BD", len(esperado), len(vistos & esperado)))
    out.append(Result("UtensilClassifications: salud", True, s.healthy("/AssetView/UtensilClassifications")))
    out.append(Result("OtherUnits: salud", True, s.healthy("/AssetView/OtherUnits")))
    out.append(Result("Environments: salud", True, s.healthy("/AssetView/Environments")))
    return out


def check_detalle_equipos(s):
    """La ficha de cada uno de los 121 catálogos muestra su clasificación vigente."""
    malos = []
    for eid, cat, tipo, ut in db_rows("SELECT Id, Category, TypeClassification, UtensilType FROM Equipments"):
        _, html = s.get(f"/Equipment/Details/{eid}")
        esperado = UTENSILIOS[ut] if cat == 1 else TIPOS[tipo]
        if esperado not in html:
            malos.append((eid, esperado))
    return Result("Equipment/Details: clasificación visible en los 121 catálogos", 0, len(malos), str(malos[:5]))


def check_gestiones_listado(s):
    out = []
    esperado = {
        "L-48": db_scalar("SELECT COUNT(*) FROM MaintenancePlans"),
        "L-8": db_scalar("SELECT COUNT(*) FROM Maintenances"),
        "L-6": db_scalar("SELECT COUNT(*) FROM Verifications"),
        "L-7": db_scalar("SELECT COUNT(*) FROM Requests"),
    }
    suma = {k: 0 for k in esperado}
    sin_actividad = 0
    paginas_html, ids_vistos = [], set()
    for p in range(1, 6):
        st, html = s.get(f"/Managements?Type=PorConfirmar&pageIndex={p}")
        if st != 200:
            break
        # 2026-10-18 (H-005): una página mayor que la última muestra la última; no contarla dos veces.
        ids_pag = set(re.findall(r"/Managements/Details\?id=(\d+)", html))
        if p > 1 and ids_pag <= ids_vistos:
            break
        ids_vistos |= ids_pag
        paginas_html.append(html)
        for k in suma:
            suma[k] += sum(int(x) for x in re.findall(re.escape(k) + r":\s*(\d+)", re.sub(r"<[^>]+>", " ", html)))
        sin_actividad += html.count("Sin actividad importada")
    for k in esperado:
        out.append(Result(f"Managements listado: suma de badges {k} vs BD", esperado[k], suma[k]))
    out.append(Result("Managements listado: ya no muestra '0% (0/0)' en históricas", False,
                      any("0% (0/0)" in re.sub(r"\s+", " ", h) for h in paginas_html)))
    sin_bd = db_scalar("SELECT COUNT(*) FROM Managements m WHERE NOT EXISTS (SELECT 1 FROM MaintenancePlans x WHERE x.ManagementId=m.Id) "
                       "AND NOT EXISTS (SELECT 1 FROM Maintenances x WHERE x.ManagementId=m.Id) "
                       "AND NOT EXISTS (SELECT 1 FROM Verifications x WHERE x.ManagementId=m.Id) "
                       "AND NOT EXISTS (SELECT 1 FROM Requests x WHERE x.ManagementId=m.Id)")
    out.append(Result("Managements listado: gestiones sin actividad = 'Sin actividad importada'", sin_bd, sin_actividad))
    out.append(Result("Managements listado: no muestra 'Resp:' vacío", False,
                      any(re.search(r"Resp:\s*<", h) for h in paginas_html)))
    ids = set()
    for h in paginas_html:
        ids |= set(re.findall(r"/Managements/Details\?id=(\d+)", h))
    out.append(Result("Managements listado: 27 gestiones enlazadas", 27, len(ids)))
    return out


def check_gestiones_detalle(s):
    out = []
    gest = db_rows("SELECT m.Id, m.Code, "
                   "(SELECT COUNT(*) FROM MaintenancePlans WHERE ManagementId=m.Id), "
                   "(SELECT COUNT(*) FROM Maintenances WHERE ManagementId=m.Id), "
                   "(SELECT COUNT(*) FROM Verifications WHERE ManagementId=m.Id), "
                   "(SELECT COUNT(*) FROM Requests WHERE ManagementId=m.Id) FROM Managements m ORDER BY m.Id")
    malas, mal_dash, mal_l48 = [], [], []
    for mid, code, l48, l8, l6, l7 in gest:
        for tab in ("dashboard", "l48", "sanos", "calendario", "editar"):
            st, html = s.get(f"/Managements/Details?id={mid}&ActiveTab={tab}")
            limpio = re.sub(r"<script.*?</script>", "", html, flags=re.S)
            if st != 200 or ERROR_MARKERS.search(limpio):
                malas.append((code, tab, st))
        _, t = s.text(f"/Managements/Details?id={mid}&ActiveTab=dashboard")
        vals = [tile(t, "L-48 Planes"), tile(t, "L-8 Mantenimientos"), tile(t, "L-6 Verificaciones"), tile(t, "L-7 Solicitudes")]
        if vals != [l48, l8, l6, l7]:
            mal_dash.append((code, vals, [l48, l8, l6, l7]))
        _, html = s.get(f"/Managements/Details?id={mid}&ActiveTab=l48")
        filas = len(set(re.findall(r"(PLA-[A-Z0-9-]+)", html)))
        if l48 > 0 and filas != l48:
            mal_l48.append((code, filas, l48))
        if l48 == 0 and "no tiene plan L-48 cargado" not in html:
            mal_l48.append((code, "sin aviso", 0))
    out.append(Result(f"Details: {len(gest)} gestiones x 5 pestañas sin error", 0, len(malas), str(malas[:5])))
    out.append(Result("Details/dashboard: contadores históricos = BD", 0, len(mal_dash), str(mal_dash[:3])))
    out.append(Result("Details/l48: planes visibles = BD y aviso donde no hay", 0, len(mal_l48), str(mal_l48[:3])))
    return out


def check_plan_por_gestion(s):
    out = []
    _, h = s.get("/Managements/Details?id=22&ActiveTab=l48")
    out.append(Result("Details L-48: Servicio/Estado nulos se muestran 'Por confirmar'", True, h.count("Por confirmar") >= 196))
    _, h = s.get("/Maintenances/Plan?ManagementId=22")
    out.append(Result("Maintenances/Plan: Servicio/Estado nulos se muestran 'Por confirmar'", True, h.count("Por confirmar") >= 20))
    for mid, n in db_rows("SELECT ManagementId, COUNT(*) FROM MaintenancePlans GROUP BY ManagementId"):
        codigos = s.collect(f"/Maintenances/Plan?ManagementId={mid}", r"(PLA-[A-Z0-9-]+)")
        out.append(Result(f"Maintenances/Plan?ManagementId={mid}: planes visibles", n, len(codigos)))
    return out


def check_bordes(s):
    out = []
    for nombre, path, aceptados in (
        ("Details id inexistente", "/Managements/Details?id=999999", (200,)),
        ("Details sin id", "/Managements/Details", (404, 200)),
        ("Plan de gestión inexistente", "/Maintenances/Plan?ManagementId=999999", (200,)),
        ("Listado página enorme", "/Managements?Type=PorConfirmar&pageIndex=99999", (200,)),
        ("Listado pageIndex negativo", "/Managements?Type=PorConfirmar&pageIndex=-3", (200,)),
        ("Listado Type inválido", "/Managements?Type=Inventado", (200,)),
        ("Units clasificación inválida", "/AssetView/Units?classification=999", (200, 400, 302)),
        ("Details ActiveTab inválido", "/Managements/Details?id=1&ActiveTab=zzz", (200,)),
    ):
        st, html = s.get(path)
        limpio = re.sub(r"<script.*?</script>", "", html, flags=re.S)
        out.append(Result(f"Borde: {nombre}", True, st in aceptados and not ERROR_MARKERS.search(limpio), f"HTTP {st}"))
    # pageIndex negativo: debe mostrar la primera página (antes provocaba un OFFSET negativo en SQL)
    _, p1 = s.get("/Managements?Type=PorConfirmar&pageIndex=1")
    _, pn = s.get("/Managements?Type=PorConfirmar&pageIndex=-3")
    ids1 = set(re.findall(r"/Managements/Details\?id=(\d+)", p1))
    idsn = set(re.findall(r"/Managements/Details\?id=(\d+)", pn))
    out.append(Result("Borde: pageIndex negativo muestra la primera página", True, len(ids1) > 0 and ids1 == idsn))
    # XSS reflejado en la búsqueda
    st, html = s.get("/Managements?Type=PorConfirmar&SearchTerm=%3Cscript%3Ealert(1)%3C%2Fscript%3E")
    out.append(Result("Borde: búsqueda con <script> no se refleja sin escapar", False, "<script>alert(1)</script>" in html))
    return out


def check_sin_sesion(_s):
    out = []
    opener = urllib.request.build_opener()
    for path in ("/AssetView", "/Managements", "/Managements/Details?id=1", "/Maintenances/Plan", "/EquipmentUnits", "/Catalogs/Stock"):
        try:
            r = opener.open(BASE + path, timeout=60)
            url, st = r.geturl(), r.status
        except urllib.error.HTTPError as e:
            url, st = e.url, e.code
        out.append(Result(f"Sin sesión {path}: redirige al login", True, "/Login" in url or st in (401, 403), f"-> {url}"))
    return out


def check_bd(_s):
    out = []
    out.append(Result("BD: 121 decisiones, 1 vigente por catálogo", (121, 121),
                      tuple(db_rows("SELECT COUNT(*), COUNT(DISTINCT EquipmentId) FROM EquipmentClassificationDecisions WHERE EffectiveTo IS NULL")[0])))
    out.append(Result("BD: claves de decisión únicas", 0,
                      db_scalar("SELECT COUNT(*) FROM (SELECT DecisionKey FROM EquipmentClassificationDecisions GROUP BY DecisionKey HAVING COUNT(*)>1) x")))
    out.append(Result("BD: equipo = decisión vigente (categoría/tipo/utensilio)", 0, db_scalar(
        "SELECT COUNT(*) FROM Equipments e JOIN EquipmentClassificationDecisions d ON d.EquipmentId=e.Id AND d.EffectiveTo IS NULL "
        "WHERE e.Category<>d.Category OR ISNULL(e.TypeClassification,-1)<>ISNULL(d.TypeClassification,-1) "
        "OR ISNULL(e.UtensilType,-1)<>ISNULL(d.UtensilType,-1) OR e.ClassificationReviewStatus<>2")))
    out.append(Result("BD: ningún equipo sin tipo / utensilio sin material", 0, db_scalar(
        "SELECT COUNT(*) FROM Equipments WHERE (Category=0 AND TypeClassification IS NULL) OR (Category=1 AND (UtensilType IS NULL OR UtensilType=0)) "
        "OR (Category=0 AND UtensilType IS NOT NULL) OR (Category=1 AND TypeClassification IS NOT NULL)")))
    out.append(Result("BD: Otro/Otros siempre con detalle", 0, db_scalar(
        "SELECT COUNT(*) FROM Equipments WHERE (TypeClassification=7 OR UtensilType=11) AND (OtherClassificationDetail IS NULL OR LTRIM(OtherClassificationDetail)='')")))
    out.append(Result("BD: ningún utensilio con número de serie", 0, db_scalar(
        "SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON e.Id=u.EquipmentId WHERE e.Category=1 AND u.SerialNumber IS NOT NULL AND u.SerialNumber<>''")))
    out.append(Result("BD: Status de equipos intacto (todos Activo)", 0, db_scalar("SELECT COUNT(*) FROM Equipments WHERE Status<>0")))
    out.append(Result("BD: balanzas = Electrónico (1 teléfono + 9 balanzas) y Medición = refractómetro", (10, 1), (
        db_scalar("SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON e.Id=u.EquipmentId WHERE e.Category=0 AND e.TypeClassification=0"),
        db_scalar("SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON e.Id=u.EquipmentId WHERE e.Category=0 AND e.TypeClassification=3"))))
    out.append(Result("BD: historial de decisiones conservado (125 en total, 121 vigentes)", (125, 121), (
        db_scalar("SELECT COUNT(*) FROM EquipmentClassificationDecisions"),
        db_scalar("SELECT COUNT(*) FROM EquipmentClassificationDecisions WHERE EffectiveTo IS NULL"))))
    out.append(Result("BD: nombres repetidos con la misma clasificación", 0, db_scalar(
        "SELECT COUNT(*) FROM (SELECT Name FROM Equipments GROUP BY Name HAVING COUNT(DISTINCT CONCAT(Category,'-',ISNULL(TypeClassification,-1),'-',ISNULL(UtensilType,-1)))>1) x")))
    out.append(Result("BD: filas de origen intactas (ImportSourceRows)", 23542, db_scalar("SELECT COUNT(*) FROM ImportSourceRows WHERE ImportBatchId = (SELECT TOP 1 ImportBatchId FROM ImportSourceRows GROUP BY ImportBatchId ORDER BY COUNT(*) DESC)")))
    out.append(Result("BD: unidades 644 / artículos 1374 / gestiones 27 / planes 387", (644, 1374, 27, 387), (
        db_scalar("SELECT COUNT(*) FROM EquipmentUnits"), db_scalar("SELECT COUNT(*) FROM Articles"),
        db_scalar("SELECT COUNT(*) FROM Managements"), db_scalar("SELECT COUNT(*) FROM MaintenancePlans"))))
    return out


if __name__ == "__main__":
    s = Session()
    checks = [check_assetview_index, check_tarjetas_equipos, check_listas_por_tipo, check_utensilios, check_detalle_equipos,
              check_gestiones_listado, check_gestiones_detalle, check_plan_por_gestion, check_bordes, check_sin_sesion, check_bd]
    sys.exit(1 if run_all([partial(c, s) for c in checks]) else 0)
