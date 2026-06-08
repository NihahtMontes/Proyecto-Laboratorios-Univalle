# Auditoria De Carga Historica

## Resumen
- Faculties: 3
- Careers: 4
- Laboratories: 14
- People/Providers: 8
- Managements: 3
- Equipments: 107
- EquipmentUnits: 556
- Verifications insertables: 0
- Requests insertables: 15
- Maintenances insertables: 1
- CostDetails insertables: 4
- Departures insertables: 2
- DepartureItems insertables: 2
- ManagementPlans insertables: 2
- Issues: 727 (416 errores)
- Rejects: 1053

## Hojas
- 0 · Facultades: dataRows=2, maxRow=107, maxCol=3
- 📋 INSTRUCCIONES: dataRows=13, maxRow=21, maxCol=5
- 0b · Carreras: dataRows=4, maxRow=107, maxCol=3
- 1 · Catálogos: dataRows=558, maxRow=1048576, maxCol=13
- 0c · Laboratorios: dataRows=14, maxRow=114, maxCol=6
- Sheet1: dataRows=0, maxRow=0, maxCol=0
- 2 · IInventario: dataRows=7, maxRow=1012, maxCol=11
- 3 · Verificaciones: dataRows=660, maxRow=1014, maxCol=7
- 4 · Solicitudes: dataRows=326, maxRow=1008, maxCol=8
- 5 · Kardex Mantenimiento: dataRows=2, maxRow=1010, maxCol=16
- 6 · Detalles de Costo: dataRows=4, maxRow=1007, maxCol=14
- 7 · Salidas (L-3): dataRows=2, maxRow=7, maxCol=9
- 8 · Plan de Mantenimiento: dataRows=4, maxRow=24, maxCol=20
- 9 · Técnicos y Proveedores: dataRows=6, maxRow=111, maxCol=6

## Hallazgos Incorporados
- Hoja 1: 558 filas utiles detectadas; 556 inventarios no duplicados.
- Hoja 1 se trata como catalogo + unidades por decision de cliente.
- Hoja 3 no genera inserts si columna A no contiene InventoryNumber real.
- Requests.Priority usa Medium=1 por defecto cuando la columna esta vacia.
- Facultad oficial normalizada: Facultad de Gastronomia y Turismo - Carrera de Gastronomía.

## Notas
- El SQL se genera offline y no se ejecuta automaticamente.
- Las verificaciones sin InventoryNumber real quedan rechazadas.
- Las solicitudes con inventarios multiples generan una solicitud por inventario valido.
