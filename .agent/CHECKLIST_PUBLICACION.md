# ✅ CHECKLIST DE PUBLICACIÓN - LABORATORIOS UNIVALLE

**Fecha de inicio:** _____________  
**Responsable:** _____________

---

## 🎯 ANTES DE COMENZAR

- [ ] Tienes acceso a FileZilla instalado
- [ ] Tienes acceso a SQL Server Management Studio (SSMS) o Azure Data Studio
- [ ] Tienes las credenciales de MonsterASP (FTP y Base de Datos)
- [ ] Has leído la guía completa en `.agent/GUIA_PUBLICACION.md`

---

## 📝 FASE 1: PREPARACIÓN DEL CÓDIGO

### Configuración Básica
- [ ] **Actualizar appsettings.json** ✅ (YA HECHO)
  - Cadena de conexión apunta a: `db40037.public.databaseasp.net`
  
### Modificación Manual Requerida
- [ ] **Modificar Program.cs**
  - Lee las instrucciones en: `.agent/MODIFICACION_PROGRAM_CS.md`
  - Agrega la línea: `builder.Services.AddScoped<DatabaseErrorHandler>();`
  - Ubicación: Después de `builder.Services.AddScoped<IReportService, ReportService>();`

### Verificación del Código
- [ ] **Compilar el proyecto** (`CTRL+SHIFT+B`)
  - ¿Compiló sin errores? → Continúa
  - ¿Hay errores? → Revisa el paso de Program.cs

### Prueba de Conexión (OPCIONAL pero RECOMENDADO)
- [ ] **Ejecutar el proyecto localmente** (`F5`)
- [ ] **Navegar a:** `/Admin/TestConnection`
- [ ] **Probar conexión** con la base de datos remota
  - ✅ Conexión exitosa → Continúa a Fase 2
  - ❌ Conexión fallida → Revisa credenciales y firewall

---

## 🗄️ FASE 2: BASE DE DATOS

### Generar Script SQL
- [ ] **Opción A:** Usar el script automático
  - Ejecuta: `.\generate_sql.ps1` desde PowerShell
  - Verifica que se creó: `deploy_to_production.sql`
  
- [ ] **Opción B:** Usar el script existente
  - Verifica que existe: `migrations_script.sql`

### Conectarse al Servidor Remoto
- [ ] Abrir **SQL Server Management Studio (SSMS)**
- [ ] Conectar con estos datos:
  - **Server:** `db40037.public.databaseasp.net`
  - **Authentication:** SQL Server Authentication
  - **Login:** `db40037`
  - **Password:** `Nf5!%3Yan?2S`
  - **Trust Server Certificate:** ✅ Activado

### Ejecutar el Script
- [ ] Click derecho en base de datos `db40037` → **New Query**
- [ ] Cargar el archivo: `deploy_to_production.sql` (o `migrations_script.sql`)
- [ ] **Ejecutar** el script (`F5`)
- [ ] **Verificar errores:**
  - Si dice "objeto ya existe" → Normal (puedes ignorar)
  - Si dice "permiso denegado" → **PROBLEMA** (contacta soporte MonsterASP)

### Verificar Tablas Creadas
- [ ] Ejecutar esta consulta:
  ```sql
  SELECT TABLE_NAME 
  FROM INFORMATION_SCHEMA.TABLES 
  WHERE TABLE_TYPE = 'BASE TABLE'
  ORDER BY TABLE_NAME;
  ```
- [ ] **Verificar que existen estas tablas:**
  - Users ✓
  - Equipments ✓
  - EquipmentUnits ✓
  - Laboratories ✓
  - Faculties ✓
  - Maintenances ✓
  - Verifications ✓
  - [Otras tablas de tu modelo]

---

## 📦 FASE 3: PUBLICACIÓN

### Publicar con Script Automático (RECOMENDADO)
- [ ] Abrir **PowerShell** en la carpeta del proyecto
- [ ] Ejecutar: `.\publish.ps1`
- [ ] Esperar a que termine
- [ ] **Verificar mensaje:** "✅ PUBLICACIÓN COMPLETADA"
- [ ] Se abrirá automáticamente la carpeta: `bin\Release\net9.0\publish\`

### Publicar Manualmente (ALTERNATIVA)
- [ ] Click derecho en el proyecto → **Publish...**
- [ ] **Target:** Folder
- [ ] **Location:** `bin\Release\net9.0\publish\`
- [ ] **Configuration:** Release
- [ ] Click en **Publish**
- [ ] Esperar mensaje: "Publish succeeded"

---

## 🌐 FASE 4: SUBIR AL SERVIDOR (FTP)

### Configurar FileZilla
- [ ] Abrir **FileZilla**
- [ ] Conectar con estos datos:
  - **Host:** `site53261.siteasp.net`
  - **Username:** `site53261`
  - **Password:** `d%7TFp!4-6cD`
  - **Port:** `21`
- [ ] Click en **Quickconnect**
- [ ] **Verificar conexión exitosa**

### Preparar el Servidor
- [ ] **Panel Derecho:** Navegar a `/wwwroot`
- [ ] **IMPORTANTE:** Seleccionar TODO el contenido de `/wwwroot`
- [ ] Click derecho → **Delete** (Eliminar todo)
- [ ] Confirmar eliminación

### Subir los Archivos
- [ ] **Panel Izquierdo:** Navegar a la carpeta de publicación:
  - `C:\Users\Wilmher\source\repos\Proyecto-Laboratorios-Univalle\bin\Release\net9.0\publish\`
- [ ] **Seleccionar TODO** el contenido (carpetas y archivos)
- [ ] **Arrastrar** al panel derecho (`/wwwroot`)
- [ ] **Esperar** a que termine la transferencia
  - Esto puede tomar **5-15 minutos** dependiendo de tu conexión

### Verificar Archivos Subidos
- [ ] En el panel derecho (`/wwwroot`), verificar que existen:
  - ✓ `Proyecto Laboratorios Univalle.dll`
  - ✓ `appsettings.json`
  - ✓ `web.config`
  - ✓ Carpeta `wwwroot/` (con subcarpetas css, js, lib, etc.)
  - ✓ Carpeta `Pages/`

---

## 🔧 FASE 5: CONFIGURACIÓN POST-DESPLIEGUE

### Primera Prueba
- [ ] Abrir navegador
- [ ] Ir a: `http://site53261.siteasp.net` (o tu URL de MonsterASP)
- [ ] **Esperar 30-60 segundos** (primera carga es lenta)
- [ ] **¿La página carga?**
  - ✅ SÍ → ¡Felicitaciones! Continúa con "Configuración Inicial"
  - ❌ NO → Continúa con "Solución de Problemas"

### Si hay Error 500 (Solución de Problemas)
- [ ] En FileZilla, navegar a `/wwwroot/web.config`
- [ ] Click derecho → **View/Edit**
- [ ] Buscar la sección `<aspNetCore ...>`
- [ ] Modificar para activar logs:
  ```xml
  <aspNetCore processPath="dotnet" 
              arguments=".\Proyecto Laboratorios Univalle.dll" 
              stdoutLogEnabled="true" 
              stdoutLogFile=".\logs\stdout" 
              hostingModel="inprocess">
  ```
- [ ] Guardar y subir de nuevo el archivo
- [ ] Crear carpeta `/wwwroot/logs/` si no existe
- [ ] Refrescar la página web
- [ ] Descargar archivo de log más reciente de `/wwwroot/logs/`
- [ ] Leer el log para identificar el error

---

## 🎉 FASE 6: CONFIGURACIÓN INICIAL

### Crear Usuario Administrador
- [ ] Navegar a: `http://[tu-sitio]/Register`
- [ ] Crear el primer usuario administrador:
  - Usuario: _____________
  - Contraseña: _____________
  - Rol: Administrador

### Deshabilitar Logs Detallados (Importante para Producción)
- [ ] Editar `/wwwroot/web.config`
- [ ] Cambiar `stdoutLogEnabled="true"` a `stdoutLogEnabled="false"`
- [ ] Guardar y subir

### Configurar HTTPS (Opcional pero Recomendado)
- [ ] Contactar soporte de MonsterASP para obtener certificado SSL
- [ ] Configurar redirección automática HTTP → HTTPS

---

## 📊 VERIFICACIÓN FINAL

- [ ] La página principal carga correctamente
- [ ] Se puede iniciar sesión
- [ ] Se pueden ver las listas (Equipos, Laboratorios, etc.)
- [ ] Se puede crear un nuevo registro (prueba con una Facultad o Ciudad)
- [ ] Los reportes funcionan (probar generar un PDF)
- [ ] No hay errores visibles en la consola del navegador (`F12`)

---

## 📝 NOTAS Y OBSERVACIONES

```
[Espacio para tus notas durante el proceso]







```

---

## 🚨 EN CASO DE PROBLEMAS

### Problema: No se puede conectar a la base de datos
**Solución:**
1. Verifica las credenciales en `appsettings.json`
2. Confirma que el firewall de MonsterASP permite tu IP
3. Contacta soporte de MonsterASP

### Problema: Error 500 después de subir
**Solución:**
1. Activa logs detallados (Fase 5)
2. Lee el archivo de log
3. Corrige el error y vuelve a publicar

### Problema: Archivos CSS/JS no cargan
**Solución:**
1. Verifica que la carpeta `wwwroot` completa se subió
2. Revisa permisos de la carpeta en el servidor
3. Fuerza refresco del navegador (`CTRL+F5`)

---

**✅ PUBLICACIÓN COMPLETADA:** ____ / ____ / ____  
**HORA:** ______

**Firma:** ___________________
