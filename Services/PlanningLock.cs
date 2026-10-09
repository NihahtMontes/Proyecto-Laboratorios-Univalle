using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Proyecto_Laboratorios_Univalle.Data;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Bloqueo exclusivo de recursos de la agenda (planificación de origen, ambiente + día de destino) durante la
/// transacción que mueve una planificación. Protege contra la doble reserva concurrente: dos personas que confirman a la
/// vez el mismo ambiente y día se serializan, y la segunda revalida ya con la primera guardada.
/// </summary>
public interface IPlanningLock
{
    /// <summary>
    /// Toma los bloqueos en orden alfabético (evita interbloqueos). Debe llamarse dentro de una transacción abierta del
    /// DbContext: se liberan al confirmar o revertir. Devuelve false si algún recurso no se obtuvo a tiempo.
    /// </summary>
    Task<bool> TryAcquireAsync(IEnumerable<string> resources, CancellationToken ct = default);
}

public static class PlanningLockResources
{
    public const string BusyMessage = "Otro usuario está modificando esta agenda; intente nuevamente";

    public static string Planning(int planningId) => $"planning:{planningId}";

    // El estado académico es propio de una gestión: suspender y reservar el mismo día deben serializarse.
    public static string ManagementDay(int managementId, DateTime date) =>
        $"management-day:{managementId}:{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

    public static string DayRecovery(int recoveryId) => $"day-recovery:{recoveryId}";

    public static string ManagementCalendar(int managementId) => $"management-calendar:{managementId}";

    // Los choques de docente son advertencias, pero su lectura debe reflejar las confirmaciones previas.
    public static string TeacherDay(int teacherId, DateTime date) =>
        $"teacher-day:{teacherId}:{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

    public static string LaboratoryDay(int laboratoryId, DateTime date) =>
        $"lab:{laboratoryId}:{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

    /// <summary>Recursos sin repetir y en orden alfabético ordinal.</summary>
    public static IReadOnlyList<string> Ordered(IEnumerable<string> resources) =>
        resources.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.Ordinal).OrderBy(r => r, StringComparer.Ordinal).ToList();
}

/// <summary>Implementación SQL Server con sp_getapplock (modo Exclusive, dueño Transaction, espera máxima de 10 s).</summary>
public sealed class SqlServerPlanningLock : IPlanningLock
{
    private const int LockTimeoutMs = 10000;

    private readonly ApplicationDbContext _context;

    public SqlServerPlanningLock(ApplicationDbContext context) => _context = context;

    public async Task<bool> TryAcquireAsync(IEnumerable<string> resources, CancellationToken ct = default)
    {
        var transaction = _context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("El bloqueo de la agenda requiere una transacción abierta.");
        var dbTransaction = transaction.GetDbTransaction();
        var connection = _context.Database.GetDbConnection();

        foreach (var resource in PlanningLockResources.Ordered(resources))
        {
            await using var command = connection.CreateCommand();
            command.Transaction = dbTransaction;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "sp_getapplock";
            command.CommandTimeout = LockTimeoutMs / 1000 + 5;

            AddParameter(command, "@Resource", DbType.String, resource);
            AddParameter(command, "@LockMode", DbType.String, "Exclusive");
            AddParameter(command, "@LockOwner", DbType.String, "Transaction");
            AddParameter(command, "@LockTimeout", DbType.Int32, LockTimeoutMs);
            var result = command.CreateParameter();
            result.ParameterName = "@Result";
            result.DbType = DbType.Int32;
            result.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(result);

            await command.ExecuteNonQueryAsync(ct);
            // 0 = concedido, 1 = concedido tras esperar; negativo = tiempo agotado, cancelado, interbloqueo o error.
            if (result.Value is not int code || code < 0)
                return false;
        }

        return true;
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

/// <summary>Sin efecto: para pruebas con EF InMemory (no hay servidor que coordine bloqueos).</summary>
public sealed class NoOpPlanningLock : IPlanningLock
{
    public Task<bool> TryAcquireAsync(IEnumerable<string> resources, CancellationToken ct = default) => Task.FromResult(true);
}

/// <summary>Rollback que no enmascara el error original si la transacción ya no está activa.</summary>
public static class TransactionExtensions
{
    public static async Task SafeRollbackAsync(this Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken ct = default)
    {
        try
        {
            await transaction.RollbackAsync(ct);
        }
        catch (InvalidOperationException)
        {
            // La transacción ya no está activa (p. ej. la conexión la revirtió): no hay nada más que deshacer.
        }
    }
}
