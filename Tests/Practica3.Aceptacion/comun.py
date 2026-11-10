"""Utilidades comunes de las pruebas de aceptación de la Práctica 3 (Playwright, navegador Chromium).

Las pruebas se ejecutan contra el servidor de pruebas (Tests/Practica3.Host), que corre el Program.cs real
del sistema sobre una base desechable con datos de prueba. Nunca se usan la base oficial ni la nube.
"""
import datetime
import json
import os
import time
from pathlib import Path

BASE = os.environ.get("PRACTICA3_URL", "http://127.0.0.1:5300")
PASSWORD = "Practica3#Univalle"
SALIDA = Path(os.environ.get("PRACTICA3_SALIDA", Path(__file__).parent / "salida"))
CAPTURAS = SALIDA / "capturas"
CAPTURAS.mkdir(parents=True, exist_ok=True)


class Registro:
    """Acumula los resultados de un ejercicio (criterios, pasos y capturas) y los guarda en JSON."""

    def __init__(self, ejercicio):
        self.ejercicio = ejercicio
        self.items = []
        self.inicio = time.strftime("%d/%m/%Y %H:%M")

    def criterio(self, codigo, descripcion, cumple, evidencia, captura=None, observacion=""):
        self.items.append({
            "codigo": codigo, "descripcion": descripcion, "cumple": bool(cumple),
            "evidencia": evidencia, "captura": captura, "observacion": observacion,
        })
        estado = "CUMPLE" if cumple else "NO CUMPLE"
        print(f"[{estado}] {codigo} {descripcion} :: {evidencia}")

    def guardar(self):
        destino = SALIDA / f"{self.ejercicio}.json"
        destino.write_text(json.dumps({"ejercicio": self.ejercicio, "fecha": self.inicio, "items": self.items},
                                      ensure_ascii=False, indent=2), encoding="utf-8")
        return destino


async def nueva_pagina(browser):
    contexto = await browser.new_context(viewport={"width": 1366, "height": 860}, device_scale_factor=1.5, locale="es-BO")
    return await contexto.new_page()


async def iniciar_sesion(page, usuario, password=PASSWORD):
    await page.goto(BASE + "/Login")
    await page.fill("#Input_UserName", usuario)
    await page.fill("#Input_Password", password)
    await page.click("button[type=submit]")
    await page.wait_for_load_state("networkidle")


async def cerrar_sesion(page):
    await page.context.clear_cookies()


async def captura(page, nombre, full_page=False, selector=None):
    ruta = CAPTURAS / f"{nombre}.png"
    await page.wait_for_timeout(350)
    if selector:
        await page.locator(selector).first.screenshot(path=str(ruta))
    else:
        await page.screenshot(path=str(ruta), full_page=full_page)
    return ruta.name


async def confirmar_alerta(page, timeout=2500):
    """Confirma el diálogo SweetAlert si aparece (acciones que piden confirmación)."""
    try:
        boton = page.locator(".swal2-confirm")
        await boton.wait_for(state="visible", timeout=timeout)
        await boton.click()
        await page.wait_for_load_state("networkidle")
        return True
    except Exception:
        return False


async def texto_alerta(page, timeout=3000):
    """Texto del mensaje emergente (éxito o error) que muestra la aplicación tras una acción."""
    try:
        popup = page.locator(".swal2-popup")
        await popup.first.wait_for(state="visible", timeout=timeout)
        await page.wait_for_timeout(400)
        partes = await page.locator(".swal2-popup .swal2-title, .swal2-popup .swal2-content, .swal2-popup .swal2-html-container").all_inner_texts()
        return "\n".join(t.strip() for t in partes if t.strip())
    except Exception:
        return ""


async def cerrar_alerta(page):
    try:
        if await page.locator(".swal2-confirm").is_visible():
            await page.locator(".swal2-confirm").click()
        await page.wait_for_timeout(300)
    except Exception:
        pass


def dia_habil(dias):
    """Fecha (yyyy-mm-dd) dentro de «dias» días corridos, saltando al lunes si cae domingo (mismo criterio que los datos de prueba)."""
    d = datetime.date.today() + datetime.timedelta(days=dias)
    while d.weekday() == 6:
        d += datetime.timedelta(days=1)
    return d.isoformat()
