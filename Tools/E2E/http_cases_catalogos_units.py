"""
Catálogos / Unidades de medida: reglas del servidor por HTTP (misma suite que Categorías, parametrizada).
Solo contra la copia QA. Nombres con prefijo 'QAC'.
"""
from http_cases_catalogos_categories import catalog_suite
from http_cases_catalogos_common import run_module

UNITS = dict(
    key="MeasurementUnits", table="MeasurementUnits", base="/Catalogs/MeasurementUnits", noun="unidad", prefix="QAC Uni",
    maxlen=60, req_msg="El nombre de la unidad es obligatorio", max_msg="no puede superar los 60",
    dup_create="Ya existe una unidad de medida con este nombre", dup_edit="Ya existe otra unidad de medida",
    created_toast="registrada exitosamente", updated_toast="actualizada correctamente",
    blocked_msg="No se puede retirar la unidad", deleted_msg="ha sido dada de baja",
    recreate_msg="ya existe un registro con el mismo nombre", article_field="Input.MeasurementUnitId",
    other_field="Input.ArticleCategoryId", other_table="ArticleCategories",
)


def run(R, c):
    catalog_suite(R, c, UNITS)


if __name__ == "__main__":
    run_module("units", run)
