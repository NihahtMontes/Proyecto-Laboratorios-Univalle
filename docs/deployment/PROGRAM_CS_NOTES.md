# 🔧 MODIFICACIÓN MANUAL REQUERIDA EN PROGRAM.CS

## IMPORTANTE
Este cambio es **OBLIGATORIO** para que el sistema de diagnóstico de errores funcione correctamente.

---

## 📍 UBICACIÓN
Archivo: `Program.cs`  
Línea: ~69 (después de la línea que dice `builder.Services.AddScoped<IReportService, ReportService>();`)

---

## ✏️ QUÉ MODIFICAR

### BUSCA esta sección (alrededor de la línea 69):

```csharp
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IVerificationReportService, VerificationReportService>();
builder.Services.AddScoped<IReportService, ReportService>();

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

### AGREGA esta línea después de `AddScoped<IReportService, ReportService>();`:

```csharp
builder.Services.AddScoped<DatabaseErrorHandler>();
```

### RESULTADO FINAL:

```csharp
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IVerificationReportService, VerificationReportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<DatabaseErrorHandler>();  // ← NUEVA LÍNEA

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

---

## ✅ CÓMO HACERLO

### Opción 1: Visual Studio

1. Abre `Program.cs`
2. Usa `CTRL+G` y ve a la línea 69
3. Coloca el cursor al final de la línea que dice `builder.Services.AddScoped<IReportService, ReportService>();`
4. Presiona `ENTER` para crear una nueva línea
5. Escribe: `builder.Services.AddScoped<DatabaseErrorHandler>();`
6. Guarda el archivo (`CTRL+S`)

### Opción 2: Desde el Editor de Código

1. Localiza la sección de servicios en `Program.cs`
2. Encuentra donde están todos los `AddScoped`
3. Agrega la nueva línea justo después de `IReportService`

---

## 🧪 VERIFICAR QUE FUNCIONÓ

Después de hacer el cambio:

1. **Compilar:** Presiona `CTRL+SHIFT+B`
2. Si no hay errores de compilación, ¡el cambio está correcto! ✅
3. Si hay errores, verifica que hayas escrito exactamente: `builder.Services.AddScoped<DatabaseErrorHandler>();`

---

## ❓ POR QUÉ ES NECESARIO

Este servicio (`DatabaseErrorHandler`) es el que:
- Registra errores de conexión en archivos de log
- Proporciona mensajes amigables al usuario
- Permite la página de diagnóstico `/Admin/TestConnection`

Sin esta línea, la aplicación dará error al intentar usar la página de prueba de conexión.

---

## 📞 SI TIENES PROBLEMAS

Si Visual Studio marca errores después de agregar la línea:
1. Verifica que el archivo `Services/DatabaseErrorHandler.cs` exista
2. Asegúrate de no haber borrado accidentalmente ningún `;` o `>` 
3. Intenta hacer "Clean Solution" y luego "Rebuild Solution"
