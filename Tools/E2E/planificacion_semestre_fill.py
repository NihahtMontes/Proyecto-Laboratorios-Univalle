"""Llena la plantilla de planificación descargada del sistema con las filas de prueba del recorrido E2E.
Uso: python planificacion_semestre_fill.py <plantilla.xlsx> <salida.xlsx> <filas.json>
filas.json = lista de objetos {subject, group, practice, date (yyyy-mm-dd), start, count, lab, teacher, students?}.
Los valores de materia, práctica y laboratorio pueden ser un prefijo: se completa con el texto exacto de la hoja Listas,
igual que haría una persona eligiendo de la lista desplegable."""
import json
import sys
from datetime import date

from openpyxl import load_workbook

template, output, rows_file = sys.argv[1:4]
rows = json.load(open(rows_file, encoding="utf-8"))
wb = load_workbook(template)
sheet = wb["Programacion"]
lists = wb["Listas"]


def from_list(column, prefix):
    for cell in lists.iter_rows(min_row=2, min_col=column, max_col=column, values_only=True):
        if cell[0] and str(cell[0]).startswith(prefix):
            return cell[0]
    return prefix  # no está en la lista: se escribe tal cual (caso de error a propósito)


for i, row in enumerate(rows, start=2):
    y, m, d = (int(x) for x in row["date"].split("-"))
    sheet.cell(i, 2, from_list(1, row["subject"]))
    sheet.cell(i, 3, row["group"])
    sheet.cell(i, 4, from_list(2, row["practice"]))
    sheet.cell(i, 6, date(y, m, d))
    sheet.cell(i, 7, row["start"])
    sheet.cell(i, 8, row["count"])
    sheet.cell(i, 9, from_list(4, row["lab"]))
    sheet.cell(i, 10, row["teacher"])
    if row.get("students"):
        sheet.cell(i, 11, row["students"])
wb.save(output)
print(f"{len(rows)} filas escritas en {output}")
