# Actualizar la base de la nube para la ronda de QA (2026-10-19)

Rama de QA: **`qa`** (origin/qa). Documento sin secretos: la contraseña de la base está solo en el archivo privado
`C:\Users\Public\BackupsUnivalle\INFORME_CONEXION_BD.md`, que no se sube a GitHub.

## 1. Qué hay que subir y por qué

| | Nube hoy (`db70630`, consultado el 2026-10-19) | Base REAL local (lo que debe quedar) |
|---|---|---|
| Migraciones | 29 (última `20261010022947_AddStudentGroups`) | 32 (última `20261018152946_CorrectiveCoexistenceAndL7Checklist`) |
| Datos | Carga del 2026-10-10 (anterior a la base REAL) | Igual a `BaseGastronomia_v1.xlsx` (0 diferencias), con las gestiones de QA |
| Gestiones activas | — | `2026-2` Preventiva (Id 4) y `CORR-2026-2` Correctiva (Id 28) |
| Usuarios | — | Solo `nihaht` (SuperAdmin) |

Como cambian **los datos y el esquema**, la vía recomendada es **restaurar el `.bak`** (opción A). El script de migraciones
(opción B) solo pone el esquema al día y deja los datos viejos de la nube.

Paquete generado (fuera del repositorio), en `C:\Users\Public\BackupsUnivalle\QA_NUBE_20261019\`:

| Archivo | Qué es |
|---|---|
| `DB_Laboratorios_Univalle_QA_NUBE_20261019.bak` | Respaldo `COPY_ONLY`, `CHECKSUM` y comprimido de la base REAL (6 MB). Pasó `RESTORE VERIFYONLY`. |
| `DB_Laboratorios_Univalle_QA_NUBE_20261019.bak.sha256` | Hash SHA-256 para comprobar que el archivo subido no se corrompió. |
| `migraciones_AddStudentGroups_a_CorrectiveCoexistence_idempotente.sql` | Solo para la opción B: las 3 migraciones que le faltan a la nube, en un script idempotente. |

Respaldo previo a los cambios de gestión del 2026-10-19: `C:\Users\Public\BackupsUnivalle\REAL_PRE_GESTIONES_QA_20261019.bak`.

## 2. Opción A (recomendada): restaurar el `.bak` en MonsterASP

1. En `https://admin.monsterasp.net`, dentro de la base `db70630`, hacer primero un **backup de seguridad** de la nube y descargarlo.
2. Comprobar el hash local antes de subir:
   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "C:\Users\Public\BackupsUnivalle\QA_NUBE_20261019\DB_Laboratorios_Univalle_QA_NUBE_20261019.bak"
   ```
   Debe coincidir con el valor del archivo `.sha256`.
3. En el panel: **Backups → Upload** con el `.bak`, y después **Restore** sobre `db70630`. El panel resuelve los nombres lógicos de los archivos.
4. Verificar desde el PC (endpoint **Remote access**, con la contraseña tomada del archivo privado):
   ```sql
   SELECT COUNT(*) FROM __EFMigrationsHistory;                               -- 32
   SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
                                                                             -- 20261018152946_CorrectiveCoexistenceAndL7Checklist
   SELECT Id, Code, Type, Status FROM Managements WHERE Status = 0;          -- 4 / 2026-2 / 0 y 28 / CORR-2026-2 / 1
   SELECT COUNT(*) FROM Articles;                                            -- 1374
   ```
5. Si la restauración deja al usuario de la base sin acceso (no debería pasar en MonsterASP), volver a asignarlo desde el panel.

**No** ejecutar `dotnet ef database update` contra la nube después de restaurar: el `.bak` ya trae las 32 migraciones.

## 3. Opción B: solo el esquema (sin datos nuevos)

Ejecutar en `db70630`, después de un backup de seguridad, el script
`migraciones_AddStudentGroups_a_CorrectiveCoexistence_idempotente.sql` (por ejemplo, con `sqlcmd -f 65001` o SSMS).
Es idempotente: cada migración se salta si ya figura en `__EFMigrationsHistory`. Para regenerarlo:

```bash
dotnet ef migrations script 20261010022947_AddStudentGroups --idempotent --project "Proyecto Laboratorios Univalle.csproj" -o migraciones.sql
```

## 4. Probadores

1. Entrar como `nihaht` y crear desde **/Users** una cuenta por persona, con su rol: Administrador, Supervisor o Cierre de práctica.
   No compartir la cuenta SuperAdmin.
2. **Si la app está publicada en MonsterASP:** publicar desde la rama `qa` (`docs/deployment/README.md`) con
   `ConnectionStrings__DefaultConnection` (endpoint **Local access**) configurado en el panel, `ASPNETCORE_ENVIRONMENT=Production`,
   `Database__AutoMigrate=false` y `Database__RunSeed=false`.
3. **Si cada persona corre la app en su PC contra la nube:**
   ```bash
   git clone https://github.com/NihahtMontes/Proyecto-Laboratorios-Univalle
   cd Proyecto-Laboratorios-Univalle
   git checkout qa
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena Remote access>" --project "Proyecto Laboratorios Univalle.csproj"
   dotnet run --project "Proyecto Laboratorios Univalle.csproj"
   ```
   La cadena se entrega por un canal privado, nunca en el repositorio ni en el chat del grupo.
4. Qué probar y cómo: `docs/INFORME_ESTADO_FUNCIONAL_2026-10-18.md`, §5. La planificación académica (§19) es lo que menos se probó.

## 5. Volver atrás

- **Nube:** restaurar el backup de seguridad que se descargó en el paso A.1.
- **Base REAL local:** restaurar `REAL_PRE_GESTIONES_QA_20261019.bak` deshace solo el cambio de gestiones del 2026-10-19.
