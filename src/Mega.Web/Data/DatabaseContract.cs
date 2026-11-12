using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Mega.Web.Data;

public sealed record FieldContract(string Name, string Type, bool Nullable);
public sealed record TableContract(string Name, IReadOnlyList<FieldContract> Fields, IReadOnlyList<string> Key, int ForeignKeys);
public sealed record SchemaObject(string Type, string Name, string Sql);

/// <summary>Contrato verificable a partir de las fuentes originales, sin modificarlas.</summary>
public sealed class DatabaseContract
{
    public const int CurrentVersion = 2;
    public string BaseSql { get; }
    public string UpgradeSql { get; }
    public IReadOnlyList<TableContract> Tables { get; }
    private readonly Dictionary<int, IReadOnlyDictionary<string, SchemaObject>> definitions = [];

    public DatabaseContract(IWebHostEnvironment environment)
    {
        var folder = Path.Combine(environment.ContentRootPath, "Data");
        var schema = File.ReadAllBytes(Path.Combine(folder, "Schema.sql"));
        var model = File.ReadAllBytes(Path.Combine(folder, "Modelo_Datos_MEGA.json"));
        VerifyHash(schema, "b570d8937cdbd9f7299f925db13998694b29a5d3820d6e75fb100eba33d11589");
        VerifyHash(model, "73e1f5b04b3080b627fafa4f60afc27b54fbfe65a4d5d43fbcb2b4ba0e285a34");
        // La copia en memoria excluye solamente las instrucciones de control del script original.
        BaseSql = string.Join('\n', System.Text.Encoding.UTF8.GetString(schema).Split('\n').Where(line =>
            line.Trim() is not ("PRAGMA foreign_keys = ON;" or "BEGIN;" or "COMMIT;" or "PRAGMA foreign_key_check;" or "PRAGMA integrity_check;")));
        UpgradeSql = File.ReadAllText(Path.Combine(folder, "Migrations", "002_Integridad.sql"));
        using var json = JsonDocument.Parse(model);
        Tables = json.RootElement.GetProperty("entities").EnumerateArray().Select(e => new TableContract(
            e.GetProperty("name").GetString()!, e.GetProperty("fields").EnumerateArray().Select(f => new FieldContract(
                f.GetProperty("name").GetString()!, f.GetProperty("type").GetString()!, f.GetProperty("nullable").GetBoolean())).ToArray(),
            e.GetProperty("primary_key").EnumerateArray().Select(k => k.GetString()!).ToArray(),
            e.GetProperty("fields").EnumerateArray().Count(f => f.GetProperty("references").ValueKind != JsonValueKind.Null))).ToArray();
        using var reference = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        reference.Open();
        using var command = reference.CreateCommand();
        command.CommandText = BaseSql;
        command.ExecuteNonQuery();
        definitions[1] = ReadObjects(reference);
        command.CommandText = UpgradeSql;
        command.ExecuteNonQuery();
        definitions[2] = ReadObjects(reference);
    }

    public IReadOnlyDictionary<string, SchemaObject> Objects(int version) => definitions.TryGetValue(version, out var result)
        ? result : throw new InvalidOperationException($"Versión de esquema {version} no compatible.");

    public static Dictionary<string, SchemaObject> ReadObjects(SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT type,name,sql FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' AND sql IS NOT NULL ORDER BY type,name";
        using var reader = command.ExecuteReader();
        var result = new Dictionary<string, SchemaObject>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var item = new SchemaObject(reader.GetString(0), reader.GetString(1), reader.GetString(2));
            result.Add(item.Type + ":" + item.Name, item);
        }
        return result;
    }

    public static string NormalizeSql(string sql) => Regex.Replace(sql, @"\s+", " ").Trim().TrimEnd(';');

    private static void VerifyHash(byte[] data, string expected)
    {
        if (Convert.ToHexStringLower(SHA256.HashData(data)) != expected)
            throw new InvalidOperationException("Las fuentes de esquema no coinciden con su procedencia. Se requiere una migración explícita; no sustituir el modelo original.");
    }
}
