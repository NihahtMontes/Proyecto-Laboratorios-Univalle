# Generadores de carga desde la base estructural del cliente

Scripts Python (openpyxl) que leen el Excel del cliente y generan SQL idempotente
**solo para `DB_Laboratorios_Univalle_DEMO_QA`** (el SQL aborta si `DB_NAME()` es otra base).

| Script | Fuente | Contenido |
|---|---|---|
| `gen_fase_a_master_sql.py` | `BaseEstructuraFinal.xlsx` (V1) | Sedes, códigos de facultades/carreras/laboratorios del Excel, laboratorios faltantes, gestión 2027-1 y laboratorio real de cada unidad |
| `gen_v3_articles_sql.py` | `BaseEstructuraFinal_V3_ARTICULOS.xlsx` (verifica SHA-256) | Categorías, unidades, 1.023 artículos, 27 actores, roles, alias e incidencias de calidad |

Uso: `python <script>.py` genera el `.sql` en `%TEMP%\mm\`; ejecutarlo con
`sqlcmd -S localhost -E -I -b -f 65001 -d DB_Laboratorios_Univalle_DEMO_QA -i <archivo.sql>`.
Las rutas del Excel están fijas al inicio de cada script. Decisiones en `docs/GUIA_IMPORTACION_HISTORICA.md`.
