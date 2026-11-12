using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Mega.Web.Services;

namespace Mega.Web.Data;

public sealed record TableStatus(string Name, int Columns, int ForeignKeys, IReadOnlyList<string> PrimaryKey, long Rows);
public sealed record DatabaseReport(string CheckedAtUtc, int SchemaVersion, string EngineVersion, bool ForeignKeysEnabled,
    string Integrity, IReadOnlyList<TableStatus> Tables, int Views, int Triggers, int Indexes, IReadOnlyList<string> Errors)
{
    public bool Healthy => Errors.Count == 0;
    public bool UpToDate => SchemaVersion == DatabaseContract.CurrentVersion;
    public int ColumnCount => Tables.Sum(t => t.Columns);
    public int ForeignKeyCount => Tables.Sum(t => t.ForeignKeys);
}

public sealed class DatabaseInspector(DatabaseContract contract)
{
    public async Task<DatabaseReport> InspectAsync(SqliteConnection connection, IModel? model = null, SqliteTransaction? transaction = null)
    {
        // Mantiene una vista consistente durante todas las consultas de diagnóstico.
        using var snapshot = transaction is null ? connection.BeginTransaction(deferred: true) : null;
        var tx = transaction ?? snapshot;
        var errors = new List<string>();
        var version = Convert.ToInt32(await ScalarAsync(connection, "PRAGMA user_version", tx));
        var engine = Convert.ToString(await ScalarAsync(connection, "SELECT sqlite_version()", tx))!;
        var foreignKeys = Convert.ToInt64(await ScalarAsync(connection, "PRAGMA foreign_keys", tx)) == 1;
        if (!foreignKeys) errors.Add("Las claves foráneas no están activadas en esta conexión.");
        var integrityRows = await StringsAsync(connection, "PRAGMA integrity_check", tx);
        var integrity = string.Join("; ", integrityRows.Take(5));
        if (integrityRows.Count != 1 || integrityRows[0] != "ok") errors.Add("La comprobación de integridad física no fue satisfactoria.");
        var fkErrors = await StringsAsync(connection, "PRAGMA foreign_key_check", tx);
        if (fkErrors.Count > 0) errors.Add($"Hay {fkErrors.Count} referencias foráneas inválidas.");
        var objects = DatabaseContract.ReadObjects(connection, tx);
        var tables = new List<TableStatus>();
        if (version is < 1 or > DatabaseContract.CurrentVersion)
        {
            errors.Add($"Versión de esquema {version} no compatible.");
            return new(Formats.Now(), version, engine, foreignKeys, integrity, tables, 0, 0, 0, errors);
        }
        var expected = contract.Objects(version);
        foreach (var (key, definition) in expected)
        {
            if (!objects.TryGetValue(key, out var actual)) errors.Add($"Falta {definition.Type}: {definition.Name}.");
            else if (DatabaseContract.NormalizeSql(definition.Sql) != DatabaseContract.NormalizeSql(actual.Sql))
                errors.Add($"La definición de {definition.Name} difiere del esquema {version}.");
        }
        foreach (var extra in objects.Where(x => !expected.ContainsKey(x.Key) && x.Value.Type != "index"))
            errors.Add($"Objeto no previsto: {extra.Value.Type} {extra.Value.Name}.");

        foreach (var table in contract.Tables)
        {
            if (!objects.ContainsKey("table:" + table.Name)) continue;
            using var columns = connection.CreateCommand();
            columns.Transaction = tx;
            columns.CommandText = $"PRAGMA table_info({Quote(table.Name)})";
            var fields = new List<(string Name, string Type, bool Nullable, int Key)>();
            using (var reader = await columns.ExecuteReaderAsync())
                while (await reader.ReadAsync()) fields.Add((reader.GetString(1), reader.GetString(2), reader.GetInt64(3) == 0, reader.GetInt32(5)));
            var key = fields.Where(f => f.Key > 0).OrderBy(f => f.Key).Select(f => f.Name).ToArray();
            var foreignCount = (await StringsAsync(connection, $"PRAGMA foreign_key_list({Quote(table.Name)})", tx)).Count;
            var rows = Convert.ToInt64(await ScalarAsync(connection, $"SELECT COUNT(*) FROM {Quote(table.Name)}", tx));
            tables.Add(new(table.Name, fields.Count, foreignCount, key, rows));
            if (!key.SequenceEqual(table.Key) || foreignCount != table.ForeignKeys || fields.Count != table.Fields.Count ||
                table.Fields.Any(f => !fields.Any(a => a.Name == f.Name && a.Type == f.Type && a.Nullable == f.Nullable)))
                errors.Add($"Columnas, nulabilidad o claves de {table.Name} no coinciden con el modelo.");
            if (model is not null) CheckMapping(model, table, errors);
        }
        // Las consultas relacionales requieren que el esquema original conserve su estructura.
        if (errors.Count == 0)
        {
            foreach (var (description, sql) in ConsistencyQueries)
                if (Convert.ToInt64(await ScalarAsync(connection, sql, tx)) > 0) errors.Add(description);
            using var receipts = connection.CreateCommand();
            receipts.Transaction = tx;
            receipts.CommandText = "SELECT comprobanteSnapshot,comprobanteHash FROM Venta";
            using var reader = await receipts.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (Formats.Hash(reader.GetString(0)) != reader.GetString(1)) { errors.Add("Un comprobante no coincide con su hash conservado."); break; }
        }
        var indexes = Convert.ToInt32(await ScalarAsync(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type='index'", tx));
        return new(Formats.Now(), version, engine, foreignKeys, integrity, tables, objects.Values.Count(o => o.Type == "view"),
            objects.Values.Count(o => o.Type == "trigger"), indexes, errors);
    }

    public static void RequireHealthy(DatabaseReport report)
    {
        if (!report.Healthy) throw new InvalidOperationException("La base requiere revisión: " + string.Join(" ", report.Errors.Take(5)));
    }

    private static void CheckMapping(IModel model, TableContract table, List<string> errors)
    {
        var entity = model.GetEntityTypes().SingleOrDefault(e => e.GetTableName() == table.Name);
        if (entity is null) { errors.Add($"Falta mapeo de {table.Name} en la aplicación."); return; }
        var store = StoreObjectIdentifier.Table(table.Name, null);
        var key = entity.FindPrimaryKey()?.Properties.Select(p => p.GetColumnName(store)).ToArray() ?? [];
        if (!key.SequenceEqual(table.Key) || entity.GetProperties().Count() != table.Fields.Count || entity.GetForeignKeys().Count() != table.ForeignKeys ||
            table.Fields.Any(f => !entity.GetProperties().Any(p => p.GetColumnName(store) == f.Name && p.IsNullable == f.Nullable &&
                (f.Type == "TEXT" ? p.ClrType == typeof(string) : (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(long)))))
            errors.Add($"El mapeo de columnas, claves o relaciones de {table.Name} es incompatible.");
    }

    internal static async Task<object?> ScalarAsync(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand(); command.CommandText = sql; command.Transaction = transaction;
        return await command.ExecuteScalarAsync();
    }
    private static async Task<List<string>> StringsAsync(SqliteConnection connection, string sql, SqliteTransaction? transaction)
    {
        using var command = connection.CreateCommand(); command.CommandText = sql; command.Transaction = transaction;
        using var reader = await command.ExecuteReaderAsync(); var results = new List<string>();
        while (await reader.ReadAsync()) results.Add(reader.GetValue(0).ToString()!);
        return results;
    }
    private static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

    private static readonly (string Description, string Sql)[] ConsistencyQueries =
    [
        ("Hay vigencias de precio superpuestas.", "SELECT COUNT(*) FROM PrecioProducto a JOIN PrecioProducto b ON a.presentacionId=b.presentacionId AND a.id<b.id WHERE (a.vigenteHasta IS NULL OR b.vigenteDesde<a.vigenteHasta) AND (b.vigenteHasta IS NULL OR a.vigenteDesde<b.vigenteHasta)"),
        ("Hay movimientos con escala o lote de otro producto.", "SELECT COUNT(*) FROM MovimientoInventario m JOIN Producto p ON p.id=m.productoId LEFT JOIN LoteExistencia l ON l.id=m.loteId WHERE m.escala<>p.escalaCantidad OR (m.loteId IS NOT NULL AND l.productoId<>m.productoId)"),
        ("Hay detalles de venta incompatibles con producto o presentación.", "SELECT COUNT(*) FROM VentaDetalle d JOIN Presentacion pr ON pr.id=d.presentacionId JOIN Producto p ON p.id=d.productoId WHERE pr.productoId<>d.productoId OR d.escalaCantidad<>p.escalaCantidad OR d.unidadBaseHistorica<>p.unidadBase"),
        ("Hay movimientos de venta que no corresponden a su operación.", "SELECT COUNT(*) FROM MovimientoInventario m JOIN VentaDetalle d ON d.id=m.ventaDetalleId JOIN Venta v ON v.id=d.ventaId JOIN OperacionInventario o ON o.id=m.operacionId WHERE m.productoId<>d.productoId OR m.operacionId<>v.operacionId OR o.tipo<>'venta' OR m.cantidadEscalada>=0"),
        ("Hay ventas cuyos detalles o pagos no cubren sus totales.", "SELECT COUNT(*) FROM Venta v WHERE v.totalCentavos<>COALESCE((SELECT SUM(d.totalCentavos) FROM VentaDetalle d WHERE d.ventaId=v.id),0) OR v.subtotalCentavos<>COALESCE((SELECT SUM(d.subtotalCentavos) FROM VentaDetalle d WHERE d.ventaId=v.id),0) OR v.impuestoCentavos<>COALESCE((SELECT SUM(d.impuestoCentavos) FROM VentaDetalle d WHERE d.ventaId=v.id),0) OR v.descuentoCentavos<>COALESCE((SELECT SUM(d.descuentoCentavos) FROM VentaDetalle d WHERE d.ventaId=v.id),0) OR v.totalCentavos<>COALESCE((SELECT SUM(p.importeCentavos) FROM Pago p WHERE p.ventaId=v.id),0)"),
        ("Hay líneas cuya salida de inventario no coincide con la cantidad base.", "SELECT COUNT(*) FROM VentaDetalle d WHERE d.cantidadBaseEscalada<>-COALESCE((SELECT SUM(m.cantidadEscalada) FROM MovimientoInventario m WHERE m.ventaDetalleId=d.id),0)"),
        ("Hay cobros de caja incompatibles con venta o pago efectivo.", "SELECT COUNT(*) FROM MovimientoCaja m JOIN Venta v ON v.id=m.ventaId JOIN TurnoCaja t ON t.id=m.turnoId WHERE m.tipo='cobro_efectivo' AND (m.turnoId<>v.turnoId OR m.operacionId<>v.operacionId OR m.registradoPor<>t.operadorId OR m.importeCentavos<>COALESCE((SELECT SUM(p.importeCentavos) FROM Pago p WHERE p.ventaId=v.id AND p.medio='efectivo'),0))"),
        ("Hay cierres cuyo esperado difiere del fondo y sus movimientos.", "SELECT COUNT(*) FROM TurnoCaja t WHERE t.estado='Cerrado' AND t.efectivoEsperadoCierreCentavos<>t.fondoRealCentavos+COALESCE((SELECT SUM(m.importeCentavos) FROM MovimientoCaja m WHERE m.turnoId=t.id),0)")
    ];
}
