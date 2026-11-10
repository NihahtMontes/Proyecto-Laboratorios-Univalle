using System.Text;

namespace Laboratorios.Tests.Practica3.Soporte;

/// <summary>
/// Registro de evidencias de la Práctica 3: cada caso de prueba anota su entrada, el resultado esperado y el
/// obtenido en un CSV por ejercicio (carpeta Practica3-evidencias junto a los binarios de prueba).
/// Las tablas de resultados del informe se generan a partir de estos archivos.
/// </summary>
public static class Evidencia
{
    private static readonly object Gate = new();

    public static string Folder
    {
        get
        {
            var dir = Environment.GetEnvironmentVariable("PRACTICA3_EVIDENCIAS")
                ?? Path.Combine(AppContext.BaseDirectory, "Practica3-evidencias");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static void Registrar(string ejercicio, string caso, string descripcion, string entrada, string esperado, string obtenido, bool cumple)
    {
        var line = string.Join(";", new[] { caso, descripcion, entrada, esperado, obtenido, cumple ? "Cumple" : "No cumple" }.Select(Csv));
        lock (Gate)
        {
            var path = Path.Combine(Folder, ejercicio + ".csv");
            if (!File.Exists(path))
                File.WriteAllText(path, "Caso;Descripción;Entrada;Resultado esperado;Resultado obtenido;Veredicto\n", new UTF8Encoding(true));
            File.AppendAllText(path, line + "\n", Encoding.UTF8);
        }
    }

    private static string Csv(string value)
    {
        value = value.Replace("\r", " ").Replace("\n", " ");
        return value.Contains(';') || value.Contains('"') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
