using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;
public sealed class ExpenseTests
{
    private static ExpenseChange Input(string amount="100")=>new(Guid.NewGuid().ToString(),"2026-01-01","Transporte ficticio",amount,"FACT-FICTICIA");
    [Fact]
    public async Task create_and_retry_record_one_expense_without_cash_effect()
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();var service=s.ServiceProvider.GetRequiredService<ExpenseService>();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var input=Input();var id=await service.SaveAsync(1,input);Assert.Equal(id,await service.SaveAsync(1,input));Assert.Equal(1,await db.Set<GastoOperativo>().CountAsync());Assert.Empty(await db.Set<MovimientoCaja>().ToListAsync());await Assert.ThrowsAsync<BusinessException>(()=>service.SaveAsync(1,input with{Amount="101"}));
    }
    [Fact]
    public async Task correction_preserves_original_and_cancellation_never_reactivates_it()
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();var service=s.ServiceProvider.GetRequiredService<ExpenseService>();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var id=await service.SaveAsync(1,Input());var old=await db.Set<GastoOperativo>().SingleAsync();var correction=Input("120") with{Corrects=id,Reason="Importe corregido",ReviewHash=ExpenseService.ReviewHash(old)};var next=await service.SaveAsync(1,correction);Assert.Equal(next,await service.SaveAsync(1,correction));db.ChangeTracker.Clear();old=await db.Set<GastoOperativo>().SingleAsync(x=>x.Id==id);Assert.Equal(10000,old.ImporteCentavos);Assert.Equal("Corregido",old.Estado);var current=await db.Set<GastoOperativo>().SingleAsync(x=>x.Id==next);await service.CancelAsync(1,next,ExpenseService.ReviewHash(current),"No corresponde");Assert.Equal(0,await db.Set<GastoOperativo>().CountAsync(x=>x.Estado=="Registrado"));Assert.Equal("Corregido",old.Estado);Assert.Equal(3,await db.Set<EventoAuditoria>().CountAsync(x=>x.Entidad=="GastoOperativo"));
    }
    [Fact]
    public async Task invalid_values_permissions_and_stale_reviews_leave_no_partial_expense()
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();var service=s.ServiceProvider.GetRequiredService<ExpenseService>();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();foreach(var input in new[]{Input("0"),Input("-1"),Input("1.001"),Input() with{Date="2099-01-01"},Input() with{Date="2026-02-30"}})await Assert.ThrowsAsync<BusinessException>(()=>service.SaveAsync(1,input));await Assert.ThrowsAsync<BusinessException>(()=>service.SaveAsync(2,Input()));Assert.Empty(await db.Set<GastoOperativo>().ToListAsync());var id=await service.SaveAsync(1,Input());await Assert.ThrowsAsync<BusinessException>(()=>service.SaveAsync(1,Input("120") with{Corrects=id,Reason="Cambio",ReviewHash="obsoleta"}));Assert.Equal("Registrado",(await db.Set<GastoOperativo>().SingleAsync()).Estado);
        using var client=f.Client();await HttpTestSupport.LoginAsync(client,"inventario");Assert.Contains("SinPermiso",(await client.GetAsync("/Gastos")).Headers.Location!.ToString());
    }
    [Fact]
    public async Task late_correction_failure_rolls_back_previous_state_and_new_record()
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();var service=s.ServiceProvider.GetRequiredService<ExpenseService>();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var id=await service.SaveAsync(1,Input());var old=await db.Set<GastoOperativo>().SingleAsync();await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_expense BEFORE INSERT ON EventoAuditoria WHEN NEW.entidad='GastoOperativo' BEGIN SELECT RAISE(ABORT,'Fallo prueba'); END;");await Assert.ThrowsAsync<DbUpdateException>(()=>service.SaveAsync(1,Input("150") with{Corrects=id,Reason="Cambio",ReviewHash=ExpenseService.ReviewHash(old)}));db.ChangeTracker.Clear();Assert.Equal("Registrado",(await db.Set<GastoOperativo>().SingleAsync()).Estado);Assert.Equal(10000,(await db.Set<GastoOperativo>().SingleAsync()).ImporteCentavos);
    }
    [Fact]
    public async Task concurrent_same_key_and_corrections_never_duplicate_effects()
    {
        using var f=new MegaFactory();var input=Input();async Task<long> Save(ExpenseChange value){using var s=f.Services.CreateScope();return await s.ServiceProvider.GetRequiredService<ExpenseService>().SaveAsync(1,value);}var ids=await Task.WhenAll(Save(input),Save(input));Assert.Equal(ids[0],ids[1]);using var inspect=f.Services.CreateScope();var db=inspect.ServiceProvider.GetRequiredService<MegaDbContext>();var old=await db.Set<GastoOperativo>().SingleAsync();var change=Input("140") with{Corrects=old.Id,Reason="Corrección",ReviewHash=ExpenseService.ReviewHash(old)};
        async Task<bool> TrySave(ExpenseChange value){try{await Save(value);return true;}catch(BusinessException){return false;}}var results=await Task.WhenAll(TrySave(change),TrySave(change with{Key=Guid.NewGuid().ToString()}));Assert.Single(results,x=>x);Assert.Equal(2,await db.Set<GastoOperativo>().CountAsync());Assert.Equal(14000,await db.Set<GastoOperativo>().Where(x=>x.Estado=="Registrado").SumAsync(x=>x.ImporteCentavos));
    }
}
