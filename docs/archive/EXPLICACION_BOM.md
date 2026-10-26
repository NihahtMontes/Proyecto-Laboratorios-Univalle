# 🛠️ Solución al Conflicto de Codificación (UTF-8 BOM)

## 🚨 El Problema: UTF-8 con BOM
Por defecto, Visual Studio en Windows guarda los archivos `.cs` y `.cshtml` con una marca de orden de bytes (**BOM** - *Byte Order Mark*).
- **Efecto**: Esta marca (`0xEF, 0xBB, 0xBF`) al inicio del archivo confunde a muchas herramientas de automatización y edición directa (como las que uso yo para editar tu código).
- **Resultado**: Errores de "failed to detect charset" o "unsupported mime type", lo que me impedía leer o modificar tus archivos directamente.

## ✅ La Solución: Script de Limpieza Masiva
He creado y ejecutado un script de PowerShell (`fix-encoding.ps1`) en la raíz de tu proyecto que realiza lo siguiente:
1. Escanea todos los archivos `.cs`, `.cshtml`, `.json` y `.md`.
2. Detecta si tienen la marca BOM.
3. Si la tienen, la elimina y guarda el archivo en **UTF-8 puro (sin BOM)**.

### Cómo usarlo en el futuro
Si vuelves a clonar el proyecto en otra máquina o tienes problemas similares, simplemente abre una terminal en la carpeta del proyecto y corre:
```powershell
powershell -ExecutionPolicy Bypass -File fix-encoding.ps1
```

## 📄 Archivos Afectados
- **ApplicationDbContext.cs**: Ya es editable y contiene las nuevas tablas de "Gestión".
- **Modelos y Enums**: Todos los archivos en C# ahora siguen el estándar moderno de la web (UTF-8 sin BOM).

---
*Este ajuste es permanente para los archivos actuales y facilita que yo pueda trabajar mucho más rápido en tu código sin errores de lectura.*
