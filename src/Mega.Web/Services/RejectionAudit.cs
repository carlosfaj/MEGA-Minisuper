using System.Security.Claims;
using Mega.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed class RejectionCapture:IDisposable
{
    private static readonly AsyncLocal<RejectionCapture?> Current=new();
    private readonly RejectionCapture? previous;
    public string? Message{get;private set;}
    public RejectionCapture(){previous=Current.Value;Current.Value=this;}
    public static void Record(string message){if(Current.Value is {} capture)capture.Message??=message;}
    public void Dispose()=>Current.Value=previous;
}
public sealed class RejectionPageFilter:IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)=>Task.CompletedTask;
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context,PageHandlerExecutionDelegate next)
    {await next();if(HttpMethods.IsPost(context.HttpContext.Request.Method)&&!context.ModelState.IsValid)RejectionCapture.Record("Formulario o solicitud con datos inválidos.");}
}
public sealed class RejectionAuditWriter(IServiceScopeFactory scopes,ILogger<RejectionAuditWriter> logger,RecoveryGate recovery)
{
    private readonly SemaphoreSlim gate=new(1,1);
    public async Task WriteAsync(HttpContext context,string reason,string result="rechazado")
    {
        if(!recovery.DatabaseSafe||!context.Items.ContainsKey(RecoveryGate.LeaseKey)||context.Items.ContainsKey("MEGA.RejectionAudited"))return;
        context.Items["MEGA.RejectionAudited"]=true;
        await gate.WaitAsync();
        try
        {
            for(var attempt=0;attempt<3;attempt++)
            {
                try
                {
                    await using var scope=scopes.CreateAsyncScope();
                    var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();db.Database.SetCommandTimeout(2);
                    long? actor=long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)&&await db.Set<Usuario>().AnyAsync(x=>x.Id==id)?id:null;
                    db.Add(new EventoAuditoria{Fecha=Formats.Now(),UsuarioId=actor,Origen=actor is null?"tarea_automatica":"persona",Accion=result=="fallo"?"Fallo de solicitud":"Rechazar solicitud",Entidad="SolicitudHTTP",EntidadId=context.Request.Path.Value,ClaveOperacion="http-"+context.TraceIdentifier,Resultado=result,VersionFormato=1,
                        DatosCambio=Formats.Json(new{metodo=context.Request.Method,ruta=context.Request.Path.Value,motivo=reason,estadoHTTP=context.Response.StatusCode,correlacion=context.TraceIdentifier})});
                    await db.SaveChangesAsync();
                    return;
                }
                catch(DbUpdateException error)when(error.InnerException is SqliteException {SqliteErrorCode:5 or 6}&&attempt<2)
                {await Task.Delay(100*(attempt+1));}
            }
        }
        catch(Exception error){logger.LogError(error,"No se pudo registrar la auditoría del rechazo HTTP {Trace}",context.TraceIdentifier);}
        finally{gate.Release();}
    }
}
public sealed class RejectionAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context,RejectionAuditWriter writer)
    {
        using var capture=new RejectionCapture();var failed=false;var authenticatedAtStart=context.User.Identity?.IsAuthenticated==true;
        try{await next(context);}
        catch{failed=true;if(!context.Response.HasStarted)context.Response.StatusCode=500;throw;}
        finally
        {
            var location=context.Response.Headers.Location.ToString();
            var redirect=Uri.TryCreate(location,UriKind.Absolute,out var target)?target.AbsolutePath:location.Split('?')[0];
            var reason=capture.Message??(context.Response.StatusCode==403||redirect=="/SinPermiso"?"Acceso rechazado por permisos.":
                context.Response.StatusCode==400&&HttpMethods.IsPost(context.Request.Method)?"Solicitud inválida o sin verificación de formulario válida.":
                redirect=="/Acceso"&&!authenticatedAtStart&&context.Request.Path!="/Acceso"?"Acceso sin sesión válida.":failed?"La operación no pudo completarse; revisar el registro del servicio.":null);
            if(reason is not null)await writer.WriteAsync(context,reason,failed?"fallo":"rechazado");
        }
    }
}
