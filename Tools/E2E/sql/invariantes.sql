-- invariantes.sql — Consultas de los invariantes de integridad (solo lectura).
-- Referencia del script Tools/E2E/sql_invariantes.py (IDs IV-01..IV-10).
-- Valores de enums en Models/Enums: ManagementType(0/1/9), ManagementStatus(0/1/2/99),
-- ManagementPlanStatus(0..3), WizardPhase(1..6), EquipmentStatus(Operational=0, OnLoan=10),
-- LoanStatus(Active=0, Overdue=2), RequestType(Technical=1).

-- IV-01 Fase vs vinculos (excluye planes con EquipmentUnitId NULL, que son exclusiones)
-- IV-01.1
SELECT Id FROM ManagementPlans WHERE EquipmentUnitId IS NOT NULL AND CurrentPhase>=3 AND RequestId IS NULL;
-- IV-01.2
SELECT Id FROM ManagementPlans WHERE EquipmentUnitId IS NOT NULL AND CurrentPhase>=4 AND MaintenanceId IS NULL;
-- IV-01.3
SELECT Id FROM ManagementPlans WHERE EquipmentUnitId IS NOT NULL AND CurrentPhase>=5 AND DepartureId IS NULL;
-- IV-01.4
SELECT p.Id FROM ManagementPlans p JOIN Managements m ON m.Id=p.ManagementId AND m.Type=1
WHERE p.EquipmentUnitId IS NOT NULL AND p.VerificationId IS NOT NULL;

-- IV-02 Unidad vs salida
-- IV-02.1
SELECT Id FROM EquipmentUnits WHERE CurrentStatus=10
AND NOT EXISTS (SELECT 1 FROM Departures d WHERE d.EquipmentUnitId=EquipmentUnits.Id AND d.Status IN (0,2));
-- IV-02.2
SELECT DISTINCT u.Id FROM EquipmentUnits u
JOIN Departures d ON d.EquipmentUnitId=u.Id AND d.Status IN (0,2)
JOIN ManagementPlans p ON p.DepartureId=d.Id AND p.PlanStatus<>2
WHERE u.CurrentStatus=0;

-- IV-03 Historial de estados
-- IV-03.1
SELECT h.EquipmentUnitId FROM EquipmentStateHistories h WHERE h.EquipmentUnitId IS NOT NULL
GROUP BY h.EquipmentUnitId HAVING SUM(CASE WHEN h.EndDate IS NULL THEN 1 ELSE 0 END) > 1;
-- IV-03.2
SELECT Id FROM EquipmentStateHistories WHERE EndDate IS NOT NULL AND EndDate < StartDate;

-- IV-04 Costos
-- IV-04.1 (espejo de CK_CostDetails_ExactlyOneParent)
SELECT Id FROM CostDetails WHERE (RequestId IS NULL AND MaintenanceId IS NULL)
OR (RequestId IS NOT NULL AND MaintenanceId IS NOT NULL);
-- IV-04.2 (solo detalles no anulados; tolerancia 0.005)
SELECT m.Id FROM Maintenances m CROSS APPLY (SELECT SUM(cd.Quantity*cd.UnitPrice) AS s
FROM CostDetails cd WHERE cd.MaintenanceId=m.Id AND cd.IsVoided=0) c
WHERE c.s IS NOT NULL AND (m.ActualCost IS NULL OR ABS(m.ActualCost-c.s)>0.005);

-- IV-05 Plan Completado (PlanStatus=2, EquipmentUnitId no NULL)
-- IV-05.1
SELECT Id FROM ManagementPlans WHERE PlanStatus=2 AND EquipmentUnitId IS NOT NULL
AND (RequestId IS NULL OR MaintenanceId IS NULL OR DepartureId IS NULL
OR KardexHistoryId IS NULL OR AcquisitionRequestId IS NULL);
-- IV-05.2
SELECT p.Id FROM ManagementPlans p JOIN Managements m ON m.Id=p.ManagementId AND m.Type=0
WHERE p.PlanStatus=2 AND p.EquipmentUnitId IS NOT NULL AND p.VerificationId IS NULL;

-- IV-06 Gestiones
-- IV-06.1
SELECT Id FROM Managements WHERE Status=0
AND Type IN (SELECT Type FROM Managements WHERE Status=0 GROUP BY Type HAVING COUNT(*)>1) ORDER BY Id;
-- IV-06.2
SELECT Id FROM Managements WHERE Status<>99
AND Code IN (SELECT Code FROM Managements WHERE Status<>99 GROUP BY Code HAVING COUNT(*)>1) ORDER BY Id;
-- IV-06.3
SELECT Id FROM Managements WHERE Type=1 AND Semester=0 AND CreatedDate>='2026-10-18';

-- IV-07 Huerfanos
-- IV-07.1 (Status 99 = Cancelada: no avanza a plan por diseno)
SELECT Id FROM Requests WHERE Type=1 AND Status<>99 AND CreatedDate>='2026-10-18'
AND NOT EXISTS (SELECT 1 FROM ManagementPlans p WHERE p.RequestId=Requests.Id);
-- IV-07.2
SELECT Id FROM Maintenances WHERE EquipmentUnitId IS NULL;

-- IV-08 Chequeo de constraints (no debe devolver filas)
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;

-- IV-09 Notificaciones duplicadas exactas
SELECT n.Id FROM Notifications n WHERE EXISTS (SELECT 1 FROM Notifications o WHERE o.Id<>n.Id
AND ISNULL(o.UserId,-1)=ISNULL(n.UserId,-1) AND o.Title=n.Title AND o.Message=n.Message
AND ISNULL(o.ManagementId,-1)=ISNULL(n.ManagementId,-1) AND o.CreatedAt=n.CreatedAt) ORDER BY n.Id;

-- IV-10 Un plan por unidad
-- IV-10.1
SELECT p.Id FROM ManagementPlans p JOIN (SELECT ManagementId, EquipmentUnitId
FROM ManagementPlans WHERE EquipmentUnitId IS NOT NULL AND PlanStatus<>2
GROUP BY ManagementId, EquipmentUnitId HAVING COUNT(*)>1) v
ON p.ManagementId=v.ManagementId AND p.EquipmentUnitId=v.EquipmentUnitId
WHERE p.PlanStatus<>2 ORDER BY p.Id;
-- IV-10.2
SELECT p.Id FROM ManagementPlans p JOIN Managements m ON m.Id=p.ManagementId AND m.Type=0
JOIN (SELECT p2.ManagementId, p2.EquipmentUnitId FROM ManagementPlans p2
WHERE p2.EquipmentUnitId IS NOT NULL GROUP BY p2.ManagementId, p2.EquipmentUnitId HAVING COUNT(*)>1) v
ON p.ManagementId=v.ManagementId AND p.EquipmentUnitId=v.EquipmentUnitId ORDER BY p.Id;
