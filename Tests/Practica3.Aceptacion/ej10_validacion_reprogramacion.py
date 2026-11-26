"""Ejercicio 10 — Validación final con el usuario: Reprogramación de una práctica.

Funcionalidad prioritaria indicada por la responsable de laboratorios («la Inge»): mover UNA práctica a otra fecha,
periodo o laboratorio, solo por el SuperAdmin, con previsualización, motivo obligatorio y sin choques.
Evaluación SIMULADA: no se dispuso de la usuaria real; el guion se ejecutó en el entorno de prueba siguiendo
exactamente los pasos que ejecutaría ella (ver acta en el informe).
"""
import asyncio
from playwright.async_api import async_playwright
from comun import dia_habil, BASE, Registro, nueva_pagina, iniciar_sesion, captura, confirmar_alerta, texto_alerta, cerrar_alerta

ORIGEN = "PRA-P3-002"


async def fila_plan(page, texto):
    await page.goto(BASE + "/LaboratoryOperations/Planificaciones")
    await page.wait_for_load_state("networkidle")
    fila = page.locator("tbody tr", has_text=texto)
    return " ".join((await fila.first.inner_text()).split()) if await fila.count() else ""


async def preparar_destino(page, fecha, lab_texto, periodo):
    opciones = await page.evaluate("[...document.querySelectorAll('#gr-rs-lab option')].map(o=>[o.value,o.text])")
    await page.fill("#gr-rs-date", fecha)
    await page.select_option("#gr-rs-lab", next(v for v, t in opciones if lab_texto in t))
    await page.select_option("#gr-rs-period", str(periodo))
    await page.click("#gr-rs-preview-btn")
    await page.wait_for_timeout(1500)
    return " ".join((await page.locator("#gr-rs-preview").inner_text()).split())


async def main():
    r = Registro("ej10_validacion")
    destino_fecha = dia_habil(6)
    async with async_playwright() as p:
        browser = await p.chromium.launch()

        # CA10-1: solo el SuperAdmin reprograma.
        gestor = await nueva_pagina(browser)
        await iniciar_sesion(gestor, "gestor.p3")
        await gestor.goto(BASE + "/LaboratoryOperations/Planificaciones")
        await gestor.wait_for_load_state("networkidle")
        fila = gestor.locator("tbody tr", has_text=ORIGEN).first
        botones = await fila.locator("a:has-text('Reprogramar')").count()
        cap_btn = await captura(gestor, "10_01a_sin_boton_reprogramar")
        pid = (await fila.locator("a[href*='L3?id=']").first.get_attribute("href")).split("id=")[1]
        await gestor.goto(BASE + f"/LaboratoryOperations/Grilla?reprogramar={pid}")
        await gestor.wait_for_load_state("networkidle")
        msg = await texto_alerta(gestor)
        cap = await captura(gestor, "10_01_permiso_superadmin")
        r.criterio("CA10-1", "Solo el SuperAdmin puede reprogramar; otros roles reciben un aviso claro",
                   botones == 0 and "Solo el SuperAdmin" in msg and await gestor.locator("#gr-rs-form").count() == 0,
                   f"Administrador: botón «Reprogramar» visible en el listado: {botones > 0}; acceso directo por URL → «{msg.replace(chr(10), ' ')}»",
                   cap, observacion=f"Apoyo: {cap_btn}")

        # Paso 1–2 del guion: el SuperAdmin abre la reprogramación desde Planificaciones.
        page = await nueva_pagina(browser)
        await iniciar_sesion(page, "admin.p3")
        antes = await fila_plan(page, ORIGEN)
        cap_lista = await captura(page, "10_02_planificaciones_antes", full_page=True)
        await page.locator("tbody tr", has_text=ORIGEN).first.locator("a:has-text('Reprogramar')").click()
        await page.wait_for_load_state("networkidle")
        cap_modo = await captura(page, "10_03_modo_reprogramacion")

        # CA10-2: previsualización antes de confirmar.
        prev = await preparar_destino(page, destino_fecha, "Cocina Caliente", 1)
        visible = await page.locator("#gr-rs-confirm").get_attribute("hidden") is None
        cap = await captura(page, "10_04_previsualizacion_disponible", selector="#gr-rs-panel")
        r.criterio("CA10-2", "Antes de confirmar se muestra una previsualización del destino (fecha, periodos, laboratorio, docente, disponibilidad)",
                   "Disponible" in prev and "Cocina Caliente" in prev and "Periodos 1–2" in prev and visible,
                   "Previsualización: " + prev[:230], cap, observacion=f"Apoyo: {cap_lista}, {cap_modo}")

        # CA10-3: destino ocupado bloqueado (se nombra la práctica que lo ocupa).
        prev_b = await preparar_destino(page, destino_fecha, "Cocina Caliente", 6)
        oculto = await page.locator("#gr-rs-confirm").get_attribute("hidden") is not None
        cap = await captura(page, "10_05_destino_ocupado", selector="#gr-rs-panel")
        r.criterio("CA10-3", "Un destino ocupado se bloquea indicando qué práctica lo ocupa",
                   "No disponible" in prev_b and "PRA-P3-003" in prev_b and oculto,
                   "Previsualización: " + prev_b[:260], cap)

        # CA10-4: un laboratorio con aforo menor que el grupo no se acepta como destino.
        prev_w = await preparar_destino(page, destino_fecha, "Pastelería", 1)
        oculto_w = await page.locator("#gr-rs-confirm").get_attribute("hidden") is not None
        cap = await captura(page, "10_06_aforo_insuficiente", selector="#gr-rs-panel")
        r.criterio("CA10-4", "Un laboratorio con aforo menor que el grupo se rechaza indicando estudiantes y capacidad",
                   "No disponible" in prev_w and "18 estudiantes" in prev_w and "admite 16" in prev_w and oculto_w,
                   "Previsualización: " + prev_w[:280] + f"; botón Confirmar oculto: {oculto_w}", cap)

        # CA10-5: el motivo es obligatorio.
        await preparar_destino(page, destino_fecha, "Cocina Caliente", 1)
        await page.fill("#gr-rs-reason", "")
        await page.click(".gr-rs-submit")
        msg = await texto_alerta(page)
        cap = await captura(page, "10_07_motivo_obligatorio")
        await cerrar_alerta(page)
        sigue = await fila_plan(page, ORIGEN)
        r.criterio("CA10-5", "Sin motivo no se reprograma",
                   "Indique el motivo" in msg and "Aprobada" in sigue,
                   f"Mensaje: «{msg.replace(chr(10), ' ')}»; la práctica sigue «Aprobada» en su fecha original", cap)

        # CA10-6: confirmación y resultado visible.
        await page.goto(BASE + "/LaboratoryOperations/Planificaciones")
        await page.locator("tbody tr", has_text=ORIGEN).first.locator("a:has-text('Reprogramar')").click()
        await page.wait_for_load_state("networkidle")
        await preparar_destino(page, destino_fecha, "Cocina Caliente", 1)
        await page.fill("#gr-rs-reason", "La docente asistirá a una capacitación institucional el día original.")
        await page.click(".gr-rs-submit")
        pregunta = await texto_alerta(page)
        cap_preg = await captura(page, "10_08_confirmar")
        await confirmar_alerta(page)
        exito = await texto_alerta(page, timeout=5000)
        cap = await captura(page, "10_09_reprogramada_con_exito")
        await cerrar_alerta(page)
        r.criterio("CA10-6", "Al confirmar se informa el éxito y la práctica aparece en el nuevo horario",
                   "Se reprogramó con éxito" in exito,
                   f"Pregunta: «{pregunta.replace(chr(10), ' ')}» → mensaje: «{exito.replace(chr(10), ' ')}»", cap, observacion=f"Apoyo: {cap_preg}")

        # CA10-7: origen marcado como Reprogramada; destino con el mismo docente y el mismo material.
        origen = await fila_plan(page, ORIGEN)
        await page.goto(BASE + "/LaboratoryOperations/Planificaciones")
        filas = await page.locator("tbody tr", has_text="Fondos y salsas madre").all_inner_texts()
        filas = [" ".join(f.split()) for f in filas]
        destino = next((f for f in filas if "Reprogramada" not in f), "")
        cap_lista2 = await captura(page, "10_10_planificaciones_despues", full_page=True)
        material = ""
        enlace = page.locator("tbody tr", has_text="Fondos y salsas madre").filter(has_not_text="Reprogramada").first.locator("a[href*='L3?id=']")
        if await enlace.count():
            await enlace.first.click()
            await page.wait_for_load_state("networkidle")
            material = " ".join((await page.inner_text("body")).split())
            cap_mat = await captura(page, "10_11_material_conservado", full_page=True)
        r.criterio("CA10-7", "El origen queda «Reprogramada» y el destino conserva docente y material",
                   "Reprogramada" in origen and "Cocina Caliente" in destino and "Carla Montaño" in destino and "Harina de trigo" in material,
                   f"Origen: {origen[:120]} · Destino: {destino[:160]} · Material del destino incluye «Harina de trigo»: {'Harina de trigo' in material}",
                   cap_lista2)

        # CA10-8: trazabilidad consultable (motivo y responsable de la reprogramación).
        await page.goto(BASE + f"/LaboratoryOperations/Grilla?date={destino_fecha}")
        await page.wait_for_load_state("networkidle")
        cuerpo = await page.content()
        motivo_visible = "capacitación institucional" in cuerpo
        marca = "Reprogramada desde otra fecha" in cuerpo
        cap = await captura(page, "10_12_grilla_destino", full_page=True)
        r.criterio("CA10-8", "La reprogramación es trazable desde la interfaz (marca en la grilla y motivo consultable)",
                   marca and motivo_visible,
                   f"Marca «Reprogramada desde otra fecha» en la grilla: {marca}; motivo consultable en pantalla: {motivo_visible}", cap,
                   observacion="El motivo se guarda en la tabla Reschedulings (verificado por la prueba unitaria existente ReschedulingServiceTests), pero ninguna pantalla lo muestra.")

        await browser.close()
    print("Resultados:", r.guardar())


if __name__ == "__main__":
    asyncio.run(main())
