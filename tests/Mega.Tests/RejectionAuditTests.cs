using System.Net;
using System.Net.Http.Json;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;
public sealed class RejectionAuditTests
{
    [Fact]
    public async Task rejected_sale_is_audited_after_rollback_without_any_commercial_effect()
    {
        using var f=new MegaFactory();using var client=f.Client();await HttpTestSupport.LoginAsync(client,"cajero");var token=await HttpTestSupport.TokenAsync(client,"/Venta");client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",token);
        var response=await client.PostAsJsonAsync("/Venta?handler=Cotizar",new[]{new LineInput(2,"unidad","1")});Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        using var s=f.Services.CreateScope();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var audit=await db.Set<EventoAuditoria>().SingleAsync(x=>x.Entidad=="SolicitudHTTP");Assert.Equal(2,audit.UsuarioId);Assert.Equal("rechazado",audit.Resultado);Assert.Contains("Abre tu turno",audit.DatosCambio);Assert.Empty(await db.Set<Venta>().ToListAsync());Assert.Empty(await db.Set<MovimientoCaja>().ToListAsync());
    }
    [Fact]
    public async Task denied_permission_and_invalid_antiforgery_are_audited()
    {
        using var f=new MegaFactory();using var client=f.Client();await HttpTestSupport.LoginAsync(client,"cajero");Assert.Contains("SinPermiso",(await client.GetAsync("/Configuracion")).Headers.Location!.ToString());
        var response=await client.PostAsync("/Caja?handler=Abrir",new FormUrlEncodedContent(new Dictionary<string,string>()));Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        using var s=f.Services.CreateScope();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var events=await db.Set<EventoAuditoria>().Where(x=>x.Entidad=="SolicitudHTTP").ToListAsync();Assert.Equal(2,events.Count);Assert.All(events,x=>Assert.Equal("rechazado",x.Resultado));Assert.Empty(await db.Set<TurnoCaja>().ToListAsync());
    }
    [Fact]
    public async Task invalid_form_and_unknown_login_do_not_record_passwords_or_request_bodies()
    {
        const string secret="NoDebeAparecerEnAuditoria2026!";using var f=new MegaFactory();using var client=f.Client();await HttpTestSupport.LoginAsync(client,"cajero");
        var response=await HttpTestSupport.PostAsync(client,"/MiCuenta",new(){["CurrentPassword"]=secret,["NewPassword"]="corta",["Confirmation"]="corta"});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var anonymous=f.Client();response=await HttpTestSupport.PostAsync(anonymous,"/Acceso",new(){["Account"]="usuario.no.existe",["Password"]=secret});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var s=f.Services.CreateScope();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var events=await db.Set<EventoAuditoria>().Where(x=>x.Entidad=="SolicitudHTTP").ToListAsync();Assert.Equal(2,events.Count);Assert.Contains(events,x=>x.UsuarioId is null&&x.Origen=="tarea_automatica");Assert.DoesNotContain(secret,string.Join("",events.Select(x=>x.DatosCambio)));Assert.DoesNotContain("usuario.no.existe",string.Join("",events.Select(x=>x.DatosCambio)));
    }
    [Fact]
    public async Task failed_confirmation_is_audited_in_a_separate_context_after_full_rollback()
    {
        using var f=new MegaFactory();using var setup=f.Services.CreateScope();var db=setup.ServiceProvider.GetRequiredService<MegaDbContext>();var op=setup.ServiceProvider.GetRequiredService<Operations>();await op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(2,"unidad","1")];var quote=await op.QuoteAsync(2,lines);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_http_print BEFORE INSERT ON SolicitudImpresion BEGIN SELECT RAISE(ABORT,'Fallo tardio prueba HTTP'); END;");
        using var client=f.Client();await HttpTestSupport.LoginAsync(client,"cajero");client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",await HttpTestSupport.TokenAsync(client,"/Venta"));var response=await client.PostAsJsonAsync("/Venta?handler=Confirmar",new SaleInput(Guid.NewGuid().ToString(),quote.Hash,"50",false,lines));Assert.Equal(HttpStatusCode.InternalServerError,response.StatusCode);
        db.ChangeTracker.Clear();Assert.Empty(await db.Set<Venta>().ToListAsync());Assert.Empty(await db.Set<Pago>().ToListAsync());Assert.Empty(await db.Set<MovimientoCaja>().ToListAsync());var audit=await db.Set<EventoAuditoria>().SingleAsync(x=>x.Entidad=="SolicitudHTTP"&&x.Resultado=="fallo");Assert.Equal(2,audit.UsuarioId);
    }
    [Fact]
    public async Task intentional_logout_and_successful_password_change_are_not_rejections()
    {
        using var f=new MegaFactory();using var client=f.Client();await HttpTestSupport.LoginAsync(client,"cajero");
        var response=await HttpTestSupport.PostAsync(client,"/Salir",new(),"/MiCuenta");Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        await HttpTestSupport.LoginAsync(client,"cajero");response=await HttpTestSupport.PostAsync(client,"/MiCuenta",new(){["CurrentPassword"]="MegaDemo2026!",["NewPassword"]="OtraClaveSegura2026!",["Confirmation"]="OtraClaveSegura2026!"});Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        using var s=f.Services.CreateScope();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();Assert.Empty(await db.Set<EventoAuditoria>().Where(x=>x.Entidad=="SolicitudHTTP").ToListAsync());Assert.True(await db.Set<EventoAuditoria>().AnyAsync(x=>x.Accion=="Cambiar contraseña propia"&&x.Resultado=="exito"));
    }
    [Fact]
    public async Task concurrent_requests_keep_actor_and_rejection_reason_separate()
    {
        using var f=new MegaFactory();using var a=f.Client();using var b=f.Client();await HttpTestSupport.LoginAsync(a,"cajero");await HttpTestSupport.LoginAsync(b,"cajero2");
        using(var setup=f.Services.CreateScope())await setup.ServiceProvider.GetRequiredService<Operations>().OpenCashAsync(2,"500",null);
        a.DefaultRequestHeaders.Add("X-CSRF-TOKEN",await HttpTestSupport.TokenAsync(a,"/Venta"));b.DefaultRequestHeaders.Add("X-CSRF-TOKEN",await HttpTestSupport.TokenAsync(b,"/Venta"));
        var responses=await Task.WhenAll(a.PostAsJsonAsync("/Venta?handler=Cotizar",new[]{new LineInput(2,"unidad","-1")}),b.PostAsJsonAsync("/Venta?handler=Cotizar",new[]{new LineInput(2,"unidad","1")}));Assert.All(responses,r=>Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode));
        using var s=f.Services.CreateScope();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var deadline=DateTime.UtcNow.AddSeconds(5);List<EventoAuditoria> events=[];
        do{events=await db.Set<EventoAuditoria>().AsNoTracking().Where(x=>x.Entidad=="SolicitudHTTP").ToListAsync();if(events.Count==2)break;await Task.Delay(30);}while(DateTime.UtcNow<deadline);
        Assert.Equal(2,events.Count);Assert.Contains("número",events.Single(x=>x.UsuarioId==2).DatosCambio.Replace("\\u00FA","ú"));Assert.Contains("Abre tu turno",events.Single(x=>x.UsuarioId==4).DatosCambio);Assert.Equal(2,events.Select(x=>x.ClaveOperacion).Distinct().Count());
    }
}
