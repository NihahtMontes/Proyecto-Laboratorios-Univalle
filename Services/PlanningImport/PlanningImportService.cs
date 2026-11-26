using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

namespace Proyecto_Laboratorios_Univalle.Services.PlanningImport;

public interface IPlanningImportService
{
    /// <summary>Plantilla de la gestión: Instrucciones, Programacion (con listas desplegables), Listas y Material_extra.</summary>
    Task<byte[]> BuildTemplateAsync(int managementId, CancellationToken ct = default);

    /// <summary>Lee el archivo subido. Lanza <see cref="ImportFileException"/> si el formato no sirve.</summary>
    ImportFile ReadFile(Stream stream, string fileName);

    /// <summary>Revisa cada fila contra el sistema y contra las demás filas, y aplica las decisiones (omitir / sobrescribir).</summary>
    Task<ImportAnalysis> AnalyzeAsync(int managementId, ImportFile file, IReadOnlyDictionary<int, string> decisions, CancellationToken ct = default);

    /// <summary>Vuelve a revisar y, si todo sigue en orden, carga todo en una sola transacción (todo o nada).</summary>
    Task<ImportApplyResult> ApplyAsync(int managementId, ImportFile file, IReadOnlyDictionary<int, string> decisions, CancellationToken ct = default);

    /// <summary>El mismo contenido del archivo con las columnas Resultado y Detalle, para corregir y volver a subir.</summary>
    byte[] BuildResultWorkbook(ImportAnalysis analysis);
}

/// <summary>
/// Carga de la planificación del semestre desde Excel (una fila = una práctica de un grupo con fecha, periodos,
/// laboratorio y docente). El material no viene en el archivo: sale de la receta de cada práctica.
/// El Excel no trae reprogramaciones ni reposiciones: esas se gestionan después en el sistema.
/// </summary>
public sealed class PlanningImportService : IPlanningImportService
{
    public const int MaxRows = 3000;
    public const string SheetRows = "Programacion";
    public const string SheetLists = "Listas";
    public const string SheetExtras = "Material_extra";
    public const string SheetHelp = "Instrucciones";
    public const string SourceType = "PlanificacionSemestre";

    public static readonly string[] Headers =
    [
        "Gestión", "Materia", "Grupo", "Práctica", "Semana académica", "Fecha", "Periodo inicial", "N.º de periodos",
        "Laboratorio", "Docente", "N.º de estudiantes", "Observaciones"
    ];

    private const string Sep = " · ";
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-BO");
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yy", "d/M/yy"];
    private static readonly string[] Titles = ["lic", "ing", "dr", "dra", "msc", "mgr", "chef", "prof", "arq", "tec", "phd"];

    private readonly ApplicationDbContext _context;
    private readonly IOccupancyService _occupancy;
    private readonly IGroupScheduleService _schedules;
    private readonly ILogger<PlanningImportService> _logger;

    public PlanningImportService(ApplicationDbContext context, IOccupancyService occupancy, IGroupScheduleService schedules,
        ILogger<PlanningImportService> logger)
    {
        _context = context;
        _occupancy = occupancy;
        _schedules = schedules;
        _logger = logger;
    }

    // ───────────────────────────────────────────────────────────── utilidades de texto

    /// <summary>Minúsculas, sin tildes y con espacios simples: para comparar lo escrito con lo del sistema.</summary>
    public static string Norm(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var space = false;
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(ch)) { space = sb.Length > 0; continue; }
            if (space) { sb.Append(' '); space = false; }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>Nombre de persona normalizado y sin título ("Lic.", "Ing."...).</summary>
    public static string NormPerson(string? name)
    {
        var tokens = Norm(name).Replace(".", " ").Replace(",", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !Titles.Contains(t));
        return string.Join(' ', tokens);
    }

    private static string HeaderKey(string? header) =>
        new(Norm(header).Where(c => c is >= 'a' and <= 'z' or >= '0' and <= '9').ToArray());

    /// <summary>Parte anterior al separador " · " (el código en "COD · Nombre"); todo el texto si no hay separador.</summary>
    private static string CodePart(string text)
    {
        var i = text.IndexOf('·');
        return (i > 0 ? text[..i] : text).Trim();
    }

    private static string NamePart(string text)
    {
        var i = text.IndexOf('·');
        return (i > 0 ? text[(i + 1)..] : text).Trim();
    }

    private static bool TryDate(string text, out DateTime date) =>
        DateTime.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool TryInt(string text, out int value)
    {
        value = 0;
        var clean = text.Trim();
        if (clean.Length == 0) return false;
        if (decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
            || decimal.TryParse(clean, NumberStyles.Number, Es, out d))
        {
            if (d != Math.Truncate(d) || d < int.MinValue || d > int.MaxValue) return false;
            value = (int)d;
            return true;
        }
        // "P3", "Periodo 3", "Semana 5": se toma el número.
        var digits = new string(clean.Where(char.IsDigit).ToArray());
        return digits.Length is > 0 and <= 4 && int.TryParse(digits, out value);
    }

    // ───────────────────────────────────────────────────────────── plantilla

    public async Task<byte[]> BuildTemplateAsync(int managementId, CancellationToken ct = default)
    {
        var management = await _context.Managements.AsNoTracking()
            .Where(m => m.Id == managementId).Select(m => new { m.Code }).FirstOrDefaultAsync(ct)
            ?? throw new ImportFileException("La gestión no existe.");

        var groups = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.ManagementId == managementId && g.Status == GeneralStatus.Activo)
            .Select(g => new { g.SubjectId, g.Name })
            .ToListAsync(ct);
        var subjectIds = groups.Select(g => g.SubjectId).Distinct().ToList();
        var subjects = await _context.Subjects.AsNoTracking()
            .Where(s => s.Status == GeneralStatus.Activo)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Code, s.Name })
            .ToListAsync(ct);
        // Primero las materias que tienen grupo en la gestión: son las que se van a usar.
        subjects = subjects.OrderBy(s => subjectIds.Contains(s.Id) ? 0 : 1).ThenBy(s => s.Name).ToList();
        var practices = await _context.Practices.AsNoTracking()
            .Where(p => p.Status == GeneralStatus.Activo)
            .Select(p => new { p.SubjectId, p.Code, p.Name, p.Number })
            .ToListAsync(ct);
        var subjectOrder = subjects.Select((s, i) => (s.Id, i)).ToDictionary(x => x.Id, x => x.i);
        practices = practices.OrderBy(p => subjectOrder.GetValueOrDefault(p.SubjectId, int.MaxValue)).ThenBy(p => p.Code).ToList();
        var labs = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo)
            .OrderBy(l => l.Code).Select(l => new { l.Code, l.Name }).ToListAsync(ct);
        var teachers = await ReschedulingService.ActiveTeachers(_context).OrderBy(t => t.Name).Select(t => t.Name).ToListAsync(ct);
        var periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync(ct);
        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.ManagementId == managementId).OrderBy(w => w.Number)
            .Select(w => new { w.Number, w.StartDate, w.EndDate }).ToListAsync(ct);

        using var package = new ExcelPackage();

        // ── Instrucciones
        var help = package.Workbook.Worksheets.Add(SheetHelp);
        var lines = new (string Text, bool Bold)[]
        {
            ($"Planificación de prácticas · Gestión {management.Code}", true),
            ("", false),
            ("Cómo llenar la hoja «Programacion»", true),
            ("1. Una fila por cada práctica de cada grupo. No deje filas en blanco en medio.", false),
            ("2. Materia, Grupo, Práctica, Laboratorio y Docente se eligen de la lista desplegable de cada celda (salen de la hoja «Listas»).", false),
            ("3. Fecha: obligatoria, con formato día/mes/año (por ejemplo 19/11/2026). Debe ser un día de clases de la gestión, de lunes a sábado.", false),
            ("4. Periodo inicial: número del periodo en que empieza (1 a 12). N.º de periodos: cuántos periodos seguidos ocupa.", false),
            ("5. Semana académica: opcional. Si la escribe, el sistema avisa cuando no coincide con la fecha.", false),
            ("6. N.º de estudiantes: opcional. Si lo deja vacío se usa el del grupo.", false),
            ("7. Gestión: opcional. Si la escribe debe ser " + management.Code + ".", false),
            ("", false),
            ("Qué NO va en este archivo", true),
            ("• El material: sale solo de la receta de cada práctica. Si una práctica necesita algo adicional, use la hoja «Material_extra».", false),
            ("• Reprogramaciones y reposiciones: se hacen después, dentro del sistema.", false),
            ("", false),
            ("Qué pasa al subirlo", true),
            ("El sistema revisa fila por fila antes de cargar nada: datos que no existen, choques de laboratorio, docente o estudiantes, días suspendidos y prácticas que ya están cargadas.", false),
            ("Para las que ya están cargadas usted decide, una por una o todas juntas: sobrescribir u omitir.", false),
            ("Solo cuando todo está resuelto se confirma, y se carga todo junto.", false),
            ("", false),
            ("Ejemplo de una fila", true),
            ($"{management.Code} | {(subjects.Count > 0 ? subjects[0].Code + Sep + subjects[0].Name : "GAS-101 · Panadería")} | A1 | (práctica de la lista) | 5 | 19/11/2026 | 3 | 2 | (laboratorio de la lista) | (docente de la lista) | 13 | ", false)
        };
        for (var i = 0; i < lines.Length; i++)
        {
            help.Cells[i + 1, 1].Value = lines[i].Text;
            help.Cells[i + 1, 1].Style.Font.Bold = lines[i].Bold;
            help.Cells[i + 1, 1].Style.WrapText = true;
        }
        help.Cells[1, 1].Style.Font.Size = 14;
        help.Column(1).Width = 130;

        // ── Listas
        var lists = package.Workbook.Worksheets.Add(SheetLists);
        string[] listHeaders = ["Materias", "Prácticas", "Grupos", "Laboratorios", "Docentes", "Periodos", "Semanas académicas"];
        for (var c = 0; c < listHeaders.Length; c++) lists.Cells[1, c + 1].Value = listHeaders[c];
        lists.Cells[1, 1, 1, listHeaders.Length].Style.Font.Bold = true;
        var subjectCodes = subjects.ToDictionary(s => s.Id, s => s.Code);
        var subjectValues = subjects.Select(s => s.Code + Sep + s.Name).ToList();
        var practiceValues = practices.Select(p => p.Code + Sep + (string.IsNullOrWhiteSpace(p.Number) ? "" : p.Number + " ") + p.Name
            + " [" + subjectCodes.GetValueOrDefault(p.SubjectId, "?") + "]").ToList();
        var groupValues = groups.Select(g => g.Name).Distinct().OrderBy(n => n).ToList();
        var labValues = labs.Select(l => l.Code + Sep + l.Name).ToList();
        var periodValues = periods.Select(p => $"{p.Order}{Sep}{p.StartTime:hh\\:mm}–{p.EndTime:hh\\:mm}").ToList();
        var weekValues = weeks.Select(w => $"{w.Number}{Sep}{w.StartDate:dd/MM} al {w.EndDate:dd/MM}").ToList();
        void Fill(int column, IReadOnlyList<string> values)
        {
            for (var i = 0; i < values.Count; i++) lists.Cells[i + 2, column].Value = values[i];
        }
        Fill(1, subjectValues); Fill(2, practiceValues); Fill(3, groupValues); Fill(4, labValues); Fill(5, teachers);
        Fill(6, periodValues); Fill(7, weekValues);
        lists.Cells.AutoFitColumns(10, 70);

        // ── Programacion
        var sheet = package.Workbook.Worksheets.Add(SheetRows);
        for (var c = 0; c < Headers.Length; c++) sheet.Cells[1, c + 1].Value = Headers[c];
        using (var head = sheet.Cells[1, 1, 1, Headers.Length])
        {
            head.Style.Font.Bold = true;
            head.Style.Font.Color.SetColor(System.Drawing.Color.White);
            head.Style.Fill.PatternType = ExcelFillStyle.Solid;
            head.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0x74, 0x60, 0xEE));
        }
        sheet.View.FreezePanes(2, 1);
        const int lastRow = 1500;
        sheet.Cells[2, 6, lastRow, 6].Style.Numberformat.Format = "dd/mm/yyyy";
        void ListValidation(int column, int listColumn, int count, string title, bool strict)
        {
            if (count == 0) return;
            var letter = ExcelCellAddress.GetColumnLetter(listColumn);
            var validation = sheet.DataValidations.AddListValidation(ExcelCellBase.GetAddress(2, column, lastRow, column));
            validation.Formula.ExcelFormula = $"{SheetLists}!${letter}$2:${letter}${count + 1}";
            validation.ShowErrorMessage = true;
            validation.ErrorStyle = strict ? ExcelDataValidationWarningStyle.stop : ExcelDataValidationWarningStyle.warning;
            validation.ErrorTitle = title;
            validation.Error = "Elija un valor de la lista desplegable.";
            validation.AllowBlank = true;
        }
        ListValidation(2, 1, subjectValues.Count, "Materia", true);
        ListValidation(3, 3, groupValues.Count, "Grupo", false);
        ListValidation(4, 2, practiceValues.Count, "Práctica", true);
        ListValidation(9, 4, labValues.Count, "Laboratorio", true);
        ListValidation(10, 5, teachers.Count, "Docente", false);
        var maxOrder = periods.Count > 0 ? periods.Max(p => p.Order) : PlanningSlot.MaxPeriods;
        void IntValidation(int column, int min, int max, string title)
        {
            var validation = sheet.DataValidations.AddIntegerValidation(ExcelCellBase.GetAddress(2, column, lastRow, column));
            validation.Operator = ExcelDataValidationOperator.between;
            validation.Formula.Value = min;
            validation.Formula2.Value = max;
            validation.ShowErrorMessage = true;
            validation.ErrorTitle = title;
            validation.Error = $"Escriba un número entre {min} y {max}.";
            validation.AllowBlank = true;
        }
        IntValidation(7, 1, maxOrder, "Periodo inicial");
        IntValidation(8, 1, maxOrder, "N.º de periodos");
        IntValidation(11, 1, 500, "N.º de estudiantes");
        int[] widths = [12, 38, 10, 52, 12, 13, 12, 12, 30, 34, 12, 40];
        for (var c = 0; c < widths.Length; c++) sheet.Column(c + 1).Width = widths[c];
        sheet.Cells[1, 1, 1, Headers.Length].Style.WrapText = true;
        sheet.Row(1).Height = 32;

        // ── Material_extra
        var extras = package.Workbook.Worksheets.Add(SheetExtras);
        string[] extraHeaders = ["Fila de Programacion", "Artículo (código)", "Cantidad"];
        for (var c = 0; c < extraHeaders.Length; c++) extras.Cells[1, c + 1].Value = extraHeaders[c];
        extras.Cells[1, 1, 1, 3].Style.Font.Bold = true;
        extras.Cells[1, 5].Value = "Opcional. Solo para material que la práctica necesita además de su receta. «Fila de Programacion» es el número de fila de Excel de esa práctica en la hoja Programacion.";
        extras.Column(1).Width = 22; extras.Column(2).Width = 24; extras.Column(3).Width = 12;

        package.Workbook.Worksheets.MoveToStart(SheetRows);
        package.Workbook.Worksheets.MoveAfter(SheetHelp, SheetRows);
        package.Workbook.View.ActiveTab = 0;
        return package.GetAsByteArray();
    }

    // ───────────────────────────────────────────────────────────── lectura

    public ImportFile ReadFile(Stream stream, string fileName)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        var file = new ImportFile { FileName = fileName, Sha256 = Convert.ToHexString(SHA256.HashData(bytes)) };

        ExcelPackage package;
        try
        {
            package = new ExcelPackage(new MemoryStream(bytes));
        }
        catch (Exception)
        {
            throw new ImportFileException("No se pudo abrir el archivo. Debe ser un Excel .xlsx válido.");
        }
        using (package)
        {
            // Generador de la Inge (.xlsm con CONFIG, MATERIAS y PLANIFICA): se convierte a las mismas filas de la
            // plantilla, con los periodos ya repartidos, y sigue la misma revisión y carga.
            if (GeneratorWorkbookReader.IsGenerator(package.Workbook))
            {
                file.Generator = GeneratorWorkbookReader.Read(package.Workbook);
                file.Rows.AddRange(FromGenerator(file.Generator));
                return file;
            }

            var sheet = package.Workbook.Worksheets.FirstOrDefault(w => Norm(w.Name) == Norm(SheetRows))
                ?? throw new ImportFileException($"El archivo no tiene la hoja «{SheetRows}». Descargue la plantilla y llénela sin cambiar los nombres de las hojas.");
            if (sheet.Dimension == null)
                throw new ImportFileException($"La hoja «{SheetRows}» está vacía.");

            var columns = new Dictionary<string, int>();
            for (var c = 1; c <= sheet.Dimension.End.Column; c++)
            {
                var key = HeaderKey(sheet.Cells[1, c].Text);
                var field = key switch
                {
                    _ when key.Contains("gestion") => "management",
                    _ when key.Contains("materia") => "subject",
                    _ when key.Contains("grupo") => "group",
                    _ when key.Contains("practica") => "practice",
                    _ when key.Contains("semana") => "week",
                    _ when key.Contains("fecha") => "date",
                    _ when key.Contains("periodo") && key.Contains("inicial") => "start",
                    _ when key.Contains("periodo") => "count",
                    _ when key.Contains("laboratorio") => "lab",
                    _ when key.Contains("docente") => "teacher",
                    _ when key.Contains("estudiante") => "students",
                    _ when key.Contains("observ") => "observations",
                    _ => null
                };
                if (field != null && !columns.ContainsKey(field)) columns[field] = c;
            }
            var required = new (string Field, string Title)[]
            {
                ("subject", "Materia"), ("group", "Grupo"), ("practice", "Práctica"), ("date", "Fecha"),
                ("start", "Periodo inicial"), ("count", "N.º de periodos"), ("lab", "Laboratorio"), ("teacher", "Docente")
            };
            var missing = required.Where(r => !columns.ContainsKey(r.Field)).Select(r => r.Title).ToList();
            if (missing.Count > 0)
                throw new ImportFileException($"A la hoja «{SheetRows}» le faltan columnas: {string.Join(", ", missing)}. Use la plantilla sin cambiar los títulos de la primera fila.");

            string Text(int row, string field) =>
                columns.TryGetValue(field, out var c) ? (sheet.Cells[row, c].Text ?? "").Trim() : "";
            string DateText(int row)
            {
                var cell = sheet.Cells[row, columns["date"]];
                return cell.Value switch
                {
                    DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    double d when d is > 20000 and < 80000 => DateTime.FromOADate(d).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    null => "",
                    var other => Convert.ToString(other, CultureInfo.InvariantCulture)?.Trim() ?? ""
                };
            }
            string Number(int row, string field)
            {
                if (!columns.TryGetValue(field, out var c)) return "";
                var value = sheet.Cells[row, c].Value;
                return value switch
                {
                    null => "",
                    double d => d.ToString(CultureInfo.InvariantCulture),
                    _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? ""
                };
            }

            for (var r = 2; r <= sheet.Dimension.End.Row; r++)
            {
                var raw = new ImportRawRow
                {
                    RowNumber = r,
                    Management = Text(r, "management"),
                    Subject = Text(r, "subject"),
                    Group = Text(r, "group"),
                    Practice = Text(r, "practice"),
                    Week = Number(r, "week"),
                    Date = DateText(r),
                    StartPeriod = Number(r, "start"),
                    PeriodCount = Number(r, "count"),
                    Laboratory = Text(r, "lab"),
                    Teacher = Text(r, "teacher"),
                    Students = Number(r, "students"),
                    Observations = Text(r, "observations")
                };
                // Fila en blanco (las validaciones de la plantilla llegan hasta la 1500): se ignora.
                if (new[] { raw.Subject, raw.Group, raw.Practice, raw.Date, raw.StartPeriod, raw.PeriodCount, raw.Laboratory, raw.Teacher }
                    .All(string.IsNullOrWhiteSpace))
                    continue;
                file.Rows.Add(raw);
                if (file.Rows.Count > MaxRows)
                    throw new ImportFileException($"El archivo tiene más de {MaxRows} prácticas. Divídalo en varios archivos.");
            }
            if (file.Rows.Count == 0)
                throw new ImportFileException($"La hoja «{SheetRows}» no tiene ninguna práctica cargada.");

            var extraSheet = package.Workbook.Worksheets.FirstOrDefault(w => Norm(w.Name) == Norm(SheetExtras));
            if (extraSheet?.Dimension != null)
            {
                for (var r = 2; r <= extraSheet.Dimension.End.Row; r++)
                {
                    var target = Convert.ToString(extraSheet.Cells[r, 1].Value, CultureInfo.InvariantCulture)?.Trim() ?? "";
                    var article = (extraSheet.Cells[r, 2].Text ?? "").Trim();
                    var quantity = Convert.ToString(extraSheet.Cells[r, 3].Value, CultureInfo.InvariantCulture)?.Trim() ?? "";
                    if (target.Length == 0 && article.Length == 0 && quantity.Length == 0) continue;
                    file.Extras.Add(new ImportRawExtra { RowNumber = r, TargetRow = target, Article = article, Quantity = quantity });
                }
            }
        }
        return file;
    }

    /// <summary>
    /// Filas del generador → filas de la plantilla. El generador no trae horas: los periodos se reparten con
    /// <see cref="GeneratorPeriodPlanner"/> y cada decisión queda como aviso de la fila.
    /// </summary>
    public static List<ImportRawRow> FromGenerator(GeneratorWorkbook generator)
    {
        // Semanas cuyo rango en CONFIG repite el de otra anterior (en 1-2027 la 6 copia la 3): sus fechas son dudosas,
        // así que esas prácticas se omiten hasta que la Inge corrija el Excel (decisión del usuario, 2026-11-23).
        var repeated = generator.Weeks.Where(w => w.Start != null)
            .GroupBy(w => w.Start).SelectMany(g => g.OrderBy(w => w.Number).Skip(1)
                .Select(w => (w.Number, First: g.Min(x => x.Number)))).ToDictionary(x => x.Number, x => x.First);
        bool Doubtful(GeneratorRow r) => r.Week is int week && repeated.ContainsKey(week);

        // Solo se evita el choque de docente: la misma letra de grupo en materias distintas no identifica a los mismos
        // estudiantes (la Inge pone hasta 4 materias del mismo semestre y letra el mismo día), así que no se usa.
        var inputs = generator.Rows.Where(r => r.Date != null && !Doubtful(r)).Select(r => new GeneratorPeriodPlanner.Input(
            r.RowNumber, r.Date!.Value, r.Laboratory, NormPerson(r.Teacher), "", Norm(r.SubjectCode) + "|" + Norm(r.Group)));
        var plan = GeneratorPeriodPlanner.Plan(inputs);
        string Num(int? n) => n?.ToString(CultureInfo.InvariantCulture) ?? "";
        return generator.Rows.Select(r =>
        {
            var p = plan.GetValueOrDefault(r.RowNumber);
            return new ImportRawRow
            {
                RowNumber = r.RowNumber,
                Subject = r.SubjectCode,
                Group = r.Group,
                Practice = r.Practice,
                Date = r.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                StartPeriod = Num(p?.Slot?.Start),
                PeriodCount = Num(p?.Slot?.Count),
                Laboratory = r.Laboratory,
                Teacher = r.Teacher,
                Students = Num(r.Students),
                Notes = [.. (r.Week is int week && repeated.TryGetValue(week, out var first)
                    ? [$"Se omite: es de la semana {week} del Excel y en CONFIG esa semana tiene las mismas fechas que la semana {first}. Cuando la Inge la corrija, vuelva a subir el archivo y se agregará sin duplicar lo ya cargado."]
                    : Array.Empty<string>()), .. p?.Notes ?? []],
                DefaultDecision = Doubtful(r) ? ImportDecisions.Skip : ""
            };
        }).ToList();
    }

    // ───────────────────────────────────────────────────────────── revisión

    private sealed record ExistingPlanning(int Id, string? Code, string Status, int ScheduleId, DateTime Date, int? StartOrder,
        int? PeriodCount, int? LaboratoryId, string? LabCode, int TeacherPersonId, int? StudentCount);

    public async Task<ImportAnalysis> AnalyzeAsync(int managementId, ImportFile file, IReadOnlyDictionary<int, string> decisions, CancellationToken ct = default)
    {
        var management = await _context.Managements.AsNoTracking()
            .Where(m => m.Id == managementId).Select(m => new { m.Id, m.Code, m.Status }).FirstOrDefaultAsync(ct)
            ?? throw new ImportFileException("La gestión no existe.");

        // ── Datos de referencia (una consulta por catálogo)
        var subjects = await _context.Subjects.AsNoTracking().Where(s => s.Status == GeneralStatus.Activo)
            .Select(s => new { s.Id, s.Code, s.Name }).ToListAsync(ct);
        var practices = await _context.Practices.AsNoTracking().Where(p => p.Status == GeneralStatus.Activo)
            .Select(p => new { p.Id, p.SubjectId, p.Code, p.Name, p.Number }).ToListAsync(ct);
        var groups = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.ManagementId == managementId && g.Status == GeneralStatus.Activo)
            .Select(g => new { g.Id, g.SubjectId, g.Name, g.TeacherPersonId, g.StudentGroupId, g.StudentCount, g.ScalingFactor })
            .ToListAsync(ct);
        var labs = await _context.Laboratories.AsNoTracking().Where(l => l.Status == GeneralStatus.Activo)
            .Select(l => new { l.Id, l.Code, l.Name, l.Capacity, l.CampusId }).ToListAsync(ct);
        var teachers = (await ReschedulingService.ActiveTeachers(_context).Select(t => new { t.Id, t.Name }).ToListAsync(ct))
            .Select(t => new { t.Id, t.Name, Key = NormPerson(t.Name) }).ToList();
        var periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync(ct);
        var maxOrder = periods.Count > 0 ? periods.Max(p => p.Order) : PlanningSlot.MaxPeriods;
        var days = (await _context.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == managementId)
            .Select(d => new { d.Date, d.Status, d.Reason, d.AcademicWeekId, Week = d.AcademicWeek.Number })
            .ToListAsync(ct)).ToDictionary(d => d.Date.Date);
        var recipePractices = (await _context.PracticeRequirements.AsNoTracking()
            .Where(r => !r.IsRemoved).Select(r => r.PracticeId).Distinct().ToListAsync(ct)).ToHashSet();

        var schedules = await _context.PracticeSchedules.AsNoTracking()
            .Where(s => s.ManagementId == managementId && !s.IsDeleted)
            .Select(s => new { s.Id, s.PracticeId, s.GroupName }).ToListAsync(ct);
        var scheduleByKey = schedules
            .GroupBy(s => (s.PracticeId, Norm(s.GroupName)))
            .ToDictionary(g => g.Key, g => g.First().Id);
        var planningRows = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == managementId && !p.IsDeleted && p.PracticeScheduleId != null)
            .Select(p => new ExistingPlanning(p.Id, p.Code, p.Status, p.PracticeScheduleId!.Value, p.ScheduledDate,
                p.StartPeriod != null ? (int?)p.StartPeriod.Order : null, p.PeriodCount, p.LaboratoryId,
                p.Laboratory != null ? p.Laboratory.Code : null, p.TeacherPersonId, p.StudentCount))
            .ToListAsync(ct);
        var planningsBySchedule = planningRows.GroupBy(p => p.ScheduleId).ToDictionary(g => g.Key, g => g.ToList());
        var deliveredPlanningIds = (await _context.Departures.AsNoTracking()
            .Where(d => d.PlanningId != null && d.Status != LoanStatus.Cancelled
                && _context.Plannings.Any(p => p.Id == d.PlanningId && p.ManagementId == managementId))
            .Select(d => d.PlanningId!.Value).Distinct().ToListAsync(ct)).ToHashSet();
        var teacherNames = teachers.ToDictionary(t => t.Id, t => t.Name);

        var generator = file.Generator;
        if (generator != null && generator.ManagementCode.Length > 0 && Norm(generator.ManagementCode) != Norm(management.Code))
            throw new ImportFileException($"El archivo es de la gestión {generator.ManagementCode} y se está cargando la gestión {management.Code}.");

        var analysis = new ImportAnalysis { ManagementId = management.Id, ManagementCode = management.Code, FileName = file.FileName };
        if (management.Status is not (ManagementStatus.Active or ManagementStatus.Inactive))
            analysis.ExtraSheetIssues.Add(AcademicManagementService.ReadOnlyMessage);
        if (generator != null)
            analysis.FileNotices.AddRange(generator.Notices);

        // ── 1) Cada fila contra los catálogos
        foreach (var raw in file.Rows)
        {
            var row = new ImportRow { Raw = raw, Decision = decisions.GetValueOrDefault(raw.RowNumber, raw.DefaultDecision) };
            analysis.Rows.Add(row);
            void Error(string column, string message) => row.Issues.Add(new ImportIssue(ImportIssueLevel.Error, column, message));
            void Clash(string column, string message) => row.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, column, message));
            void Warn(string column, string message) => row.Issues.Add(new ImportIssue(ImportIssueLevel.Warning, column, message));

            foreach (var note in raw.Notes)
                Warn("Horario", note);

            if (!string.IsNullOrWhiteSpace(raw.Management) && Norm(raw.Management) != Norm(management.Code))
                Error("Gestión", $"La fila dice «{raw.Management}» y se está cargando la gestión {management.Code}. Borre el dato o corrija la gestión.");

            // Materia
            if (string.IsNullOrWhiteSpace(raw.Subject))
                Error("Materia", "Falta la materia.");
            else
            {
                var code = Norm(CodePart(raw.Subject));
                var name = Norm(NamePart(raw.Subject));
                var full = Norm(raw.Subject);
                var subject = subjects.FirstOrDefault(s => Norm(s.Code) == code)
                    ?? subjects.FirstOrDefault(s => Norm(s.Name) == full || Norm(s.Name) == name);
                if (subject == null)
                    Error("Materia", $"La materia «{raw.Subject}» no existe o está inactiva. Elíjala de la lista desplegable.");
                else
                {
                    row.SubjectId = subject.Id;
                    row.SubjectLabel = subject.Name;
                }
            }

            // Práctica (del catálogo de la materia)
            if (string.IsNullOrWhiteSpace(raw.Practice))
                Error("Práctica", "Falta la práctica.");
            else
            {
                // La plantilla escribe "COD · P3 Nombre [MATERIA]": el corchete final es solo una ayuda visual.
                var text = raw.Practice;
                var bracket = text.LastIndexOf('[');
                if (bracket > 0 && text.TrimEnd().EndsWith(']')) text = text[..bracket].Trim();
                var code = Norm(CodePart(text));
                var name = Norm(NamePart(text));
                var byCode = practices.Where(p => Norm(p.Code) == code).ToList();
                var own = row.SubjectId is int sid ? practices.Where(p => p.SubjectId == sid).ToList() : [];
                var practice = byCode.FirstOrDefault(p => row.SubjectId == null || p.SubjectId == row.SubjectId)
                    ?? own.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Number) && Norm(p.Number) == Norm(text))
                    ?? own.FirstOrDefault(p => Norm(p.Name) == Norm(text) || Norm(p.Name) == name
                        || Norm((p.Number + " " + p.Name).Trim()) == name);
                if (practice == null && byCode.Count > 0 && row.SubjectId != null)
                {
                    var owner = subjects.FirstOrDefault(s => s.Id == byCode[0].SubjectId)?.Name ?? "otra materia";
                    Error("Práctica", $"La práctica «{byCode[0].Code}» es de {owner}, no de {row.SubjectLabel}.");
                }
                else if (practice == null)
                    Error("Práctica", row.SubjectId == null
                        ? $"No se pudo ubicar la práctica «{raw.Practice}» (primero corrija la materia)."
                        : $"La práctica «{raw.Practice}» no existe en el catálogo de {row.SubjectLabel}. Elíjala de la lista o créela en Prácticas.");
                else
                {
                    row.PracticeId = practice.Id;
                    row.PracticeNumber = practice.Number;
                    row.PracticeLabel = string.IsNullOrWhiteSpace(practice.Name) ? practice.Code : practice.Name.Trim();
                    if (row.SubjectId == null)
                    {
                        row.SubjectId = practice.SubjectId;
                        row.SubjectLabel = subjects.FirstOrDefault(s => s.Id == practice.SubjectId)?.Name;
                    }
                    if (!recipePractices.Contains(practice.Id))
                        Warn("Práctica", "La práctica no tiene receta: se cargará sin material. Agréguelo después en Requerimientos.");
                }
            }

            // Grupo de la materia en la gestión
            int? groupTeacherId = null;
            int? groupStudents = null;
            if (string.IsNullOrWhiteSpace(raw.Group))
                Error("Grupo", "Falta el grupo.");
            else
            {
                row.GroupName = raw.Group.Trim().ToUpperInvariant();
                if (row.SubjectId is int sid)
                {
                    var group = groups.FirstOrDefault(g => g.SubjectId == sid && Norm(g.Name) == Norm(row.GroupName));
                    if (group == null)
                        Error("Grupo", $"El grupo {row.GroupName} no existe para {row.SubjectLabel} en la gestión {management.Code}. Créelo antes en Grupos.");
                    else
                    {
                        row.GroupName = group.Name;
                        row.CourseGroupId = group.Id;
                        row.StudentGroupId = group.StudentGroupId;
                        row.ScalingFactor = group.ScalingFactor;
                        groupTeacherId = group.TeacherPersonId;
                        groupStudents = group.StudentCount;
                    }
                }
            }

            // Fecha y semana
            if (string.IsNullOrWhiteSpace(raw.Date))
                Error("Fecha", "Falta la fecha. Todas las prácticas deben tener fecha.");
            else if (!TryDate(raw.Date, out var date))
                Error("Fecha", $"«{raw.Date}» no es una fecha válida. Use día/mes/año, por ejemplo 19/11/2026.");
            else if (date.DayOfWeek == DayOfWeek.Sunday)
                Error("Fecha", $"El {date:dd/MM/yyyy} es domingo: no hay actividades académicas.");
            else if (!days.TryGetValue(date.Date, out var day))
                Error("Fecha", $"El {date:dd/MM/yyyy} está fuera del calendario académico de la gestión {management.Code}.");
            else
            {
                row.Date = date.Date;
                row.AcademicWeekId = day.AcademicWeekId;
                row.WeekNumber = day.Week;
                if (day.Status != AcademicDayStatus.Enabled)
                    Clash("Fecha", $"El {date:dd/MM/yyyy} está suspendido{(string.IsNullOrWhiteSpace(day.Reason) ? "" : $" ({day.Reason.Trim()})")}. Elija otra fecha.");
                if (!string.IsNullOrWhiteSpace(raw.Week) && TryInt(raw.Week, out var week) && week != day.Week)
                    Warn("Semana académica", $"El archivo dice semana {week}, pero el {date:dd/MM/yyyy} cae en la semana {day.Week}. Se usa la semana {day.Week}.");
                if (date.Date < DateTime.Today)
                    Warn("Fecha", "La fecha ya pasó.");
            }

            // Periodos
            if (!TryInt(raw.StartPeriod, out var startOrder))
                Error("Periodo inicial", string.IsNullOrWhiteSpace(raw.StartPeriod) ? "Falta el periodo inicial." : $"«{raw.StartPeriod}» no es un número de periodo.");
            else if (periods.FirstOrDefault(p => p.Order == startOrder) is not { } startPeriod)
                Error("Periodo inicial", $"El periodo {startOrder} no existe (van del 1 al {maxOrder}).");
            else
            {
                row.StartOrder = startOrder;
                row.StartPeriodId = startPeriod.Id;
            }
            if (!TryInt(raw.PeriodCount, out var count))
                Error("N.º de periodos", string.IsNullOrWhiteSpace(raw.PeriodCount) ? "Falta la cantidad de periodos." : $"«{raw.PeriodCount}» no es una cantidad de periodos.");
            else if (count < 1)
                Error("N.º de periodos", "La cantidad de periodos debe ser 1 o más.");
            else if (row.StartOrder is int start && !PlanningSlot.Fits(start, count, maxOrder))
                Error("N.º de periodos", $"El bloque no cabe en la jornada: empieza en el periodo {start} y con {count} periodos pasa del periodo {maxOrder}.");
            else
                row.PeriodCount = count;

            // Laboratorio
            int? capacity = null;
            if (string.IsNullOrWhiteSpace(raw.Laboratory))
                Error("Laboratorio", "Falta el laboratorio.");
            else
            {
                var code = Norm(CodePart(raw.Laboratory));
                var name = Norm(NamePart(raw.Laboratory));
                var lab = labs.FirstOrDefault(l => Norm(l.Code) == code)
                    ?? labs.FirstOrDefault(l => Norm(l.Name) == Norm(raw.Laboratory) || Norm(l.Name) == name);
                if (lab == null)
                    Error("Laboratorio", $"El laboratorio «{raw.Laboratory}» no existe o está inactivo. Elíjalo de la lista desplegable.");
                else
                {
                    row.LaboratoryId = lab.Id;
                    row.LaboratoryLabel = lab.Code;
                    row.CampusId = lab.CampusId;
                    capacity = lab.Capacity;
                }
            }

            // Docente (por nombre)
            if (string.IsNullOrWhiteSpace(raw.Teacher))
                Error("Docente", "Falta el docente.");
            else
            {
                var key = NormPerson(raw.Teacher);
                var exact = teachers.Where(t => t.Key == key).ToList();
                if (exact.Count == 1)
                {
                    row.TeacherId = exact[0].Id;
                    row.TeacherName = exact[0].Name;
                }
                else if (exact.Count > 1)
                    Error("Docente", $"Hay {exact.Count} docentes llamados «{raw.Teacher}». Diferéncielos en Personas antes de cargar.");
                else
                {
                    var tokens = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var candidates = tokens.Length == 0 ? [] : teachers
                        .Select(t => new { t.Id, t.Name, Tokens = t.Key.Split(' ', StringSplitOptions.RemoveEmptyEntries) })
                        .Select(t => new { t.Id, t.Name, Shared = tokens.Count(x => t.Tokens.Contains(x)), Total = t.Tokens.Length })
                        .Where(t => t.Shared > 0)
                        .OrderByDescending(t => t.Shared).ThenBy(t => t.Total).ToList();
                    // Coincidencia única que contiene todas las palabras escritas (p. ej. falta el segundo apellido).
                    var contained = candidates.Where(t => t.Shared == tokens.Length && tokens.Length >= 2).ToList();
                    if (contained.Count == 1)
                    {
                        row.TeacherId = contained[0].Id;
                        row.TeacherName = contained[0].Name;
                        if (generator == null)
                            Warn("Docente", $"«{raw.Teacher}» se interpretó como {contained[0].Name}. Si no es esa persona, corrija el nombre.");
                    }
                    else
                    {
                        var hint = candidates.Count > 0
                            ? " ¿Quiso decir " + string.Join(" o ", candidates.Take(3).Select(c => c.Name)) + "?"
                            : " Regístrelo antes en Personas como docente activo.";
                        Error("Docente", $"No hay un docente activo llamado «{raw.Teacher}».{hint}");
                    }
                }
                if (row.TeacherId is int teacherId && groupTeacherId is int expected && expected != teacherId)
                    Warn("Docente", $"El grupo {row.GroupName} tiene como docente a {teacherNames.GetValueOrDefault(expected, "otro docente")}.");
            }

            // Estudiantes y aforo
            if (!string.IsNullOrWhiteSpace(raw.Students))
            {
                if (!TryInt(raw.Students, out var students) || students is < 1 or > 500)
                    Error("N.º de estudiantes", $"«{raw.Students}» no es una cantidad de estudiantes válida (1 a 500).");
                else
                    row.Students = students;
            }
            var effectiveStudents = row.Students ?? groupStudents;
            if (effectiveStudents is null && row.CourseGroupId != null)
                Warn("N.º de estudiantes", "Ni la fila ni el grupo tienen número de estudiantes: el material que depende de eso se cargará con la cantidad base.");
            if (row.LaboratoryLabel != null
                && PlanningAssignmentRules.CapacityError(effectiveStudents, capacity, row.LaboratoryLabel) is string capacityError)
                Error("N.º de estudiantes", capacityError);
        }

        // ── 2) Material extra
        var articleCodes = file.Extras.Select(e => e.Article.Trim()).Where(c => c.Length > 0).Distinct().ToList();
        var articles = articleCodes.Count == 0 ? [] : await _context.Articles.AsNoTracking()
            .Where(a => articleCodes.Contains(a.Code) && a.Status == GeneralStatus.Activo)
            .Select(a => new { a.Id, a.Code, a.Name }).ToListAsync(ct);
        var rowsByNumber = analysis.Rows.ToDictionary(r => r.RowNumber);
        foreach (var extra in file.Extras)
        {
            var where = $"{SheetExtras}, fila {extra.RowNumber}";
            if (!TryInt(extra.TargetRow, out var target) || !rowsByNumber.TryGetValue(target, out var targetRow))
            {
                analysis.ExtraSheetIssues.Add($"{where}: «{extra.TargetRow}» no es una fila con práctica de la hoja {SheetRows}.");
                continue;
            }
            var article = articles.FirstOrDefault(a => Norm(a.Code) == Norm(extra.Article));
            if (article == null)
            {
                targetRow.Issues.Add(new ImportIssue(ImportIssueLevel.Error, "Material extra", $"{where}: el artículo «{extra.Article}» no existe o está inactivo."));
                continue;
            }
            if (!decimal.TryParse(extra.Quantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
            {
                targetRow.Issues.Add(new ImportIssue(ImportIssueLevel.Error, "Material extra", $"{where}: la cantidad «{extra.Quantity}» debe ser un número mayor a 0."));
                continue;
            }
            targetRow.ExtraLines.Add(new ImportExtraLine(article.Id, article.Code + Sep + article.Name, quantity));
        }

        // ── 3) ¿Ya está cargada?
        var labCodes = labs.ToDictionary(l => l.Id, l => l.Code);
        foreach (var row in analysis.Rows.Where(r => r.PracticeId != null && r.GroupName != null))
        {
            if (!scheduleByKey.TryGetValue((row.PracticeId!.Value, Norm(row.GroupName)), out var scheduleId)) continue;
            row.ScheduleId = scheduleId;
            var linked = planningsBySchedule.GetValueOrDefault(scheduleId) ?? [];
            var occupying = linked.Where(p => PlanningStatus.Occupies(p.Status)).ToList();
            var waiting = linked.FirstOrDefault(p => p.Status == PlanningStatus.PendingRecovery);
            var existing = occupying.OrderByDescending(p => p.Id).FirstOrDefault() ?? waiting;
            if (existing == null)
            {
                var pending = linked.FirstOrDefault(p => PlanningStatus.PendingReview.Contains(p.Status));
                if (pending != null)
                    row.Issues.Add(new ImportIssue(ImportIssueLevel.Warning, "Práctica",
                        $"Hay una solicitud del docente en revisión para esta práctica ({L3Codes.Format(pending.Id, pending.Code)}). Al cargar esta fila conviene rechazarla o retirarla."));
                continue;   // programación libre: se adopta al crear
            }

            row.State = ImportRowState.Existing;
            row.ExistingPlanningId = existing.Id;
            row.ExistingCode = L3Codes.Format(existing.Id, existing.Code);
            row.ExistingDate = existing.Date.Date;
            row.ExistingLaboratoryId = existing.LaboratoryId;
            row.ExistingSummary = string.Join(Sep, new[]
            {
                existing.Date.ToString("dd/MM/yyyy"),
                existing.StartOrder is int o && existing.PeriodCount is int c ? PlanningSlot.PeriodsText(o, c) : "sin periodos",
                existing.LabCode ?? "sin laboratorio",
                teacherNames.GetValueOrDefault(existing.TeacherPersonId, "docente no activo"),
                PlanningStatus.Label(existing.Status)
            });
            row.OverwriteBlockReason =
                occupying.Count > 1 ? "tiene varias sesiones planificadas"
                : existing.Status == PlanningStatus.PendingRecovery ? "está pendiente de reposición por un día suspendido"
                : deliveredPlanningIds.Contains(existing.Id) ? "ya tiene material entregado"
                : existing.Status == PlanningStatus.Finalized ? "ya está finalizada"
                : existing.Status is not (PlanningStatus.Planned or PlanningStatus.Approved) ? $"está en estado {PlanningStatus.Label(existing.Status)}"
                : null;
            row.CanOverwrite = row.OverwriteBlockReason == null;
            if (row.HasSlot && existing.Date.Date == row.Date && existing.StartOrder == row.StartOrder && existing.PeriodCount == row.PeriodCount
                && existing.LaboratoryId == row.LaboratoryId && existing.TeacherPersonId == row.TeacherId
                && (row.Students == null || row.Students == existing.StudentCount) && row.ExtraLines.Count == 0)
                row.State = ImportRowState.Identical;
        }

        // Generador: al volver a importar se actualiza lo que se puede y lo que ya tiene movimientos no se toca.
        if (generator != null)
        {
            foreach (var row in analysis.Rows.Where(r => r.State == ImportRowState.Existing && r.Decision.Length == 0))
            {
                row.Decision = row.CanOverwrite ? ImportDecisions.Overwrite : ImportDecisions.Skip;
                if (!row.CanOverwrite)
                    row.Issues.Add(new ImportIssue(ImportIssueLevel.Warning, "Práctica",
                        $"Ya está cargada ({row.ExistingCode}) y {row.OverwriteBlockReason}: no se actualiza."));
            }
        }

        // Filas que participan (no omitidas, y las ya cargadas solo si se van a sobrescribir).
        bool Active(ImportRow r) => r.Decision != ImportDecisions.Skip && r.State != ImportRowState.Identical
            && (r.State == ImportRowState.New || (r.Decision == ImportDecisions.Overwrite && r.CanOverwrite));

        // ── 4) Duplicados dentro del archivo (misma práctica y grupo)
        foreach (var group in analysis.Rows
            .Where(r => r.PracticeId != null && r.GroupName != null && r.Decision != ImportDecisions.Skip)
            .GroupBy(r => (r.PracticeId, Norm(r.GroupName)))
            .Where(g => g.Count() > 1))
        {
            foreach (var row in group)
            {
                var others = string.Join(", ", group.Where(r => r != row).Select(r => r.RowNumber));
                row.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Práctica",
                    $"Fila repetida: la misma práctica para el grupo {row.GroupName} también está en la fila {others}. Deje una sola (omita o borre las demás)."));
            }
        }

        // ── 5) Choques contra lo ya planificado y entre filas
        var active = analysis.Rows.Where(r => Active(r) && r.HasSlot).ToList();
        if (active.Count > 0)
        {
            var from = active.Min(r => r.Date!.Value);
            var to = active.Max(r => r.Date!.Value);
            var blocks = await _occupancy.GetAsync(new OccupancyQuery(from, to), ct);
            // Lo que se sobrescribe deja su lugar: no cuenta como ocupación.
            var replaced = analysis.Rows.Where(r => r.State == ImportRowState.Existing && r.Decision == ImportDecisions.Overwrite && r.CanOverwrite)
                .Select(r => r.ExistingPlanningId!.Value).ToHashSet();
            var blocksByDate = blocks.Where(b => b.PlanningId is not int id || !replaced.Contains(id))
                .GroupBy(b => b.Date.Date).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var row in active)
            {
                var date = row.Date!.Value;
                var start = row.StartOrder!.Value;
                var count = row.PeriodCount!.Value;
                foreach (var b in blocksByDate.GetValueOrDefault(date) ?? [])
                {
                    if (b.LaboratoryId == row.LaboratoryId && PlanningSlot.OverlapsWithGap(b.StartOrder, b.PeriodCount, start, count))
                        row.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Laboratorio",
                            $"El laboratorio {row.LaboratoryLabel} ya está ocupado por {b.Label}, {b.PeriodsText.ToLowerInvariant()}."));
                    if (row.TeacherId is int teacherId && b.TeacherPersonId == teacherId && PlanningSlot.Overlaps(b.StartOrder, b.PeriodCount, start, count))
                        row.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Docente",
                            $"{row.TeacherName} ya tiene otra práctica en ese horario: {b.Label}, {b.PeriodsText.ToLowerInvariant()}."));
                }
            }

            foreach (var sameDay in active.GroupBy(r => r.Date!.Value))
            {
                var list = sameDay.ToList();
                for (var i = 0; i < list.Count; i++)
                {
                    for (var j = i + 1; j < list.Count; j++)
                    {
                        var a = list[i];
                        var b = list[j];
                        var overlap = PlanningSlot.Overlaps(a.StartOrder!.Value, a.PeriodCount!.Value, b.StartOrder!.Value, b.PeriodCount!.Value);
                        if (a.LaboratoryId == b.LaboratoryId
                            && PlanningSlot.OverlapsWithGap(a.StartOrder.Value, a.PeriodCount.Value, b.StartOrder.Value, b.PeriodCount.Value))
                        {
                            a.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Laboratorio", $"Choca con la fila {b.RowNumber}: mismo laboratorio ({a.LaboratoryLabel}) en periodos superpuestos."));
                            b.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Laboratorio", $"Choca con la fila {a.RowNumber}: mismo laboratorio ({b.LaboratoryLabel}) en periodos superpuestos."));
                        }
                        if (overlap && a.TeacherId != null && a.TeacherId == b.TeacherId)
                        {
                            a.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Docente", $"Choca con la fila {b.RowNumber}: {a.TeacherName} tendría dos prácticas a la vez."));
                            b.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Docente", $"Choca con la fila {a.RowNumber}: {b.TeacherName} tendría dos prácticas a la vez."));
                        }
                        if (overlap && a.StudentGroupId != null && a.StudentGroupId == b.StudentGroupId)
                        {
                            a.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Grupo", $"Choca con la fila {b.RowNumber}: los estudiantes del grupo {a.GroupName} tendrían dos prácticas a la vez."));
                            b.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Grupo", $"Choca con la fila {a.RowNumber}: los estudiantes del grupo {b.GroupName} tendrían dos prácticas a la vez."));
                        }
                    }
                }
            }

            // Horario de clases del grupo de estudiantes: no puede coincidir con clases de OTRAS materias.
            foreach (var row in active.Where(r => r.CourseGroupId != null))
            {
                var check = await _schedules.CheckDestinationAsync(row.CourseGroupId!.Value, row.Date!.Value, row.StartOrder!.Value, row.PeriodCount!.Value, ct);
                if (check.Collisions.Count > 0)
                {
                    var names = string.Join(", ", check.Collisions.Select(c => c.SubjectName).Distinct());
                    row.Issues.Add(new ImportIssue(ImportIssueLevel.Clash, "Grupo", $"El grupo {row.GroupName} tiene clase de {names} en ese horario."));
                }
            }
        }

        // ── 6) Acción de cada fila
        foreach (var row in analysis.Rows)
        {
            row.Action = row.Decision == ImportDecisions.Skip ? ImportRowAction.Skip
                : row.State == ImportRowState.Identical && !row.HasErrors ? ImportRowAction.Unchanged
                : row.HasErrors || row.HasClashes ? ImportRowAction.Blocked
                : row.State == ImportRowState.Existing
                    ? (row.Decision == ImportDecisions.Overwrite && row.CanOverwrite ? ImportRowAction.Overwrite : ImportRowAction.NeedsDecision)
                : ImportRowAction.Create;
        }
        return analysis;
    }

    // ───────────────────────────────────────────────────────────── carga

    public async Task<ImportApplyResult> ApplyAsync(int managementId, ImportFile file, IReadOnlyDictionary<int, string> decisions, CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // Se revisa otra vez dentro de la transacción: entre la vista previa y la confirmación pudo cambiar algo.
            var analysis = await AnalyzeAsync(managementId, file, decisions, ct);
            if (analysis.ExtraSheetIssues.Count > 0 || !analysis.CanApply)
            {
                await transaction.RollbackAsync(ct);
                return new ImportApplyResult(false,
                    "No se cargó nada: desde la última revisión cambió algo o quedan filas por resolver. Revise la lista otra vez.",
                    0, 0, null, analysis);
            }

            var periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync(ct);
            var toCreate = analysis.Rows.Where(r => r.Action == ImportRowAction.Create).ToList();
            var toOverwrite = analysis.Rows.Where(r => r.Action == ImportRowAction.Overwrite).ToList();
            var practiceIds = toCreate.Concat(toOverwrite).Select(r => r.PracticeId!.Value).Distinct().ToList();
            var recipes = (await _context.PracticeRequirements.AsNoTracking()
                .Where(r => practiceIds.Contains(r.PracticeId) && !r.IsRemoved && r.Article!.Status == GeneralStatus.Activo)
                .Select(r => new { r.PracticeId, r.ArticleId, r.Quantity, r.IsScalable })
                .ToListAsync(ct))
                .GroupBy(r => r.PracticeId).ToDictionary(g => g.Key, g => g.ToList());
            var groupStudents = await _context.CourseGroups.AsNoTracking()
                .Where(g => g.ManagementId == managementId)
                .Select(g => new { g.Id, g.StudentCount, g.IsDemonstrative }).ToDictionaryAsync(g => g.Id, ct);

            var batch = new ImportBatch
            {
                Code = $"PLAN-{DateTime.UtcNow:yyyyMMddHHmmss}",
                SourceType = SourceType,
                ContractVersion = "plan-v1",
                SourceName = file.FileName.Length > 200 ? file.FileName[..200] : file.FileName,
                SourceSha256 = file.Sha256,
                Status = ImportBatchStatus.Official,
                Notes = $"Planificación del semestre {analysis.ManagementCode}: {toCreate.Count} nuevas, {toOverwrite.Count} sobrescritas, "
                    + $"{analysis.SkipCount} omitidas, {analysis.UnchangedCount} sin cambios."
            };
            _context.ImportBatches.Add(batch);
            await _context.SaveChangesAsync(ct);

            List<PlanningRequirement> BuildRequirements(ImportRow row, int? students)
            {
                var lines = new List<PlanningRequirement>();
                foreach (var line in recipes.GetValueOrDefault(row.PracticeId!.Value) ?? [])
                {
                    var (quantity, note) = RequirementPickerService.Scale(line.Quantity, line.IsScalable, students, row.ScalingFactor);
                    if (quantity is not > 0) continue;   // receta sin cantidad: no se inventa
                    lines.Add(new PlanningRequirement
                    {
                        ArticleId = line.ArticleId,
                        RequestedQuantity = quantity,
                        ExpectedQuantity = (int)Math.Ceiling(quantity.Value),
                        Observations = note
                    });
                }
                foreach (var extra in row.ExtraLines)
                {
                    var same = lines.FirstOrDefault(l => l.ArticleId == extra.ArticleId);
                    if (same != null)
                    {
                        same.RequestedQuantity = (same.RequestedQuantity ?? 0) + extra.Quantity;
                        same.ExpectedQuantity = (int)Math.Ceiling(same.RequestedQuantity.Value);
                        same.Observations = "Receta más material extra del Excel.";
                    }
                    else
                        lines.Add(new PlanningRequirement
                        {
                            ArticleId = extra.ArticleId,
                            RequestedQuantity = extra.Quantity,
                            ExpectedQuantity = (int)Math.Ceiling(extra.Quantity),
                            Observations = "Material extra del Excel."
                        });
                }
                return lines;
            }

            void ApplyRow(ImportRow row, Planning planning, PracticeSchedule schedule, int? students)
            {
                planning.ManagementId = managementId;
                planning.AcademicWeekId = row.AcademicWeekId;
                planning.AcademicWeek = row.WeekNumber ?? planning.AcademicWeek;
                planning.LaboratoryId = row.LaboratoryId;
                planning.CampusId = row.CampusId;
                planning.AllLaboratories = false;
                planning.TeacherPersonId = row.TeacherId!.Value;
                planning.SubjectId = row.SubjectId;
                planning.CourseGroupId = row.CourseGroupId;
                planning.GroupName = row.GroupName;
                planning.PracticeName = row.PracticeLabel!.Length <= 255 ? row.PracticeLabel : row.PracticeLabel[..255];
                planning.PracticeNumber = row.PracticeNumber;
                if (!string.IsNullOrWhiteSpace(row.Raw.Observations))
                    planning.Observations = row.Raw.Observations.Trim();
                planning.StudentCount = row.Students ?? planning.StudentCount;
                PlanningSlot.Apply(planning, row.Date!.Value, row.StartPeriodId!.Value, row.PeriodCount!.Value, periods);

                schedule.ManagementId = managementId;
                schedule.SubjectId = row.SubjectId!.Value;
                schedule.PracticeId = row.PracticeId!.Value;
                schedule.CourseGroupId = row.CourseGroupId;
                schedule.GroupName = row.GroupName;
                schedule.TeacherPersonId = row.TeacherId.Value;
                schedule.LaboratoryId = row.LaboratoryId;
                schedule.AcademicWeekId = row.AcademicWeekId;
                schedule.WeekNumber = row.WeekNumber is >= 1 and <= 30 ? row.WeekNumber : null;
                schedule.ExecutionDate = planning.ScheduledDate.Date;
                schedule.StartTime = planning.StartTime;
                schedule.EndTime = planning.EndTime;
                schedule.StudentCount = students ?? schedule.StudentCount;
                schedule.ScalingFactor = row.ScalingFactor ?? schedule.ScalingFactor;
                schedule.ImportBatchId = batch.Id;
            }

            // Nuevas: en lotes de 100 (regla del proyecto), todas dentro de la misma transacción.
            foreach (var chunk in toCreate.Chunk(100))
            {
                var created = new List<(Planning Planning, PracticeSchedule Schedule, bool NewSchedule)>();
                var adoptIds = chunk.Where(r => r.ScheduleId != null).Select(r => r.ScheduleId!.Value).ToList();
                var adopted = adoptIds.Count == 0 ? [] : await _context.PracticeSchedules.AsTracking()
                    .Where(s => adoptIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
                foreach (var row in chunk)
                {
                    var group = row.CourseGroupId is int gid ? groupStudents.GetValueOrDefault(gid) : null;
                    var students = row.Students ?? group?.StudentCount;
                    var newSchedule = row.ScheduleId == null || !adopted.ContainsKey(row.ScheduleId.Value);
                    var schedule = newSchedule
                        ? new PracticeSchedule { Code = "TMP-" + Guid.NewGuid().ToString("N")[..20], IsDemonstrative = group?.IsDemonstrative }
                        : adopted[row.ScheduleId!.Value];
                    schedule.Status = PracticeScheduleStatus.Scheduled;
                    var planning = new Planning { Status = PlanningStatus.Approved, RequestDate = DateTime.UtcNow };
                    ApplyRow(row, planning, schedule, students);
                    foreach (var line in BuildRequirements(row, students)) planning.Requirements.Add(line);
                    if (newSchedule) _context.PracticeSchedules.Add(schedule);
                    planning.PracticeSchedule = schedule;
                    _context.Plannings.Add(planning);
                    created.Add((planning, schedule, newSchedule));
                }
                await _context.SaveChangesAsync(ct);
                foreach (var (planning, schedule, newSchedule) in created)
                {
                    planning.Code = L3Codes.Format(planning.Id);
                    if (newSchedule) schedule.Code = $"PRG-{schedule.Id:00000}";
                }
                await _context.SaveChangesAsync(ct);
                _context.ChangeTracker.Clear();
            }

            // Sobrescritas: se mueve la planificación existente; su material se conserva (si no tenía, se carga la receta).
            foreach (var chunk in toOverwrite.Chunk(100))
            {
                var ids = chunk.Select(r => r.ExistingPlanningId!.Value).ToList();
                var plannings = await _context.Plannings.AsTracking().Include(p => p.PracticeSchedule)
                    .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
                var withMaterial = (await _context.PlanningRequirements.AsNoTracking()
                    .Where(r => ids.Contains(r.PlanningId)).Select(r => r.PlanningId).Distinct().ToListAsync(ct)).ToHashSet();
                foreach (var row in chunk)
                {
                    var planning = plannings[row.ExistingPlanningId!.Value];
                    var schedule = planning.PracticeSchedule!;
                    var group = row.CourseGroupId is int gid ? groupStudents.GetValueOrDefault(gid) : null;
                    var students = row.Students ?? planning.StudentCount ?? group?.StudentCount;
                    ApplyRow(row, planning, schedule, students);
                    if (!withMaterial.Contains(planning.Id))
                        foreach (var line in BuildRequirements(row, students))
                        {
                            line.PlanningId = planning.Id;
                            _context.PlanningRequirements.Add(line);
                        }
                }
                await _context.SaveChangesAsync(ct);
                _context.ChangeTracker.Clear();
            }

            await transaction.CommitAsync(ct);
            var first = toCreate.Concat(toOverwrite).OrderBy(r => r.Date).First();
            return new ImportApplyResult(true,
                $"Planificación cargada: {toCreate.Count} práctica(s) nuevas y {toOverwrite.Count} sobrescritas. Quedaron aprobadas, con el material de su receta.",
                toCreate.Count, toOverwrite.Count, batch.Code, analysis, first.Date, first.LaboratoryId);
        }
        catch (Exception ex) when (ex is DbUpdateException or ArgumentException or InvalidOperationException)
        {
            try { await transaction.RollbackAsync(ct); } catch (InvalidOperationException) { }
            _context.ChangeTracker.Clear();
            _logger.LogError(ex, "No se pudo cargar la planificación del semestre de la gestión {ManagementId}", managementId);
            return new ImportApplyResult(false, "No se cargó nada: ocurrió un error al guardar. Vuelva a revisar el archivo e intente de nuevo.", 0, 0, null, null);
        }
    }

    // ───────────────────────────────────────────────────────────── Excel de resultados

    public byte[] BuildResultWorkbook(ImportAnalysis analysis)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add(SheetRows);
        for (var c = 0; c < Headers.Length; c++) sheet.Cells[1, c + 1].Value = Headers[c];
        sheet.Cells[1, Headers.Length + 1].Value = "Resultado";
        sheet.Cells[1, Headers.Length + 2].Value = "Qué corregir";
        sheet.Cells[1, 1, 1, Headers.Length + 2].Style.Font.Bold = true;
        sheet.View.FreezePanes(2, 1);
        foreach (var row in analysis.Rows)
        {
            // Misma fila que en el archivo original, para que los números de fila de los mensajes coincidan.
            var r = row.RowNumber;
            var raw = row.Raw;
            sheet.Cells[r, 1].Value = raw.Management;
            sheet.Cells[r, 2].Value = raw.Subject;
            sheet.Cells[r, 3].Value = raw.Group;
            sheet.Cells[r, 4].Value = raw.Practice;
            sheet.Cells[r, 5].Value = raw.Week;
            if (TryDate(raw.Date, out var date))
            {
                sheet.Cells[r, 6].Value = date;
                sheet.Cells[r, 6].Style.Numberformat.Format = "dd/mm/yyyy";
            }
            else sheet.Cells[r, 6].Value = raw.Date;
            sheet.Cells[r, 7].Value = TryInt(raw.StartPeriod, out var s) ? s : raw.StartPeriod;
            sheet.Cells[r, 8].Value = TryInt(raw.PeriodCount, out var n) ? n : raw.PeriodCount;
            sheet.Cells[r, 9].Value = raw.Laboratory;
            sheet.Cells[r, 10].Value = raw.Teacher;
            sheet.Cells[r, 11].Value = TryInt(raw.Students, out var st) ? st : raw.Students;
            sheet.Cells[r, 12].Value = raw.Observations;
            sheet.Cells[r, 13].Value = row.ResultText;
            sheet.Cells[r, 14].Value = string.Join(" | ", row.Issues.Select(i => $"[{i.Column}] {i.Message}"));
            var color = row.Action switch
            {
                ImportRowAction.Blocked => System.Drawing.Color.FromArgb(0xFD, 0xE2, 0xE7),
                ImportRowAction.NeedsDecision => System.Drawing.Color.FromArgb(0xFF, 0xE9, 0xCC),
                ImportRowAction.Create or ImportRowAction.Overwrite => System.Drawing.Color.FromArgb(0xE8, 0xF8, 0xF4),
                _ => System.Drawing.Color.FromArgb(0xF1, 0xF5, 0xF9)
            };
            sheet.Cells[r, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[r, 13].Style.Fill.BackgroundColor.SetColor(color);
        }
        int[] widths = [12, 38, 10, 52, 12, 13, 12, 12, 30, 34, 12, 30, 36, 110];
        for (var c = 0; c < widths.Length; c++) sheet.Column(c + 1).Width = widths[c];
        return package.GetAsByteArray();
    }
}
