-- Classification correction review
-- Generated offline from Excel-derived seed data. Review classification-review.csv before executing.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ABATIDOR FASTER | Units: 1 | Rule: Equipo: frio por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 9, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ABATIDOR FASTER'
  AND ISNULL(Brand, N'') = ISNULL(N'AFINOX', N'')
  AND ISNULL(Model, N'') = ISNULL(N'FASTER 5T GF 230V', N'');

-- ACCES POINT | Units: 1 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ACCES POINT'
  AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'AIR-CAP370', N'');

-- ACCES POINT | Units: 2 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ACCES POINT'
  AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'AIR-CAP3702E-A-K9', N'');

-- ALL IN ONE | Units: 1 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ALL IN ONE'
  AND ISNULL(Brand, N'') = ISNULL(N'HP', N'')
  AND ISNULL(Model, N'') = ISNULL(N'24-E015LA', N'');

-- ALL IN ONE | Units: 1 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ALL IN ONE'
  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'10BB-A0C900', N'');

-- ALL IN ONE | Units: 1 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ALL IN ONE'
  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'THINK CENTRE M73Z', N'');

-- AMASADORA | Units: 1 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'AMASADORA'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- APARATO TELEFONICO | Units: 1 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'APARATO TELEFONICO'
  AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'CP-3905', N'');

-- BALANZA ELECTRONICA | Units: 1 | Rule: Equipo: medicion/PCC por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 3, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'BALANZA ELECTRONICA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- BALANZA ELECTRONICA | Units: 2 | Rule: Equipo: medicion/PCC por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 3, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'BALANZA ELECTRONICA'
  AND ISNULL(Brand, N'') = ISNULL(N'ELECTRONIC SCALE', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- BALANZA ELECTRONICA | Units: 4 | Rule: Equipo: medicion/PCC por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 3, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'BALANZA ELECTRONICA'
  AND ISNULL(Brand, N'') = ISNULL(N'OHAUS', N'')
  AND ISNULL(Model, N'') = ISNULL(N'RANGER R31P30', N'');

-- BANCA DE MADERA | Units: 2 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'BANCA DE MADERA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- BATIDORA | Units: 4 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'BATIDORA'
  AND ISNULL(Brand, N'') = ISNULL(N'KITCHENAID', N'')
  AND ISNULL(Model, N'') = ISNULL(N'5KSM7591', N'');

-- BATIDORA | Units: 1 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'BATIDORA'
  AND ISNULL(Brand, N'') = ISNULL(N'KITCHENAID', N'')
  AND ISNULL(Model, N'') = ISNULL(N'5KSM7591,', N'');

-- BATIDORA | Units: 1 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'BATIDORA'
  AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- CAFETERA IND | Units: 1 | Rule: Equipo: calor por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 8, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CAFETERA IND'
  AND ISNULL(Brand, N'') = ISNULL(N'ASTORIA', N'')
  AND ISNULL(Model, N'') = ISNULL(N'INDUS. ITALIANA', N'');

-- CAMARA CONSERVADORA | Units: 1 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CAMARA CONSERVADORA'
  AND ISNULL(Brand, N'') = ISNULL(N'ASBER', N'')
  AND ISNULL(Model, N'') = ISNULL(N'ARR-43', N'');

-- CAMARA DE VIDEO | Units: 1 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CAMARA DE VIDEO'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- CAMARA DE VIDEO | Units: 3 | Rule: Equipo: seguridad industrial por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 13, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CAMARA DE VIDEO'
  AND ISNULL(Brand, N'') = ISNULL(N'HIKVISION', N'')
  AND ISNULL(Model, N'') = ISNULL(N'DS-2CEE55A2N-IRN', N'');

-- CAMARA DE VIDEO | Units: 1 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CAMARA DE VIDEO'
  AND ISNULL(Brand, N'') = ISNULL(N'HIKVISION', N'')
  AND ISNULL(Model, N'') = ISNULL(N'DS2CC5192N-IR1', N'');

-- CAMARA DE VIDEO | Units: 1 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CAMARA DE VIDEO'
  AND ISNULL(Brand, N'') = ISNULL(N'SONY BALUMS', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- CASILLERO METALICO | Units: 39 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CASILLERO METALICO'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- COCINA INDUSTRIAL | Units: 25 | Rule: Equipo: calor por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 8, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'COCINA INDUSTRIAL'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- COCINA INDUSTRIAL | Units: 8 | Rule: Equipo: calor por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 8, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'COCINA INDUSTRIAL'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(N'AISI304L 2B', N'');

-- CPU DE ESCRITORIO | Units: 4 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CPU DE ESCRITORIO'
  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'THINK CENTRE', N'');

-- CREDENZA DE MELAMINA | Units: 1 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'CREDENZA DE MELAMINA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- ESCRITORIO DE MADERA | Units: 6 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ESCRITORIO DE MADERA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- ESCRITORIO DE MELAMINA MODULAR | Units: 1 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ESCRITORIO DE MELAMINA MODULAR'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- ESCRITORIO METALICO | Units: 1 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ESCRITORIO METALICO'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- ESTANTE BAR DE MADERA | Units: 1 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ESTANTE BAR DE MADERA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- ESTANTE METÁLICO | Units: 1 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ESTANTE METÁLICO'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- ESTANTE PORTA BANDEJAS | Units: 1 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ESTANTE PORTA BANDEJAS'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- EXTINTOR | Units: 5 | Rule: Equipo: seguridad industrial por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 13, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTINTOR'
  AND ISNULL(Brand, N'') = ISNULL(N'ABC', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- EXTINTOR | Units: 1 | Rule: Equipo: seguridad industrial por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 13, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTINTOR'
  AND ISNULL(Brand, N'') = ISNULL(N'CYLINDERS', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- EXTINTOR | Units: 3 | Rule: Equipo: seguridad industrial por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 13, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTINTOR'
  AND ISNULL(Brand, N'') = ISNULL(N'FANACIM', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- EXTINTOR | Units: 1 | Rule: Equipo: seguridad industrial por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 13, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTINTOR'
  AND ISNULL(Brand, N'') = ISNULL(N'MMB CILINDERS', N'')
  AND ISNULL(Model, N'') = ISNULL(N'FNC 10', N'');

-- EXTINTOR | Units: 3 | Rule: Equipo: seguridad industrial por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 13, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTINTOR'
  AND ISNULL(Brand, N'') = ISNULL(N'MMB CYLINDERS', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- EXTINTOR TIPO K | Units: 6 | Rule: Equipo: seguridad industrial por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 13, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTINTOR TIPO K'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- EXTRACTOR DE AIRE | Units: 1 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTRACTOR DE AIRE'
  AND ISNULL(Brand, N'') = ISNULL(N'LOREN SID', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- EXTRACTOR DE HUMOS Y GRASAS | Units: 8 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTRACTOR DE HUMOS Y GRASAS'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- EXTRACTOR DE HUMOS Y GRASAS | Units: 6 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'EXTRACTOR DE HUMOS Y GRASAS'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- FERMENTADOR | Units: 1 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'FERMENTADOR'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(N'A155 304L2B', N'');

-- FREIDORA | Units: 1 | Rule: Equipo: calor por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 8, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'FREIDORA'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- GAVETERO DE MELAMINA | Units: 1 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'GAVETERO DE MELAMINA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- HORNO | Units: 8 | Rule: Equipo: calor por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 8, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'HORNO'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- HORNO CONVECTOR | Units: 1 | Rule: Equipo: calor por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 8, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'HORNO CONVECTOR'
  AND ISNULL(Brand, N'') = ISNULL(N'ARIANNA', N'')
  AND ISNULL(Model, N'') = ISNULL(N'XEFT-04HS-ELDV', N'');

-- LAVAPLATOS DE ACERO INOXIDABLE | Units: 17 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'LAVAPLATOS DE ACERO INOXIDABLE'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- LAVAPLATOS DE ACERO INOXIDABLE | Units: 13 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'LAVAPLATOS DE ACERO INOXIDABLE'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- LICUADORA | Units: 7 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'LICUADORA'
  AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- LICUADORA | Units: 2 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'LICUADORA'
  AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'')
  AND ISNULL(Model, N'') = ISNULL(N'XPERT', N'');

-- MESA DE MADERA | Units: 8 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MESA DE MADERA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- MESA METALICA | Units: 5 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MESA METALICA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- MESA METALICA | Units: 29 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MESA METALICA'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- MESON DE METAL | Units: 1 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MESON DE METAL'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- MESON REFRIGERADOR | Units: 2 | Rule: Equipo: frio por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 9, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MESON REFRIGERADOR'
  AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'')
  AND ISNULL(Model, N'') = ISNULL(N'VMR2PS-280E', N'');

-- MESON ROBUSTO | Units: 1 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MESON ROBUSTO'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- MESON ROBUSTO | Units: 3 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MESON ROBUSTO'
  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')
  AND ISNULL(Model, N'') = ISNULL(N'A155 304L2B', N'');

-- MICROONDA | Units: 1 | Rule: Equipo: calor por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 8, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MICROONDA'
  AND ISNULL(Brand, N'') = ISNULL(N'PANASONIC', N'')
  AND ISNULL(Model, N'') = ISNULL(N'NN-ST34HM', N'');

-- MOLINO DE COFFIE | Units: 1 | Rule: Equipo: maquina rotativa por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 12, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MOLINO DE COFFIE'
  AND ISNULL(Brand, N'') = ISNULL(N'FIORENZATO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'F64 E', N'');

-- MUEBLE DE MADERA PARA COMPUTADORA | Units: 1 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MUEBLE DE MADERA PARA COMPUTADORA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- MUEBLE DE MELAMINA PARA COMPUTADORA | Units: 5 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MUEBLE DE MELAMINA PARA COMPUTADORA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- MUEBLE METALICO PARA COMPUTADORA | Units: 2 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'MUEBLE METALICO PARA COMPUTADORA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- PINZA KELLY | Units: 2 | Rule: Utensilio: menaje de cocina por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 5, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'PINZA KELLY'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- PORTA BANDEJAS | Units: 1 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'PORTA BANDEJAS'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- PORTA TABLAS | Units: 23 | Rule: Utensilio: menaje de cocina por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 5, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'PORTA TABLAS'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- PROYECTOR LED/LASER | Units: 1 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'PROYECTOR LED/LASER'
  AND ISNULL(Brand, N'') = ISNULL(N'CASIO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'(YW-40) XJ-F20XN', N'');

-- PROYECTOR LED/LASER | Units: 4 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'PROYECTOR LED/LASER'
  AND ISNULL(Brand, N'') = ISNULL(N'CASIO', N'')
  AND ISNULL(Model, N'') = ISNULL(N'XJ-F20XN', N'');

-- REFRIGERADOR | Units: 1 | Rule: Equipo: frio por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 9, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'REFRIGERADOR'
  AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'')
  AND ISNULL(Model, N'') = ISNULL(N'VREF-1000BEN', N'');

-- REFRIGERADOR | Units: 1 | Rule: Equipo: frio por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 9, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'REFRIGERADOR'
  AND ISNULL(Brand, N'') = ISNULL(N'VENTUS SORP', N'')
  AND ISNULL(Model, N'') = ISNULL(N'VREF-1000BEN', N'');

-- ROUTER | Units: 2 | Rule: Equipo: electronico/informatico por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 0, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'ROUTER'
  AND ISNULL(Brand, N'') = ISNULL(N'VIEW SONIC', N'')
  AND ISNULL(Model, N'') = ISNULL(N'WPG-370', N'');

-- SILLA DE MADERA FIJA | Units: 47 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'SILLA DE MADERA FIJA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- SILLA DE MADERA FIJA TAPIZ TELA | Units: 12 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'SILLA DE MADERA FIJA TAPIZ TELA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- SILLA METALICA FIJA TAPIZ CUERINA | Units: 3 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'SILLA METALICA FIJA TAPIZ CUERINA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- SILLA METALICA FIJA TAPIZ TELA | Units: 1 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'SILLA METALICA FIJA TAPIZ TELA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- SILLA METALICA GIRATORIA TAPIZ TELA | Units: 1 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'SILLA METALICA GIRATORIA TAPIZ TELA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- TELEVISOR LCD | Units: 4 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'TELEVISOR LCD'
  AND ISNULL(Brand, N'') = ISNULL(N'SONY', N'')
  AND ISNULL(Model, N'') = ISNULL(N'KDL-40BX455', N'');

-- TELEVISOR LED | Units: 2 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'TELEVISOR LED'
  AND ISNULL(Brand, N'') = ISNULL(N'HAIER', N'')
  AND ISNULL(Model, N'') = ISNULL(N'LE55B8500DUA', N'');

-- TELEVISOR LED | Units: 3 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'TELEVISOR LED'
  AND ISNULL(Brand, N'') = ISNULL(N'LG', N'')
  AND ISNULL(Model, N'') = ISNULL(N'50LN5400', N'');

-- TELEVISOR LED | Units: 1 | Rule: Equipo: audiovisual por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 14, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'TELEVISOR LED'
  AND ISNULL(Brand, N'') = ISNULL(N'SAMSUNG', N'')
  AND ISNULL(Model, N'') = ISNULL(N'UN48J5000AGXZS', N'');

-- TERMO TANQUE | Units: 1 | Rule: Equipo: calor por nombre/descripcion.
UPDATE Equipments
SET Category = 0, UtensilType = 0, TypeClassification = 8, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'TERMO TANQUE'
  AND ISNULL(Brand, N'') = ISNULL(N'A6', N'')
  AND ISNULL(Model, N'') = ISNULL(N'27284', N'');

-- THERMOMIX | Units: 1 | Rule: Utensilio: vajilla/servicio por nombre/descripcion.
UPDATE Equipments
SET Category = 1, UtensilType = 10, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'THERMOMIX'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(N'TM6', N'');

-- VITRINA DE PARED | Units: 6 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'VITRINA DE PARED'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- VITRINA METALICA | Units: 1 | Rule: Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.
UPDATE Equipments
SET Category = 2, UtensilType = 0, TypeClassification = 7, LastModifiedDate = SYSUTCDATETIME()
WHERE Name = N'VITRINA METALICA'
  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')
  AND ISNULL(Model, N'') = ISNULL(NULL, N'');

-- Suggested equipment catalog updates: 83
COMMIT TRANSACTION;
