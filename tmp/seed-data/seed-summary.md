# Seed Data Historico

## Resumen
- Faculties: 3
- Careers: 4
- Laboratories: 14
- People/Providers: 8
- Managements: 3
- Equipments: 107
- EquipmentUnits: 556
- Chains: 8
- Preventive chains: 7
- Corrective chains: 1
- Verifications: 7
- Requests: 16
- Maintenances: 3
- Departures: 2
- Kardex histories: 3
- CostDetails: 7
- Origins Excel: 724
- Origins Inferido: 31
- Seed rejects: 0

## Hallazgos
- Modo seed: genera MERGE idempotente offline; no ejecuta SQL.
- Inferencia limitada a inventarios con registros transaccionales reales en hojas 4-8.
- Correctivos empiezan en L7; no se infiere L6 para correctivos.
- Las verificaciones sin inventario real permanecen fuera del seed.

## Cadenas
- 2023-2 / 49236: Correctivo=False; L6=INF_VER_20232_49236; L7=INF_REQ_20232_49236; L8=INF_MNT_20232_49236; L3=N/A; Kardex=INF_KDX_20232_49236; L12=N/A; State=6/8
- 2025-1 / 17355: Correctivo=False; L6=INF_VER_20251_17355; L7=REQ_R22_17355; L8=N/A; L3=N/A; Kardex=N/A; L12=N/A; State=3/4
- 2025-1 / 34179: Correctivo=False; L6=INF_VER_20251_34179; L7=REQ_R10_34179; L8=INF_MNT_20251_34179; L3=DEP_R6_34179; Kardex=INF_KDX_20251_34179; L12=PUR_CUCHARA001_34179; State=6/9
- 2025-1 / 34180: Correctivo=False; L6=INF_VER_20251_34180; L7=REQ_R11_34180; L8=N/A; L3=N/A; Kardex=N/A; L12=N/A; State=3/4
- 2025-1 / 34744: Correctivo=False; L6=INF_VER_20251_34744; L7=REQ_R7_34744; L8=N/A; L3=N/A; Kardex=N/A; L12=N/A; State=3/4
- 2025-1 / 35528: Correctivo=True; L6=N/A; L7=REQ_R42_35528; L8=MNT_R6_35528; L3=N/A; Kardex=INF_KDX_20251_35528; L12=N/A; State=6/8
- 2025-1 / 39170: Correctivo=False; L6=INF_VER_20251_39170; L7=REQ_R6_39170; L8=N/A; L3=N/A; Kardex=N/A; L12=N/A; State=3/4
- 2025-1 / 49236: Correctivo=False; L6=INF_VER_20251_49236; L7=REQ_R94_49236; L8=N/A; L3=N/A; Kardex=N/A; L12=N/A; State=3/4
