"""
Catálogos / Grupos de estudiantes (Create, Edit, Delete): reglas del servidor por HTTP.
Solo contra la copia QA y SOLO en la gestión 2026-2 (Id 4): crear un grupo es una escritura grande, así que se usan materias de
semestres pequeños (Nutrición y dietética: semestre 7 con 1 materia y semestre 4 con 2) y se borran todos los grupos creados.
Letras con prefijo 'Q' (QZ, QY, QXW...) para no chocar con datos reales.
"""
import json
import re
import urllib.parse

from http_forms import parse_messages
from http_cases_catalogos_common import (check, check_404, check_anon, expect, run_module, scalar, sql, tokenless)

B = "/Catalogs/StudentGroups"
LABEL = "StudentGroups"
MG = 4          # gestión 2026-2 (Inactiva, editable)
MG_DONE = 1     # gestión 2025-1 (Completada)
CAREER = 3      # Nutrición y dietética
LETTER_MSG = "La letra del grupo debe tener de 1 a 3 letras (A, B, C...)."


def L(s):
    return f"{LABEL}/{s}"


def sg(gid):
    r = sql("select Id, Code, ManagementId, CareerId, Semester, Letter, Name, StudentCount, ScalingFactor, LaboratoryId, Status from StudentGroups where Id=?", gid)
    return tuple(r[0]) if r else None


def sg_by_name(name, mg=MG):
    return scalar("select top 1 Id from StudentGroups where Name=? and ManagementId=? order by Id desc", name, mg)


def cgs(gid):
    return [tuple(r) for r in sql("select Id, SubjectId, Name, TeacherPersonId, LaboratoryId, StudentCount, ScalingFactor, Status, ManagementId from CourseGroups where StudentGroupId=? order by SubjectId", gid)]


def body(letter, subjects, mg=MG, career=CAREER, sem=7, count="20", factor=None, lab=None, status=None):
    """subjects: [(subjectId, teacherId|None, labId|None, selected=True)]"""
    f = [("Input.ManagementId", mg), ("Input.CareerId", career), ("Input.Semester", sem), ("Input.Letter", letter)]
    if count is not None:
        f.append(("Input.StudentCount", count))
    if factor is not None:
        f.append(("Input.ScalingFactor", factor))
    if lab is not None:
        f.append(("Input.LaboratoryId", lab))
    if status is not None:
        f.append(("Input.Status", status))
    for i, s in enumerate(subjects):
        sid, teacher, slab = s[0], s[1], s[2]
        selected = s[3] if len(s) > 3 else True
        f.append((f"Input.Subjects[{i}].SubjectId", sid))
        if selected:
            f.append((f"Input.Subjects[{i}].Selected", "true"))
        if teacher is not None:
            f.append((f"Input.Subjects[{i}].TeacherPersonId", teacher))
        if slab is not None:
            f.append((f"Input.Subjects[{i}].LaboratoryId", slab))
    return f


def run(R, c):
    # ---------------------------------------------------------------- acceso sin sesión
    for p in (B, f"{B}/Create", f"{B}/Edit/1"):
        check_anon(R, LABEL, p)
    check_anon(R, LABEL, f"{B}/Create", post=True)
    check_anon(R, LABEL, f"{B}?handler=Delete&id=1", post=True)

    # ---------------------------------------------------------------- estado inicial (sin duplicados previos)
    n0 = scalar("select count(*) from StudentGroups")
    cg0 = scalar("select count(*) from CourseGroups where ManagementId=?", MG)
    pre = scalar("select count(*) from StudentGroups where ManagementId=? and CareerId=? and (Name like 'Q%7' or Name like 'Q%4')", MG, CAREER)
    check(R, L("Preparación: no hay grupos QAC previos en 2026-2 / Nutrición"), pre == 0, f"StudentGroups totales={n0}, previos={pre}")
    legacy_names = scalar("select count(*) from CourseGroups where ManagementId=? and SubjectId in (15,34,41) and Name like 'Q%'", MG)
    check(R, L("Preparación: no hay grupos de materia 'Q*' previos en las materias a usar"), legacy_names == 0, f"previos={legacy_names}")

    st, page = c.get(f"{B}/Create?managementId={MG}")
    cfg = json.loads(re.search(r'<script type="application/json" id="sgConfig">(.*?)</script>', page, re.S).group(1))
    teachers = [t["id"] for t in cfg["teachers"]]
    labs = [x["id"] for x in cfg["laboratories"]]
    T1, T2 = teachers[0], teachers[1]
    LAB = labs[0]
    tech = scalar("select top 1 Id from People where Category=1 and Status=0 order by Id")   # técnico: no es docente
    check(R, L("Preparación: hay docentes y laboratorios en el formulario"), len(teachers) >= 2 and labs and tech, f"docentes={len(teachers)} labs={len(labs)}")
    TOKEN = f"{B}/Create?managementId={MG}"

    def post(fields, name):
        return c.raw_post(f"{B}/Create", fields, token_from=TOKEN, name=name)

    S15 = (15, T1, None)
    # ---------------------------------------------------------------- GET Create / Index: gestión inexistente o completada
    st, page = c.get(f"{B}/Create?managementId={MG_DONE}")
    toasts = parse_messages(page)[1]
    check(R, L("Create GET en gestión completada: vuelve al listado con el error"), st == 200 and any("está completada: sus grupos son de solo lectura" in t for _, t in toasts), f"status={st} toasts={toasts[:2]}")
    st, page = c.get(f"{B}/Create?managementId=99999")
    toasts = parse_messages(page)[1]
    check(R, L("Create GET en gestión inexistente: aviso 'Active o elija una gestión semestral para registrar grupos.'"),
          st == 200 and any("Active o elija una gestión semestral para registrar grupos." in t for _, t in toasts), f"status={st} toasts={toasts[:2]}")
    st, page = c.get(f"{B}/Create?managementId={MG}&subjectId=15")
    check(R, L("Create GET con subjectId=15 precarga carrera 3 y semestre 7"), st == 200 and '"careerId":3,"semester":7' in page.replace(" ", ""), f"status={st}")
    st, _ = c.get(f"{B}/Create?managementId={MG}&subjectId=99999")
    check(R, L("Create GET con subjectId inexistente responde 200"), st == 200, f"status={st}")
    st, page = c.get(f"{B}?managementId={MG_DONE}")
    check(R, L("Index en gestión completada: 'Solo lectura' y sin 'Nuevo grupo'"), st == 200 and "Solo lectura" in page and "Nuevo grupo" not in page, f"status={st}")
    st, page = c.get(f"{B}?managementId=99999")
    check(R, L("Index con gestión inexistente: 'La gestión solicitada no existe.'"), st == 200 and any("La gestión solicitada no existe." in t for _, t in parse_messages(page)[1]), f"status={st}")
    for q in ({"careerId": "abc"}, {"careerId": "99999"}, {"managementId": "abc"}):
        st, _ = c.get(f"{B}?{urllib.parse.urlencode(q)}")
        check(R, L(f"Index: {q} responde 200"), st == 200, f"status={st}")
    st, page = c.get(f"{B}?managementId={MG}")
    check(R, L("Index 2026-2 sin grupos: 'Aún no hay grupos en 2026-2'"), n0 != 0 or "Aún no hay grupos en 2026-2" in page, f"status={st}")

    # ---------------------------------------------------------------- Create: validaciones (ninguna debe crear nada)
    for letter, desc in (("", "vacía"), (" ", "solo espacio"), ("ABCD", "4 letras"), ("A1", "letra y dígito"), ("1", "dígito"), ("Ñ", "Ñ"), ("A B", "con espacio interior"), ("A-", "con guion"), ("<b>", "HTML")):
        r = post(body(letter, [S15]), f"letra {desc}")
        expect(R, L(f"Create: letra {desc} ({letter!r})"), r, status=200, error=LETTER_MSG)
    r = post(body("QZ", [S15], mg=99999), "gestión inexistente")
    expect(R, L("Create: gestión inexistente"), r, status=200, error="La gestión seleccionada no existe o no es semestral.")
    r = post(body("QZ", [S15], mg=MG_DONE), "gestión completada")
    expect(R, L("Create: gestión completada (2025-1)"), r, status=200, error="La gestión 2025-1 está completada: sus grupos son de solo lectura.")
    r = post(body("QZ", [S15], career=99999), "carrera inexistente")
    expect(R, L("Create: carrera inexistente"), r, status=200, error="La carrera seleccionada no existe o no está activa.")
    for sem in ("0", "13", "-1"):
        r = post(body("QZ", [S15], sem=sem), f"semestre {sem}")
        expect(R, L(f"Create: semestre {sem}"), r, status=200, error="El semestre debe estar entre 1 y 12.")
    r = post(body("QZ", [S15], sem=""), "semestre vacío")
    expect(R, L("Create: semestre vacío"), r, status=200, error="El semestre debe estar entre 1 y 12.")
    for cnt in ("1001", "-1"):
        r = post(body("QZ", [S15], count=cnt), f"estudiantes {cnt}")
        expect(R, L(f"Create: estudiantes {cnt}"), r, status=200, error="El número de estudiantes debe estar entre 0 y 1000.")
    r = post(body("QZ", [S15], factor="1001"), "factor 1001")
    expect(R, L("Create: factor 1001"), r, status=200, error="El factor debe estar entre 0 y 1000.")
    r = post(body("QZ", [S15], factor="-1"), "factor -1")
    expect(R, L("Create: factor -1"), r, status=200, error="El factor debe estar entre 0 y 1000.")
    r = post(body("QZ", [S15], lab="99999"), "laboratorio habitual inexistente")
    expect(R, L("Create: laboratorio habitual inexistente"), r, status=200, error="El laboratorio habitual no existe o no está activo.")
    expect(R, L("Create: sin materias"), post(body("QZ", []), "sin materias"), status=200, error="Marque al menos una materia del semestre para el grupo.")
    expect(R, L("Create: materia sin marcar"), post(body("QZ", [(15, T1, None, False)]), "no marcada"), status=200, error="Marque al menos una materia del semestre para el grupo.")
    expect(R, L("Create: materia sin docente"), post(body("QZ", [(15, None, None)]), "sin docente"), status=200, error="Asigne el docente de Nutricion en el paciente critico.")
    expect(R, L("Create: docente que es técnico (no docente)"), post(body("QZ", [(15, tech, None)]), "técnico"), status=200,
           error="El docente elegido para Nutricion en el paciente critico no existe o no está activo.")
    expect(R, L("Create: docente inexistente"), post(body("QZ", [(15, 99999, None)]), "docente inexistente"), status=200,
           error="El docente elegido para Nutricion en el paciente critico no existe o no está activo.")
    expect(R, L("Create: materia de otro semestre de la misma carrera"), post(body("QZ", [(34, T1, None)]), "otro semestre"), status=200,
           error="La materia Dietoterapia i lnd no es una materia activa del 7.º semestre de la carrera.")
    expect(R, L("Create: materia de otra carrera"), post(body("QZ", [(20, T1, None)]), "otra carrera"), status=200,
           error="La materia Alimentos y bebidas iii lth no es una materia activa del 7.º semestre de la carrera.")
    expect(R, L("Create: materia inexistente"), post(body("QZ", [(99999, T1, None)]), "materia inexistente"), status=200, error="Una de las materias elegidas no existe.")
    expect(R, L("Create: materia repetida"), post(body("QZ", [(15, T1, None), (15, T2, None)]), "repetida"), status=200, error="Hay materias repetidas en el grupo.")
    expect(R, L("Create: laboratorio de la materia inexistente"), post(body("QZ", [(15, T1, 99999)]), "lab materia"), status=200,
           error="Uno de los laboratorios elegidos no existe o no está activo.")
    # choque con un grupo de materia suelto (legacy) con el mismo nombre
    legacy = c.raw_post("/Catalogs/CourseGroups/Create",
                        [("Input.ManagementId", MG), ("Input.SubjectId", 41), ("Input.Name", "QA4"), ("Input.TeacherPersonId", T1), ("Input.StudentCount", "5"), ("Input.SlotsJson", "[]")],
                        token_from="/Catalogs/CourseGroups/Create?handler=Legacy", name="grupo suelto QA4")
    legacy_id = scalar("select top 1 Id from CourseGroups where ManagementId=? and SubjectId=41 and Name='QA4' order by Id desc", MG)
    check(R, L("Preparación: grupo de materia suelto 'QA4' para la materia 41"), legacy.status == 302 and legacy_id is not None, f"status={legacy.status} errores={legacy.errors[:2]}")
    expect(R, L("Create: una materia ya tiene un grupo suelto con ese nombre (QA + semestre 4)"),
           post(body("QA", [(41, T1, None)], sem=4), "choque con grupo suelto"), status=200,
           error="La materia Tecnica dietoterapica lnd ya tiene un grupo QA4 en esta gestión (fuera de este grupo).")
    if legacy_id:
        c.submit(f"/Catalogs/CourseGroups/Delete/{legacy_id}", {})
    check(R, L("Create: ninguna validación fallida creó grupos ni materias de grupo"),
          scalar("select count(*) from StudentGroups") == n0 and scalar("select count(*) from CourseGroups where ManagementId=?", MG) == cg0,
          f"StudentGroups {n0}->{scalar('select count(*) from StudentGroups')}; CourseGroups {cg0}->{scalar('select count(*) from CourseGroups where ManagementId=?', MG)}")

    # ---------------------------------------------------------------- Create: válido
    r = post(body("qz", [(15, T1, None)], factor="5"), "g1 válido")
    expect(R, L("Create: válido (letra 'qz' en minúscula, semestre 7)"), r, status=302, redirect="/Catalogs/CourseGroups/Horario", toast_kind="success",
           toast="Grupo QZ7 creado con 1 materia(s). Ahora acomode su horario semanal.")
    g1 = sg_by_name("QZ7")
    row = sg(g1) if g1 else None
    check(R, L("Create: fila StudentGroups (nombre QZ7 = letra en mayúsculas + semestre, estudiantes 20, factor 5, Activo)"),
          row and row[6] == "QZ7" and row[5] == "QZ" and row[4] == 7 and row[3] == CAREER and row[2] == MG and row[7] == 20 and row[8] == 5 and row[10] == 0 and row[1], str(row))
    cg = cgs(g1) if g1 else []
    check(R, L("Create: se crea 1 grupo de materia con el nombre, docente, estudiantes y factor del grupo"),
          len(cg) == 1 and cg[0][1] == 15 and cg[0][2] == "QZ7" and cg[0][3] == T1 and cg[0][5] == 20 and cg[0][6] == 5 and cg[0][8] == MG, str(cg))
    r = post(body("QZ", [S15]), "duplicado")
    expect(R, L("Create: duplicado (mismo semestre/carrera, letra 'QZ')"), r, status=200, error="Ya existe el grupo QZ7 en este semestre de la carrera.")
    r = post(body("qz", [S15]), "duplicado minúscula")
    expect(R, L("Create: duplicado con la letra en minúscula"), r, status=200, error="Ya existe el grupo QZ7 en este semestre de la carrera.")
    r = post(body(" qz ", [S15]), "duplicado con espacios")
    expect(R, L("Create: duplicado con espacios alrededor de la letra"), r, status=200, error="Ya existe el grupo QZ7 en este semestre de la carrera.")
    check(R, L("Create: tras los duplicados sigue habiendo un solo QZ7"), scalar("select count(*) from StudentGroups where Name='QZ7' and ManagementId=?", MG) == 1)
    # letra de 3 letras (límite superior) y con laboratorio habitual + laboratorio propio de la materia
    r = post(body("qxw", [(15, T2, LAB)], lab=str(LAB), count="0", factor="0"), "g3 3 letras")
    expect(R, L("Create: letra de 3 letras, estudiantes 0, factor 0, laboratorio habitual y de la materia"), r, status=302, redirect="/Catalogs/CourseGroups/Horario", toast="Grupo QXW7 creado")
    g3 = sg_by_name("QXW7")
    check(R, L("Create: QXW7 con estudiantes=0, factor=0 y laboratorio"), g3 and sg(g3)[7] == 0 and sg(g3)[8] == 0 and sg(g3)[9] == LAB, str(sg(g3) if g3 else None))
    # grupo con 2 materias en el semestre 4 (se usará para probar agregar/quitar materias en Edit)
    r = post(body("QY", [(34, T1, None)], sem=4), "g2")
    expect(R, L("Create: grupo QY4 con una de las 2 materias del semestre 4"), r, status=302, redirect="/Catalogs/CourseGroups/Horario", toast="Grupo QY4 creado con 1 materia(s).")
    g2 = sg_by_name("QY4")
    # estudiantes no numéricos: el ModelState no se revisa
    r = post(body("QV", [S15], count="abc"), "estudiantes abc")
    gv = sg_by_name("QV7")
    check(R, L("Create: estudiantes='abc' no crea un grupo con el dato perdido (se rechaza o se informa)"), gv is None, f"status={r.status} errores={r.errors[:2]} fila={sg(gv) if gv else None}")
    r = post(body("QV", [S15], count=""), "estudiantes vacío")
    gv2 = sg_by_name("QV7")
    check(R, L("Create: estudiantes vacío crea el grupo con NULL (opcional)"), gv2 is not None and sg(gv2)[7] is None, f"status={r.status} fila={sg(gv2) if gv2 else None}")
    extra = [x for x in (gv, gv2) if x]
    tokenless(R, c, L("Create"), f"{B}/Create", body("QWT", [S15]))
    check(R, L("Create sin token no creó nada"), sg_by_name("QWT7") is None)

    # ---------------------------------------------------------------- Index con el grupo creado
    st, page = c.get(f"{B}?managementId={MG}")
    check(R, L("Index: el grupo QZ7 aparece listado con acciones de Editar"), st == 200 and "QZ7" in page and f"{B}/Edit/{g1}" in page, f"status={st}")
    st, page = c.get(f"{B}?managementId={MG}&careerId={CAREER}")
    check(R, L("Index: filtro por carrera 3 muestra QZ7"), "QZ7" in page)
    st, page = c.get(f"{B}?managementId={MG}&careerId=1")
    check(R, L("Index: un filtro por carrera sin grupos se ignora (muestra todos) y no da error"), st == 200 and "QZ7" in page, f"status={st}")

    # ---------------------------------------------------------------- Edit
    st, _ = c.get(f"{B}/Edit/{g1}")
    check(R, L("Edit GET del grupo existente responde 200"), st == 200, f"status={st}")
    st, page = c.get(f"{B}/Edit/99999")
    check(R, L("Edit GET grupo inexistente: vuelve al listado con 'El grupo no existe.'"), st == 200 and any("El grupo no existe." in t for _, t in parse_messages(page)[1]), f"status={st}")
    check_404(R, c, LABEL, f"{B}/Edit/abc")
    TOK_EDIT = f"{B}/Edit/{g1}"

    def epost(gid, fields, name):
        return c.raw_post(f"{B}/Edit/{gid}", fields, token_from=f"{B}/Edit/{gid}", name=name)

    r = epost(99999, body("QZ", [S15], status="0"), "edit inexistente")
    expect(R, L("Edit POST grupo inexistente (no es 404: vuelve al listado con el error)"), r, status=302, redirect=B, toast="El grupo no existe.", toast_kind="error")
    r = epost(g1, body("QW", [S15], status="0", factor="5"), "renombrar")
    expect(R, L("Edit: cambiar la letra QZ -> QW (válido)"), r, status=302, redirect=B, toast_kind="success", toast="Grupo QW7 actualizado.")
    check(R, L("Edit: StudentGroups y CourseGroups pasan a llamarse QW7"), sg(g1)[6] == "QW7" and sg(g1)[5] == "QW" and all(x[2] == "QW7" for x in cgs(g1)), f"{sg(g1)} {cgs(g1)}")
    for letter, desc in (("", "vacía"), ("ABCD", "4 letras"), ("A1", "letra y dígito"), ("Ñ", "Ñ")):
        r = epost(g1, body(letter, [S15], status="0"), f"edit letra {desc}")
        expect(R, L(f"Edit: letra {desc}"), r, status=200, error=LETTER_MSG)
    r = epost(g3, body("QW", [(15, T2, LAB)], status="0"), "edit duplicado")
    expect(R, L("Edit: letra QW ya usada por QW7 en el mismo semestre/carrera"), r, status=200, error="Ya existe el grupo QW7 en este semestre de la carrera.")
    check(R, L("Edit: el duplicado no cambió QXW7"), sg(g3)[6] == "QXW7")
    r2 = epost(g1, body("QW", [], status="0"), "sin materias vacío")
    expect(R, L("Edit: sin materias marcadas"), r2, status=200, error="Marque al menos una materia del semestre para el grupo.")
    check(R, L("Edit: sin materias marcadas no quitó la materia del grupo"), len(cgs(g1)) == 1)
    r = epost(g1, body("QW", [(15, None, None)], status="0"), "sin docente")
    expect(R, L("Edit: materia sin docente"), r, status=200, error="Asigne el docente de Nutricion en el paciente critico.")
    r = epost(g1, body("QW", [(15, tech, None)], status="0"), "docente técnico")
    expect(R, L("Edit: docente que es técnico"), r, status=200, error="El docente elegido para Nutricion en el paciente critico no existe o no está activo.")
    r = epost(g1, body("QW", [S15], status="0", count="1001"), "estudiantes 1001")
    expect(R, L("Edit: estudiantes 1001"), r, status=200, error="El número de estudiantes debe estar entre 0 y 1000.")
    r = epost(g1, body("QW", [S15], status="0", lab="99999"), "lab inexistente")
    expect(R, L("Edit: laboratorio habitual inexistente"), r, status=200, error="El laboratorio habitual no existe o no está activo.")
    r = epost(g1, body("QW", [(34, T1, None)], status="0"), "materia de otro semestre")
    expect(R, L("Edit: materia de otro semestre"), r, status=200, error="no es una materia activa del 7.º semestre de la carrera.")
    # cambiar docente / estudiantes / factor / laboratorio
    r = epost(g1, body("QW", [(15, T2, LAB)], status="0", count="35", factor="7", lab=str(LAB)), "cambiar docente")
    expect(R, L("Edit: cambiar docente, estudiantes, factor y laboratorio"), r, status=302, redirect=B, toast="Grupo QW7 actualizado.")
    cg = cgs(g1)
    check(R, L("Edit: se sincroniza docente/estudiantes/factor/laboratorio en la materia del grupo"),
          sg(g1)[7] == 35 and sg(g1)[8] == 7 and cg[0][3] == T2 and cg[0][5] == 35 and cg[0][6] == 7 and cg[0][4] == LAB, f"{sg(g1)} {cg}")
    # gestión/carrera/semestre manipulados: se ignoran
    r = epost(g1, body("QW", [(15, T2, LAB)], mg=MG_DONE, career=1, sem=1, status="0", count="35", factor="7", lab=str(LAB)), "tamper")
    expect(R, L("Edit: gestión/carrera/semestre manipulados en el POST se ignoran"), r, status=302, redirect=B)
    check(R, L("Edit: tras el POST manipulado la gestión, carrera y semestre del grupo no cambiaron"), sg(g1)[2] == MG and sg(g1)[3] == CAREER and sg(g1)[4] == 7, str(sg(g1)))
    # estado
    r = epost(g1, body("QW", [(15, T2, LAB)], status="1", count="35", factor="7", lab=str(LAB)), "inactivo")
    expect(R, L("Edit: Inactivo"), r, status=302, redirect=B)
    check(R, L("Edit: Inactivo se propaga a la materia del grupo"), sg(g1)[10] == 1 and all(x[7] == 1 for x in cgs(g1)), f"{sg(g1)} {cgs(g1)}")
    r = epost(g1, body("QW", [(15, T2, LAB)], status="2", count="35", factor="7", lab=str(LAB)), "eliminado forzado")
    check(R, L("Edit: Estado=Eliminado forzado por POST no elimina el grupo (queda Activo)"), r.status == 302 and sg(g1)[10] == 0, f"status={r.status} Status BD={sg(g1)[10]}")
    r = epost(g1, body("QW", [(15, T2, LAB)], status="7", count="35", factor="7", lab=str(LAB)), "estado 7")
    check(R, L("Edit: Estado=7 (fuera del enum) no da 500 y no corrompe el grupo"), r.status in (200, 302) and sg(g1)[10] in (0, 1), f"status={r.status} errores={r.errors[:2]} Status BD={sg(g1)[10]}")
    tokenless(R, c, L("Edit"), f"{B}/Edit/{g1}", body("QW", [S15], status="0"))
    # agregar y quitar materias en el grupo QY4 (semestre 4: materias 34 y 41)
    r = epost(g2, body("QY", [(34, T1, None), (41, T2, None)], sem=4, status="0"), "agregar materia")
    expect(R, L("Edit: marcar una 2.ª materia del semestre la agrega al grupo"), r, status=302, redirect=B, toast="Grupo QY4 actualizado.")
    check(R, L("Edit: el grupo QY4 tiene ahora 2 materias (34 y 41)"), [x[1] for x in cgs(g2)] == [34, 41], str(cgs(g2)))
    r = epost(g2, body("QY", [(34, T1, None, False), (41, T2, None)], sem=4, status="0"), "quitar materia")
    expect(R, L("Edit: desmarcar una materia la quita del grupo (borrado físico)"), r, status=302, redirect=B)
    check(R, L("Edit: QY4 queda solo con la materia 41 y la materia 34 ya no tiene grupo QY4"),
          [x[1] for x in cgs(g2)] == [41] and scalar("select count(*) from CourseGroups where SubjectId=34 and Name='QY4'") == 0, str(cgs(g2)))
    r = epost(g2, body("QY", [(34, T1, None), (34, T2, None)], sem=4, status="0"), "repetida")
    expect(R, L("Edit: materia repetida"), r, status=200, error="Hay materias repetidas en el grupo.")

    # ---------------------------------------------------------------- Delete
    def dpost(gid, name):
        return c.raw_post(f"{B}?handler=Delete&id={gid}", [], token_from=f"{B}?managementId={MG}", name=name)

    r = dpost(99999, "borrar inexistente")
    expect(R, L("Delete: grupo inexistente"), r, status=302, toast="El grupo no existe.", toast_kind="error")
    tokenless(R, c, L("Delete"), f"{B}?handler=Delete&id={g1}", [])
    check(R, L("Delete sin token no borró nada"), sg(g1) is not None)
    cg_ids = [x[0] for x in cgs(g1)]
    r = dpost(g1, "borrar g1")
    expect(R, L("Delete: grupo con su materia"), r, status=302, redirect=B, toast_kind="success",
           toast="Grupo QW7 eliminado junto con sus materias y horario. Las planificaciones conservan el nombre del grupo.")
    check(R, L("Delete: borrado físico del grupo y de sus materias"), sg(g1) is None and all(scalar("select count(*) from CourseGroups where Id=?", i) == 0 for i in cg_ids), f"{sg(g1)}")
    st, page = c.get(f"{B}?managementId={MG}")
    check(R, L("Index: el grupo borrado desaparece del listado"), f"{B}/Edit/{g1}" not in page)
    r = dpost(g1, "borrar de nuevo")
    expect(R, L("Delete: borrar de nuevo el mismo grupo"), r, status=302, toast="El grupo no existe.", toast_kind="error")

    # ---------------------------------------------------------------- limpieza
    for gid in (g3, g2, *extra):
        if gid and sg(gid):
            dpost(gid, "limpieza")
    after_sg = scalar("select count(*) from StudentGroups")
    after_cg = scalar("select count(*) from CourseGroups where ManagementId=?", MG)
    check(R, L("Limpieza: StudentGroups y CourseGroups de 2026-2 vuelven al recuento inicial"), after_sg == n0 and after_cg == cg0,
          f"StudentGroups {n0}->{after_sg}; CourseGroups {cg0}->{after_cg}")


if __name__ == "__main__":
    run_module("studentgroups", run)
