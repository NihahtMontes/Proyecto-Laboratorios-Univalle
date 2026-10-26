"""
Utilidades comunes de las pruebas HTTP de inventario y procesos de mantenimiento (L-6 -> L-7 -> L-8 -> L-3 -> Kardex -> L-12).
Solo contra la copia desechable de QA (http_forms ya se niega a correr con otra base). No es un caso de prueba por si mismo.

- PClient: Client con deteccion de anomalias (500, pagina de error, texto en ingles) y textarea sin el salto de linea inicial que el navegador descarta.
- lab_propio(): crea (o reutiliza) un laboratorio "QAP Lab <modulo>" en la facultad 1 para no pisar planes de otros trabajadores.
- nueva_unidad() / planificar(): crean unidades propias (inventario QAP<run><sufijo>) y las agregan a la gestion preventiva activa 30 por Managements/PlanAssets.
- l6(), l7(), l8(), l3(), kardex(), l12(): recorren el wizard por HTTP como lo hace la interfaz; llevar_a() deja un plan nuevo en la fase pedida.
- plan() / unidad() / sql(): lecturas SOLO LECTURA a la base QA para comprobar el efecto real.
- finish(): imprime la tabla, anomalias y escribe outputs/qa-usuario/http_cases_procesos_<modulo>.txt
"""
import contextlib
import io
import json
import os
import random
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from datetime import date, timedelta
from pathlib import Path

from lxml import html as LH

import e2e_lib
from http_forms import Client, Result as HttpResult, _NoRedirect, check, expect, parse_messages  # noqa: F401

BASE = e2e_lib.BASE
OUT_DIR = Path(__file__).resolve().parents[2] / "outputs" / "qa-usuario"

# ---- constantes del escenario (ver la exploracion en el informe) ----
MGMT = 30          # gestion Preventiva Activa 2027-2
FAC = 1            # facultad de la gestion
EQUIP = 1          # definicion de equipo Confirmed (Extractor de humos y grasas)
CAREER = 1
TECH_INT = 1       # persona interna (tecnico interno)
TECH_EXT = 4       # persona externa (Casatermo srl)
MGMT_CERRADA = 27  # gestion Completada (solo para el sondeo de "gestion cerrada")

ANOMALIES: list[str] = []
SEEN_ERR: list[str] = []
_EN = re.compile(r"\b(The (field|value)|is invalid|is not valid|field is required|must be|must not|A value is required|Failed to|Unhandled|An error|maximum length|minimum length)\b")
_ERR_PAGE = re.compile(r"Codigo tecnico de seguimiento|An unhandled exception|NullReferenceException|InvalidOperationException|SqlException|Stack trace|DbUpdateException", re.I)

HOY = date.today()


def d(offset=0):
    return (HOY + timedelta(days=offset)).isoformat()


def note(where, status, text, errors=None, toasts=None, location=None):
    errors = errors or []
    toasts = toasts or []
    if status >= 500:
        ANOMALIES.append(f"HTTP {status} en {where}")
    if location and "/Error" in location and "ReturnUrl" not in location:   # /Error?ReturnUrl=... es el AccessDenied normal
        ANOMALIES.append(f"redirect a /Error desde {where} -> {location}")
    if text and _ERR_PAGE.search(re.sub(r"<script.*?</script>", "", text, flags=re.S)):
        ANOMALIES.append(f"pagina de error/excepcion visible en {where} (status {status})")
    for e in errors:
        if _EN.search(e):
            ANOMALIES.append(f"texto en ingles en validacion de {where}: {e!r}")
    for k, t in toasts:
        if _EN.search(t):
            ANOMALIES.append(f"texto en ingles en toast ({k}) de {where}: {t[:230]!r}")


class PClient(Client):
    def get(self, path):
        status, text = super().get(path)
        note(f"GET {path[:100]}", status, text)
        return status, text

    def _post(self, action, fields, follow, name):
        res = super()._post(action, fields, follow, name)
        note(f"POST {action.replace(BASE, '')[:100]} [{name}]", res.status, res.text, res.errors, res.toasts, res.location)
        return res

    def form(self, path, action_contains=None, index=0):
        status, page = self.get(path)
        if status != 200:
            raise RuntimeError(f"GET {path} -> {status}")
        doc = LH.fromstring(page)
        forms = [f for f in doc.xpath("//form[translate(@method,'POST','post')='post']")
                 if not (f.get("action") or "").endswith("/Logout") and f.xpath(".//input[@name='__RequestVerificationToken']")]
        if action_contains:
            forms = [f for f in forms if action_contains in (f.get("action") or "")]
        if not forms:
            raise RuntimeError(f"Sin formulario POST en {path} ({action_contains or 'cualquiera'})")
        f = forms[index]
        action = urllib.parse.urljoin(BASE + path, f.get("action") or path)
        fields = []
        for el in f.xpath(".//input|.//select|.//textarea"):
            name = el.get("name")
            if not name or el.get("disabled") is not None:
                continue
            tag = el.tag
            if tag == "input":
                typ = (el.get("type") or "text").lower()
                if typ in ("submit", "button", "image", "file", "reset"):
                    continue
                if typ in ("checkbox", "radio"):
                    if el.get("checked") is not None:
                        fields.append((name, el.get("value") or "on"))
                    continue
                fields.append((name, el.get("value") or ""))
            elif tag == "textarea":
                txt = el.text_content()
                if txt.startswith("\r\n"):
                    txt = txt[2:]
                elif txt.startswith("\n"):
                    txt = txt[1:]
                fields.append((name, txt))
            elif tag == "select":
                opts = el.xpath(".//option")
                chosen = [o for o in opts if o.get("selected") is not None] or opts[:1]
                for o in chosen:
                    fields.append((name, o.get("value") if o.get("value") is not None else o.text_content().strip()))
        return action, fields


def client(user=None, pw=None) -> PClient:
    return PClient(user or os.environ.get("E2E_USER", "qa_super"), pw or os.environ["E2E_PW"])


_counter = [0]


def tag() -> str:
    """Sufijo unico de solo letras (a-z)."""
    _counter[0] += 1
    n = (int(time.time()) % (26 ** 6)) * 100 + _counter[0]
    s = ""
    while n:
        n, r = divmod(n, 26)
        s += chr(97 + r)
    return s


RUN = tag()          # identifica esta ejecucion; todos los inventarios y nombres la llevan
PREFIX = f"QAP{RUN}"


# ---------------------------------------------------------------- base de datos (solo lectura)
def sql(q, *params):
    return e2e_lib.db_rows(q, *params)


def scalar(q, *params):
    return e2e_lib.db_scalar(q, *params)


def plan(pid):
    """Estado del plan en la BD como diccionario (None si no existe)."""
    rows = sql("select CurrentPhase,CurrentState,PlanStatus,IsDraft,VerificationId,RequestId,MaintenanceId,DepartureId,KardexHistoryId,"
               "AcquisitionRequestId,ManagementId,EquipmentUnitId,DraftPhase from ManagementPlans where Id=?", pid)
    if not rows:
        return None
    r = rows[0]
    keys = ["phase", "state", "pstatus", "draft", "ver", "req", "mnt", "dep", "kardex", "acq", "mgmt", "unit", "draftphase"]
    return dict(zip(keys, [int(x) if isinstance(x, (int, bool)) or (x is not None and not isinstance(x, str)) else x for x in r]))


def unidad(uid):
    rows = sql("select CurrentStatus,PhysicalCondition,LaboratoryId,InventoryNumber,EquipmentId,SerialNumber,CareerId,Notes,LocationResolutionStatus from EquipmentUnits where Id=?", uid)
    if not rows:
        return None
    r = rows[0]
    return dict(zip(["status", "cond", "lab", "inv", "equip", "serial", "career", "notes", "locres"], list(r)))


# ---------------------------------------------------------------- envio de formularios
def apply_overrides(fields, ov):
    ov = ov or {}
    out, seen = [], set()
    for k, v in fields:
        if k in ov:
            if k in seen:
                continue
            seen.add(k)
            val = ov[k]
            if val is None:
                continue
            for item in (val if isinstance(val, list) else [val]):
                out.append((k, str(item)))
        else:
            out.append((k, v))
    for k, val in ov.items():
        if k not in seen and not any(k == f[0] for f in fields) and val is not None:
            for item in (val if isinstance(val, list) else [val]):
                out.append((k, str(item)))
    return out


def with_handler(url, handler):
    if not handler:
        return url
    parts = urllib.parse.urlsplit(url)
    q = [(k, v) for k, v in urllib.parse.parse_qsl(parts.query, keep_blank_values=True) if k != "handler"]
    q.insert(0, ("handler", handler))
    return urllib.parse.urlunsplit((parts.scheme, parts.netloc, parts.path, urllib.parse.urlencode(q), ""))


def post_form(c, page, ov=None, handler=None, name="", follow=True, index=0, action_contains=None, extra=None, post_to=None):
    """GET `page`, toma su formulario, aplica overrides ({campo: valor|None|[valores]}), anade `extra` [(k,v)] y envia al
    action del formulario (o `post_to`) con el handler pedido (p. ej. Draft, Close). Si el GET falla devuelve un Result con ese status."""
    try:
        action, fields = c.form(page, action_contains, index)
    except RuntimeError as exc:
        m = re.search(r"-> (\d+)", str(exc))
        return HttpResult(int(m.group(1)) if m else 0, name=name)
    out = apply_overrides(fields, ov)
    if extra:
        out += [(k, str(v)) for k, v in extra]
    url = post_to or action
    return c._post(with_handler(url, handler), out, follow, name)


def post_raw(c, url, fields, token_page, follow=True, name=""):
    """POST de campos arbitrarios con el token de antifalsificacion de `token_page`."""
    _, page = c.get(token_page)
    m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
    tok = m.group(1) if m else ""
    return c._post(urllib.parse.urljoin(BASE, url), [("__RequestVerificationToken", tok)] + [(k, str(v)) for k, v in fields], follow, name)


def get_nofollow(c, path):
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(c.jar), _NoRedirect)
    try:
        r = op.open(BASE + path, timeout=120)
        return r.status, None
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Location")


def anon(method, path, fields=None):
    op = urllib.request.build_opener(_NoRedirect)
    data = urllib.parse.urlencode(fields or [("x", "1")]).encode() if method == "POST" else None
    req = urllib.request.Request(BASE + path, data=data, headers={"Content-Type": "application/x-www-form-urlencoded"} if data else {})
    try:
        r = op.open(req, timeout=60)
        return r.status, None
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Location")


def check_anon(R, label, path, post=False):
    st, loc = anon("POST" if post else "GET", path)
    check(R, f"{label}: sin sesion {'POST' if post else 'GET'} {path} -> 302 /Login", st == 302 and "/login" in (loc or "").lower(), f"status={st} location={loc}")


def tokenless(R, c, label, action, fields):
    res = c.raw_post(action, fields, follow=False, name=label)
    check(R, f"{label}: POST sin token antifalsificacion -> 400", res.status == 400, f"status={res.status}")
    return res


def multipart_post(c, action, fields, files, token_page=None, follow=True, name=""):
    """POST multipart/form-data. files: [(campo, nombre_archivo, bytes, content_type)]."""
    fields = list(fields)
    if token_page:
        _, page = c.get(token_page)
        tok = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
        fields = [("__RequestVerificationToken", tok)] + fields
    boundary = "----qap" + uuid.uuid4().hex
    body = bytearray()
    for k, v in fields:
        body += f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode("utf-8")
    for field, filename, content, ctype in files:
        body += (f'--{boundary}\r\nContent-Disposition: form-data; name="{field}"; filename="{filename}"\r\n'
                 f'Content-Type: {ctype}\r\n\r\n').encode()
        body += content + b"\r\n"
    body += f"--{boundary}--\r\n".encode()
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(c.jar), _NoRedirect)
    req = urllib.request.Request(urllib.parse.urljoin(BASE, action), data=bytes(body),
                                 headers={"Content-Type": f"multipart/form-data; boundary={boundary}"})
    status, location, text = 0, None, ""
    try:
        r = opener.open(req, timeout=180)
        status, text = r.status, r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        status, location = e.code, e.headers.get("Location")
        text = e.read().decode("utf-8", "replace")
    if status in (301, 302, 303) and location and follow:
        _, text = c.get(location)
    res = HttpResult(status, location, text=text, name=name)
    if res.text:
        try:
            res.errors, res.toasts = parse_messages(res.text)
        except Exception:
            pass
    note(f"POST(multipart) {action[:100]} [{name}]", res.status, res.text, res.errors, res.toasts, res.location)
    return res


def json_post(c, url, body, token_page=None, token=True, content_type="application/json", raw=None, method="POST"):
    """POST JSON con cookies de c y cabecera RequestVerificationToken. Devuelve (status, texto)."""
    headers = {"Content-Type": content_type}
    if token:
        _, page = c.get(token_page or "/")
        m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
        if m:
            headers["RequestVerificationToken"] = m.group(1)
    data = raw if raw is not None else json.dumps(body).encode("utf-8")
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(c.jar), _NoRedirect)
    req = urllib.request.Request(urllib.parse.urljoin(BASE, url), data=data if method == "POST" else None, headers=headers, method=method)
    try:
        r = opener.open(req, timeout=120)
        return r.status, r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


# ---------------------------------------------------------------- fixtures: laboratorio, unidades, planes
def lab_propio(c, modulo):
    """Laboratorio 'QAP Lab <modulo>' en la facultad 1 (se crea una vez y se reutiliza)."""
    nombre = f"QAP Lab {modulo}"
    lid = scalar("select top 1 Id from Laboratories where Name=? and FacultyId=? and Status=0 order by Id desc", nombre, FAC)
    if lid:
        return int(lid)
    res = post_form(c, "/Laboratories/Create", {"Input.FacultyId": str(FAC), "Input.Name": nombre, "Input.Floor": "1"}, name="crear laboratorio propio")
    lid = scalar("select top 1 Id from Laboratories where Name=? and FacultyId=? and Status=0 order by Id desc", nombre, FAC)
    if not lid:
        raise RuntimeError(f"No se pudo crear el laboratorio {nombre}: status={res.status} errores={res.errors} toasts={res.toasts}")
    return int(lid)


def nueva_unidad(c, lab, sufijo, equipo=EQUIP, **ov):
    """Crea una unidad por EquipmentUnits/Create. Devuelve (id, inventario)."""
    inv = f"{PREFIX}{sufijo}"
    campos = {"Input.EquipmentId": str(equipo), "Input.InventoryNumber": inv, "Input.CareerId": str(CAREER),
              "Input.FacultyId": str(FAC), "Input.LaboratoryId": str(lab)}
    campos.update(ov)
    res = post_form(c, "/EquipmentUnits/Create", campos, name=f"fixture unidad {inv}")
    uid = scalar("select top 1 Id from EquipmentUnits where InventoryNumber=? order by Id desc", inv)
    if not uid:
        raise RuntimeError(f"No se pudo crear la unidad {inv}: status={res.status} errores={res.errors} toasts={res.toasts}")
    return int(uid), inv


def planificar(c, unit_ids):
    """Agrega unidades propias al plan de la gestion 30 (PlanAssets/ApplySelection). Devuelve {unit_id: plan_id}."""
    page = f"/Managements/PlanAssets?id={MGMT}&SearchTerm={PREFIX}"
    _, fields = c.form(page, "ApplySelection")
    actuales = [v for k, v in fields if k == "SelectedUnitIds"]
    sel = list(dict.fromkeys(actuales + [str(u) for u in unit_ids]))
    res = post_form(c, page, {"SelectedUnitIds": sel}, action_contains="ApplySelection", name="fixture planificar")
    out = {}
    for u in unit_ids:
        pid = scalar("select top 1 Id from ManagementPlans where ManagementId=? and EquipmentUnitId=? order by Id desc", MGMT, u)
        if not pid:
            raise RuntimeError(f"La unidad {u} no entro al plan: status={res.status} errores={res.errors} toasts={res.toasts}")
        out[u] = int(pid)
    return out


def desplanificar(c, unit_ids_a_quitar):
    page = f"/Managements/PlanAssets?id={MGMT}&SearchTerm={PREFIX}"
    _, fields = c.form(page, "ApplySelection")
    actuales = [v for k, v in fields if k == "SelectedUnitIds"]
    sel = [u for u in actuales if int(u) not in set(unit_ids_a_quitar)]
    return post_form(c, page, {"SelectedUnitIds": sel if sel else None}, action_contains="ApplySelection", name="fixture desplanificar")


# ---- fases del wizard (como las envia la interfaz) ----
def l6(c, plan_id, unit_id, lab, cond=4, obs="", draft=False, name="L-6", filas=None, mgmt=MGMT, extra_fields=None, follow=False, wizard=True):
    """MassCreate L-6 de una o varias filas. filas: lista de (plan_id, unit_id, cond, obs); si no se da, una sola fila."""
    filas = filas or [(plan_id, unit_id, cond, obs)]
    q = f"/Verifications/MassCreate?{'isWizard=true&' if wizard else ''}labId={lab}&managementId={mgmt}"
    campos = [("IsWizard", "true" if wizard else "false"), ("LabId", str(lab)), ("ManagementId", str(mgmt))]
    for i, (p, u, cnd, ob) in enumerate(filas):
        campos += [(f"Rows[{i}].PlanId", p), (f"Rows[{i}].EquipmentUnitId", u), (f"Rows[{i}].Condition", cnd), (f"Rows[{i}].Observations", ob)]
    if extra_fields:
        campos += extra_fields
    return post_raw(c, with_handler(q, "Draft" if draft else None), campos, q, follow=follow, name=name)


def l7(c, plan_id, ov=None, draft=False, name="L-7", mgmt=MGMT, extra=None, follow=False):
    page = f"/Requests/Create?isWizard=true&ManagementPlanId={plan_id}&ManagementId={mgmt}"
    base = {"Input.TechnicalChecklistConfirmed": "true", "Input.Description": "Falla reportada por QAP"}
    base.update(ov or {})
    return post_form(c, page, base, handler="Draft" if draft else None, name=name, follow=follow, extra=extra)


TAREAS_OK = {f"Input.Tasks[{i}].IsCompleted": "true" for i in range(4)}


def l8(c, plan_id, ov=None, draft=False, name="L-8", tech=TECH_INT, extra=None, follow=False, wizard=True):
    page = f"/Maintenances/Create?{'isWizard=true&' if wizard else ''}managementPlanId={plan_id}"
    base = {"Input.TechnicianId": str(tech), "Input.Status": "2", "Input.Description": "Trabajo de mantenimiento QAP",
            "Input.CompletionPercentage": "100", "Input.StartDate": f"{d(1)}T08:00", "Input.EndDate": f"{d(2)}T17:00",
            "Input.CostDetails[0].Concept": "Repuesto QAP", "Input.CostDetails[0].Category": "0",
            "Input.CostDetails[0].Quantity": "1", "Input.CostDetails[0].UnitPrice": "150"}
    base.update(TAREAS_OK)
    base.update(ov or {})
    return post_form(c, page, base, handler="Draft" if draft else None, name=name, follow=follow, extra=extra)


def l3(c, plan_id, unit_id, lab, salida=None, retorno=None, draft=False, name="L-3", filas=None, mgmt=MGMT, wizard=True, follow=False):
    q = f"/Departures/MassCreate?{'isWizard=true&' if wizard else ''}labId={lab}&managementId={mgmt}"
    filas = filas or [(plan_id, unit_id, "true")]
    campos = [("IsWizard", "true" if wizard else "false"), ("LabId", str(lab)), ("ManagementId", str(mgmt)),
              ("DepartureDate", salida or d(0)), ("EstimatedReturnDate", retorno or d(14))]
    for i, (p, u, inc) in enumerate(filas):
        campos += [(f"Rows[{i}].PlanId", p), (f"Rows[{i}].EquipmentUnitId", u), (f"Rows[{i}].ProductName", "x"), (f"Rows[{i}].Include", inc)]
    return post_raw(c, with_handler(q, "Draft" if draft else None), campos, q, follow=follow, name=name)


def kardex(c, plan_id, ov=None, draft=False, name="Kardex", quitar=None, wizard=True, follow=False):
    page = f"/Kardex/Create?{'isWizard=true&' if wizard else ''}managementPlanId={plan_id}"
    base = {"Input.ActualReturnDate": d(3), "Input.SuggestedNextMaintenanceDate": d(400), "Input.SatisfactionLevel": "4",
            "Input.Recommendations": "Revisar cada 6 meses", "Input.Observations": "Todo en orden", "Input.Description": "Trabajo QAP"}
    base.update(ov or {})
    for k in (quitar or []):
        base[k] = None
    return post_form(c, page, base, handler="Draft" if draft else "Close", name=name, follow=follow)


def l12(c, plan_id, ov=None, draft=False, name="L-12", wizard=True, follow=False):
    page = f"/Acquisitions/Create?{'isWizard=true&' if wizard else ''}managementPlanId={plan_id}"
    base = {"Input.InvestmentCode": "INV-QAP", "Input.CostCenter": "CC-QAP", "Input.Description": "Justificacion QAP"}
    base.update(ov or {})
    return post_form(c, page, base, handler="Draft" if draft else None, name=name, follow=follow)


FASES = ["P1", "P1OK", "P2", "P3", "P4", "P5", "P6", "P7"]


def fechas_l8_recientes():
    """Inicio/fin de L-8 en UTC 'casi ahora' (fin = siguiente minuto): el historial de estado exige Fin >= Inicio y el rollback cierra el historial en UtcNow,
    asi que el fin debe ser posterior al alta de la unidad y anterior al momento del rollback (esperar >= 70 s antes de revertir)."""
    from datetime import datetime
    fin = (datetime.utcnow() + timedelta(seconds=60)).replace(second=0, microsecond=0)
    ini = fin - timedelta(hours=1)
    return {"Input.StartDate": ini.strftime("%Y-%m-%dT%H:%M"), "Input.EndDate": fin.strftime("%Y-%m-%dT%H:%M")}


def llevar_a(c, lab, sufijo, destino, tech=TECH_INT, past=False, **unit_ov):
    """Crea una unidad + plan propios y los deja en `destino`:
    P1 L-6 pendiente | P1OK L-6 sano | P2 L-6 con falla (espera L-7) | P3 espera L-8 | P4 espera L-3 (L-8 completado) |
    P5 espera Kardex (unidad OnLoan) | P6 espera L-12 | P7 completado. Devuelve dict(unit, inv, plan).
    past=True usa fechas pasadas en L-8/L-3/Kardex (necesario para poder revertir con el rollback: el historial se cierra 'ahora')."""
    uid, inv = nueva_unidad(c, lab, sufijo, **unit_ov)
    pid = planificar(c, [uid])[uid]
    ctx = {"unit": uid, "inv": inv, "plan": pid, "lab": lab}
    idx = FASES.index(destino)
    if idx >= 1:
        if destino == "P1OK":
            l6(c, pid, uid, lab, cond=4, name=f"{sufijo} L-6 sano")
        else:
            l6(c, pid, uid, lab, cond=2, obs="Falla de motor QAP", name=f"{sufijo} L-6 falla")
    if idx >= 3:
        l7(c, pid, name=f"{sufijo} L-7")
    if idx >= 4:
        l8(c, pid, name=f"{sufijo} L-8", tech=tech, ov=(fechas_l8_recientes() if past else None))
    if idx >= 5:
        l3(c, pid, uid, lab, name=f"{sufijo} L-3", salida=(d(-8) if past else None), retorno=(d(-3) if past else None))
    if idx >= 6:
        kardex(c, pid, name=f"{sufijo} Kardex", ov=({"Input.ActualReturnDate": d(-5)} if past else None))
    if idx >= 7:
        l12(c, pid, name=f"{sufijo} L-12")
    return ctx


# ---------------------------------------------------------------- resultados
def toast_text(res, kind=None):
    return " | ".join(t for k, t in res.toasts if kind is None or k == kind)


def dump(res):
    return f"status={res.status} loc={res.location} err={res.errors[:3]} toasts={[(k, t[:160]) for k, t in res.toasts[:3]]}"


def fase(R, label, pid, phase, state, pstatus=None, draft=None):
    p = plan(pid) or {}
    ok = p.get("phase") == phase and p.get("state") == state and (pstatus is None or p.get("pstatus") == pstatus) and (draft is None or bool(p.get("draft")) == draft)
    check(R, f"{label}: plan #{pid} en fase {phase} / estado {state}" + (f" / PlanStatus {pstatus}" if pstatus is not None else ""), ok, f"observado={p}")
    return ok


def finish(R, modulo):
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        fails = e2e_lib.run_all([lambda: R])
    out = buf.getvalue()
    anomalies = list(dict.fromkeys(ANOMALIES))
    out += f"-- ANOMALIAS ({len(anomalies)}): 500 / paginas de error / texto en ingles detectados en las respuestas\n"
    for a in anomalies:
        out += f"   * {a}\n"
    print(out)
    try:
        OUT_DIR.mkdir(parents=True, exist_ok=True)
        (OUT_DIR / f"http_cases_procesos_{modulo}.txt").write_text(out, encoding="utf-8")
    except Exception as exc:
        print("No se pudo escribir el resultado:", exc)
    return fails


def run_module(modulo, func):
    R = []
    c = client()
    try:
        func(R, c)
    except Exception as exc:
        import traceback
        traceback.print_exc()
        R.append(e2e_lib.Result(f"EXCEPCION de la prueba en {modulo}", "sin excepcion", f"{type(exc).__name__}: {exc}"))
    sys.exit(1 if finish(R, modulo) else 0)
