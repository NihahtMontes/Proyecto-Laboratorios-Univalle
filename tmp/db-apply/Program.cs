using Microsoft.Data.SqlClient;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: DbApply <connection-string> <sql-file>");
    return 2;
}

var connectionString = args[0];
var sqlPath = args[1];
var sql = await File.ReadAllTextAsync(sqlPath);

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

await using var command = connection.CreateCommand();
command.CommandText = sql;
command.CommandTimeout = 180;

await using var reader = await command.ExecuteReaderAsync();
var resultSet = 0;
do
{
    resultSet++;
    var fieldCount = reader.FieldCount;
    if (fieldCount <= 0)
    {
        continue;
    }

    Console.WriteLine($"-- Result set {resultSet}");
    for (var i = 0; i < fieldCount; i++)
    {
        Console.Write(i == 0 ? reader.GetName(i) : "\t" + reader.GetName(i));
    }
    Console.WriteLine();

    while (await reader.ReadAsync())
    {
        for (var i = 0; i < fieldCount; i++)
        {
            var value = await reader.IsDBNullAsync(i) ? "" : reader.GetValue(i)?.ToString();
            Console.Write(i == 0 ? value : "\t" + value);
        }
        Console.WriteLine();
    }
}
while (await reader.NextResultAsync());
