-- Fixtures MÍNIMOS de conflictos de material para la copia de QA (NO usar contra otra base).
-- Escenarios sobre el martes 2026-11-03 (semana 15 de la gestión 4), con existencias registradas en ECONO por la QA:
--   E1 Overlap  : Silpat (stock 10): QA-CF-01 (H-01, P1-P2, 6) y QA-CF-02 (H-02, P2-P3, 6)   -> pico 12 en P2, faltan 2
--   E2 Shortage : Sifon  (stock 10): QA-CF-03 (H-03, P4-P5, 12)                              -> faltan 2
--   E3 Rotation : Laja   (stock 10): QA-CF-04 (H-04, P6, 6) y QA-CF-05 (H-05, P7, 6)         -> pico 6, suma 12 (rotación)
--   E4 NoStock  : Wok    (sin stock): QA-CF-06 (H-06, P8, 2)
-- Idempotente: borra y recrea las filas QA-CF-%. Ejecutar:  sqlcmd -S localhost -E -I -d DB_Laboratorios_Univalle_QA_E2E -b -i <este archivo>
SET NOCOUNT ON;
IF DB_NAME() <> N'DB_Laboratorios_Univalle_QA_E2E' THROW 50000, 'SEGURIDAD: este script solo corre en la copia de QA', 1;

DECLARE @d date = '2026-11-03';
DECLARE @mgmt int = 4;
DECLARE @wk int = (SELECT TOP 1 Id FROM AcademicWeeks WHERE ManagementId = @mgmt AND StartDate <= @d AND EndDate >= @d);
DECLARE @weekNo int = (SELECT TOP 1 [Number] FROM AcademicWeeks WHERE Id = @wk);
DECLARE @tpl int = (SELECT TOP 1 Id FROM Plannings WHERE ManagementId = @mgmt AND [Status] = N'Aprobada' AND CourseGroupId IS NULL ORDER BY Id DESC);
IF @wk IS NULL OR @tpl IS NULL THROW 50001, 'Sin semana o plantilla de planificación', 1;

DELETE FROM PlanningRequirements WHERE PlanningId IN (SELECT Id FROM Plannings WHERE Code LIKE N'QA-CF-%');
DELETE FROM Plannings WHERE Code LIKE N'QA-CF-%';

DECLARE @silpat int = (SELECT TOP 1 Id FROM Articles WHERE Name = N'Silpat');
DECLARE @sifon int = (SELECT TOP 1 Id FROM Articles WHERE Name = N'Sifon');
DECLARE @laja int = (SELECT TOP 1 Id FROM Articles WHERE Name = N'Laja');
DECLARE @wok int = (SELECT TOP 1 Id FROM Articles WHERE Name = N'Wok');

CREATE TABLE #s (code nvarchar(20), lab int, p int, cnt int, st nvarchar(20), art int, qty decimal(18,2), grp nvarchar(20));
INSERT INTO #s VALUES
 (N'QA-CF-01', 1, 1, 2, N'Planificada', @silpat, 6,  N'QACF1'),
 (N'QA-CF-02', 2, 2, 2, N'Planificada', @silpat, 6,  N'QACF2'),
 (N'QA-CF-03', 3, 4, 2, N'Planificada', @sifon,  12, N'QACF3'),
 (N'QA-CF-04', 4, 6, 1, N'Aprobada',    @laja,   6,  N'QACF4'),
 (N'QA-CF-05', 5, 7, 1, N'Aprobada',    @laja,   6,  N'QACF5'),
 (N'QA-CF-06', 6, 8, 1, N'Planificada', @wok,    2,  N'QACF6');

INSERT INTO Plannings (SubjectId, GroupId, TeacherId, LaboratoryId, PracticeName, AcademicWeek, ScheduledDate, [Status], CreatedDate, LastModifiedDate,
                       CreatedById, ModifiedById, IsDeleted, DeletedDate, Observations, ScheduledEndDate, TeacherPersonId, AcademicWeekId, AllLaboratories,
                       CampusId, Code, CourseGroupId, EndTime, GroupName, ManagementId, PracticeNumber, PracticeScheduleId, RequestDate, StartTime,
                       StartPeriodId, PeriodCount, StatusBeforeRecovery)
SELECT t.SubjectId, NULL, NULL, s.lab, N'QA fixture ' + s.code, @weekNo,
       DATEADD(SECOND, DATEDIFF(SECOND, 0, CAST(p1.StartTime AS datetime)), CAST(@d AS datetime2)), s.st, SYSUTCDATETIME(), SYSUTCDATETIME(),
       t.CreatedById, t.ModifiedById, 0, NULL, N'Fixture de QA', DATEADD(SECOND, DATEDIFF(SECOND, 0, CAST(p2.EndTime AS datetime)), CAST(@d AS datetime2)),
       t.TeacherPersonId, @wk, 0, t.CampusId, s.code, NULL, p2.EndTime, s.grp, @mgmt, t.PracticeNumber, NULL, SYSUTCDATETIME(), p1.StartTime,
       s.p, s.cnt, NULL
FROM #s s
JOIN Plannings t ON t.Id = @tpl
JOIN ClassPeriods p1 ON p1.Id = s.p
JOIN ClassPeriods p2 ON p2.Id = s.p + s.cnt - 1;

INSERT INTO PlanningRequirements (PlanningId, EquipmentUnitId, ExpectedQuantity, DeliveredQuantity, ReturnedQuantity, ArticleId, Code, Observations, RequestedQuantity)
SELECT pl.Id, NULL, CEILING(s.qty), 0, 0, s.art, NULL, NULL, s.qty
FROM #s s JOIN Plannings pl ON pl.Code = s.code;

SELECT pl.Id, pl.Code, pl.[Status], pl.LaboratoryId, pl.StartPeriodId, pl.PeriodCount, r.ArticleId, r.RequestedQuantity
FROM Plannings pl JOIN PlanningRequirements r ON r.PlanningId = pl.Id WHERE pl.Code LIKE N'QA-CF-%' ORDER BY pl.Code;
DROP TABLE #s;
