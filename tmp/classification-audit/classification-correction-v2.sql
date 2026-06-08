-- ============================================================

-- CLASSIFICATION CORRECTION V2 - APPLY

-- Source: Excel-derived seed/audit artifacts. Generated offline.

-- Updates only Equipments.Category, Equipments.UtensilType and Equipments.TypeClassification.

-- ============================================================

SET XACT_ABORT ON;

BEGIN TRANSACTION;



DECLARE @Updated TABLE (EquipmentKey nvarchar(600), Name nvarchar(250), OldCategory int, NewCategory int, OldUtensilType int, NewUtensilType int, OldTypeClassification int, NewTypeClassification int);



-- ABATIDOR FASTER | AFINOX | FASTER 5T GF 230V

-- Regla: Equipo: frio/refrigeracion por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 9,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ABATIDOR FASTER|AFINOX|FASTER 5T GF 230V', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ABATIDOR FASTER'

  AND ISNULL(Brand, N'') = ISNULL(N'AFINOX', N'')

  AND ISNULL(Model, N'') = ISNULL(N'FASTER 5T GF 230V', N'');



-- ACCES POINT | CISCO | AIR-CAP370

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ACCES POINT|CISCO|AIR-CAP370', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ACCES POINT'

  AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'AIR-CAP370', N'');



-- ACCES POINT | CISCO | AIR-CAP3702E-A-K9

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ACCES POINT|CISCO|AIR-CAP3702E-A-K9', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ACCES POINT'

  AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'AIR-CAP3702E-A-K9', N'');



-- ALL IN ONE | HP | 24-E015LA

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ALL IN ONE|HP|24-E015LA', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ALL IN ONE'

  AND ISNULL(Brand, N'') = ISNULL(N'HP', N'')

  AND ISNULL(Model, N'') = ISNULL(N'24-E015LA', N'');



-- ALL IN ONE | LENOVO | 10BB-A0C900

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ALL IN ONE|LENOVO|10BB-A0C900', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ALL IN ONE'

  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'10BB-A0C900', N'');



-- ALL IN ONE | LENOVO | THINK CENTRE M73Z

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ALL IN ONE|LENOVO|THINK CENTRE M73Z', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ALL IN ONE'

  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'THINK CENTRE M73Z', N'');



-- AMASADORA | WILDA

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'AMASADORA|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'AMASADORA'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- APARATO TELEFONICO | CISCO | CP-3905

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'APARATO TELEFONICO|CISCO|CP-3905', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'APARATO TELEFONICO'

  AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'CP-3905', N'');



-- BACHA DE LAVADO

-- Regla: Decision usuario: caso de revision clasificado conservadoramente como Otro.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BACHA DE LAVADO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BACHA DE LAVADO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- BALANZA ELECTRONICA

-- Regla: Equipo: instrumental de medicion por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 3,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BALANZA ELECTRONICA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BALANZA ELECTRONICA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- BALANZA ELECTRONICA | ELECTRONIC SCALE

-- Regla: Equipo: instrumental de medicion por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 3,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BALANZA ELECTRONICA|ELECTRONIC SCALE|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BALANZA ELECTRONICA'

  AND ISNULL(Brand, N'') = ISNULL(N'ELECTRONIC SCALE', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- BALANZA ELECTRONICA | OHAUS | RANGER R31P30

-- Regla: Equipo: instrumental de medicion por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 3,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BALANZA ELECTRONICA'

  AND ISNULL(Brand, N'') = ISNULL(N'OHAUS', N'')

  AND ISNULL(Model, N'') = ISNULL(N'RANGER R31P30', N'');



-- BANCA DE MADERA

-- Regla: Otro: mobiliario/infraestructura sin clasificacion de equipo tecnico.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BANCA DE MADERA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BANCA DE MADERA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- BATIDORA | KITCHENAID | 5KSM7591

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BATIDORA|KITCHENAID|5KSM7591', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BATIDORA'

  AND ISNULL(Brand, N'') = ISNULL(N'KITCHENAID', N'')

  AND ISNULL(Model, N'') = ISNULL(N'5KSM7591', N'');



-- BATIDORA | KITCHENAID | 5KSM7591,

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BATIDORA|KITCHENAID|5KSM7591,', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BATIDORA'

  AND ISNULL(Brand, N'') = ISNULL(N'KITCHENAID', N'')

  AND ISNULL(Model, N'') = ISNULL(N'5KSM7591,', N'');



-- BATIDORA | OSTER

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BATIDORA|OSTER|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BATIDORA'

  AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- BEBEDERO DE AGUA | IBBL | BAG 40

-- Regla: Equipo: frio/refrigeracion por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 9,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'BEBEDERO DE AGUA|IBBL|BAG 40', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'BEBEDERO DE AGUA'

  AND ISNULL(Brand, N'') = ISNULL(N'IBBL', N'')

  AND ISNULL(Model, N'') = ISNULL(N'BAG 40', N'');



-- CAFETERA IND | ASTORIA | INDUS. ITALIANA

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CAFETERA IND|ASTORIA|INDUS. ITALIANA', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CAFETERA IND'

  AND ISNULL(Brand, N'') = ISNULL(N'ASTORIA', N'')

  AND ISNULL(Model, N'') = ISNULL(N'INDUS. ITALIANA', N'');



-- CAMARA CONSERVADORA | ASBER | ARR-43

-- Regla: Correccion critica: camara conservadora es equipo de frio, no audiovisual.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 9,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CAMARA CONSERVADORA|ASBER|ARR-43', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CAMARA CONSERVADORA'

  AND ISNULL(Brand, N'') = ISNULL(N'ASBER', N'')

  AND ISNULL(Model, N'') = ISNULL(N'ARR-43', N'');



-- CAMARA DE VIDEO

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CAMARA DE VIDEO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CAMARA DE VIDEO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- CAMARA DE VIDEO | HIKVISION | DS-2CEE55A2N-IRN

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CAMARA DE VIDEO'

  AND ISNULL(Brand, N'') = ISNULL(N'HIKVISION', N'')

  AND ISNULL(Model, N'') = ISNULL(N'DS-2CEE55A2N-IRN', N'');



-- CAMARA DE VIDEO | HIKVISION | DS2CC5192N-IR1

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CAMARA DE VIDEO|HIKVISION|DS2CC5192N-IR1', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CAMARA DE VIDEO'

  AND ISNULL(Brand, N'') = ISNULL(N'HIKVISION', N'')

  AND ISNULL(Model, N'') = ISNULL(N'DS2CC5192N-IR1', N'');



-- CAMARA DE VIDEO | SONY BALUMS

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CAMARA DE VIDEO|SONY BALUMS|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CAMARA DE VIDEO'

  AND ISNULL(Brand, N'') = ISNULL(N'SONY BALUMS', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- CAMPANA DE EXTRACCION

-- Regla: Correccion critica: campana/extractor es equipo electrico instalado.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 15,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CAMPANA DE EXTRACCION||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CAMPANA DE EXTRACCION'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- CAMPANA DE EXTRACCION | WILDA

-- Regla: Correccion critica: campana/extractor es equipo electrico instalado.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 15,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CAMPANA DE EXTRACCION|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CAMPANA DE EXTRACCION'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- CARRITO

-- Regla: Otro: mobiliario/infraestructura sin clasificacion de equipo tecnico.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CARRITO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CARRITO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- CASILLERO METALICO

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CASILLERO METALICO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CASILLERO METALICO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- COCINA INDUSTRIAL | WILDA

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'COCINA INDUSTRIAL|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'COCINA INDUSTRIAL'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- COCINA INDUSTRIAL | WILDA | AISI304L 2B

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'COCINA INDUSTRIAL|WILDA|AISI304L 2B', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'COCINA INDUSTRIAL'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(N'AISI304L 2B', N'');



-- CPU DE ESCRITORIO | LENOVO | THINK CENTRE

-- Regla: Correccion critica: activo informatico clasificado como equipo informatico.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CPU DE ESCRITORIO'

  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'THINK CENTRE', N'');



-- CREDENZA DE MELAMINA

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'CREDENZA DE MELAMINA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'CREDENZA DE MELAMINA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- ENVASADORA AL VACIO | VENTUS

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ENVASADORA AL VACIO|VENTUS|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ENVASADORA AL VACIO'

  AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- ESCRITORIO DE MADERA

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ESCRITORIO DE MADERA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ESCRITORIO DE MADERA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- ESCRITORIO DE MELAMINA MODULAR

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ESCRITORIO DE MELAMINA MODULAR||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ESCRITORIO DE MELAMINA MODULAR'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- ESCRITORIO METALICO

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ESCRITORIO METALICO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ESCRITORIO METALICO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- ESTANTE BAR DE MADERA

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ESTANTE BAR DE MADERA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ESTANTE BAR DE MADERA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- ESTANTE METÁLICO

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ESTANTE METALICO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ESTANTE METÁLICO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- ESTANTE PORTA BANDEJAS

-- Regla: Utensilio: panaderia/reposteria/pasteleria por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 7,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ESTANTE PORTA BANDEJAS||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ESTANTE PORTA BANDEJAS'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- EXTINTOR | ABC

-- Regla: Equipo: seguridad industrial por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 13,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTINTOR|ABC|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTINTOR'

  AND ISNULL(Brand, N'') = ISNULL(N'ABC', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- EXTINTOR | CYLINDERS

-- Regla: Equipo: seguridad industrial por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 13,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTINTOR|CYLINDERS|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTINTOR'

  AND ISNULL(Brand, N'') = ISNULL(N'CYLINDERS', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- EXTINTOR | FANACIM

-- Regla: Equipo: seguridad industrial por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 13,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTINTOR|FANACIM|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTINTOR'

  AND ISNULL(Brand, N'') = ISNULL(N'FANACIM', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- EXTINTOR | MMB CILINDERS | FNC 10

-- Regla: Equipo: seguridad industrial por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 13,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTINTOR|MMB CILINDERS|FNC 10', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTINTOR'

  AND ISNULL(Brand, N'') = ISNULL(N'MMB CILINDERS', N'')

  AND ISNULL(Model, N'') = ISNULL(N'FNC 10', N'');



-- EXTINTOR | MMB CYLINDERS

-- Regla: Equipo: seguridad industrial por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 13,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTINTOR|MMB CYLINDERS|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTINTOR'

  AND ISNULL(Brand, N'') = ISNULL(N'MMB CYLINDERS', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- EXTINTOR TIPO K

-- Regla: Equipo: seguridad industrial por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 13,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTINTOR TIPO K||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTINTOR TIPO K'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- EXTRACTOR DE AIRE | LOREN SID

-- Regla: Decision usuario: caso de revision clasificado conservadoramente como Otro.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTRACTOR DE AIRE|LOREN SID|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTRACTOR DE AIRE'

  AND ISNULL(Brand, N'') = ISNULL(N'LOREN SID', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- EXTRACTOR DE HUMOS Y GRASAS

-- Regla: Correccion critica: campana/extractor es equipo electrico instalado.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 15,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTRACTOR DE HUMOS Y GRASAS||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTRACTOR DE HUMOS Y GRASAS'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- EXTRACTOR DE HUMOS Y GRASAS | WILDA

-- Regla: Correccion critica: campana/extractor es equipo electrico instalado.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 15,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'EXTRACTOR DE HUMOS Y GRASAS'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- FERMENTADOR | WILDA | A155 304L2B

-- Regla: Correccion critica: fermentador es equipo termico/de proceso.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'FERMENTADOR|WILDA|A155 304L2B', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'FERMENTADOR'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(N'A155 304L2B', N'');



-- FLITRO ABLANDADOR DE AGUA

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'FLITRO ABLANDADOR DE AGUA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'FLITRO ABLANDADOR DE AGUA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- FREIDORA | WILDA

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'FREIDORA|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'FREIDORA'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- GARRAFA

-- Regla: Decision usuario: caso de revision clasificado conservadoramente como Otro.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'GARRAFA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'GARRAFA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- GARRAFA | FANACIM

-- Regla: Decision usuario: caso de revision clasificado conservadoramente como Otro.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'GARRAFA|FANACIM|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'GARRAFA'

  AND ISNULL(Brand, N'') = ISNULL(N'FANACIM', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- GAVETERO DE MELAMINA

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'GAVETERO DE MELAMINA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'GAVETERO DE MELAMINA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- HORNO | WILDA

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'HORNO|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'HORNO'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- HORNO CONVECTOR | ARIANNA | XEFT-04HS-ELDV

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'HORNO CONVECTOR|ARIANNA|XEFT-04HS-ELDV', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'HORNO CONVECTOR'

  AND ISNULL(Brand, N'') = ISNULL(N'ARIANNA', N'')

  AND ISNULL(Model, N'') = ISNULL(N'XEFT-04HS-ELDV', N'');



-- LAVAPLATOS DE ACERO INOXIDABLE

-- Regla: Decision usuario: caso de revision clasificado conservadoramente como Otro.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'LAVAPLATOS DE ACERO INOXIDABLE||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'LAVAPLATOS DE ACERO INOXIDABLE'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- LAVAPLATOS DE ACERO INOXIDABLE | WILDA

-- Regla: Decision usuario: caso de revision clasificado conservadoramente como Otro.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'LAVAPLATOS DE ACERO INOXIDABLE'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- LICUADORA | OSTER

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'LICUADORA|OSTER|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'LICUADORA'

  AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- LICUADORA | OSTER | XPERT

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'LICUADORA|OSTER|XPERT', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'LICUADORA'

  AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'')

  AND ISNULL(Model, N'') = ISNULL(N'XPERT', N'');



-- MESA DE MADERA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESA DE MADERA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESA DE MADERA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MESA METALICA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESA METALICA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESA METALICA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MESA METALICA | WILDA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESA METALICA|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESA METALICA'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MESON DE METAL

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESON DE METAL||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESON DE METAL'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MESON DE METAL | WILDA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESON DE METAL|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESON DE METAL'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MESON REFRIGERADOR | VENTUS | VMR2PS-280E

-- Regla: Correccion tecnica: meson refrigerador es equipo de frio, no mobiliario.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 9,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESON REFRIGERADOR|VENTUS|VMR2PS-280E', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESON REFRIGERADOR'

  AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'')

  AND ISNULL(Model, N'') = ISNULL(N'VMR2PS-280E', N'');



-- MESON ROBUSTO

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESON ROBUSTO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESON ROBUSTO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MESON ROBUSTO | WILDA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESON ROBUSTO|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESON ROBUSTO'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MESON ROBUSTO | WILDA | A155 304L2B

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MESON ROBUSTO|WILDA|A155 304L2B', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MESON ROBUSTO'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(N'A155 304L2B', N'');



-- MICROONDA | LG | MH8236GIR

-- Regla: Correccion critica: microonda es equipo de calor.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MICROONDA|LG|MH8236GIR', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MICROONDA'

  AND ISNULL(Brand, N'') = ISNULL(N'LG', N'')

  AND ISNULL(Model, N'') = ISNULL(N'MH8236GIR', N'');



-- MICROONDA | PANASONIC | NN-ST34HM

-- Regla: Correccion critica: microonda es equipo de calor.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MICROONDA|PANASONIC|NN-ST34HM', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MICROONDA'

  AND ISNULL(Brand, N'') = ISNULL(N'PANASONIC', N'')

  AND ISNULL(Model, N'') = ISNULL(N'NN-ST34HM', N'');



-- MICROONDA | SAMSUNG | MG402MADXBB

-- Regla: Correccion critica: microonda es equipo de calor.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MICROONDA|SAMSUNG|MG402MADXBB', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MICROONDA'

  AND ISNULL(Brand, N'') = ISNULL(N'SAMSUNG', N'')

  AND ISNULL(Model, N'') = ISNULL(N'MG402MADXBB', N'');



-- MOLINO DE COFFIE | FIORENZATO | F64 E

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MOLINO DE COFFIE|FIORENZATO|F64 E', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MOLINO DE COFFIE'

  AND ISNULL(Brand, N'') = ISNULL(N'FIORENZATO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'F64 E', N'');



-- MONITOR LCD | LENOVO | 2580AB1

-- Regla: Correccion critica: activo informatico clasificado como equipo informatico.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MONITOR LCD|LENOVO|2580AB1', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MONITOR LCD'

  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'2580AB1', N'');



-- MUEBLE DE MADERA PARA COMPUTADORA

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MUEBLE DE MADERA PARA COMPUTADORA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MUEBLE DE MADERA PARA COMPUTADORA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MUEBLE DE MELAMINA PARA COMPUTADORA

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MUEBLE DE MELAMINA PARA COMPUTADORA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MUEBLE DE MELAMINA PARA COMPUTADORA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- MUEBLE METALICO PARA COMPUTADORA

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'MUEBLE METALICO PARA COMPUTADORA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'MUEBLE METALICO PARA COMPUTADORA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- PINZA KELLY

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'PINZA KELLY||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'PINZA KELLY'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- PIZARRA CON MARCO METALICO ACRILICO

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'PIZARRA CON MARCO METALICO ACRILICO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'PIZARRA CON MARCO METALICO ACRILICO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- PORTA BANDEJAS

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'PORTA BANDEJAS||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'PORTA BANDEJAS'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- PORTA TABLAS

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'PORTA TABLAS||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'PORTA TABLAS'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- PORTA UTENSILIOS

-- Regla: Correccion critica: porta utensilios es utensilio/menaje.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'PORTA UTENSILIOS||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'PORTA UTENSILIOS'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- PROYECTOR LED/LASER | CASIO | (YW-40) XJ-F20XN

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'PROYECTOR LED/LASER|CASIO|(YW-40) XJ-F20XN', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'PROYECTOR LED/LASER'

  AND ISNULL(Brand, N'') = ISNULL(N'CASIO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'(YW-40) XJ-F20XN', N'');



-- PROYECTOR LED/LASER | CASIO | XJ-F20XN

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'PROYECTOR LED/LASER|CASIO|XJ-F20XN', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'PROYECTOR LED/LASER'

  AND ISNULL(Brand, N'') = ISNULL(N'CASIO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'XJ-F20XN', N'');



-- REFRIGERADOR | VENTUS | VREF-1000BEN

-- Regla: Equipo: frio/refrigeracion por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 9,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'REFRIGERADOR|VENTUS|VREF-1000BEN', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'REFRIGERADOR'

  AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'')

  AND ISNULL(Model, N'') = ISNULL(N'VREF-1000BEN', N'');



-- REFRIGERADOR | VENTUS SORP | VREF-1000BEN

-- Regla: Equipo: frio/refrigeracion por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 9,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'REFRIGERADOR|VENTUS SORP|VREF-1000BEN', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'REFRIGERADOR'

  AND ISNULL(Brand, N'') = ISNULL(N'VENTUS SORP', N'')

  AND ISNULL(Model, N'') = ISNULL(N'VREF-1000BEN', N'');



-- REPISA DE ACERO INOXIDABLE

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'REPISA DE ACERO INOXIDABLE||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'REPISA DE ACERO INOXIDABLE'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- REPISA DE ACERO INOXIDABLE | WILDA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'REPISA DE ACERO INOXIDABLE|WILDA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'REPISA DE ACERO INOXIDABLE'

  AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- ROUTER | VIEW SONIC | WPG-370

-- Regla: Equipo: informatico o telecomunicaciones por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'ROUTER|VIEW SONIC|WPG-370', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'ROUTER'

  AND ISNULL(Brand, N'') = ISNULL(N'VIEW SONIC', N'')

  AND ISNULL(Model, N'') = ISNULL(N'WPG-370', N'');



-- SILLA DE MADERA FIJA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'SILLA DE MADERA FIJA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'SILLA DE MADERA FIJA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- SILLA DE MADERA FIJA TAPIZ TELA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'SILLA DE MADERA FIJA TAPIZ TELA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'SILLA DE MADERA FIJA TAPIZ TELA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- SILLA METALICA FIJA TAPIZ CUERINA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'SILLA METALICA FIJA TAPIZ CUERINA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'SILLA METALICA FIJA TAPIZ CUERINA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- SILLA METALICA FIJA TAPIZ TELA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'SILLA METALICA FIJA TAPIZ TELA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'SILLA METALICA FIJA TAPIZ TELA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- SILLA METALICA GIRATORIA TAPIZ TELA

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'SILLA METALICA GIRATORIA TAPIZ TELA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'SILLA METALICA GIRATORIA TAPIZ TELA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- SOUS VIDE | METVISA

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'SOUS VIDE|METVISA|', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'SOUS VIDE'

  AND ISNULL(Brand, N'') = ISNULL(N'METVISA', N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- TABURETE METALICO

-- Regla: Correccion critica: mobiliario/infraestructura clasificado como Otro/Mobiliario.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TABURETE METALICO||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TABURETE METALICO'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- TECLADO | DELUX | K8060

-- Regla: Correccion critica: activo informatico clasificado como equipo informatico.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TECLADO|DELUX|K8060', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TECLADO'

  AND ISNULL(Brand, N'') = ISNULL(N'DELUX', N'')

  AND ISNULL(Model, N'') = ISNULL(N'K8060', N'');



-- TECLADO | HP | PR1101U

-- Regla: Correccion critica: activo informatico clasificado como equipo informatico.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TECLADO|HP|PR1101U', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TECLADO'

  AND ISNULL(Brand, N'') = ISNULL(N'HP', N'')

  AND ISNULL(Model, N'') = ISNULL(N'PR1101U', N'');



-- TECLADO | LENOVO | KU-0225

-- Regla: Correccion critica: activo informatico clasificado como equipo informatico.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TECLADO|LENOVO|KU-0225', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TECLADO'

  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'KU-0225', N'');



-- TECLADO | LENOVO | SK - 8825

-- Regla: Correccion critica: activo informatico clasificado como equipo informatico.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 6,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TECLADO|LENOVO|SK - 8825', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TECLADO'

  AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'')

  AND ISNULL(Model, N'') = ISNULL(N'SK - 8825', N'');



-- TELEVISOR LCD | SONY | KDL-40BX455

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TELEVISOR LCD|SONY|KDL-40BX455', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TELEVISOR LCD'

  AND ISNULL(Brand, N'') = ISNULL(N'SONY', N'')

  AND ISNULL(Model, N'') = ISNULL(N'KDL-40BX455', N'');



-- TELEVISOR LED | HAIER | LE55B8500DUA

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TELEVISOR LED|HAIER|LE55B8500DUA', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TELEVISOR LED'

  AND ISNULL(Brand, N'') = ISNULL(N'HAIER', N'')

  AND ISNULL(Model, N'') = ISNULL(N'LE55B8500DUA', N'');



-- TELEVISOR LED | LG | 50LN5400

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TELEVISOR LED|LG|50LN5400', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TELEVISOR LED'

  AND ISNULL(Brand, N'') = ISNULL(N'LG', N'')

  AND ISNULL(Model, N'') = ISNULL(N'50LN5400', N'');



-- TELEVISOR LED | SAMSUNG | UN48J5000AGXZS

-- Regla: Equipo: audiovisual por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 14,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TELEVISOR LED|SAMSUNG|UN48J5000AGXZS', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TELEVISOR LED'

  AND ISNULL(Brand, N'') = ISNULL(N'SAMSUNG', N'')

  AND ISNULL(Model, N'') = ISNULL(N'UN48J5000AGXZS', N'');



-- TERMO TANQUE | A6 | 27284

-- Regla: Equipo: calor por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 8,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'TERMO TANQUE|A6|27284', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'TERMO TANQUE'

  AND ISNULL(Brand, N'') = ISNULL(N'A6', N'')

  AND ISNULL(Model, N'') = ISNULL(N'27284', N'');



-- THERMOMIX | TM6

-- Regla: Equipo: maquina rotativa o motorizada por nombre/descripcion.

UPDATE Equipments

SET Category = 0,

    UtensilType = 0,

    TypeClassification = 12,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'THERMOMIX||TM6', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'THERMOMIX'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(N'TM6', N'');



-- VITRINA DE PARED

-- Regla: Otro: mobiliario/infraestructura sin clasificacion de equipo tecnico.

UPDATE Equipments

SET Category = 2,

    UtensilType = 0,

    TypeClassification = 2,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'VITRINA DE PARED||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'VITRINA DE PARED'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



-- VITRINA METALICA

-- Regla: Utensilio: menaje o pieza removible de cocina por nombre/descripcion.

UPDATE Equipments

SET Category = 1,

    UtensilType = 5,

    TypeClassification = 7,

    LastModifiedDate = SYSUTCDATETIME()

OUTPUT N'VITRINA METALICA||', inserted.Name, deleted.Category, inserted.Category, deleted.UtensilType, inserted.UtensilType, deleted.TypeClassification, inserted.TypeClassification

INTO @Updated (EquipmentKey, Name, OldCategory, NewCategory, OldUtensilType, NewUtensilType, OldTypeClassification, NewTypeClassification)

WHERE Name = N'VITRINA METALICA'

  AND ISNULL(Brand, N'') = ISNULL(NULL, N'')

  AND ISNULL(Model, N'') = ISNULL(NULL, N'');



SELECT COUNT(*) AS UpdatedRows FROM @Updated;

SELECT * FROM @Updated ORDER BY Name, EquipmentKey;



SELECT 'EquipmentWithUtensilType' AS CheckName, COUNT(*) AS InvalidCount FROM Equipments WHERE Category = 0 AND UtensilType <> 0

UNION ALL

SELECT 'UtensilWithoutSpecificType', COUNT(*) FROM Equipments WHERE Category = 1 AND UtensilType = 0

UNION ALL

SELECT 'OtherWithUtensilType', COUNT(*) FROM Equipments WHERE Category = 2 AND UtensilType <> 0;





-- Seguridad: no confirma cambios por defecto.

ROLLBACK TRANSACTION;

-- COMMIT TRANSACTION;

