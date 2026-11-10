"""Ejercicio 8 — Aceptación de un proceso completo: cierre de una práctica de laboratorio.

Proceso central (perspectiva del encargado de laboratorio, rol «Cierre de práctica», usuario Mario Quispe):
  1. Ver las prácticas del día.  2. Entregar el material (salida L-3).  3. Recibir la devolución.
  4. Registrar el incidente L-5 del material dañado.  5. Finalizar la práctica.
Verificación posterior (responsable de laboratorios, Rodrigo Vargas): existencias del Economato y estado de la práctica.

Precondiciones (datos de prueba): práctica PRA-P3-006 «Técnicas de corte de vegetales», hoy, Cocina Caliente,
periodos 9–10, estado Aprobada; requiere cuchillo de chef ×4, bowl de acero ×6 y tabla de picar ×6;
existencias del Economato: cuchillo 10, bowl 20, tabla 15.
"""
import asyncio
from playwright.async_api import async_playwright
from comun import BASE, Registro, nueva_pagina, iniciar_sesion, captura, confirmar_alerta, texto_alerta, cerrar_alerta

CODIGO = "PRA-P3-006"


async def existencias(page):
    """Lee las existencias del Economato desde la pantalla Catálogos › Existencias."""
    await page.goto(BASE + "/Catalogs/Stock")
    await page.wait_for_load_state("networkidle")
    valores = {}
    for corto, nombre in [("cuchillo", "Cuchillo de chef"), ("bowl", "Bowl de acero inoxidable"), ("tabla", "Tabla de picar")]:
        fila = page.locator("tbody tr", has_text=nombre).first
        valores[corto] = float((await fila.locator("td").nth(2).inner_text()).strip().replace(",", "."))
    return valores


async def set_select(page, selector, value=None, label=None):
    """Elige una opción en un select (también si está decorado con select2)."""
    await page.evaluate("""([sel, value, label]) => {
        const s = document.querySelector(sel);
        const opt = [...s.options].find(o => (value !== null && o.value === value) || (label !== null && o.text.includes(label)));
        s.value = opt.value; s.dispatchEvent(new Event('change', {bubbles: true}));
        if (window.jQuery) window.jQuery(s).trigger('change');
    }""", [selector, value, label])


async def main():
    r = Registro("ej08_proceso")
    async with async_playwright() as p:
        browser = await p.chromium.launch()
        gestor = await nueva_pagina(browser)
        await iniciar_sesion(gestor, "gestor.p3")
        stock_inicial = await existencias(gestor)
        cap_stock0 = await captura(gestor, "08_00_existencias_iniciales", full_page=True)

        page = await nueva_pagina(browser)
        await iniciar_sesion(page, "cierre.p3")

        # Paso 1: prácticas del día.
        cuerpo = await page.inner_text("body")
        cap1 = await captura(page, "08_01_practicas_del_dia", full_page=True)
        r.criterio("CA8-1", "El encargado ve la práctica del día al ingresar", "/Cierre" in page.url and "Técnicas de corte" in cuerpo,
                   f"Pantalla inicial {page.url.replace(BASE, '')}; lista la práctica {CODIGO} «Técnicas de corte de vegetales»", cap1)

        # Paso 2: entrega del material (salida L-3).
        contenedor = page.locator("tr:has-text('Técnicas de corte'), .card:has-text('Técnicas de corte')").last
        destino = await contenedor.locator("a[href*='/LaboratoryOperations/L3?id=']").first.get_attribute("href")
        await page.goto(BASE + destino)
        await page.wait_for_load_state("networkidle")
        cap2 = await captura(page, "08_02_formulario_entrega", full_page=True)
        await page.click(".js-departure-confirm")
        await confirmar_alerta(page)
        mensaje_salida = await texto_alerta(page)
        cap3 = await captura(page, "08_03_salida_registrada")
        await cerrar_alerta(page)
        stock_tras_salida = await existencias(gestor)
        cap3b = await captura(gestor, "08_04_existencias_tras_salida", full_page=True)
        r.criterio("CA8-2", "Al entregar el material se registra la salida y se descuentan las existencias",
                   "registrada" in mensaje_salida.lower() and stock_tras_salida == {"cuchillo": 6, "bowl": 14, "tabla": 9},
                   f"Mensaje: «{mensaje_salida.splitlines()[-1] if mensaje_salida else ''}». Existencias antes {stock_inicial} → después {stock_tras_salida}",
                   cap3, observacion=f"Apoyo: {cap2}, {cap3b}")

        # Paso 3: devolución (vuelven 4 cuchillos, 6 tablas y 5 bowls; un bowl se rompió).
        await page.goto(BASE + destino)
        await page.wait_for_load_state("networkidle")
        await set_select(page, "#Return_ResponsiblePersonId", label="Ruth Arnez")
        filas = page.locator("#form-return tr")
        for i in range(await filas.count()):
            texto = await filas.nth(i).inner_text()
            campo = filas.nth(i).locator("input[type=number]")
            if await campo.count() == 0:
                continue
            if "Bowl" in texto:
                await campo.fill("5")
            elif "Cuchillo" in texto:
                await campo.fill("4")
            elif "Tabla" in texto:
                await campo.fill("6")
        await page.fill("#Return_Observations", "Un bowl volvió con una fisura: se registra incidente L-5.")
        cap4 = await captura(page, "08_05_formulario_devolucion", full_page=True)
        await page.click(".js-return-confirm")
        await confirmar_alerta(page)
        mensaje_dev = await texto_alerta(page)
        await cerrar_alerta(page)
        cuerpo = await page.inner_text("body")
        bloqueada = await page.locator("#formFinalizeL3 button[disabled]").count() > 0
        cap5 = await captura(page, "08_06_devolucion_registrada", full_page=True)
        r.criterio("CA8-3", "La devolución se registra y el faltante impide finalizar sin incidente",
                   "registrada" in mensaje_dev.lower() and bloqueada,
                   f"Mensaje: «{mensaje_dev.splitlines()[-1] if mensaje_dev else ''}»; botón «Finalizar práctica» deshabilitado por el bowl faltante: {bloqueada}", cap5,
                   observacion=f"Apoyo: {cap4}")

        # Paso 4: incidente L-5 por el bowl roto.
        l5 = page.locator("a[href*='/LaboratoryOperations/L5']").first
        await l5.click()
        await page.wait_for_load_state("networkidle")
        await set_select(page, "#Input_ArticleId", label="Bowl de acero")
        await set_select(page, "#Input_IncidentType", value="Rotura")
        await page.fill("#Input_AffectedQuantity", "1")
        await set_select(page, "#Input_ResponsibleType", value="ESTUDIANTE")
        await page.fill("#Input_ResponsibleName", "Estudiante de prueba (Grupo A)")
        await page.fill("#Input_DamageDescription", "El bowl cayó durante el emplatado y presenta una fisura en la base.")
        cap6 = await captura(page, "08_07_formulario_L5", full_page=True)
        await page.click(".js-l5-confirm")
        await confirmar_alerta(page)
        mensaje_l5 = await texto_alerta(page)
        cap7 = await captura(page, "08_08_L5_registrado")
        await cerrar_alerta(page)
        r.criterio("CA8-4", "El incidente L-5 queda registrado para el material dañado",
                   any(k in mensaje_l5.lower() for k in ["registr", "incidente"]),
                   f"Mensaje: «{mensaje_l5.splitlines()[-1] if mensaje_l5 else ''}»", cap7, observacion=f"Apoyo: {cap6}")

        # Paso 5: finalizar la práctica.
        await page.goto(BASE + destino)
        await page.wait_for_load_state("networkidle")
        habilitado = await page.locator("#formFinalizeL3 button[disabled]").count() == 0
        await page.click("#formFinalizeL3 button[type=submit]")
        await confirmar_alerta(page)
        mensaje_fin = await texto_alerta(page)
        cap8 = await captura(page, "08_09_practica_finalizada")
        await cerrar_alerta(page)
        r.criterio("CA8-5", "Con todo devuelto o cubierto por L-5 la práctica se finaliza",
                   habilitado and "finaliz" in mensaje_fin.lower(),
                   f"Botón habilitado tras el L-5: {habilitado}; mensaje: «{mensaje_fin.splitlines()[-1] if mensaje_fin else ''}»", cap8)

        # Verificación del resultado final (responsable de laboratorios).
        stock_final = await existencias(gestor)
        cap9 = await captura(gestor, "08_10_existencias_finales", full_page=True)
        await gestor.goto(BASE + "/LaboratoryOperations/Planificaciones")
        await gestor.wait_for_load_state("networkidle")
        fila_plan = gestor.locator("tr", has_text=CODIGO)
        estado = await fila_plan.first.inner_text() if await fila_plan.count() else ""
        cap10 = await captura(gestor, "08_11_estado_finalizada", full_page=True)
        r.criterio("CA8-6", "El resultado final se refleja en existencias y en el estado de la práctica",
                   "Finalizada" in estado and stock_final == {"cuchillo": 10, "bowl": 19, "tabla": 15},
                   f"Existencias finales {stock_final} (inicio {stock_inicial}; esperado: cuchillo 10, bowl 19 porque uno se rompió, tabla 15); estado en Planificaciones: {'Finalizada' if 'Finalizada' in estado else estado[:80]}",
                   cap10, observacion=f"Apoyo: {cap9}, {cap_stock0}")

        await browser.close()
    print("Resultados:", r.guardar())


if __name__ == "__main__":
    asyncio.run(main())
