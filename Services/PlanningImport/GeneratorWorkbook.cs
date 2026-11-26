using System.Globalization;
using System.Text.RegularExpressions;
using OfficeOpenXml;

namespace Proyecto_Laboratorios_Univalle.Services.PlanningImport;

/// <summary>Semana académica declarada en la hoja CONFIG del generador ("SEMANA DEL 22/02/2027 AL 27/02/2027").</summary>
public sealed record GeneratorWeek(int Number, DateTime? Start, DateTime? End, string Text);

/// <summary>Materia × grupo habilitado en la hoja MATERIAS del generador.</summary>
public sealed record GeneratorGroup(
    int RowNumber, string SubjectCode, string SubjectName, string Career, int? Semester, string Teacher,
    string Group, int? Students, int? Factor, int? Weekday, string Laboratory, bool Demonstrative);

/// <summary>Práctica programada en la hoja PLANIFICA (una fila = una práctica de un grupo en un día y laboratorio, sin horas).</summary>
public sealed record GeneratorRow(
    int RowNumber, string SubjectCode, string Group, string Career, int? Semester, string Practice, string Description,
    int? Week, DateTime? Date, string Laboratory, string Teacher, int? Students);

/// <summary>
/// Contenido útil del «GENERADOR_PEDIDO» de la Inge (.xlsm con hojas CONFIG, MATERIAS y PLANIFICA).
/// Solo se usa la planificación: las hojas de recetas, PRODUCTO y COSTOMATERIA se ignoran por ahora.
/// </summary>
public sealed class GeneratorWorkbook
{
    public string Title { get; init; } = "";
    /// <summary>Código de gestión del sistema ("2027-1") deducido del título "… GESTIÓN 1-2027"; vacío si no se pudo leer.</summary>
    public string ManagementCode { get; init; } = "";
    public List<GeneratorWeek> Weeks { get; init; } = [];
    public List<GeneratorGroup> Groups { get; init; } = [];
    public List<GeneratorRow> Rows { get; init; } = [];
    /// <summary>Avisos del archivo completo (no de una fila): semanas repetidas, hojas vacías, etc.</summary>
    public List<string> Notices { get; init; } = [];

    /// <summary>Inicio de clases: el lunes de la primera semana declarada.</summary>
    public DateTime? ClassesStart => Weeks.Where(w => w.Start != null).Select(w => w.Start).Min();

    /// <summary>Fin de clases: el último día de la última semana declarada.</summary>
    public DateTime? ClassesEnd => Weeks.Where(w => w.End != null).Select(w => w.End).Max();
}

/// <summary>Lee el generador de la Inge. No consulta la base: solo transforma el Excel.</summary>
public static class GeneratorWorkbookReader
{
    public const string SheetConfig = "CONFIG";
    public const string SheetSubjects = "MATERIAS";
    public const string SheetPlanning = "PLANIFICA";

    private static readonly Regex TitleCode = new(@"(?<sem>[12])\s*-\s*(?<year>20\d{2})", RegexOptions.Compiled);
    private static readonly Regex WeekDates = new(@"(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.Compiled);
    private static readonly Regex ShortLab = new(@"^(?<prefix>[A-Za-z]+)\s*-?\s*(?<num>\d)$", RegexOptions.Compiled);

    /// <summary>True si el libro es el generador (tiene PLANIFICA y MATERIAS) y no la plantilla del sistema.</summary>
    public static bool IsGenerator(ExcelWorkbook workbook) =>
        Find(workbook, SheetPlanning) != null && Find(workbook, SheetSubjects) != null;

    private static ExcelWorksheet? Find(ExcelWorkbook workbook, string name) =>
        workbook.Worksheets.FirstOrDefault(w => PlanningImportService.Norm(w.Name) == PlanningImportService.Norm(name));

    private static string Key(object? header) =>
        new(PlanningImportService.Norm(Convert.ToString(header, CultureInfo.InvariantCulture))
            .Where(c => c is >= 'a' and <= 'z' or >= '0' and <= '9').ToArray());

    private static string Text(object? value) => value switch
    {
        null => "",
        double d => d.ToString(CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ => (Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").Trim()
    };

    private static int? Int(object? value) => value switch
    {
        null => null,
        double d when d == Math.Truncate(d) => (int)d,
        int i => i,
        _ => int.TryParse(Text(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null
    };

    private static DateTime? Date(object? value) => value switch
    {
        DateTime dt => dt.Date,
        double d when d is > 20000 and < 80000 => DateTime.FromOADate(d).Date,
        string s when DateTime.TryParseExact(s.Trim(), ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var p) => p.Date,
        _ => null
    };

    /// <summary>"H-5" → "H-05", "K-1" → "K-01" (como están en el sistema); otros códigos quedan igual.</summary>
    public static string NormalizeLabCode(string? code)
    {
        var text = (code ?? "").Trim().ToUpperInvariant();
        var m = ShortLab.Match(text);
        return m.Success ? $"{m.Groups["prefix"].Value}-0{m.Groups["num"].Value}" : text;
    }

    /// <summary>Día en texto (MARTES, MIÉRCOLES…) → 1 = lunes … 6 = sábado.</summary>
    public static int? Weekday(string? day) => PlanningImportService.Norm(day) switch
    {
        "lunes" => 1, "martes" => 2, "miercoles" => 3, "jueves" => 4, "viernes" => 5, "sabado" => 6, _ => null
    };

    public static GeneratorWorkbook Read(ExcelWorkbook workbook)
    {
        var notices = new List<string>();
        var (title, weeks) = ReadConfig(Find(workbook, SheetConfig), notices);
        var groups = ReadSubjects(Find(workbook, SheetSubjects)!, notices);
        var rows = ReadPlanning(Find(workbook, SheetPlanning)!);
        if (rows.Count == 0)
            throw new ImportFileException($"La hoja «{SheetPlanning}» no tiene ninguna práctica.");

        var match = TitleCode.Match(title);
        var code = match.Success ? $"{match.Groups["year"].Value}-{match.Groups["sem"].Value}" : "";

        // Semanas repetidas: el sistema numera las semanas por fecha, así que solo se avisa.
        foreach (var dup in weeks.Where(w => w.Start != null).GroupBy(w => w.Start).Where(g => g.Count() > 1))
            notices.Add($"En CONFIG las semanas {string.Join(" y ", dup.Select(w => w.Number))} tienen el mismo rango ({dup.First().Text}). "
                + "Se usa la fecha de ejecución de cada práctica y el sistema numera las semanas por fecha, así que la numeración puede correrse respecto del Excel.");

        // Las hojas de recetas, PRODUCTO y COSTOMATERIA no se importan todavía (decisión 2026-11-23): no se revisan.

        return new GeneratorWorkbook { Title = title, ManagementCode = code, Weeks = weeks, Groups = groups, Rows = rows, Notices = notices };
    }

    private static (string Title, List<GeneratorWeek> Weeks) ReadConfig(ExcelWorksheet? sheet, List<string> notices)
    {
        var weeks = new List<GeneratorWeek>();
        if (sheet?.Dimension == null)
        {
            notices.Add($"No se encontró la hoja «{SheetConfig}»: no se puede deducir la gestión ni las semanas.");
            return ("", weeks);
        }
        var title = "";
        var inWeeks = false;
        for (var r = 1; r <= sheet.Dimension.End.Row; r++)
        {
            var a = Text(sheet.Cells[r, 1].Value);
            var b = Text(sheet.Cells[r, 2].Value);
            if (Key(a) == "tituloglobal") title = b;
            if (Key(a) == "semanaacademica") { inWeeks = true; continue; }
            if (!inWeeks) continue;
            if (Int(sheet.Cells[r, 1].Value) is not int number) { if (weeks.Count > 0) inWeeks = false; continue; }
            var dates = WeekDates.Matches(b).Select(m => Date(m.Value)).ToList();
            weeks.Add(new GeneratorWeek(number, dates.ElementAtOrDefault(0), dates.ElementAtOrDefault(1), b));
        }
        return (title, weeks);
    }

    private static Dictionary<string, int> Headers(ExcelWorksheet sheet)
    {
        var map = new Dictionary<string, int>();
        for (var c = 1; c <= sheet.Dimension.End.Column; c++)
        {
            var key = Key(sheet.Cells[1, c].Value);
            if (key.Length > 0 && !map.ContainsKey(key)) map[key] = c;
        }
        return map;
    }

    private static List<GeneratorGroup> ReadSubjects(ExcelWorksheet sheet, List<string> notices)
    {
        var list = new List<GeneratorGroup>();
        if (sheet.Dimension == null) return list;
        var h = Headers(sheet);
        object? V(int r, string key) => h.TryGetValue(key, out var c) ? sheet.Cells[r, c].Value : null;
        for (var r = 2; r <= sheet.Dimension.End.Row; r++)
        {
            var code = Text(V(r, "codigomateria"));
            var group = Text(V(r, "grupo")).ToUpperInvariant();
            if (code.Length == 0 || group.Length == 0) continue;
            if (h.ContainsKey("habilitar") && Key(V(r, "habilitar")) != "si") continue;
            list.Add(new GeneratorGroup(r, code, Text(V(r, "materia")), Text(V(r, "carrera")), Int(V(r, "semestre")),
                Text(V(r, "nombredocente")), group, Int(V(r, "cantestudiantes")), Int(V(r, "factormateria")),
                Weekday(Text(V(r, "dia"))), NormalizeLabCode(Text(V(r, "laboratorio"))), Int(V(r, "demostrativa")) == 1));
        }
        return list;
    }

    private static List<GeneratorRow> ReadPlanning(ExcelWorksheet sheet)
    {
        var list = new List<GeneratorRow>();
        if (sheet.Dimension == null) return list;
        var h = Headers(sheet);
        var required = new[] { ("codigomateria", "CODIGO_MATERIA"), ("grupo", "GRUPO"), ("practica", "PRACTICA"), ("fechaejecucion", "FECHA EJECUCION"), ("laboratorio", "LABORATORIO") };
        var missing = required.Where(x => !h.ContainsKey(x.Item1)).Select(x => x.Item2).ToList();
        if (missing.Count > 0)
            throw new ImportFileException($"A la hoja «{SheetPlanning}» le faltan columnas: {string.Join(", ", missing)}.");
        object? V(int r, string key) => h.TryGetValue(key, out var c) ? sheet.Cells[r, c].Value : null;
        for (var r = 2; r <= sheet.Dimension.End.Row; r++)
        {
            var code = Text(V(r, "codigomateria"));
            var practice = Text(V(r, "practica"));
            if (code.Length == 0 && practice.Length == 0) continue;
            list.Add(new GeneratorRow(r, code, Text(V(r, "grupo")).ToUpperInvariant(), Text(V(r, "carrera")), Int(V(r, "semestre")),
                practice, Text(V(r, "descripciondepractica")), Int(V(r, "semanaacademica")), Date(V(r, "fechaejecucion")),
                NormalizeLabCode(Text(V(r, "laboratorio"))), Text(V(r, "nombredocente")), Int(V(r, "cantestudiantes"))));
            if (list.Count > PlanningImportService.MaxRows)
                throw new ImportFileException($"El archivo tiene más de {PlanningImportService.MaxRows} prácticas. Divídalo en varios archivos.");
        }
        return list;
    }
}

/// <summary>
/// Reparte periodos a las prácticas del generador, que solo trae día y laboratorio (decisión del usuario, 2026-11-23):
/// 1 grupo en el laboratorio ese día → P2–P5; 2 grupos → P2–P5 y P7–P10; 3 grupos → P1–P4, P5–P8 y P9–P12 seguidos
/// (la limpieza es de 10 minutos en el receso). Van en el orden de las filas del Excel. Las filas del mismo
/// laboratorio con el mismo SessionKey (materia y grupo) son una sesión y comparten un bloque. Si el docente o el
/// grupo de estudiantes ya quedó ocupado a esa hora en otro laboratorio, se busca la combinación de bloques del día
/// con menos movidas; si no hay ninguna, la fila que choca queda para corregir a mano en la grilla.
/// </summary>
public static class GeneratorPeriodPlanner
{
    public sealed record Slot(int Start, int Count)
    {
        public int Last => Start + Count - 1;
        public override string ToString() => Count == 1 ? $"P{Start}" : $"P{Start}–P{Last}";
    }

    public sealed record Input(int RowNumber, DateTime Date, string Laboratory, string TeacherKey, string StudentGroupKey, string SessionKey = "");

    public sealed record Result(int RowNumber, Slot? Slot, IReadOnlyList<string> Notes);

    public const int Duration = 4;
    public static readonly Slot Morning = new(2, Duration);
    public static readonly Slot Afternoon = new(7, Duration);
    public static readonly Slot[] Packed = [new(1, Duration), new(5, Duration), new(9, Duration)];

    /// <summary>Bloques por defecto según cuántos grupos caen ese día en el laboratorio.</summary>
    public static IReadOnlyList<Slot> SlotsFor(int groups) => groups switch
    {
        <= 1 => [Morning],
        2 => [Morning, Afternoon],
        _ => Packed
    };

    /// <summary>Todos los bloques candidatos al buscar uno libre, en orden de preferencia.</summary>
    private static readonly Slot[] Candidates = [Morning, Afternoon, .. Packed];

    private static bool Overlaps(Slot a, Slot b) => PlanningSlot.Overlaps(a.Start, a.Count, b.Start, b.Count);

    /// <summary>Tope de nodos explorados al buscar la combinación de bloques de un día (evita explosión combinatoria).</summary>
    private const int MaxNodosBusqueda = 200_000;

    /// <summary>Una sesión: filas del mismo laboratorio que comparten bloque porque tienen el mismo SessionKey.</summary>
    private sealed class Session(string lab, List<Input> rows)
    {
        public string Lab { get; } = lab;
        public List<Input> Rows { get; } = rows;   // ordenadas por fila del Excel
        public int First => Rows[0].RowNumber;
        public string Teacher => Rows[0].TeacherKey;
        public string Group => Rows[0].StudentGroupKey;
    }

    /// <summary>Dos sesiones chocan si comparten docente o grupo de estudiantes y sus bloques se solapan.</summary>
    private static bool Choca(Session a, Slot ba, Session b, Slot bb) =>
        ((a.Teacher.Length > 0 && a.Teacher == b.Teacher) || (a.Group.Length > 0 && a.Group == b.Group)) && Overlaps(ba, bb);

    /// <summary>Comparación lexicográfica de vectores de rangos (desempate de disposiciones y soluciones).</summary>
    private sealed class RangosComparer : IComparer<int[]>
    {
        public static readonly RangosComparer Default = new();
        public int Compare(int[]? x, int[]? y)
        {
            for (var i = 0; i < (x?.Length ?? 0) && i < (y?.Length ?? 0); i++)
                if (x![i] != y![i]) return x[i].CompareTo(y![i]);
            return (x?.Length ?? 0).CompareTo(y?.Length ?? 0);
        }
    }

    public static IReadOnlyDictionary<int, Result> Plan(IEnumerable<Input> inputs)
    {
        var slot = new Dictionary<int, Slot?>();
        var notes = new Dictionary<int, List<string>>();
        void Note(int row, string text) => (notes.TryGetValue(row, out var l) ? l : notes[row] = []).Add(text);

        // Rango de la sesión i del laboratorio si cae en el bloque b: 0 en su bloque por defecto; 1, 2, … en los demás candidatos.
        int Rango(IReadOnlyList<Slot> defecto, int i, Slot b)
        {
            if (b == defecto[i]) return 0;
            var k = 1;
            foreach (var c in Candidates)
            {
                if (c == defecto[i]) continue;
                if (c == b) return k;
                k++;
            }
            return int.MaxValue;
        }

        // Disposiciones de un laboratorio: bloques para sus sesiones ubicables, ordenadas por costo (suma de rangos,
        // desempatado por el vector de rangos). Con 3 ubicables solo caben las permutaciones de los bloques seguidos.
        List<Slot[]> Disposiciones(List<Session> ub, IReadOnlyList<Slot> defecto)
        {
            IEnumerable<Slot[]> Perms()
            {
                for (var i = 0; i < Packed.Length; i++)
                    for (var j = 0; j < Packed.Length; j++)
                        for (var k = 0; k < Packed.Length; k++)
                            if (i != j && i != k && j != k)
                                yield return [Packed[i], Packed[j], Packed[k]];
            }
            IEnumerable<Slot[]> raw = ub.Count switch
            {
                1 => Candidates.Select(c => new[] { c }),
                2 => from a in Candidates from b in Candidates where a != b && !Overlaps(a, b) select new[] { a, b },
                _ => Perms()
            };
            return [.. raw.Select(d => (d, rangos: d.Select((b, i) => Rango(defecto, i, b)).ToArray()))
                .OrderBy(t => t.rangos.Sum()).ThenBy(t => t.rangos, RangosComparer.Default).Select(t => t.d)];
        }

        var all = inputs.ToList();
        foreach (var day in all.GroupBy(i => i.Date.Date))
        {
            // 1) Sesiones: filas del mismo laboratorio y mismo SessionKey no vacío = una sesión; sin SessionKey, cada fila es su propia sesión.
            var sesiones = day.GroupBy(i => (i.Laboratory, Clave: i.SessionKey.Length > 0 ? i.SessionKey : $"#{i.RowNumber}"))
                .Select(g => new Session(g.Key.Laboratory, [.. g.OrderBy(i => i.RowNumber)]))
                .ToList();
            var porLab = sesiones.GroupBy(s => s.Lab).ToDictionary(g => g.Key, g => g.OrderBy(s => s.First).ToList());

            var ubicables = new Dictionary<string, List<Session>>();        // hasta 3 sesiones por laboratorio
            var porDefecto = new Dictionary<string, IReadOnlyList<Slot>>(); // bloques por defecto del laboratorio
            var bloque = new Dictionary<Session, Slot>();                   // bloque final de cada sesión ubicable

            foreach (var (lab, lista) in porLab)
            {
                ubicables[lab] = [.. lista.Take(3)];
                porDefecto[lab] = SlotsFor(ubicables[lab].Count);
                // Del 4.º grupo en adelante no hay bloque: queda para ubicar a mano en la grilla.
                foreach (var s in lista.Skip(ubicables[lab].Count))
                    foreach (var r in s.Rows)
                    {
                        slot[r.RowNumber] = null;
                        Note(r.RowNumber, $"El {day.Key:dd/MM/yyyy} hay {lista.Count} grupos en {lab} y solo caben 3 bloques de 4 periodos: ubique esta práctica a mano en la grilla.");
                    }
            }

            // 2) Elegir la disposición de cada laboratorio. Los unidos por un docente o grupo compartido se resuelven juntos.
            List<string> labs = [.. porLab.Keys];
            var padre = labs.ToDictionary(l => l, l => l);
            string Find(string l) { while (padre[l] != l) l = padre[l] = padre[padre[l]]; return l; }
            void Union(string a, string b) { a = Find(a); b = Find(b); if (a != b) padre[a] = b; }
            var visto = new Dictionary<string, string>();   // "T:docente" / "G:grupo" → primer laboratorio que lo trae
            foreach (var lab in labs)
                foreach (var s in ubicables[lab])
                    foreach (var clave in new[] { s.Teacher.Length > 0 ? "T:" + s.Teacher : null, s.Group.Length > 0 ? "G:" + s.Group : null })
                        if (clave != null)
                        {
                            if (visto.TryGetValue(clave, out var otro)) Union(otro, lab);
                            else visto[clave] = lab;
                        }

            foreach (var comp in labs.GroupBy(Find))
            {
                List<string> compLabs = [.. comp.OrderBy(l => ubicables[l].Min(s => s.First))];
                if (compLabs.Count == 1)
                {
                    var solo = compLabs[0];
                    for (var i = 0; i < ubicables[solo].Count; i++) bloque[ubicables[solo][i]] = porDefecto[solo][i];
                    continue;
                }

                // DFS por laboratorio de la componente: gana la combinación con menor costo (suma de rangos) y, en
                // empate, el vector de rangos por fila inicial. Se poda si el costo parcial ya supera al mejor.
                var disps = compLabs.ToDictionary(l => l, l => Disposiciones(ubicables[l], porDefecto[l]));
                var elegidas = new Slot[compLabs.Count][];
                var asignadas = new List<(Session s, Slot b, int i)>();   // sesiones ya ubicadas de laboratorios anteriores
                var mejorCosto = int.MaxValue;
                var mejorVec = null as int[];
                var mejor = null as Slot[][];
                var nodos = 0;

                void Dfs(int li, int costo)
                {
                    if (costo > mejorCosto) return;
                    if (li == compLabs.Count)
                    {
                        var vec = asignadas.OrderBy(t => t.s.First).Select(t => Rango(porDefecto[t.s.Lab], t.i, t.b)).ToArray();
                        if (costo < mejorCosto || RangosComparer.Default.Compare(vec, mejorVec ?? []) < 0)
                        {
                            mejorCosto = costo;
                            mejorVec = vec;
                            mejor = elegidas.Select(e => (Slot[])e.Clone()).ToArray();
                        }
                        return;
                    }
                    var lab = compLabs[li];
                    var ub = ubicables[lab];
                    var defecto = porDefecto[lab];
                    foreach (var d in disps[lab])
                    {
                        if (++nodos > MaxNodosBusqueda) return;
                        // Se rechaza si alguna sesión choca con una ya ubicada de otro laboratorio.
                        var choca = false;
                        for (var i = 0; i < ub.Count && !choca; i++)
                            foreach (var (o, ob, _) in asignadas)
                                if (Choca(ub[i], d[i], o, ob)) { choca = true; break; }
                        if (choca) continue;
                        var agregadas = ub.Select((s, j) => (s, b: d[j], j)).ToList();
                        asignadas.AddRange(agregadas);
                        elegidas[li] = d;
                        Dfs(li + 1, costo + d.Select((b, i) => Rango(defecto, i, b)).Sum());
                        asignadas.RemoveRange(asignadas.Count - agregadas.Count, agregadas.Count);
                        if (nodos > MaxNodosBusqueda) return;
                    }
                }

                Dfs(0, 0);
                if (mejor != null && nodos <= MaxNodosBusqueda)
                {
                    for (var li = 0; li < compLabs.Count; li++)
                        for (var i = 0; i < ubicables[compLabs[li]].Count; i++)
                            bloque[ubicables[compLabs[li]][i]] = mejor[li][i];
                }
                else
                {
                    // Sin combinación válida (o búsqueda cortada por el tope): bloques por defecto y aviso en la
                    // primera sesión que choque con una anterior.
                    foreach (var lab in compLabs)
                        for (var i = 0; i < ubicables[lab].Count; i++) bloque[ubicables[lab][i]] = porDefecto[lab][i];
                    var puestas = new List<(Session s, Slot b)>();
                    foreach (var s in compLabs.SelectMany(l => ubicables[l]).OrderBy(s => s.First))
                    {
                        var b = bloque[s];
                        var otra = puestas.FirstOrDefault(t => Choca(s, b, t.s, t.b));
                        if (otra.s != null)
                        {
                            var quien = s.Teacher.Length > 0 && s.Teacher == otra.s.Teacher ? "el docente" : "el grupo de estudiantes";
                            foreach (var r in s.Rows)
                                Note(r.RowNumber, $"No se encontró un bloque libre: {quien} ya tiene práctica en {b} (fila {otra.s.First}, {otra.s.Lab}). Corríjalo en la grilla.");
                            break;
                        }
                        puestas.Add((s, b));
                    }
                }
            }

            // 3) Avisos de movidas: sesiones que no quedaron en su bloque por defecto.
            var delDia = ubicables.Values.SelectMany(v => v).ToList();
            foreach (var s in delDia.OrderBy(s => s.First))
            {
                var defecto = porDefecto[s.Lab][ubicables[s.Lab].IndexOf(s)];
                var final = bloque[s];
                if (final == defecto) continue;
                var otra = delDia.FirstOrDefault(o => o != s && Choca(s, defecto, o, bloque[o]));
                var quien = otra != null && s.Teacher.Length > 0 && s.Teacher == otra.Teacher ? "el docente" : "el grupo de estudiantes";
                foreach (var r in s.Rows)
                    Note(r.RowNumber, otra != null
                        ? $"Se movió a {final} (le tocaba {defecto}) para que {quien} no tenga dos prácticas a la vez (fila {otra!.First}, {otra.Lab})."
                        : $"Se movió a {final} (le tocaba {defecto}) al reacomodar el día para evitar un choque de docente.");
            }

            // 4) Día cargado: 3 o más grupos seguidos en el mismo laboratorio.
            foreach (var (lab, lista) in porLab)
                if (lista.Count >= 3)
                    foreach (var s in ubicables[lab])
                        foreach (var r in s.Rows)
                            Note(r.RowNumber, $"Día cargado: {lista.Count} grupos en {lab} el {day.Key:dd/MM/yyyy}; van seguidos con 10 minutos de limpieza entre prácticas ({bloque[s]}).");

            // 5) El bloque de la sesión se reparte entre sus filas, seguidas en el orden del Excel.
            foreach (var s in delDia)
            {
                var b = bloque[s];
                var k = s.Rows.Count;
                int[] tamaños = k == 1 ? [4] : k == 2 ? [2, 2] : k == 3 ? [2, 1, 1] : [1, 1, 1, 1];
                var inicio = b.Start;
                for (var i = 0; i < k; i++)
                {
                    var r = s.Rows[i];
                    if (i < tamaños.Length)
                    {
                        slot[r.RowNumber] = new Slot(inicio, tamaños[i]);
                        inicio += tamaños[i];
                    }
                    else
                    {
                        // Desde la 5.ª práctica del grupo no caben más periodos en el bloque: queda a mano.
                        slot[r.RowNumber] = null;
                        Note(r.RowNumber, $"El {day.Key:dd/MM/yyyy} este grupo tiene {k} prácticas en {s.Lab} y un bloque solo tiene 4 periodos: ubique esta práctica a mano en la grilla.");
                    }
                }
                if (k >= 2)
                    foreach (var r in s.Rows)
                        if (slot[r.RowNumber] is Slot propia)
                            Note(r.RowNumber, $"El grupo tiene {k} prácticas este día en {s.Lab}: comparten su bloque {b} (esta: {propia}).");
            }
        }

        return all.ToDictionary(i => i.RowNumber,
            i => new Result(i.RowNumber, slot.GetValueOrDefault(i.RowNumber), notes.TryGetValue(i.RowNumber, out var l) ? l : []));
    }
}
