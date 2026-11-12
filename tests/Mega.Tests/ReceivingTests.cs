using System.Net;
using System.Text.RegularExpressions;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public sealed class ReceivingTests
{
    private sealed class Fixture : IDisposable
    {
        public MegaFactory Factory { get; } = new(); public IServiceScope Scope { get; }
        public MegaDbContext Db => Scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        public ReceivingService Service => Scope.ServiceProvider.GetRequiredService<ReceivingService>();
        public Operations Op => Scope.ServiceProvider.GetRequiredService<Operations>();
        public Fixture() => Scope = Factory.Services.CreateScope();
        public Task<long> Supplier() => Service.SaveSupplierAsync(1, new(null, 0, "Proveedor ficticio", "PRUEBA-01", null, null, "Contacto ficticio", true));
        public async Task<ReceivingChange> Draft(string? cost = "24") => new(null, 0, Guid.NewGuid().ToString(), "REC-" + Guid.NewGuid().ToString("N"), await Supplier(), "DOC-001", "2026-11-12", "Prueba ficticia", [new(2, "10", "8", cost, null, null, "Dos unidades dañadas, rechazadas")]);
        public async Task<Autorizacion> Send(ReceivingChange draft, long userId = 3)
        { var id = await Service.SaveAsync(userId, draft); await Service.SubmitAsync(userId, id, 1); Db.ChangeTracker.Clear(); return await Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x => x.RecepcionId == id); }
        public void Dispose() { Scope.Dispose(); Factory.Dispose(); }
    }
    [Fact]
    public async Task T02_pending_does_not_move_stock_approved_receipt_is_atomic_and_recoverable()
    {
        using var f = new Fixture(); var auth = await f.Send(await f.Draft());
        Assert.Equal(30m, (await f.Op.StocksAsync())[2]); Assert.Equal(1, await f.Db.Set<OperacionInventario>().CountAsync());
        var op = await f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Cantidades y documento revisados");
        Assert.NotNull(op); Assert.Equal(38m, (await f.Op.StocksAsync())[2]);
        Assert.Equal(op, await f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Reintento tras pérdida de respuesta")); f.Db.ChangeTracker.Clear();
        Assert.Single(await f.Db.Set<MovimientoInventario>().Where(x => x.RecepcionDetalleId != null).ToListAsync());
        var movement = await f.Db.Set<MovimientoInventario>().SingleAsync(x => x.RecepcionDetalleId != null); Assert.Equal(24000000, movement.CostoUnitEscalado); Assert.Equal("conocido", movement.EstadoCosto);
        Assert.Equal(2, await f.Db.Set<OperacionInventario>().CountAsync()); Assert.Equal("Confirmada", (await f.Db.Set<Recepcion>().SingleAsync()).Estado);
        Assert.Empty(await f.Db.Set<MovimientoCaja>().ToListAsync()); Assert.True(await f.Db.Set<EventoAuditoria>().AnyAsync(x => x.AutorizacionId == auth.Id));
    }
    [Fact]
    public async Task T10_edit_invalidates_review_and_rejects_previous_version_before_any_effect()
    {
        using var f = new Fixture(); var draft = await f.Draft(); var auth = await f.Send(draft);
        await f.Service.SaveAsync(3, draft with { Id = auth.RecepcionId, Version = 1, Lines = [new(2, "20", "20", "22", null, null, null)] });
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Revisión obsoleta")); f.Db.ChangeTracker.Clear();
        Assert.Equal(30m, (await f.Op.StocksAsync())[2]); Assert.NotNull((await f.Db.Set<Autorizacion>().SingleAsync(x => x.Id == auth.Id)).InvalidadaEn);
        var next = await f.Service.SubmitAsync(3, auth.RecepcionId!.Value, 2); f.Db.ChangeTracker.Clear(); var current = await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x => x.Id == next);
        await f.Service.DecideAsync(1, next, 2, current.ContenidoHash, true, "Nueva versión revisada"); Assert.Equal(50m, (await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task T10_same_account_cannot_self_approve_even_if_manager()
    {
        using var f = new Fixture(); var auth = await f.Send(await f.Draft(), 1);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Autoaprobación"));
        await f.Service.DecideAsync(5, auth.Id, 1, auth.ContenidoHash, true, "Segunda cuenta revisó"); Assert.Equal(38m, (await f.Op.StocksAsync())[2]);
    }
    [Theory]
    [InlineData(2)] [InlineData(3)]
    public async Task cashier_and_inventory_cannot_decide(long id)
    {
        using var f = new Fixture(); var auth = await f.Send(await f.Draft());
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.DecideAsync(id, auth.Id, 1, auth.ContenidoHash, true, "Intento sin permiso"));
        Assert.Equal(30m, (await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task incomplete_draft_preserves_unknown_cost_and_cannot_be_submitted()
    {
        using var f = new Fixture(); var draft = await f.Draft(null); var id = await f.Service.SaveAsync(3, draft); f.Db.ChangeTracker.Clear();
        var d = await f.Db.Set<RecepcionDetalle>().SingleAsync(); Assert.Null(d.CostoUnitEscalado); Assert.Null(d.EscalaCosto); Assert.Equal("desconocido", d.EstadoCosto);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SubmitAsync(3, id, 1)); Assert.Empty(await f.Db.Set<Autorizacion>().Where(x => x.RecepcionId == id).ToListAsync());
    }
    [Fact]
    public async Task rejected_receipt_preserves_review_and_no_stock_until_new_version()
    {
        using var f = new Fixture(); var draft = await f.Draft(); var auth = await f.Send(draft);
        Assert.Null(await f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, false, "Documento incorrecto")); Assert.Equal(30m, (await f.Op.StocksAsync())[2]);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Cambio de decisión")); f.Db.ChangeTracker.Clear();
        await f.Service.SaveAsync(3, draft with { Id = auth.RecepcionId, Version = 1, Document = "DOC-002" });
        await f.Service.SubmitAsync(3, auth.RecepcionId!.Value, 2); Assert.Equal(2, await f.Db.Set<Autorizacion>().CountAsync(x => x.RecepcionId == auth.RecepcionId));
    }
    [Fact]
    public async Task current_catalog_and_raw_payload_changes_cannot_reuse_an_earlier_review()
    {
        using var f = new Fixture(); var auth = await f.Send(await f.Draft());
        var supplier = await f.Db.Set<Proveedor>().SingleAsync(); supplier.Nombre = "Proveedor cambiado"; supplier.Version++; await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Viejo proveedor")); f.Db.ChangeTracker.Clear();
        Assert.Equal(30m, (await f.Op.StocksAsync())[2]); Assert.Contains("Proveedor ficticio", (await f.Db.Set<Autorizacion>().SingleAsync(x => x.Id == auth.Id)).ContenidoSnapshot);
        supplier = await f.Db.Set<Proveedor>().SingleAsync(); supplier.Nombre = "Proveedor ficticio"; supplier.Version--; await f.Db.SaveChangesAsync();
        var d = await f.Db.Set<RecepcionDetalle>().SingleAsync(); d.CostoUnitEscalado++; await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Costo cambiado sin versión")); Assert.Equal(30m, (await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task lot_expiry_partial_acceptance_and_conversion_preserve_origin()
    {
        using var f = new Fixture(); var draft = await f.Draft(); var product = await f.Db.Set<Producto>().SingleAsync(x => x.Id == 1); product.ControlVencimiento = 1; await f.Db.SaveChangesAsync();
        var catalog = f.Scope.ServiceProvider.GetRequiredService<CatalogService>(); var p = await catalog.SavePresentationAsync(1, new(null, 1, 1, 0, "Saco prueba 25 lb", "25", true));
        draft = draft with { Lines = [new(p, "2.000", "1.500", "14.123456", "2027-12-31", "LOTE-PRUEBA", "Rechazo parcial documentado")] };
        var auth = await f.Send(draft); await f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Lote revisado"); f.Db.ChangeTracker.Clear();
        Assert.Equal(87.5m, (await f.Op.StocksAsync())[1]); var lot = await f.Db.Set<LoteExistencia>().SingleAsync(); Assert.Equal("LOTE-PRUEBA", lot.Referencia);
        var m = await f.Db.Set<MovimientoInventario>().SingleAsync(x => x.LoteId == lot.Id); Assert.Equal(37500, m.CantidadEscalada); Assert.Equal(14123456, m.CostoUnitEscalado); Assert.Equal(lot.RecepcionDetalleId, m.RecepcionDetalleId);
    }
    [Fact]
    public async Task required_lot_expiry_and_reason_are_checked_before_submission()
    {
        using var f = new Fixture(); var draft = await f.Draft(); var product = await f.Db.Set<Producto>().SingleAsync(x => x.Id == 2); product.ControlVencimiento = 1; await f.Db.SaveChangesAsync();
        var id = await f.Service.SaveAsync(3, draft); await Assert.ThrowsAsync<BusinessException>(() => f.Service.SubmitAsync(3, id, 1)); f.Db.ChangeTracker.Clear();
        await f.Service.SaveAsync(3, draft with { Id = id, Version = 1, Lines = [new(2, "10", "8", "24", "2020-01-01", "Vencido", "Daños")] });
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SubmitAsync(3, id, 2)); f.Db.ChangeTracker.Clear();
        await f.Service.SaveAsync(3, draft with { Id = id, Version = 2, Lines = [new(2, "10", "8", "24", "2030-01-01", "Nuevo", null)] });
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SubmitAsync(3, id, 3)); Assert.Equal(30m, (await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task invalid_precision_accepted_over_received_duplicate_document_and_other_editor_are_rejected()
    {
        using var f = new Fixture(); var draft = await f.Draft();
        foreach (var line in new[] { new ReceivingLine(2,"1.5","1","24",null,null,null), new(2,"1","2","24",null,null,null), new(2,"1","1","24.0000001",null,null,null) })
        { await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(3, draft with { Lines = [line] })); f.Db.ChangeTracker.Clear(); }
        var id = await f.Service.SaveAsync(3, draft);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(1, draft with { Id = id, Version = 1 }));
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(3, draft with { Key = Guid.NewGuid().ToString(), Reference = "OTRA" })); Assert.Equal(30m, (await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task late_failure_rolls_back_decision_operation_and_lot()
    {
        using var f = new Fixture(); var auth = await f.Send((await f.Draft()) with { Lines = [new(2,"1","1","24","2030-01-01","Lote",null)] });
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_receiving BEFORE INSERT ON MovimientoInventario WHEN NEW.recepcionDetalleId IS NOT NULL BEGIN SELECT RAISE(ABORT,'Fallo de prueba'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Prueba de atomicidad")); f.Db.ChangeTracker.Clear();
        Assert.Equal("pendiente", (await f.Db.Set<Autorizacion>().SingleAsync(x => x.Id == auth.Id)).Decision); Assert.Equal("Pendiente", (await f.Db.Set<Recepcion>().SingleAsync()).Estado);
        Assert.Equal(1, await f.Db.Set<OperacionInventario>().CountAsync()); Assert.Empty(await f.Db.Set<LoteExistencia>().ToListAsync()); Assert.Equal(30m, (await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task concurrent_retry_creates_exactly_one_receiving_operation()
    {
        using var f = new Fixture(); var auth = await f.Send(await f.Draft());
        async Task<long?> Decide()
        { using var scope = f.Factory.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<ReceivingService>().DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Revisado"); }
        var ids = await Task.WhenAll(Task.Run(Decide), Task.Run(Decide)); Assert.Equal(ids[0], ids[1]); f.Db.ChangeTracker.Clear();
        Assert.Equal(38m, (await f.Op.StocksAsync())[2]); Assert.Equal(1, await f.Db.Set<OperacionInventario>().CountAsync(x => x.Tipo == "entrada_recepcion"));
    }
    [Fact]
    public async Task supplier_permissions_versions_and_deactivation_block_new_receipts()
    {
        using var f = new Fixture(); var id = await f.Supplier();
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveSupplierAsync(3, new(id,1,"Cambiar",null,null,null,null,true)));
        await f.Service.SaveSupplierAsync(1, new(id,1,"Proveedor ficticio","PRUEBA-01",null,null,null,false));
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveSupplierAsync(1, new(id,1,"Obsoleto",null,null,null,null,true)));
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(3, new(null,0,Guid.NewGuid().ToString(),"INACTIVO",id,null,null,null,[])));
    }
    [Fact]
    public async Task stock_capacity_is_checked_without_partial_approval()
    {
        using var f = new Fixture(); var auth = await f.Send((await f.Draft()) with { Lines = [new(2,long.MaxValue.ToString(),long.MaxValue.ToString(),"1",null,null,null)] });
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.DecideAsync(1, auth.Id, 1, auth.ContenidoHash, true, "Desbordamiento")); f.Db.ChangeTracker.Clear();
        Assert.Equal("pendiente", (await f.Db.Set<Autorizacion>().SingleAsync(x => x.Id == auth.Id)).Decision); Assert.Equal(30m, (await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task actual_review_form_posts_the_exact_snapshot_with_antiforgery()
    {
        using var f = new Fixture(); var auth = await f.Send(await f.Draft()); using var client = f.Factory.Client(); await HttpTestSupport.LoginAsync(client);
        var page = "/Recepciones/Revision?id=" + auth.Id; var html = await (await client.GetAsync(page)).Content.ReadAsStringAsync(); Assert.Contains(auth.ContenidoHash, html);
        var response = await HttpTestSupport.PostAsync(client, "/Recepciones/Revision?handler=Approve", new() { ["Id"] = auth.Id.ToString(), ["ExpectedVersion"] = "1", ["ExpectedHash"] = auth.ContenidoHash, ["Reason"] = "Documento revisado desde formulario" }, page);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal(38m, (await f.Op.StocksAsync())[2]);
        using var cashier = f.Factory.Client(); await HttpTestSupport.LoginAsync(cashier,"cajero"); Assert.Contains("SinPermiso", (await cashier.GetAsync("/Recepciones")).Headers.Location!.ToString());
    }
}
