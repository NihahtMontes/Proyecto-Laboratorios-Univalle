-- Consultas de SOLO LECTURA para el documento «Estado de la base de datos» y el backlog.
-- Uso (desde la raíz; -I por los índices filtrados, -f 65001 para UTF-8):
--   sqlcmd -S localhost -E -I -d DB_Laboratorios_Univalle_REAL -W -s "|" -f 65001 \
--     -i .claude/skills/presentar-sprint/scripts/consultas_bd.sql > outputs/presentacion-sprint<N>/datos/bd.txt
-- Nombres reales de tablas: Equipments (no Equipment), People, Plannings, PlanningRequirements, ArticleStocks…
-- Enums: DataQualityIssueSeverity 0 Información · 1 Advertencia · 2 Error · 3 Crítica
--        DataQualityIssueStatus 0 Abierta · 1 Corregida · 2 Excepción aceptada · 3 Descartada
--        ManagementType 0 Preventiva · 1 Correctiva · 9 Por confirmar (histórica importada)
--        ManagementStatus 0 Activa · 1 Inactiva · 2 Completada (histórico)
SET NOCOUNT ON;

SELECT GETDATE() AS consulta;

PRINT '== Volúmenes por tabla (solo tablas con filas)';
SELECT t.name AS tabla, SUM(p.rows) AS filas
FROM sys.tables t JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY t.name HAVING SUM(p.rows) > 0 ORDER BY t.name;

PRINT '== Completitud de datos clave (total / con dato)';
SELECT 'Equipos: años de vida útil' AS dato, COUNT(*) AS total, SUM(CASE WHEN UsefulLifeYears > 0 THEN 1 ELSE 0 END) AS con_dato FROM Equipments
UNION ALL SELECT 'Unidades: fecha de adquisición', COUNT(*), SUM(CASE WHEN AcquisitionDate IS NOT NULL THEN 1 ELSE 0 END) FROM EquipmentUnits
UNION ALL SELECT 'Unidades: fecha de fabricación', COUNT(*), SUM(CASE WHEN ManufacturingDate IS NOT NULL THEN 1 ELSE 0 END) FROM EquipmentUnits
UNION ALL SELECT 'Unidades: valor de adquisición', COUNT(*), SUM(CASE WHEN ISNULL(AcquisitionValue, 0) > 0 THEN 1 ELSE 0 END) FROM EquipmentUnits
UNION ALL SELECT 'Unidades: número de serie', COUNT(*), SUM(CASE WHEN ISNULL(SerialNumber, '') <> '' THEN 1 ELSE 0 END) FROM EquipmentUnits
UNION ALL SELECT 'Unidades: número de inventario', COUNT(*), SUM(CASE WHEN ISNULL(InventoryNumber, '') <> '' THEN 1 ELSE 0 END) FROM EquipmentUnits
UNION ALL SELECT 'Laboratorios: capacidad', COUNT(*), SUM(CASE WHEN ISNULL(Capacity, 0) > 0 THEN 1 ELSE 0 END) FROM Laboratories
UNION ALL SELECT 'Artículos: vínculo a equipo', COUNT(*), SUM(CASE WHEN EquipmentId IS NOT NULL THEN 1 ELSE 0 END) FROM Articles;

PRINT '== Recetas, existencias y catálogo';
SELECT 'Artículos usados en recetas' AS dato, COUNT(DISTINCT ArticleId) AS n FROM PracticeRequirements
UNION ALL SELECT 'Artículos de recetas SIN existencia', COUNT(DISTINCT pr.ArticleId) FROM PracticeRequirements pr
    WHERE NOT EXISTS (SELECT 1 FROM ArticleStocks s WHERE s.ArticleId = pr.ArticleId)
UNION ALL SELECT 'Materias', COUNT(*) FROM Subjects
UNION ALL SELECT 'Materias SIN prácticas de catálogo', COUNT(*) FROM Subjects s WHERE NOT EXISTS (SELECT 1 FROM Practices p WHERE p.SubjectId = s.Id)
UNION ALL SELECT 'Equipos SIN unidades', COUNT(*) FROM Equipments e WHERE NOT EXISTS (SELECT 1 FROM EquipmentUnits u WHERE u.EquipmentId = e.Id)
UNION ALL SELECT 'Grupos de estudiantes (A1…)', COUNT(*) FROM StudentGroups
UNION ALL SELECT 'Salidas L-3 reales', COUNT(*) FROM Departures;

PRINT '== Vida útil por años';
SELECT UsefulLifeYears AS anios, COUNT(*) AS equipos FROM Equipments GROUP BY UsefulLifeYears ORDER BY UsefulLifeYears;

PRINT '== Gestiones por tipo y estado, y las activas';
SELECT Type, Status, COUNT(*) AS gestiones FROM Managements GROUP BY Type, Status;
SELECT Id, Code, Type, Status, Semester FROM Managements WHERE Status = 0;

PRINT '== Planificaciones por gestión';
SELECT m.Code, COUNT(*) AS planificaciones FROM Plannings p JOIN Managements m ON m.Id = p.ManagementId GROUP BY m.Code ORDER BY m.Code;

PRINT '== Observaciones de calidad (DataQualityIssues)';
SELECT COUNT(*) AS total, SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS abiertas FROM DataQualityIssues;
SELECT CASE Severity WHEN 0 THEN 'Información' WHEN 1 THEN 'Advertencia' WHEN 2 THEN 'Error' ELSE 'Crítica' END AS severidad, COUNT(*) AS n
FROM DataQualityIssues GROUP BY Severity ORDER BY Severity;
SELECT IssueCode, COUNT(*) AS n FROM DataQualityIssues GROUP BY IssueCode ORDER BY n DESC;
SELECT TOP 10 EntityName, FieldName, COUNT(*) AS n FROM DataQualityIssues
WHERE IssueCode = 'VALUE_PENDING_CONFIRMATION' GROUP BY EntityName, FieldName ORDER BY n DESC;
SELECT EntityName, FieldName, COUNT(*) AS n FROM DataQualityIssues WHERE Severity >= 2 GROUP BY EntityName, FieldName;
WITH x AS (SELECT IssueCode, LEFT(Description, 160) AS ejemplo, ROW_NUMBER() OVER (PARTITION BY IssueCode ORDER BY Id) AS rn FROM DataQualityIssues)
SELECT IssueCode, ejemplo FROM x WHERE rn = 1 ORDER BY IssueCode;
