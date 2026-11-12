using System.Text.Json;
using Mega.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Data;

/// <summary>Crea o actualiza el esquema de forma atómica; nunca recrea una base existente.</summary>
public sealed class DatabaseSchema(MegaDbContext db, DatabaseContract contract, DatabaseInspector inspector, ILogger<DatabaseSchema> logger)
{
    public string? LastBackupPath { get; private set; }

    public async Task EnsureAsync(bool demo)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        var version = Convert.ToInt32(await DatabaseInspector.ScalarAsync(connection, "PRAGMA user_version"));
        if (version is < 0 or > DatabaseContract.CurrentVersion) throw new InvalidOperationException($"Versión de esquema {version} no compatible; no se modificó la base.");
        if (version == 0)
        {
            using var creation = connection.BeginTransaction();
            version = Convert.ToInt32(await DatabaseInspector.ScalarAsync(connection, "PRAGMA user_version", creation));
            if (version == 0)
            {
                var objects = Convert.ToInt64(await DatabaseInspector.ScalarAsync(connection, "SELECT COUNT(*) FROM sqlite_master WHERE name NOT LIKE 'sqlite_%'", creation));
                if (objects != 0) throw new InvalidOperationException("La base tiene objetos sin versión reconocida. Se conserva para revisión; no se inicializa encima.");
                await ExecuteAsync(connection, creation, contract.BaseSql);
                await ExecuteAsync(connection, creation, contract.UpgradeSql);
                await ExecuteAsync(connection, creation, $"PRAGMA user_version={DatabaseContract.CurrentVersion}");
                DatabaseInspector.RequireHealthy(await inspector.InspectAsync(connection, db.Model, creation));
            }
            creation.Commit();
            version = Convert.ToInt32(await DatabaseInspector.ScalarAsync(connection, "PRAGMA user_version"));
        }
        DatabaseInspector.RequireHealthy(await inspector.InspectAsync(connection, db.Model));
        await ValidateModeAsync(connection, demo);
        if (version == DatabaseContract.CurrentVersion) return;

        // La copia es consistente incluso si la aplicación anterior mantiene una conexión abierta.
        LastBackupPath = await BackupBeforeMigrationAsync(connection, version);
        using var transaction = connection.BeginTransaction();
        version = Convert.ToInt32(await DatabaseInspector.ScalarAsync(connection, "PRAGMA user_version", transaction));
        if (version == 1)
        {
            DatabaseInspector.RequireHealthy(await inspector.InspectAsync(connection, db.Model, transaction));
            await ExecuteAsync(connection, transaction, contract.UpgradeSql);
            await ExecuteAsync(connection, transaction, $"PRAGMA user_version={DatabaseContract.CurrentVersion}");
            using var audit = connection.CreateCommand();
            audit.Transaction = transaction;
            audit.CommandText = "INSERT INTO EventoAuditoria(fecha,origen,accion,entidad,claveOperacion,resultado,datosCambio,versionFormato) VALUES($date,'tarea_automatica','Actualizar esquema','BaseDatos',$key,'exito',$data,1)";
            audit.Parameters.AddWithValue("$date", Formats.Now());
            audit.Parameters.AddWithValue("$key", "esquema-2-" + Guid.NewGuid());
            audit.Parameters.AddWithValue("$data", Formats.Json(new { versionFormato = 1, versionAnterior = 1, versionNueva = 2, servicio = "Migrador", copiaPrevia = Path.GetFileName(LastBackupPath) }));
            await audit.ExecuteNonQueryAsync();
        }
        else if (version != DatabaseContract.CurrentVersion) throw new InvalidOperationException("La versión cambió durante la actualización. No se aplicaron cambios parciales.");
        DatabaseInspector.RequireHealthy(await inspector.InspectAsync(connection, db.Model, transaction));
        transaction.Commit();
        logger.LogInformation("Base actualizada al esquema {Version}. Copia previa conservada en {Backup}", DatabaseContract.CurrentVersion, LastBackupPath);
    }

    public static async Task ValidateModeAsync(SqliteConnection connection, bool demo)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT datosCambio FROM EventoAuditoria WHERE claveOperacion='inicializacion' ORDER BY id LIMIT 1";
        var raw = await command.ExecuteScalarAsync();
        if (raw is string data)
        {
            using var metadata = JsonDocument.Parse(data);
            if (!metadata.RootElement.TryGetProperty("demostracion", out var mode) || mode.GetBoolean() != demo)
                throw new InvalidOperationException("La base pertenece a otro modo de operación. Selecciona una base separada; no se convierten datos ficticios en reales.");
        }
        else if (Convert.ToInt64(await DatabaseInspector.ScalarAsync(connection, "SELECT COUNT(*) FROM Usuario")) > 0)
            throw new InvalidOperationException("La base contiene cuentas sin un registro de inicialización verificable; requiere revisión.");
    }

    private async Task<string> BackupBeforeMigrationAsync(SqliteConnection source, int version)
    {
        var folder = Path.Combine(Path.GetDirectoryName(source.DataSource)!, "PreMigration");
        Directory.CreateDirectory(folder);
        var name = $"{Path.GetFileNameWithoutExtension(source.DataSource)}.v{version}.{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}.{Guid.NewGuid():N}.db";
        var path = Path.Combine(folder, name);
        using var backup = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true, Pooling = false }.ToString());
        await backup.OpenAsync();
        source.BackupDatabase(backup);
        DatabaseInspector.RequireHealthy(await inspector.InspectAsync(backup));
        return path;
    }

    private static async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
