// Script para limpiar las plantillas Excel (quitar hojas extra)
// Ejecutar UNA VEZ con: dotnet script CleanTemplates.csx
// O manualmente: abrir cada .xlsx en Excel, borrar las hojas extra, guardar.

#r "nuget: EPPlus, 7.5.2"
using OfficeOpenXml;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var templatesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates");

foreach (var file in Directory.GetFiles(templatesDir, "*.xlsx"))
{
    var fileName = Path.GetFileName(file);
    
    using var package = new ExcelPackage(new FileInfo(file));
    var count = package.Workbook.Worksheets.Count;
    
    if (count <= 1)
    {
        Console.WriteLine($"  {fileName}: OK (1 hoja)");
        continue;
    }

    Console.WriteLine($"  {fileName}: {count} hojas -> limpiando...");
    
    // Crear un paquete NUEVO con solo la primera hoja (evita corrupción de XML)
    var backupPath = file + ".backup";
    File.Copy(file, backupPath, true);
    
    using var newPackage = new ExcelPackage();
    var source = package.Workbook.Worksheets[0];
    newPackage.Workbook.Worksheets.Add(source.Name, source);
    
    newPackage.SaveAs(new FileInfo(file));
    Console.WriteLine($"  {fileName}: limpio! (1 hoja). Backup en {Path.GetFileName(backupPath)}");
}

Console.WriteLine("\nListo. Ahora EliminarHojasExtra ya no es necesario.");
