# 🚀 GUÍA COMPLETA PARA PUBLICAR EL PROYECTO LABORATORIOS UNIVALLE

**Fecha:** 9 de Febrero de 2026  
**Proyecto:** Sistema de Gestión de Laboratorios - Universidad del Valle  
**Destino:** MonsterASP + SQL Server Remoto

---

## 📋 DATOS DE ACCESO

### Base de Datos (SQL Server)
- **Servidor:** `db40037.public.databaseasp.net`
- **Base de Datos:** `db40037`
- **Usuario:** `db40037`
- **Contraseña:** `Nf5!%3Yan?2S`

### FTP (FileZilla)
- **Host:** `site53261.siteasp.net`
- **Usuario:** `site53261`
- **Contraseña:** `d%7TFp!4-6cD`
- **Carpeta destino:** `/wwwroot`

---

## ✅ FASE 1: PREPARAR EL CÓDIGO

### Paso 1.1: Configuración de Conexión ✓ COMPLETADO

El archivo `appsettings.json` ya ha sido actualizado con la cadena de conexión correcta:

```json
"ConnectionStrings": {
    "DefaultConnection": "Server=db40037.public.databaseasp.net;Database=db40037;User Id=db40037;Password=Nf5!%3Yan?2S;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

### Paso 1.2: Manejo de Excepciones ✓ COMPLETADO

Se ha creado el servicio `DatabaseErrorHandler.cs` en `Services/` para:
- ✅ Registrar errores de conexión en archivos de log
- ✅ Proporcionar mensajes amigables al usuario
- ✅ Diagnosticar problemas de conexión

### Paso 1.3: Registrar el Servicio en Program.cs ⚠️ ACCIÓN REQUERIDA

**Debes agregar manualmente esta línea en `Program.cs`:**

Busca la línea 69 que dice:
```csharp
builder.Services.AddScoped<IReportService, ReportService>();
```

Y agrega DESPUÉS de ella:
```csharp
builder.Services.AddScoped<DatabaseErrorHandler>();
```

Debería quedar así:
```csharp
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<DatabaseErrorHandler>();  // ← NUEVA LÍNEA

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

### Paso 1.4: Crear Página de Diagnóstico de Conexión

Vamos a crear una página especial que te permitirá verificar la conexión a la base de datos antes de publicar.

**Ubicación:** `Pages/Admin/TestConnection.cshtml`

---

## 🗄️ FASE 2: PREPARAR LA BASE DE DATOS

### Paso 2.1: Generar el Script SQL

**Opción A: Usar el script ya generado**
Ya tienes un archivo llamado `migrations_script.sql` en la raíz del proyecto. Este es tu script completo.

**Opción B: Generar uno nuevo desde Visual Studio**

1. Abre **Package Manager Console** (`Tools` > `NuGet Package Manager` > `Package Manager Console`)

2. Ejecuta este comando para generar un script SQL:
   ```powershell
   Script-Migration -From 0 -To [NombreÚltimaMigración] -Output "deploy_script.sql"
   ```

3. Alternativamente, usa el comando más simple:
   ```powershell
   dotnet ef migrations script --output "deploy_to_production.sql"
   ```

### Paso 2.2: Conectar a la Base de Datos Remota

Usa **SQL Server Management Studio (SSMS)** o **Azure Data Studio**:

1. Abre SSMS
2. En la ventana de conexión:
   - **Server name:** `db40037.public.databaseasp.net`
   - **Authentication:** SQL Server Authentication
   - **Login:** `db40037`
   - **Password:** `Nf5!%3Yan?2S`
3. Marca la opción **Trust server certificate** si aparece
4. Click en **Connect**

### Paso 2.3: Ejecutar el Script

1. Una vez conectado, haz click derecho en la base de datos `db40037`
2. Selecciona **New Query**
3. Abre el archivo `migrations_script.sql` (o el que generaste)
4. Copia todo el contenido al query window
5. Click en **Execute** (F5)

⚠️ **IMPORTANTE:** Si hay errores de "objeto ya existe", es normal en segunda ejecución. Ignora esos errores.

### Paso 2.4: Verificar las Tablas

Ejecuta esta consulta para verificar:
```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

Deberías ver tablas como:
- `Users`
- `Equipments`
- `EquipmentUnits`
- `Laboratories`
- `Faculties`
- `Maintenances`
- `Verifications`
- etc.

---

## 📦 FASE 3: PUBLICAR Y SUBIR

### Paso 3.1: Compilar el Proyecto

1. En Visual Studio, haz **click derecho** en el proyecto
2. Selecciona **Build** (o presiona `Ctrl+Shift+B`)
3. Verifica que no haya errores en la ventana de **Output**

### Paso 3.2: Publicar (Publish)

1. Click derecho en el proyecto → **Publish...**
2. En el diálogo que aparece:
   - **Target:** Folder
   - **Location:** `bin\Release\net9.0\publish\` (o cualquier carpeta que prefieras)
   - **Configuration:** Release
3. Click en **Publish**
4. Espera a que termine (aparecerá "Publish succeeded" en la salida)

### Paso 3.3: Configurar FileZilla

1. Abre **FileZilla**
2. Ingresa los datos en la barra superior:
   - **Host:** `site53261.siteasp.net`
   - **Username:** `site53261`
   - **Password:** `d%7TFp!4-6cD`
   - **Port:** `21` (FTP estándar)
3. Click en **Quickconnect**

### Paso 3.4: Subir los Archivos

1. **PANEL IZQUIERDO (Local):**
   - Navega a la carpeta donde publicaste, ejemplo:
     `C:\Users\Wilmher\source\repos\Proyecto-Laboratorios-Univalle\bin\Release\net9.0\publish\`

2. **PANEL DERECHO (Remoto):**
   - Navega a `/wwwroot`
   - ⚠️ **IMPORTANTE:** Borra todo el contenido actual de `/wwwroot`

3. **Subir:**
   - Selecciona **TODOS** los archivos y carpetas del panel izquierdo
   - Arrástralos al panel derecho (carpeta `/wwwroot`)
   - Espera a que termine la transferencia (puede tomar varios minutos)

---

## 🔧 FASE 4: CONFIGURACIÓN POST-DESPLIEGUE

### Paso 4.1: Verificar el web.config

FileZilla debe haber subido un `web.config`. Si no existe, Visual Studio lo genera automáticamente. 

### Paso 4.2: Habilitar Logs Detallados (Solo para depuración)

Si la aplicación no carga correctamente, necesitas activar logs:

1. En FileZilla, navega a `/wwwroot`
2. Encuentra el archivo `web.config`
3. Click derecho → **View/Edit**
4. Busca la sección `<aspNetCore>` y modifícala para que quede así:

```xml
<aspNetCore processPath="dotnet" 
            arguments=".\Proyecto Laboratorios Univalle.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```

5. Guarda el archivo
6. Crea una carpeta llamada `logs` dentro de `/wwwroot` si no existe
7. Los errores se guardarán en `/wwwroot/logs/stdout_xxx.log`

### Paso 4.3: Primera Ejecución

1. Abre tu navegador
2. Visita: `http://site53261.siteasp.net` o la URL que te proporcionó MonsterASP
3. **La primera carga puede tardar 30-60 segundos** (la aplicación se está inicializando)

---

## 🚨 SOLUCIÓN DE PROBLEMAS COMUNES

### Error 500 - Internal Server Error

**Causa:** Error de configuración o problema con la base de datos

**Solución:**
1. Habilita los logs detallados (Paso 4.2)
2. Descarga el archivo de log más reciente de `/wwwroot/logs/`
3. Revisa el contenido del log para identificar el error específico

### Error de Conexión a Base de Datos

**Síntomas:**
- La aplicación carga pero muestra error al acceder a datos
- Timeout al conectar

**Solución:**
1. Verifica que el firewall de MonsterASP permita conexiones desde tu servidor web
2. Confirma las credenciales en `appsettings.json`
3. Prueba la conexión desde SSMS usando la dirección IP del servidor web de MonsterASP

### Las migraciones no se aplicaron

**Síntoma:**
- Error: "Invalid object name 'Users'" o similar

**Solución:**
1. Las migraciones **NO** se ejecutan automáticamente en producción
2. Debes ejecutar el script SQL manualmente (Fase 2)
3. O ejecutar este comando desde Package Manager Console apuntando a producción:
   ```powershell
   Update-Database -Connection "Server=db40037.public.databaseasp.net;Database=db40037;User Id=db40037;Password=Nf5!%3Yan?2S;TrustServerCertificate=True"
   ```

### Archivos Estáticos no Cargan (CSS, JS, imágenes)

**Solución:**
1. Verifica que la carpeta `wwwroot` completa se haya subido
2. Especialmente las carpetas:
   - `wwwroot/css/`
   - `wwwroot/js/`
   - `wwwroot/lib/`
   - `wwwroot/NiceAdmin/` (si existe)

---

## 📝 CHECKLIST FINAL

Antes de dar por terminado, verifica:

- [ ] `appsettings.json` tiene la cadena de conexión correcta
- [ ] El script SQL se ejecutó exitosamente en el servidor remoto
- [ ] Todas las tablas existen en la base de datos remota
- [ ] El proyecto se compiló sin errores
- [ ] La publicación (Publish) se completó exitosamente
- [ ] Todos los archivos se subieron a `/wwwroot` vía FTP
- [ ] El archivo `web.config` existe en el servidor
- [ ] La carpeta `wwwroot` completa (con CSS, JS, etc.) se subió
- [ ] La aplicación carga en el navegador (aunque sea con error)
- [ ] Los logs están habilitados para diagnóstico

---

## 🎯 PRÓXIMOS PASOS (DESPUÉS DE PUBLICAR)

1. **Crear el usuario administrador inicial:**
   - Navega a `/Register` en tu aplicación publicada
   - Crea el primer usuario administrador

2. **Deshabilitar logs detallados:**
   - Cambia `stdoutLogEnabled="false"` en `web.config`
   - Esto mejora el rendimiento y la seguridad

3. **Configurar HTTPS:**
   - MonsterASP debe proporcionarte un certificado SSL
   - Asegúrate de que tu dominio funcione con `https://`

4. **Backup de Base de Datos:**
   - Configura backups automáticos en el panel de MonsterASP
   - O crea un script para backups programados

---

## 📞 CONTACTO DE SOPORTE

Si encuentras problemas que no puedas resolver:

1. **Logs del servidor:** Descarga y revisa `/wwwroot/logs/stdout_xxx.log`
2. **Logs de aplicación:** Revisa el archivo `logs/connection_errors.txt` que genera `DatabaseErrorHandler`
3. **Soporte de MonsterASP:** Contacta su soporte técnico con los logs

---

**¡Buena suerte con el despliegue! 🚀**
