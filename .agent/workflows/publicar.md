---
description: Guía completa para publicar el proyecto en MonsterASP
---

# Workflow: Publicar Proyecto

Este workflow te guía paso a paso para publicar el proyecto en el servidor de producción MonsterASP.

## 📚 Documentos de Referencia

Antes de comenzar, revisa estos documentos:

1. **Guía Completa:** `.agent/GUIA_PUBLICACION.md`
2. **Checklist Interactivo:** `.agent/CHECKLIST_PUBLICACION.md`
3. **Modificación de Program.cs:** `.agent/MODIFICACION_PROGRAM_CS.md`

## 🚀 Pasos Rápidos

### 1. Modificar Program.cs (OBLIGATORIO - Primera vez)

Lee las instrucciones en `.agent/MODIFICACION_PROGRAM_CS.md` y agrega esta línea:

```csharp
builder.Services.AddScoped<DatabaseErrorHandler>();
```

Ubicación: Después de la línea `builder.Services.AddScoped<IReportService, ReportService>();`

### 2. Generar Script SQL

// turbo
```powershell
.\generate_sql.ps1
```

Esto generará el archivo `deploy_to_production.sql` que debes ejecutar en SSMS.

### 3. Conectar a SQL Server Remoto

Abre **SQL Server Management Studio (SSMS)** y conéctate con:
- **Server:** `db40037.public.databaseasp.net`
- **Login:** `db40037`
- **Password:** `Nf5!%3Yan?2S`

### 4. Ejecutar Script SQL

1. Click derecho en base de datos `db40037` → New Query
2. Cargar y ejecutar el archivo `deploy_to_production.sql`
3. Verificar que las tablas se crearon correctamente

### 5. Publicar el Proyecto

// turbo
```powershell
.\publish.ps1
```

Esto compilará y publicará el proyecto en `bin\Release\net9.0\publish\`

### 6. Subir vía FTP con FileZilla

**Configuración de conexión:**
- **Host:** `site53261.siteasp.net`
- **Usuario:** `site53261`
- **Contraseña:** `d%7TFp!4-6cD`

**Pasos:**
1. Conectar a FileZilla
2. Navegar a `/wwwroot` en el panel derecho
3. **ELIMINAR** todo el contenido de `/wwwroot`
4. Navegar a `bin\Release\net9.0\publish\` en el panel izquierdo
5. Seleccionar TODO y arrastrarlo a `/wwwroot`
6. Esperar a que termine la transferencia

### 7. Verificar el Despliegue

1. Navegar a `http://site53261.siteasp.net`
2. Esperar 30-60 segundos (primera carga)
3. Verificar que la aplicación carga correctamente

## 🧪 Prueba de Conexión (Opcional pero Recomendado)

Antes de publicar, puedes probar la conexión a la base de datos remota:

1. Ejecuta el proyecto localmente (`F5`)
2. Navega a `/Admin/TestConnection`
3. Click en "Probar Conexión"

Si la conexión es exitosa, procede con la publicación.

## 📋 Seguimiento

Usa el checklist interactivo en `.agent/CHECKLIST_PUBLICACION.md` para marcar cada paso completado.

## 🚨 Solución de Problemas

Si encuentras problemas, consulta la sección "SOLUCIÓN DE PROBLEMAS COMUNES" en `.agent/GUIA_PUBLICACION.md`

## 📞 Soporte

Para problemas que no puedas resolver:
1. Revisa los logs en `/wwwroot/logs/stdout_xxx.log` (en el servidor)
2. Revisa los logs locales en `logs/connection_errors.txt`
3. Contacta al soporte de MonsterASP con los logs
