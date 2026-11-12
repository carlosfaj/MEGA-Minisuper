using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;

namespace Mega.Tests;

public sealed class DatabaseTests
{
    private sealed class Fixture : IDisposable
    {
        public MegaFactory Factory { get; } = new();
        public IServiceScope Scope { get; }
        public string Folder { get; } = Path.Combine(Path.GetTempPath(), "mega-db-" + Guid.NewGuid().ToString("N"));
        public MegaDbContext Db => Scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        public Operations Op => Scope.ServiceProvider.GetRequiredService<Operations>();
        public DatabaseContract Contract => Scope.ServiceProvider.GetRequiredService<DatabaseContract>();
        public DatabaseInspector Inspector => Scope.ServiceProvider.GetRequiredService<DatabaseInspector>();
        public Fixture() { Scope = Factory.Services.CreateScope(); Directory.CreateDirectory(Folder); }
        public async Task<SqliteConnection> ConnectionAsync()
        {
            await Db.Database.OpenConnectionAsync();
            return (SqliteConnection)Db.Database.GetDbConnection();
        }
        public async Task<long> SaleAsync()
        {
            await Op.OpenCashAsync(2, "480", "Fondo menor para pruebas");
            List<LineInput> lines = [new(1, "peso_manual", "2.000"), new(2, "unidad", "1")];
            var quote = await Op.QuoteAsync(2, lines);
            return await Op.ConfirmAsync(2, new(Guid.NewGuid().ToString(), quote.Hash, "100", false, lines));
        }
        public async Task<string> LegacyCopyAsync()
        {
            var path = Path.Combine(Folder, "legacy.db");
            using var target = Open(path);
            (await ConnectionAsync()).BackupDatabase(target);
            using var command = target.CreateCommand();
            foreach (var (key, item) in Contract.Objects(2).Where(x => !Contract.Objects(1).ContainsKey(x.Key)))
            {
                command.CommandText = $"DROP {item.Type} \"{item.Name}\"";
                command.ExecuteNonQuery();
            }
            command.CommandText = "PRAGMA user_version=1"; command.ExecuteNonQuery();
            return path;
        }
        public MegaDbContext Context(string path) => new(new DbContextOptionsBuilder<MegaDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true, Pooling = false }.ToString()).Options);
        public DatabaseSchema Migrator(MegaDbContext db, DatabaseContract? contract = null) => new(db, contract ?? Contract,
            contract is null ? Inspector : new DatabaseInspector(contract), NullLogger<DatabaseSchema>.Instance);
        public void Dispose()
        {
            Scope.Dispose(); Factory.Dispose();
            var full = Path.GetFullPath(Folder);
            var expectedParent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (full.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith("mega-db-"))
                Directory.Delete(full, recursive: true);
        }
    }
    private static SqliteConnection Open(string path, bool readOnly = false)
    {
        var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true, Pooling = false,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate }.ToString()); c.Open(); return c;
    }
    private static async Task<object?> Scalar(SqliteConnection c, string sql)
    {
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; return await cmd.ExecuteScalarAsync();
    }

    [Fact]
    public async Task complete_schema_and_ef_mapping_match_39_tables_499_columns_103_relations()
    {
        using var f = new Fixture();
        var report = await f.Inspector.InspectAsync(await f.ConnectionAsync(), f.Db.Model);
        Assert.True(report.Healthy, string.Join("; ", report.Errors)); Assert.True(report.UpToDate);
        Assert.Equal(39, report.Tables.Count); Assert.Equal(499, report.ColumnCount); Assert.Equal(103, report.ForeignKeyCount);
        Assert.Equal(2, report.Views); Assert.Equal(21, report.Triggers); Assert.Equal(159, report.Indexes);
        Assert.Equal(new[] { "promocionId", "presentacionId" }, report.Tables.Single(t => t.Name == "PromocionProducto").PrimaryKey);
        Assert.Equal(new[] { "productoId", "impuestoId", "vigenteDesde" }, report.Tables.Single(t => t.Name == "ProductoImpuesto").PrimaryKey);
    }

    [Fact]
    public async Task composite_keys_allow_multiple_products_in_one_promotion_and_multiple_tax_periods()
    {
        using var f = new Fixture(); var now = Formats.Now();
        var promotion = new Promocion { Nombre = "Prueba sin uso comercial", Tipo = "porcentaje", ValorEscalado = 10, EscalaValor = 0,
            Inicio = "2030-01-01T00:00:00.000Z", Fin = "2031-01-01T00:00:00.000Z", Activa = 0, CreadoPor = 1, CreadoEn = now, Version = 1 };
        var tax = new Impuesto { Nombre = "Condición ficticia", TasaEscalada = 0, EscalaTasa = 0, VigenteDesde = now, EsExencion = 1,
            MotivoExencion = "Solo prueba técnica", Activo = 0, CreadoPor = 1, CreadoEn = now };
        f.Db.AddRange(promotion, tax); await f.Db.SaveChangesAsync();
        f.Db.AddRange(new PromocionProducto { PromocionId = promotion.Id, PresentacionId = 1, ReglaAplicacion = "exclusiva", Prioridad = 1 },
            new PromocionProducto { PromocionId = promotion.Id, PresentacionId = 2, ReglaAplicacion = "exclusiva", Prioridad = 2 });
        f.Db.AddRange(new ProductoImpuesto { ProductoId = 1, ImpuestoId = tax.Id, VigenteDesde = "2030-01-01", VigenteHasta = "2031-01-01" },
            new ProductoImpuesto { ProductoId = 1, ImpuestoId = tax.Id, VigenteDesde = "2031-01-01" });
        await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
        var first = await f.Db.Set<PromocionProducto>().FindAsync(promotion.Id, 1L);
        var second = await f.Db.Set<PromocionProducto>().FindAsync(promotion.Id, 2L);
        Assert.NotNull(first); Assert.NotNull(second); Assert.NotSame(first, second);
        Assert.Equal(2, await f.Db.Set<ProductoImpuesto>().CountAsync());
        DatabaseInspector.RequireHealthy(await f.Inspector.InspectAsync(await f.ConnectionAsync(), f.Db.Model));
    }

    [Fact]
    public async Task upgrade_preserves_legacy_sales_and_makes_a_valid_previous_copy()
    {
        using var f = new Fixture(); await f.SaleAsync();
        var legacy = await f.LegacyCopyAsync(); using var context = f.Context(legacy);
        var snapshot = (await context.Set<Venta>().SingleAsync()).ComprobanteSnapshot;
        var before = await context.Set<EventoAuditoria>().CountAsync();
        var migrator = f.Migrator(context); await migrator.EnsureAsync(demo: true);
        Assert.NotNull(migrator.LastBackupPath); Assert.True(File.Exists(migrator.LastBackupPath));
        using var backup = Open(migrator.LastBackupPath!, readOnly: true);
        var old = await f.Inspector.InspectAsync(backup); Assert.True(old.Healthy); Assert.Equal(1, old.SchemaVersion);
        var report = await f.Inspector.InspectAsync((SqliteConnection)context.Database.GetDbConnection(), context.Model);
        Assert.True(report.Healthy); Assert.Equal(2, report.SchemaVersion);
        Assert.Equal(1, await context.Set<Venta>().CountAsync()); Assert.Equal(1, await context.Set<Pago>().CountAsync());
        Assert.Equal(2, await context.Set<VentaDetalle>().CountAsync()); Assert.Equal(snapshot, (await context.Set<Venta>().SingleAsync()).ComprobanteSnapshot);
        Assert.Equal(before + 1, await context.Set<EventoAuditoria>().CountAsync());
        Assert.Equal(8600, (await context.Set<ResumenCajaVista>().SingleAsync()).NetoCentavos);
        Assert.Equal(48000, (await context.Set<ExistenciaVista>().SingleAsync(x => x.ProductoId == 1)).CantidadEscalada);
        var operations = new Operations(context, f.Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());
        List<LineInput> next = [new(2, "unidad", "1")];
        var quote = await operations.QuoteAsync(2, next);
        await operations.ConfirmAsync(2, new(Guid.NewGuid().ToString(), quote.Hash, "50", false, next));
        Assert.Equal(2, await context.Set<Venta>().CountAsync());
        Assert.Equal(60400, (await context.Set<ResumenCajaVista>().SingleAsync()).EsperadoCentavos);
    }

    [Fact]
    public async Task repeated_initialization_does_not_duplicate_seed_or_migration_events()
    {
        using var f = new Fixture(); var events = await f.Db.Set<EventoAuditoria>().CountAsync();
        await f.Scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        await f.Scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        Assert.Equal(5, await f.Db.Set<Usuario>().CountAsync()); Assert.Equal(6, await f.Db.Set<Producto>().CountAsync());
        Assert.Equal(events, await f.Db.Set<EventoAuditoria>().CountAsync());
    }

    [Fact]
    public async Task unrecognized_version_or_unversioned_existing_data_is_never_overwritten()
    {
        using var f = new Fixture(); var path = Path.Combine(f.Folder, "unknown.db");
        using (var connection = Open(path))
        {
            await Scalar(connection, "CREATE TABLE OtroDato(id INTEGER PRIMARY KEY, valor TEXT); INSERT INTO OtroDato VALUES(1,'conservar'); PRAGMA user_version=99");
        }
        using var context = f.Context(path);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Migrator(context).EnsureAsync(true));
        using (var c = Open(path)) { Assert.Equal("conservar", await Scalar(c, "SELECT valor FROM OtroDato")); await Scalar(c, "PRAGMA user_version=0"); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Migrator(context).EnsureAsync(true));
        using var check = Open(path); Assert.Equal("conservar", await Scalar(check, "SELECT valor FROM OtroDato"));
        Assert.Equal(1L, await Scalar(check, "SELECT COUNT(*) FROM sqlite_master WHERE type='table'"));
    }

    [Fact]
    public async Task inspector_detects_missing_guards_and_does_not_repair_them_silently()
    {
        using var f = new Fixture(); var c = await f.ConnectionAsync();
        await Scalar(c, "DROP TRIGGER tr_EventoAuditoria_append_delete");
        var report = await f.Inspector.InspectAsync(c, f.Db.Model);
        Assert.False(report.Healthy); Assert.Contains(report.Errors, e => e.Contains("tr_EventoAuditoria_append_delete"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Scope.ServiceProvider.GetRequiredService<DatabaseSchema>().EnsureAsync(true));
    }

    [Fact]
    public async Task overlapping_legacy_prices_block_upgrade_before_modifying_data()
    {
        using var f = new Fixture(); var legacy = await f.LegacyCopyAsync();
        using (var c = Open(legacy)) await Scalar(c, "INSERT INTO PrecioProducto(presentacionId,precioCentavos,vigenteDesde,creadoPor,creadoEn) VALUES(1,2500,'2100-01-01T00:00:00.000Z',1,'2100-01-01T00:00:00.000Z')");
        using var context = f.Context(legacy); var migrator = f.Migrator(context);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => migrator.EnsureAsync(true));
        Assert.Contains("superpuestas", exception.Message); Assert.Null(migrator.LastBackupPath);
        using var check = Open(legacy); Assert.Equal(1L, await Scalar(check, "PRAGMA user_version"));
        Assert.Equal(7L, await Scalar(check, "SELECT COUNT(*) FROM PrecioProducto"));
    }

    [Fact]
    public async Task failed_migration_rolls_back_objects_version_and_rows()
    {
        using var f = new Fixture(); var legacy = await f.LegacyCopyAsync();
        var source = f.Scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var custom = Path.Combine(f.Folder, "contract"); Directory.CreateDirectory(Path.Combine(custom, "Data", "Migrations"));
        File.Copy(Path.Combine(source, "Data", "Schema.sql"), Path.Combine(custom, "Data", "Schema.sql"));
        File.Copy(Path.Combine(source, "Data", "Modelo_Datos_MEGA.json"), Path.Combine(custom, "Data", "Modelo_Datos_MEGA.json"));
        await File.WriteAllTextAsync(Path.Combine(custom, "Data", "Migrations", "002_Integridad.sql"), f.Contract.UpgradeSql + "\nUPDATE Producto SET escalaCantidad=2;");
        var contract = new DatabaseContract(new TestEnvironment { ContentRootPath = custom });
        using var context = f.Context(legacy); var migrator = f.Migrator(context, contract);
        await Assert.ThrowsAsync<SqliteException>(() => migrator.EnsureAsync(true));
        using var check = Open(legacy); Assert.Equal(1L, await Scalar(check, "PRAGMA user_version"));
        Assert.Equal(0L, await Scalar(check, "SELECT COUNT(*) FROM sqlite_master WHERE type IN ('view','trigger')"));
        Assert.True((await f.Inspector.InspectAsync(check)).Healthy); Assert.True(File.Exists(migrator.LastBackupPath));
    }

    [Fact]
    public async Task readonly_diagnosis_keeps_file_and_business_records_unchanged()
    {
        using var f = new Fixture(); var path = await f.LegacyCopyAsync();
        var hash = SHA256.HashData(await File.ReadAllBytesAsync(path));
        using (var c = Open(path, readOnly: true)) Assert.True((await f.Inspector.InspectAsync(c)).Healthy);
        Assert.Equal(hash, SHA256.HashData(await File.ReadAllBytesAsync(path)));
    }

    [Theory]
    [InlineData("UPDATE EventoAuditoria SET accion='alterado' WHERE id=1", "auditoria")]
    [InlineData("DELETE FROM EventoAuditoria WHERE id=1", "auditoria")]
    [InlineData("UPDATE MovimientoInventario SET cantidadEscalada=1 WHERE id=1", "cantidad")]
    [InlineData("DELETE FROM MovimientoInventario WHERE id=1", "inventario")]
    public async Task database_rejects_mutation_of_audit_and_stock_history(string sql, string message)
    {
        using var f = new Fixture(); var c = await f.ConnectionAsync();
        var error = await Assert.ThrowsAsync<SqliteException>(() => Scalar(c, sql)); Assert.Contains(message, error.Message);
    }

    [Theory]
    [InlineData("UPDATE Venta SET comprobanteSnapshot='{}'", "comprobante")]
    [InlineData("DELETE FROM Venta", "ventas")]
    [InlineData("UPDATE VentaDetalle SET totalCentavos=1", "detalle")]
    [InlineData("DELETE FROM VentaDetalle", "detalle")]
    [InlineData("UPDATE Pago SET recibidoCentavos=99999", "pagos")]
    [InlineData("DELETE FROM Pago", "pagos")]
    [InlineData("UPDATE MovimientoCaja SET importeCentavos=1", "caja")]
    [InlineData("DELETE FROM MovimientoCaja", "caja")]
    public async Task confirmed_sale_payment_and_cash_history_are_preserved(string sql, string message)
    {
        using var f = new Fixture(); await f.SaleAsync();
        var c = await f.ConnectionAsync();
        var error = await Assert.ThrowsAsync<SqliteException>(() => Scalar(c, sql)); Assert.Contains(message, error.Message);
        Assert.True((await f.Inspector.InspectAsync(await f.ConnectionAsync())).Healthy);
    }

    [Fact]
    public async Task price_intervals_reject_overlap_and_accept_adjacent_periods()
    {
        using var f = new Fixture(); var c = await f.ConnectionAsync();
        const string insert = "INSERT INTO PrecioProducto(presentacionId,precioCentavos,vigenteDesde,creadoPor,creadoEn) VALUES(1,2500,'2100-01-01T00:00:00.000Z',1,'2100-01-01T00:00:00.000Z')";
        var error = await Assert.ThrowsAsync<SqliteException>(() => Scalar(c, insert)); Assert.Contains("superpuestas", error.Message);
        await Scalar(c, "UPDATE PrecioProducto SET vigenteHasta='2100-01-01T00:00:00.000Z' WHERE presentacionId=1");
        await Scalar(c, insert);
        Assert.Equal(2L, await Scalar(c, "SELECT COUNT(*) FROM PrecioProducto WHERE presentacionId=1"));
        await Assert.ThrowsAsync<SqliteException>(() => Scalar(c, "UPDATE PrecioProducto SET vigenteHasta=NULL WHERE presentacionId=1 AND vigenteDesde<'2100'"));
    }

    [Fact]
    public async Task closed_shift_cannot_be_rewritten_and_view_preserves_expected()
    {
        using var f = new Fixture(); await f.SaleAsync(); var shift = (await f.Op.CashAsync(2)).Shift!;
        await f.Op.CloseCashAsync(2, shift.Id, shift.Version, "560", "Diferencia de prueba");
        var c = await f.ConnectionAsync();
        await Assert.ThrowsAsync<SqliteException>(() => Scalar(c, "UPDATE TurnoCaja SET efectivoEsperadoCierreCentavos=0"));
        await Assert.ThrowsAsync<SqliteException>(() => Scalar(c, "DELETE FROM TurnoCaja"));
        Assert.Equal(56600, (await f.Db.Set<ResumenCajaVista>().SingleAsync()).EsperadoCentavos);
    }

    [Fact]
    public async Task extra_payment_cannot_exceed_sale_total()
    {
        using var f = new Fixture(); await f.SaleAsync(); var c = await f.ConnectionAsync();
        var error = await Assert.ThrowsAsync<SqliteException>(() => Scalar(c,
            "INSERT INTO Pago(ventaId,secuencia,medio,importeCentavos,recibidoCentavos,cambioCentavos,estadoVerificacion,verificadoPor,verificadoEn) SELECT ventaId,99,'efectivo',1,1,0,'verificado',verificadoPor,verificadoEn FROM Pago LIMIT 1"));
        Assert.Contains("cobertura", error.Message); Assert.Equal(1, await f.Db.Set<Pago>().CountAsync());
    }

    [Fact]
    public async Task foreign_keys_alone_do_not_allow_a_lot_from_another_product()
    {
        using var f = new Fixture(); await f.SaleAsync();
        var lot = new LoteExistencia { ProductoId = 2, CreadoEn = Formats.Now(), Referencia = "Lote ficticio de otro producto" };
        f.Db.Add(lot); await f.Db.SaveChangesAsync();
        var sql = CloneFirstRow(f.Contract, "MovimientoInventario", new Dictionary<string, string>
            { ["loteId"] = lot.Id.ToString(), ["secuencia"] = "99" }, "ventaDetalleId IS NOT NULL");
        var c = await f.ConnectionAsync();
        var error = await Assert.ThrowsAsync<SqliteException>(() => Scalar(c, sql)); Assert.Contains("producto o unidad", error.Message);
        Assert.True((await f.Inspector.InspectAsync(c)).Healthy);
    }

    [Fact]
    public async Task detail_cannot_use_a_presentation_belonging_to_another_product()
    {
        using var f = new Fixture(); await f.SaleAsync();
        var sql = CloneFirstRow(f.Contract, "VentaDetalle", new Dictionary<string, string> { ["numeroLinea"] = "99", ["productoId"] = "2" }, "productoId=1");
        var c = await f.ConnectionAsync();
        var error = await Assert.ThrowsAsync<SqliteException>(() => Scalar(c, sql)); Assert.Contains("presentacion", error.Message);
        Assert.Equal(2, await f.Db.Set<VentaDetalle>().CountAsync());
    }

    private static string CloneFirstRow(DatabaseContract contract, string table, Dictionary<string, string> overrides, string filter)
    {
        var fields = contract.Tables.Single(t => t.Name == table).Fields.Where(f => f.Name != "id").Select(f => f.Name).ToArray();
        var names = string.Join(",", fields.Select(n => "\"" + n + "\""));
        var values = string.Join(",", fields.Select(n => overrides.GetValueOrDefault(n) ?? "\"" + n + "\""));
        return $"INSERT INTO \"{table}\"({names}) SELECT {values} FROM \"{table}\" WHERE {filter} ORDER BY id LIMIT 1";
    }

    [Fact]
    public async Task non_demo_initialization_uses_supplied_password_owner_and_no_fictitious_products()
    {
        const string password = "ClaveSoloPrueba2026!";
        using var factory = new MegaFactory(demoData: false, bootstrapPassword: password);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        var user = await db.Set<Usuario>().SingleAsync();
        var role = await db.Set<Rol>().SingleAsync(x => x.Id == user.RolId);
        Assert.Equal("Propietario", role.Nombre); Assert.Empty(await db.Set<Producto>().ToListAsync());
        var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<Usuario>>();
        Assert.Equal(Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, password));
        Assert.NotEqual(password, user.PasswordHash);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Mega.Web";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = "";
        public string WebRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
