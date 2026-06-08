# Auditoria De Clasificacion Tecnica V2

Generada offline desde los artefactos de seed/auditoria existentes. El script de aplicacion fue ejecutado en SQL Server DEV.

## Conteos

- Catalogos revisados: 107
- Unidades representadas por la auditoria: 556
- Catalogos con UPDATE aplicado/sugerido: 107
- Catalogos sin cambio sugerido: 0
- Catalogos pendientes de revision manual: 0
- Unidades con ambiente respaldado: 7
- Unidades sin ambiente respaldado: 549

## Distribucion Final Sugerida Por Categoria

| Categoria | Catalogos |
|---|---:|
| Equipo | 68 |
| Otro | 27 |
| Utensilio | 12 |

| Categoria | Unidades |
|---|---:|
| Equipo | 196 |
| Otro | 287 |
| Utensilio | 73 |

## Distribucion Final Sugerida Por Subclasificacion Tecnica

| Subclasificacion | Catalogos |
|---|---:|
| Equipos audiovisuales | 10 |
| Equipos de calor | 13 |
| Equipos de frio | 6 |
| Equipos de seguridad industrial | 6 |
| Equipos electricos | 4 |
| Informatico / Software | 17 |
| Instrumental de Medicion | 3 |
| Maquinas rotativas | 9 |
| Mobiliario | 21 |
| Otro | 18 |

| Subclasificacion | Unidades |
|---|---:|
| Equipos audiovisuales | 21 |
| Equipos de calor | 57 |
| Equipos de frio | 7 |
| Equipos de seguridad industrial | 19 |
| Equipos electricos | 33 |
| Informatico / Software | 33 |
| Instrumental de Medicion | 7 |
| Maquinas rotativas | 19 |
| Mobiliario | 244 |
| Otro | 116 |

## Utensilios Por Tipo

| Tipo utensilio | Catalogos |
|---|---:|
| Menaje de cocina | 11 |
| Panaderia, Reposteria y Pasteleria | 1 |

## Casos Criticos Corregidos

- CAMARA CONSERVADORA / ASBER: Equipo - Equipos de frio.
- MICROONDA / LG: Equipo - Equipos de calor.
- MICROONDA / PANASONIC: Equipo - Equipos de calor.
- MICROONDA / SAMSUNG: Equipo - Equipos de calor.
- CAMPANA DE EXTRACCION: Equipo - Equipos electricos.
- CAMPANA DE EXTRACCION / WILDA: Equipo - Equipos electricos.
- FERMENTADOR / WILDA: Equipo - Equipos de calor.
- CPU DE ESCRITORIO / LENOVO: Equipo - Informatico / Software.
- CASILLERO METALICO: Otro - Mobiliario.
- MESON DE METAL: Otro - Mobiliario.
- MESON DE METAL / WILDA: Otro - Mobiliario.
- MESON REFRIGERADOR / VENTUS: Equipo - Equipos de frio.
- MESON ROBUSTO: Otro - Mobiliario.
- MESON ROBUSTO / WILDA: Otro - Mobiliario.
- MESON ROBUSTO / WILDA: Otro - Mobiliario.
- LAVAPLATOS DE ACERO INOXIDABLE: Otro - Otro.
- LAVAPLATOS DE ACERO INOXIDABLE / WILDA: Otro - Otro.
- PORTA UTENSILIOS: Utensilio - Otro - Menaje de cocina.
- EXTRACTOR DE AIRE / LOREN SID: Otro - Otro.

## Scripts

- `classification-correction-v2-apply.sql`: version aplicada con `COMMIT TRANSACTION`.
- `classification-correction-v2.sql`: version segura para revision, termina en `ROLLBACK TRANSACTION`.

## Catálogos extra de la base

Además de los 107 catálogos derivados del Excel, se ajustaron 5 catálogos existentes en la base: Batidora Industrial, Microondas Industrial, Horno Rational, Microscopio Binocular y Espectrofotómetro UV-Vis.

## Validaciones En BD

- `EquipmentWithUtensilType = 0`.
- `UtensilWithoutSpecificType = 0`.
- `OtherWithUtensilType = 0`.
- Ambientes: no se infirieron masivamente; `environment-coverage.csv` reporta unidades con y sin ambiente respaldado.

