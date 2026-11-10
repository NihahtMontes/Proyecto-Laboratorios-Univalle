"""Ejercicio 7 — Aceptación del registro de información (entidad principal: Laboratorio).

Usuario final: Rodrigo Vargas (rol Administrador, gestor.p3). Perfil sin permiso: Lucía Fernández (Supervisor).
Requisito del proyecto: el responsable registra los ambientes (laboratorios) de la facultad para poder planificar
prácticas en ellos; cada laboratorio pertenece a una facultad, recibe un código LAB-##### automático y su nombre
no puede repetirse dentro de la misma facultad.
"""
import asyncio
from playwright.async_api import async_playwright
from comun import BASE, Registro, nueva_pagina, iniciar_sesion, captura, texto_alerta, cerrar_alerta

NOMBRE = "Cocina Internacional"


async def main():
    r = Registro("ej07_registro")
    async with async_playwright() as p:
        browser = await p.chromium.launch()
        page = await nueva_pagina(browser)
        await iniciar_sesion(page, "gestor.p3")

        # Paso 1: abrir el formulario desde el listado de laboratorios.
        await page.goto(BASE + "/Laboratories")
        await page.wait_for_load_state("networkidle")
        cap_listado = await captura(page, "07_01_listado_inicial")
        await page.goto(BASE + "/Laboratories/Create")
        await page.wait_for_load_state("networkidle")
        cap_form = await captura(page, "07_02_formulario_vacio")
        campos = {
            "Unidad académica (Facultad)": await page.locator("[name='Input.FacultyId']").count(),
            "Nombre": await page.locator("[name='Input.Name']").count(),
            "Piso": await page.locator("[name='Input.Floor']").count(),
            "Descripción": await page.locator("[name='Input.Description']").count(),
        }
        obligatorios = await page.locator("label .text-danger, label:has-text('*'), .required").count()
        r.criterio("CA7-1", "El formulario presenta los campos requeridos",
                   all(v == 1 for v in campos.values()),
                   "Campos presentes: " + ", ".join(k for k, v in campos.items() if v) +
                   "; código LAB-##### generado automáticamente (no editable)", cap_form)

        # Paso 2: enviar el formulario vacío (validación de obligatorios).
        await page.click("button[type=submit]:has-text('Registrar')")
        await page.wait_for_load_state("networkidle")
        mensajes = [m.strip() for m in await page.locator(".help-block, .field-validation-error").all_inner_texts() if m.strip()]
        cap_vacio = await captura(page, "07_03_validacion_obligatorios")
        nuevos = await page.locator("text=" + NOMBRE).count()
        r.criterio("CA7-2", "Los campos obligatorios se validan",
                   any("obligatori" in m.lower() for m in mensajes) and await page.locator("text=" + NOMBRE).count() == 0,
                   "Al enviar vacío: " + " | ".join(dict.fromkeys(mensajes)), cap_vacio)
        r.criterio("CA7-3", "El sistema informa los errores de forma comprensible",
                   "Debe seleccionar una facultad académica" in mensajes and "El nombre es obligatorio" in mensajes,
                   "Mensajes en español, junto a cada campo: " + " | ".join(dict.fromkeys(mensajes)), cap_vacio)

        # Paso 3: registro válido.
        await page.select_option("[name='Input.FacultyId']", label="Facultad de Gastronomía y Turismo")
        await page.fill("[name='Input.Name']", NOMBRE)
        await page.fill("[name='Input.Floor']", "Segundo piso")
        await page.fill("[name='Input.Description']", "Ambiente con seis estaciones de cocción y campana extractora.")
        cap_lleno = await captura(page, "07_04_formulario_lleno")
        await page.click("button[type=submit]:has-text('Registrar')")
        await page.wait_for_load_state("networkidle")
        exito = await texto_alerta(page)
        cap_exito = await captura(page, "07_05_registro_exitoso")
        await cerrar_alerta(page)
        fila = page.locator("tr", has_text=NOMBRE)
        en_listado = await fila.count() > 0
        codigo = ""
        if en_listado:
            texto_fila = await fila.first.inner_text()
            codigo = next((t for t in texto_fila.replace("\t", " ").split() if t.startswith("LAB-")), "")

        # Paso 4: consultar el registro guardado (detalle).
        detalle_ok = False
        cap_detalle = None
        enlace = fila.first.locator("a[href*='Details']")
        if en_listado and await enlace.count() > 0:
            await enlace.first.click()
            await page.wait_for_load_state("networkidle")
            cuerpo = await page.inner_text("body")
            detalle_ok = NOMBRE in cuerpo and "Segundo piso" in cuerpo
            cap_detalle = await captura(page, "07_06_detalle_registro", full_page=True)
        r.criterio("CA7-4", "El registro válido se guarda y puede consultarse posteriormente",
                   "registrado" in exito.lower() and en_listado and detalle_ok,
                   f"Mensaje: «{exito.splitlines()[-1] if exito else ''}»; aparece en el listado con código {codigo}; "
                   f"el detalle muestra nombre y piso: {detalle_ok}", cap_exito, observacion=f"Capturas de apoyo: {cap_lleno}, {cap_detalle}")

        # Paso 5: operación no permitida por la regla de negocio (nombre duplicado en la misma facultad).
        await page.goto(BASE + "/Laboratories/Create")
        await page.select_option("[name='Input.FacultyId']", label="Facultad de Gastronomía y Turismo")
        await page.fill("[name='Input.Name']", "cocina internacional")
        await page.click("button[type=submit]:has-text('Registrar')")
        await page.wait_for_load_state("networkidle")
        dup = " | ".join(m.strip() for m in await page.locator(".field-validation-error, .validation-summary-errors li").all_inner_texts() if m.strip())
        cap_dup = await captura(page, "07_07_duplicado_rechazado")
        await page.goto(BASE + "/Laboratories")
        repetidos = await page.locator("tr", has_text=NOMBRE).count()

        # Paso 6: operación no permitida por permisos (Supervisor intenta registrar).
        sup = await nueva_pagina(browser)
        await iniciar_sesion(sup, "supervisor.p3")
        await sup.goto(BASE + "/Laboratories/Create")
        await sup.wait_for_load_state("networkidle")
        denegado = "AccesoDenegado" in sup.url or "acceso" in (await sup.inner_text("body")).lower()
        cap_perm = await captura(sup, "07_08_supervisor_sin_permiso")
        r.criterio("CA7-5", "El sistema impide las operaciones no permitidas por las reglas de negocio",
                   "Ya existe un laboratorio con este nombre" in dup and repetidos == 1 and denegado,
                   f"Duplicado (misma facultad, otra capitalización): «{dup}»; filas con ese nombre: {repetidos}. "
                   f"Supervisor en /Laboratories/Create → {sup.url.replace(BASE, '')}", cap_dup, observacion=f"Captura de permisos: {cap_perm}")

        await browser.close()
    print("Resultados:", r.guardar())


if __name__ == "__main__":
    asyncio.run(main())
