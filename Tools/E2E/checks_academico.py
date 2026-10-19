"""
Prueba E2E (solo lectura) del grupo ACADEMICO y ARTICULOS contra la app local y la BD REAL.

Pantallas cubiertas (URL -> como se obtienen todos los registros):
- /Catalogs/Articles            : paginado ?pageIndex=N (20/pag) recorriendo todas las paginas
                                  via enlaces Edit/{id}; BD: Articles con Status<>2 (esperado 1374).
- /Catalogs/ArticleCategories   : igual; BD Status<>2 (10).
- /Catalogs/MeasurementUnits    : igual; BD Status<>2 (5).
- /Catalogs/Subjects            : igual; BD Status<>2 (41).
- /Catalogs/Practices           : igual; BD Status<>2 (196) + receta en Details/{id} de 3 practicas
                                  vs PracticeRequirements (IsRemoved=0, total BD 2111).
- /Catalogs/Stock               : una tabla por laboratorio; se itera ?LaboratoryFilter={id} para
                                  todos los laboratorios con existencias y se suma; BD: ArticleStocks (700).
- /Catalogs/StudentGroups       : sin paginacion; gestiona activa por defecto; BD StudentGroups (0).
- /Catalogs/CourseGroups        : exige filtro de gestion (por defecto solo la activa): se itera
                                  ?ManagementFilter={id} por cada gestion con grupos; BD Status<>2 (147).
- /LaboratoryOperations/Planificaciones : lista completa por gestion sin paginacion: se itera
                                  ?managementId={id}; BD: Plannings IsDeleted=0 (1037: 559 en 2026-1,
                                  478 en 2026-2). 4 con laboratorio vacio (la pantalla los muestra como
                                  "—", no como «Sin ambiente») y 9 con fecha 14/10/2026 en 2026-1
                                  (8 mas en 2026-2). Detalle L-3 de 2 planificaciones vs PlanningRequirements.
- Salud simple: /LaboratoryOperations (panel), Grilla, CalendarioAcademico, Demanda, Reposiciones,
  /Cierre, /Schedules (vease notas en check_salud y check_schedules).
"""
import re
import sys
from functools import partial
from urllib.parse import quote

from e2e_lib import Result, Session, db_scalar, db_rows, run_all

PAG = "pageIndex"  # parametro de paginacion comun en los listados


def _badge(text, unidad):
    """Lee el contador visible "N unidad-del-listado" de una pantalla (-1 si no aparece)."""
    m = re.search(rf"(\d+) {re.escape(unidad)}", text)
    return int(m.group(1)) if m else -1


def _ids_edit(s, ruta, regex):
    """Recorre todas las paginas de un listado y devuelve los ids unicos via enlaces Edit."""
    return s.collect(ruta, regex, page_param=PAG)


def check_articles_total(s):
    esperado = db_scalar("SELECT COUNT(*) FROM Articles WHERE Status<>2")
    ids = _ids_edit(s, "/Catalogs/Articles", r"/Catalogs/Articles/Edit/(\d+)")
    return Result("Articles: total visible vs BD", esperado, len(ids),
                  "recorriendo ?pageIndex=1..N (20/pag) via enlaces Edit")


def check_articles_search(s):
    """Busca 3 articulos concretos por codigo (incluido el de nombre repetido ART-L33-0256,
    cuyo nombre en BD ya trae el sufijo "Paila para chicharron (ART-L33-0256)")."""
    codigo_duplicado = db_scalar("SELECT Code FROM Articles WHERE Code='ART-L33-0256' AND Status<>2")
    extras = db_rows("SELECT MIN(Id), MAX(Id) FROM Articles WHERE Status<>2")
    codigos = [codigo_duplicado] + [db_scalar("SELECT Code FROM Articles WHERE Id=?", i) for i in tuple(extras[0])]
    out = []
    for codigo in codigos:
        _, html = s.get(f"/Catalogs/Articles?SearchTerm={quote(codigo)}")
        enlaces = set(re.findall(r"/Catalogs/Articles/Edit/(\d+)", html))
        # La pantalla busca por Name o Code con Contains: se replica el mismo filtro en BD.
        n_bd = db_scalar("SELECT COUNT(*) FROM Articles WHERE Status<>2 AND (Code LIKE ? OR Name LIKE ?)",
                         f"%{codigo}%", f"%{codigo}%")
        badge = re.search(r'<strong class="text-dark">(\d+)</strong> artículo\(s\)', html)
        out.append(Result(f"Articles: busqueda '{codigo}'", n_bd, len(enlaces),
                          f"contador visible={badge.group(1) if badge else '?'}"))
    return out


def _check_catalogo_simple(s, ruta, regex_enlace, unidad, sql, nombre):
    esperado = db_scalar(sql)
    ids = _ids_edit(s, ruta, regex_enlace)
    return Result(nombre, esperado, len(ids), f"listado {ruta} con ?{PAG}=1..N")


def check_categories(s):
    return _check_catalogo_simple(s, "/Catalogs/ArticleCategories", r"/Catalogs/ArticleCategories/Edit/(\d+)",
                                  "categoría(s)", "SELECT COUNT(*) FROM ArticleCategories WHERE Status<>2",
                                  "ArticleCategories: total visible vs BD")


def check_units(s):
    return _check_catalogo_simple(s, "/Catalogs/MeasurementUnits", r"/Catalogs/MeasurementUnits/Edit/(\d+)",
                                  "unidad(es)", "SELECT COUNT(*) FROM MeasurementUnits WHERE Status<>2",
                                  "MeasurementUnits: total visible vs BD")


def check_subjects(s):
    return _check_catalogo_simple(s, "/Catalogs/Subjects", r"/Catalogs/Subjects/Edit/(\d+)",
                                  "materia(s)", "SELECT COUNT(*) FROM Subjects WHERE Status<>2",
                                  "Subjects: total visible vs BD")


def check_practices(s):
    return _check_catalogo_simple(s, "/Catalogs/Practices", r"/Catalogs/Practices/Details/(\d+)",
                                  "práctica(s)", "SELECT COUNT(*) FROM Practices WHERE Status<>2",
                                  "Practices: total visible vs BD")


def check_practices_recipes(s):
    """Abre Details de 3 practicas (las de receta mas larga) y compara las lineas visibles
    (cada linea de receta renderiza un boton con data-quantity=) con PracticeRequirements."""
    esperado_total = db_scalar("SELECT COUNT(*) FROM PracticeRequirements WHERE IsRemoved=0")
    out = [Result("Practices: total recetas en BD (agregado)", 2111, esperado_total,
                  "guarda del conjunto de datos; el detalle por practica se valida en pantalla")]
    top3 = db_rows("SELECT TOP 3 r.PracticeId, COUNT(*) n FROM PracticeRequirements r "
                   "WHERE r.IsRemoved=0 GROUP BY r.PracticeId ORDER BY n DESC")
    for pid, n_bd in top3:
        _, html = s.get(f"/Catalogs/Practices/Details/{pid}")
        visibles = len(re.findall(r'data-quantity="', html))
        out.append(Result(f"Practices: receta Details/{pid} vs BD", n_bd, visibles,
                          "lineas con boton de edicion (data-quantity)"))
    return out


def check_stock(s):
    """La pantalla solo muestra UN laboratorio a la vez (defecto ECONO) y lista TODOS los articulos
    activos de ese laboratorio; las filas con registro real son las que no dicen "sin registro".
    Se itera ?LaboratoryFilter={id} por cada laboratorio con existencias en BD y se suma."""
    labs = db_rows("SELECT s.LaboratoryId, l.Code, COUNT(*) FROM ArticleStocks s "
                   "JOIN Laboratories l ON l.Id=s.LaboratoryId GROUP BY s.LaboratoryId, l.Code ORDER BY l.Code")
    out, suma = [], 0
    for lab_id, codigo, n_bd in labs:
        # 2026-10-18 (D-6): el listado pagina de a 20; se recorren todas las páginas del laboratorio.
        _, text = s.text(f"/Catalogs/Stock?LaboratoryFilter={lab_id}")
        articulos = _badge(text, "artículo(s)")
        sin_registro = text.count("sin registro")
        for pagina in range(2, -(-max(articulos, 0) // 20) + 1):
            _, t = s.text(f"/Catalogs/Stock?LaboratoryFilter={lab_id}&pageIndex={pagina}")
            sin_registro += t.count("sin registro")
        visibles = articulos - sin_registro
        suma += visibles
        out.append(Result(f"Stock: laboratorio {codigo} vs BD", n_bd, visibles,
                          f"filas visibles={articulos} - {sin_registro} sin registro"))
    total_bd = db_scalar("SELECT COUNT(*) FROM ArticleStocks")
    out.append(Result("Stock: total ArticleStocks sumando laboratorios", total_bd, suma, "iterando LaboratoryFilter"))
    return out


def check_student_groups(s):
    """Sin paginacion y siempre acotado a una gestion (la activa por defecto). BD: StudentGroups."""
    esperado = db_scalar("SELECT COUNT(*) FROM StudentGroups")
    _, text = s.text("/Catalogs/StudentGroups")
    return Result("StudentGroups: total visible vs BD (gestion activa)", esperado,
                  _badge(text, "grupo(s)"), "pantalla sin paginacion; BD total=0")


def check_course_groups(s):
    """Por defecto solo muestra la gestion activa: se itera ?ManagementFilter={id} para cada
    gestion con grupos en BD y se suma (CourseGroups con Status<>2 = 147)."""
    por_gestion = db_rows("SELECT ManagementId, COUNT(*) FROM CourseGroups WHERE Status<>2 "
                          "GROUP BY ManagementId ORDER BY ManagementId")
    out, suma = [], 0
    for mid, n_bd in por_gestion:
        ids = _ids_edit(s, f"/Catalogs/CourseGroups?ManagementFilter={mid}", r"/Catalogs/CourseGroups/Edit/(\d+)")
        suma += len(ids)
        out.append(Result(f"CourseGroups: gestion {mid} visible vs BD", n_bd, len(ids),
                          f"iterando ManagementFilter={mid} con ?{PAG}=1..N"))
    out.append(Result("CourseGroups: total Status<>2 sumando gestiones", 147, suma,
                      "BD 55 (gestion 3) + 51 (gestion 4) + 41 (gestion 27)"))
    return out


def check_planificaciones(s):
    """La pantalla lista la gestion completa sin paginacion: se itera ?managementId={id} por cada
    gestion con planificaciones y se comparan los codigos PRA-##### visibles con la BD."""
    por_gestion = db_rows("SELECT p.ManagementId, m.Code, COUNT(*) FROM Plannings p "
                          "JOIN Managements m ON m.Id=p.ManagementId WHERE p.IsDeleted=0 "
                          "GROUP BY p.ManagementId, m.Code ORDER BY m.Code")
    out, suma = [], 0
    for mid, codigo, n_bd in por_gestion:
        _, text = s.text(f"/LaboratoryOperations/Planificaciones?managementId={mid}")
        visibles = len(re.findall(r"PRA-\d{5,}", text))
        suma += visibles
        out.append(Result(f"Planificaciones: gestion {codigo} visible vs BD", n_bd, visibles,
                          f"contador visible={_badge(text, 'planificación(es)')}"))
        # La fecha 14/10/2026 (dd/MM) de las L-3 de 2026 debe aparecer: 9 en 2026-1 (8 en 2026-2).
        n_fecha_bd = db_scalar("SELECT COUNT(*) FROM Plannings WHERE IsDeleted=0 AND ManagementId=? "
                               "AND CONVERT(date, ScheduledDate)='2026-10-14'", mid)
        visibles_fecha = len(re.findall(r"14/10/2026", text))
        out.append(Result(f"Planificaciones {codigo}: sesiones del 14/10/2026 visibles", n_fecha_bd, visibles_fecha,
                          "contador de la fecha en las filas dd/MM/yyyy"))
    out.append(Result("Planificaciones: total IsDeleted=0 sumando gestiones", 1037, suma,
                      "BD 559 (2026-1) + 478 (2026-2)"))
    # Hallazgo: las 4 planificaciones sin laboratorio se ven como "—"; la etiqueta «Sin ambiente»
    # solo existe en Reposiciones. Se valida el dato en BD como guarda del conjunto esperado.
    sin_lab = db_scalar("SELECT COUNT(*) FROM Plannings WHERE IsDeleted=0 AND LaboratoryId IS NULL")
    out.append(Result("Planificaciones: sin ambiente en BD (pantalla muestra '—')", 4, sin_lab,
                      "la pantalla no etiqueta 'Sin ambiente' en Planificaciones"))
    return out


def check_planificaciones_detalle(s):
    """Abre Requerimientos (paso 2 del L-3) de 2 planificaciones y compara las filas visibles
    (cada requerimiento renderiza un tr con data-focus-key) con PlanningRequirements en BD."""
    out = []
    top2 = db_rows("SELECT TOP 2 d.PlanningId, COUNT(*) n FROM PlanningRequirements d "
                   "JOIN Plannings p ON p.Id=d.PlanningId WHERE p.IsDeleted=0 "
                   "GROUP BY d.PlanningId ORDER BY n DESC")
    for pid, n_bd in top2:
        _, html = s.get(f"/LaboratoryOperations/Requerimientos?planId={pid}")
        visibles = len(re.findall(r'data-focus-key="art-', html))
        out.append(Result(f"L-3 PRA-{pid:05d}: requerimientos visibles vs BD", n_bd, visibles,
                          "filas de la tabla de requerimientos"))
    total = db_scalar("SELECT COUNT(*) FROM PlanningRequirements")
    out.append(Result("PlanningRequirements: total en BD (referencia)", 11451, total,
                      "guarda del conjunto; el detalle por planificacion se valida arriba"))
    return out


def check_cierre(s):
    """/Cierre solo muestra el dia actual (con rol de gestion se puede cambiar ?Date=).
    Si el usuario no tiene el rol se reporta sin comparacion para no falsar la prueba."""
    _, text = s.text("/Cierre")
    visibles = len(re.findall(r"PRA-\d{5,}", text))
    if re.search(r"permiso|denegad", text, re.I):
        return Result("Cierre: sesiones de hoy visibles (sin permiso de rol)", "n/d", visibles,
                      "el usuario E2E no tiene rol de cierre")
    esperado = db_scalar("SELECT COUNT(*) FROM Plannings WHERE IsDeleted=0 "
                         "AND CONVERT(date, ScheduledDate)=CONVERT(date, GETDATE()) "
                         "AND Status NOT IN ('Cancelada','Reprogramada','PendienteReposicion','Repuesta')")
    return Result("Cierre: sesiones de hoy visibles vs BD", esperado, visibles,
                  "la pantalla no permite ver el resto de la BD (solo el dia elegido)")


def check_schedules(s):
    """HALLAZGO: /Schedules NO lista las programaciones PracticeSchedules (PRG-#####): muestra los
    ManagementPlans (mantenimiento de equipos) de la gestion ACTIVA unicamente. Las programaciones
    de practicas no tienen pantalla propia: se acceden desde la planificacion (wizard) o la grilla.
    Se compara el contador visible con la BD de ManagementPlans y se guarda el total 869 en BD."""
    _, text = s.text("/Schedules")
    activa = db_scalar("SELECT TOP 1 Id FROM Managements WHERE Status=0 ORDER BY Year DESC, Semester DESC")
    esperado = db_scalar("SELECT COUNT(*) FROM ManagementPlans WHERE ManagementId=?", activa)
    out = [Result("Schedules: intervenciones visibles vs BD (ManagementPlans gestion activa)",
                  esperado, _badge(text, "intervención(es)"),
                  "pantalla acotada a la gestion activa; sin paginacion")]
    programaciones = db_scalar("SELECT COUNT(*) FROM PracticeSchedules")
    out.append(Result("PracticeSchedules en BD (sin pantalla que las liste)", 869, programaciones,
                      "hallazgo: no existe listado de programaciones PRG- en la app"))
    return out


def check_salud(s):
    """Check de salud (200 sin excepcion) para todas las pantallas del grupo."""
    rutas = [
        "/Catalogs/Articles",
        "/Catalogs/ArticleCategories",
        "/Catalogs/MeasurementUnits",
        "/Catalogs/Subjects",
        "/Catalogs/Practices",
        "/Catalogs/Stock",
        "/Catalogs/StudentGroups",
        "/Catalogs/CourseGroups",
        "/LaboratoryOperations",
        "/LaboratoryOperations/Planificaciones",
        "/LaboratoryOperations/Grilla",
        "/LaboratoryOperations/CalendarioAcademico",
        "/LaboratoryOperations/Demanda",
        "/LaboratoryOperations/Reposiciones",
        "/Cierre",
        "/Schedules",
    ]
    return [Result(f"Salud {ruta}", True, s.healthy(ruta)) for ruta in rutas]


CHECKS = [
    check_articles_total,
    check_articles_search,
    check_categories,
    check_units,
    check_subjects,
    check_practices,
    check_practices_recipes,
    check_stock,
    check_student_groups,
    check_course_groups,
    check_planificaciones,
    check_planificaciones_detalle,
    check_cierre,
    check_schedules,
    check_salud,
]

if __name__ == "__main__":
    session = Session()
    sys.exit(1 if run_all([partial(c, session) for c in CHECKS]) else 0)
