"""Casos de ejemplo (patrón para el resto de maestros): Países y Ciudades, reglas del servidor. Solo contra la copia QA."""
import os
import time

import e2e_lib
from http_forms import Client, expect

R = []
c = Client(os.environ.get("E2E_USER", "qa_super"), os.environ["E2E_PW"])
SUF = str(int(time.time()))[-5:]
NOMBRE = f"Pais QA {SUF}".replace("0", "o").replace("1", "i").replace("2", "z").replace("3", "e").replace("4", "a").replace("5", "s").replace("6", "g").replace("7", "t").replace("8", "b").replace("9", "p")

# ---- Países: crear ----
expect(R, "Países/Create: vacío", c.submit("/Countries/Create", {"Input.Name": ""}), status=200, error="obligatorio")
expect(R, "Países/Create: 1 carácter", c.submit("/Countries/Create", {"Input.Name": "A"}), status=200, error="entre 2 y 100")
expect(R, "Países/Create: 101 caracteres", c.submit("/Countries/Create", {"Input.Name": "x" * 101}), status=200, error="entre 2 y 100")
expect(R, "Países/Create: duplicado (otra capitalización y espacios)", c.submit("/Countries/Create", {"Input.Name": " BOLIVIA "}), status=200, error="Ya existe un país")
expect(R, "Países/Create: válido", c.submit("/Countries/Create", {"Input.Name": NOMBRE}), status=302, redirect="/Countries", toast=f"País '{NOMBRE}' registrado exitosamente.", toast_kind="success")

# ---- Países: HTML hostil (el servidor lo acepta; debe mostrarse escapado en el listado) ----
hostil = "<b>QA</b>"
r = c.submit("/Countries/Create", {"Input.Name": hostil})
expect(R, "Países/Create: nombre con HTML se registra o se rechaza sin excepción", r, status=None)
listado = c.get("/Countries")[1]
R.append(e2e_lib.Result("Países/Index: el HTML hostil no se ejecuta (aparece escapado)", True, "<b>QA</b>" not in listado))

e2e_lib.run_all([lambda: R])
