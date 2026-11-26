"""Ejercicio 9 — Aceptación de escenarios excepcionales (matriz).

Cada escenario se ejecuta con la aplicación como lo haría el usuario y comprueba: comportamiento esperado,
mensaje comprensible e integridad de los datos (nada se guarda a medias).
"""
import asyncio
from playwright.async_api import async_playwright
from comun import dia_habil, BASE, Registro, nueva_pagina, iniciar_sesion, captura, confirmar_alerta, texto_alerta, cerrar_alerta


async def contar_labs(page):
    await page.goto(BASE + "/Laboratories")
    await page.wait_for_load_state("networkidle")
    return await page.locator("tbody tr").count()


async def stock(page, nombre):
    await page.goto(BASE + "/Catalogs/Stock")
    await page.wait_for_load_state("networkidle")
    fila = page.locator("tbody tr", has_text=nombre).first
    return float((await fila.locator("td").nth(2).inner_text()).strip().replace(",", "."))


async def id_planificacion(page, codigo):
    await page.goto(BASE + "/LaboratoryOperations/Planificaciones")
    await page.wait_for_load_state("networkidle")
    href = await page.locator("tr", has_text=codigo).first.locator("a[href*='L3?id=']").first.get_attribute("href")
    return int(href.split("id=")[1])


async def main():
    r = Registro("ej09_excepcionales")
    async with async_playwright() as p:
        browser = await p.chromium.launch()
        gestor = await nueva_pagina(browser)
        await iniciar_sesion(gestor, "gestor.p3")

        # E9-1 Dato duplicado.
        antes = await contar_labs(gestor)
        await gestor.goto(BASE + "/Laboratories/Create")
        await gestor.select_option("[name='Input.FacultyId']", label="Facultad de Gastronomía y Turismo")
        await gestor.fill("[name='Input.Name']", "Cocina Fría")
        await gestor.click("button[type=submit]:has-text('Registrar')")
        await gestor.wait_for_load_state("networkidle")
        msg = " | ".join(t.strip() for t in await gestor.locator(".field-validation-error").all_inner_texts() if t.strip())
        cap = await captura(gestor, "09_01_duplicado")
        despues = await contar_labs(gestor)
        r.criterio("E9-1", "Registrar un laboratorio duplicado (Cocina Fría ya existe en la facultad)",
                   "Ya existe un laboratorio con este nombre" in msg and antes == despues,
                   f"Mensaje: «{msg}»; laboratorios {antes} → {despues}", cap,
                   observacion="Esperado: se rechaza con mensaje junto al campo Nombre y no se crea ningún registro.")

        # E9-2a Sin permisos: un Administrador intenta reprogramar (exclusivo del SuperAdmin).
        pid = await id_planificacion(gestor, "PRA-P3-002")
        await gestor.goto(BASE + f"/LaboratoryOperations/Grilla?reprogramar={pid}")
        await gestor.wait_for_load_state("networkidle")
        msg = await texto_alerta(gestor)
        modo = await gestor.locator("#gr-rs-form").count()
        cap = await captura(gestor, "09_02_reprogramar_sin_permiso")
        await cerrar_alerta(gestor)
        r.criterio("E9-2a", "Reprogramar una práctica sin el rol necesario (Administrador)",
                   "Solo el SuperAdmin puede reprogramar" in msg and modo == 0,
                   f"Mensaje: «{msg.splitlines()[-1] if msg else ''}»; formulario de reprogramación visible: {modo > 0}", cap,
                   observacion="Esperado: la grilla se abre en modo consulta y se informa que la acción es exclusiva del SuperAdmin.")

        # E9-2b Sin permisos: el encargado de cierre intenta entrar al catálogo de laboratorios.
        cierre = await nueva_pagina(browser)
        await iniciar_sesion(cierre, "cierre.p3")
        await cierre.goto(BASE + "/Laboratories/Create")
        await cierre.wait_for_load_state("networkidle")
        cap = await captura(cierre, "09_03_cierre_sin_permiso")
        r.criterio("E9-2b", "El rol «Cierre de práctica» intenta registrar un laboratorio",
                   "/Laboratories" not in cierre.url,
                   f"Redirigido a {cierre.url.replace(BASE, '')}; solo ve su módulo (Cierre, L-3, L-5)", cap,
                   observacion="Esperado: no puede abrir pantallas ajenas a su rol; vuelve a «Cierre de práctica».")

        # E9-3 Información incompleta: nueva planificación sin datos.
        admin = await nueva_pagina(browser)
        await iniciar_sesion(admin, "admin.p3")
        await admin.goto(BASE + "/LaboratoryOperations/Planificaciones")
        total_antes = await admin.locator("tbody tr").count()
        await admin.goto(BASE + "/LaboratoryOperations/Planificacion?managementId=1")
        await admin.wait_for_load_state("networkidle")
        await admin.evaluate("() => { const f = document.getElementById('form-planificacion'); f.setAttribute('novalidate', ''); }")
        await admin.locator("#form-planificacion button[type=submit]").last.click()
        await admin.wait_for_load_state("networkidle")
        mensajes = [t.strip() for t in await admin.locator(".field-validation-error, .validation-summary-errors li, .help-block").all_inner_texts() if t.strip()]
        cap = await captura(admin, "09_04_planificacion_incompleta", full_page=True)
        await admin.goto(BASE + "/LaboratoryOperations/Planificaciones")
        total_despues = await admin.locator("tbody tr").count()
        r.criterio("E9-3", "Registrar una planificación con información incompleta",
                   len(mensajes) >= 2 and total_antes == total_despues,
                   f"Mensajes: {' | '.join(dict.fromkeys(mensajes))}; planificaciones {total_antes} → {total_despues}", cap,
                   observacion="Esperado: se indica cada dato faltante y no se crea la planificación.")

        # E9-4a Recurso no disponible: salida L-3 con existencia insuficiente.
        mant_antes = await stock(gestor, "Mantequilla sin sal")
        pid4 = await id_planificacion(gestor, "PRA-P3-004")
        await gestor.goto(BASE + f"/LaboratoryOperations/L3?id={pid4}")
        await gestor.wait_for_load_state("networkidle")
        await gestor.click(".js-departure-confirm")
        await confirmar_alerta(gestor)
        msg = await texto_alerta(gestor)
        cap = await captura(gestor, "09_05_stock_insuficiente")
        await cerrar_alerta(gestor)
        sin_salida = await gestor.locator(".js-departure-confirm").count() > 0
        mant_despues = await stock(gestor, "Mantequilla sin sal")
        r.criterio("E9-4a", "Entregar material que ya no está disponible (mantequilla 5 kg; existencia 2 kg)",
                   "Stock insuficiente" in msg and mant_antes == mant_despues and sin_salida,
                   f"Mensaje: «{msg.splitlines()[-1] if msg else ''}»; existencia {mant_antes:g} → {mant_despues:g}; la salida sigue pendiente: {sin_salida}", cap,
                   observacion="Esperado: se bloquea la salida completa, sin descontar nada.")

        # E9-4b Recurso no disponible: reprogramar a un laboratorio ocupado.
        await admin.goto(BASE + f"/LaboratoryOperations/Grilla?reprogramar={pid}")
        await admin.wait_for_load_state("networkidle")
        opciones = await admin.evaluate("[...document.querySelectorAll('#gr-rs-lab option')].map(o=>[o.value,o.text])")
        cc = next(v for v, t in opciones if "Cocina Caliente" in t)
        fecha_destino = dia_habil(6)
        await admin.fill("#gr-rs-date", fecha_destino)
        await admin.select_option("#gr-rs-lab", cc)
        await admin.select_option("#gr-rs-period", "6")
        await admin.click("#gr-rs-preview-btn")
        await admin.wait_for_timeout(1500)
        prev = await admin.locator("#gr-rs-preview").inner_text()
        oculto = await admin.locator("#gr-rs-confirm").get_attribute("hidden") is not None
        cap = await captura(admin, "09_06_laboratorio_ocupado", selector="#gr-rs-panel")
        r.criterio("E9-4b", "Reprogramar a un laboratorio ya ocupado (Cocina Caliente, periodos 6–7)",
                   "No disponible" in prev and "ya está ocupado" in prev and oculto,
                   "Previsualización: " + " ".join(prev.split())[:260] + f"; botón Confirmar oculto: {oculto}", cap,
                   observacion="Esperado: el destino se bloquea, se nombra la práctica que lo ocupa y no se permite confirmar.")

        await browser.close()
    print("Resultados:", r.guardar())


if __name__ == "__main__":
    asyncio.run(main())
