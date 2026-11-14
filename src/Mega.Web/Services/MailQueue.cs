using System.Security.Cryptography;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;
public sealed record MailLease(long RequestId,string Token,long AttemptId,MailDocument Document);
public sealed class MailQueue(MegaDbContext db,AccessControl access,Operations operations,SettingsService settings,ReportService reports,OperationalFiles files)
{
    public static string ReviewHash(SolicitudCorreo row)=>Formats.Hash(Formats.Json(row));
    private static string Hash(byte[] content)=>Convert.ToHexStringLower(SHA256.HashData(content));
    public async Task<long> EnqueueAsync(long userId,string key,string recipient,ReportFilter filter,string format)
    {
        await access.ManagerAsync(userId);if(!Guid.TryParseExact(key,"D",out _))throw new BusinessException("Recarga la solicitud de correo.");recipient=MailConfiguration.Address(recipient);
        if(format is not ("pdf" or "xlsx"))throw new BusinessException("Selecciona PDF o Excel para el adjunto.");var parameters=Formats.Json(new{filter,format});
        var existing=await db.Set<SolicitudCorreo>().AsNoTracking().SingleOrDefaultAsync(x=>x.ClaveSolicitud==key);
        if(existing is not null)return Recover(existing);
        var report=await reports.RunAsync(userId,filter);var bytes=format=="pdf"?ReportExport.Pdf(report):ReportExport.Excel(report);var reference=Guid.NewGuid().ToString("N")+"."+format;Directory.CreateDirectory(files.Reports);var path=files.ReportPath(reference);await File.WriteAllBytesAsync(path,bytes);
        try
        {
            await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);existing=await db.Set<SolicitudCorreo>().AsNoTracking().SingleOrDefaultAsync(x=>x.ClaveSolicitud==key);if(existing is not null){var id=Recover(existing);File.Delete(path);await tx.CommitAsync();return id;}
            var row=new SolicitudCorreo{SolicitadoPor=userId,Fecha=Formats.Now(),Destinatario=recipient,ReporteTipo=filter.Kind,ParametrosReporte=parameters,ReporteReferencia=reference,ReporteHash=Hash(bytes),ReporteGeneradoEn=report.GeneratedAt,Estado="Pendiente",ClaveSolicitud=key,MessageId=Guid.NewGuid().ToString("N")+"@mega.local"};
            db.Add(row);await db.SaveChangesAsync();operations.Audit(userId,"Solicitar correo de reporte","SolicitudCorreo",row.Id.ToString(),key,new{row.Destinatario,row.ReporteTipo,row.ReporteHash,row.MessageId,filtros=filter,formato=format});await db.SaveChangesAsync();await tx.CommitAsync();return row.Id;
        }
        catch{try{File.Delete(path);}catch(IOException){}throw;}
        long Recover(SolicitudCorreo row){if(row.SolicitadoPor!=userId||row.Destinatario!=recipient||row.ParametrosReporte!=parameters)throw new BusinessException("La clave pertenece a otra solicitud de correo.");return row.Id;}
    }
    public async Task<byte[]> DownloadAsync(long userId,long id)
    {await access.ManagerAsync(userId);var row=await db.Set<SolicitudCorreo>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("La solicitud no existe.");return await ReadFileAsync(row);}
    private async Task<byte[]> ReadFileAsync(SolicitudCorreo row)
    {
        try{var bytes=await File.ReadAllBytesAsync(files.ReportPath(row.ReporteReferencia));if(Hash(bytes)!=row.ReporteHash)throw new BusinessException("La huella del reporte no coincide. Requiere revisión.");return bytes;}
        catch(Exception error)when(error is IOException or UnauthorizedAccessException){throw new BusinessException("El reporte congelado no está disponible. Revisa sus archivos y respaldo.");}
    }
    public async Task ResolveAsync(long userId,long id,string hash,string resolution,string reason,bool checkedServer)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);var row=await db.Set<SolicitudCorreo>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("La solicitud no existe.");
        if(ReviewHash(row)!=hash||row.Estado is not ("Fallida" or "Incierta" or "Pendiente" or "En_cuarentena"))throw new BusinessException("La solicitud cambió o requiere finalizar su reserva/conciliación. Revisa el estado actual.");
        if(row.Estado=="En_cuarentena"&&!await db.Set<Restauracion>().AnyAsync(x=>x.Id==row.RestauracionId&&x.Estado=="Validada"&&x.ConciliacionEstado=="Completada"&&x.NucleoReabiertoEn!=null))throw new BusinessException("El Propietario debe conciliar y reabrir antes de resolver la cuarentena.");
        if(!checkedServer)throw new BusinessException("Comprueba el servidor y el destinatario antes de resolver.");reason=AccessControl.Text(reason,"Motivo de resolución",1000);
        if(resolution is not ("reintentar_mismo" or "no_reenviar" or "nueva_solicitud"))throw new BusinessException("Selecciona una resolución válida.");
        if(resolution=="reintentar_mismo"&&row.Estado!="Fallida")throw new BusinessException("El resultado incierto no permite reintentar el mismo envío. Puedes cerrar o solicitar explícitamente uno nuevo advirtiendo el posible duplicado.");
        var before=row.Estado;row.Estado=resolution=="reintentar_mismo"?"Pendiente":"Resuelta";row.ResueltoPor=userId;row.ResueltoEn=Formats.Now();row.Resolucion=resolution;row.MotivoResolucion=reason;row.LeaseToken=null;row.LeaseHasta=null;row.ProximoIntentoEn=null;
        if(resolution=="reintentar_mismo")row.UltimoError=null;
        SolicitudCorreo? replacement=null;
        if(resolution=="nueva_solicitud"){replacement=new(){SolicitadoPor=userId,Fecha=Formats.Now(),Destinatario=row.Destinatario,ReporteTipo=row.ReporteTipo,ParametrosReporte=row.ParametrosReporte,ReporteReferencia=row.ReporteReferencia,ReporteHash=row.ReporteHash,ReporteGeneradoEn=row.ReporteGeneradoEn,Estado="Pendiente",ClaveSolicitud=Guid.NewGuid().ToString(),MessageId=Guid.NewGuid().ToString("N")+"@mega.local"};db.Add(replacement);}
        await db.SaveChangesAsync();operations.Audit(userId,"Resolver correo","SolicitudCorreo",id.ToString(),Guid.NewGuid().ToString(),new{antes=before,despues=row.Estado,resolucion=resolution,motivo=reason,nuevaSolicitudId=replacement?.Id,comprobacionDeclarada=checkedServer});await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<MailLease?> ClaimAsync()
    {
        await using var tx=await db.Database.BeginTransactionAsync();var now=Formats.Now();var expired=await db.Set<SolicitudCorreo>().Where(x=>x.Estado=="En_transmision"&&string.Compare(x.LeaseHasta,now)<=0).ToListAsync();
        foreach(var old in expired)
        {
            var attempt=await db.Set<IntentoCorreo>().SingleOrDefaultAsync(x=>x.SolicitudCorreoId==old.Id&&x.LeaseToken==old.LeaseToken&&x.Resultado=="en_curso");if(attempt is not null){attempt.Fin=now;attempt.Resultado="incierta";attempt.EtapaSMTP="registro_resultado";attempt.ErrorCodigo="Reserva vencida sin resultado confirmado.";}
            old.Estado="Incierta";old.UltimoError="Reserva vencida. No se reenviará automáticamente; comprueba servidor y destinatario.";old.LeaseToken=null;old.LeaseHasta=null;AutoAudit(old.Id,"Reserva de correo vencida","incierto",new{intentoId=attempt?.Id});
        }
        await db.SaveChangesAsync();var config=await settings.CurrentAsync();var row=await db.Set<SolicitudCorreo>().Where(x=>x.Estado=="Pendiente"&&(x.ProximoIntentoEn==null||string.Compare(x.ProximoIntentoEn,now)<=0)).OrderBy(x=>x.Id).FirstOrDefaultAsync();
        if(row is null){await tx.CommitAsync();return null;}
        if(config.SmtpHost is null){row.UltimoError="Correo deshabilitado. El reporte permanece conservado en este equipo.";await db.SaveChangesAsync();await tx.CommitAsync();return null;}
        byte[] content;
        try{content=await ReadFileAsync(row);}
        catch(BusinessException error){row.Estado="Fallida";row.UltimoError=error.Message;AutoAudit(row.Id,"Pausar correo por archivo","rechazado",new{row.UltimoError});await db.SaveChangesAsync();await tx.CommitAsync();return null;}
        row.Estado="En_transmision";row.LeaseToken=Guid.NewGuid().ToString();row.LeaseHasta=DateTime.UtcNow.AddMinutes(3).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");row.UltimoError=null;
        var attemptRow=new IntentoCorreo{SolicitudCorreoId=row.Id,ConfiguracionId=config.Id,Numero=checked((await db.Set<IntentoCorreo>().Where(x=>x.SolicitudCorreoId==row.Id).MaxAsync(x=>(long?)x.Numero)??0)+1),Inicio=now,Resultado="en_curso",EtapaSMTP="conexion",MessageId=row.MessageId,LeaseToken=row.LeaseToken};
        db.Add(attemptRow);await db.SaveChangesAsync();AutoAudit(row.Id,"Reservar envío de correo","pendiente",new{intentoId=attemptRow.Id,attemptRow.Numero,configuracionId=config.Id,row.LeaseHasta});await db.SaveChangesAsync();await tx.CommitAsync();return new(row.Id,row.LeaseToken,attemptRow.Id,new(config,row.Destinatario,row.MessageId,row.ReporteTipo,"MEGA-"+row.ReporteTipo+Path.GetExtension(row.ReporteReferencia),content));
    }
    public async Task FinishAsync(MailLease lease,MailAcceptance? accepted,MailDispatchException? failure)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var row=await db.Set<SolicitudCorreo>().SingleAsync(x=>x.Id==lease.RequestId);var attempt=await db.Set<IntentoCorreo>().SingleAsync(x=>x.Id==lease.AttemptId&&x.LeaseToken==lease.Token);
        if(row.Estado!="En_transmision"||row.LeaseToken!=lease.Token)
        {if(accepted is not null&&attempt.RespuestaReferencia is null){attempt.RespuestaReferencia=accepted.Reference;AutoAudit(row.Id,"Aceptación SMTP tardía","incierto",new{intentoId=attempt.Id,estadoConservado=row.Estado});await db.SaveChangesAsync();}await tx.CommitAsync();return;}
        var uncertain=failure?.Uncertain==true||string.CompareOrdinal(row.LeaseHasta,Formats.Now())<=0;var retry=!uncertain&&accepted is null&&failure?.Retryable==true&&attempt.Numero<3;
        attempt.Fin=Formats.Now();attempt.Resultado=uncertain?"incierta":accepted is not null?"aceptada_SMTP":"no_aceptada";attempt.EtapaSMTP=accepted is not null?"respuesta_final":failure?.Stage??"registro_resultado";attempt.CodigoSMTP=failure?.SmtpCode;attempt.RespuestaReferencia=accepted?.Reference;attempt.ErrorCodigo=failure?.Code;
        row.Estado=uncertain?"Incierta":accepted is not null?"Aceptada_SMTP":retry?"Pendiente":"Fallida";row.AceptadoEn=row.Estado=="Aceptada_SMTP"?attempt.Fin:null;row.ProximoIntentoEn=retry?DateTime.UtcNow.AddMinutes(2*attempt.Numero).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"):null;row.UltimoError=uncertain?"Resultado incierto. No se reenviará automáticamente; revisa servidor y destinatario.":failure?.Code;row.LeaseToken=null;row.LeaseHasta=null;
        AutoAudit(row.Id,"Resultado SMTP",uncertain?"incierto":accepted is not null?"exito":retry?"pendiente":"fallo",new{intentoId=attempt.Id,attempt.Resultado,attempt.EtapaSMTP,attempt.CodigoSMTP,row.ProximoIntentoEn});await db.SaveChangesAsync();await tx.CommitAsync();
    }
    private void AutoAudit(long id,string action,string result,object data)=>db.Add(new EventoAuditoria{Fecha=Formats.Now(),Origen="tarea_automatica",Accion=action,Entidad="SolicitudCorreo",EntidadId=id.ToString(),ClaveOperacion=Guid.NewGuid().ToString(),Resultado=result,DatosCambio=Formats.Json(data),VersionFormato=1});
}
public sealed class MailDispatcher(IServiceScopeFactory scopes,IMailTransport transport,RecoveryGate recovery)
{
    private readonly SemaphoreSlim gate=new(1,1);
    public async Task<bool> RunOnceAsync()
    {
        using var databaseLease=recovery.TryWorker();if(databaseLease is null)return false;
        if(!await gate.WaitAsync(0))return false;
        try
        {
            MailLease? lease;await using(var scope=scopes.CreateAsyncScope())lease=await scope.ServiceProvider.GetRequiredService<MailQueue>().ClaimAsync();if(lease is null)return false;
            MailAcceptance? accepted=null;MailDispatchException? failure=null;try{accepted=await transport.SendAsync(lease.Document);}catch(MailDispatchException error){failure=error;}catch{failure=new("El agente perdió el resultado del envío.",true,false,"registro_resultado");}
            await using var finish=scopes.CreateAsyncScope();await finish.ServiceProvider.GetRequiredService<MailQueue>().FinishAsync(lease,accepted,failure);return true;
        }
        finally{gate.Release();}
    }
}
public sealed class MailWorker(MailDispatcher dispatcher,IWebHostEnvironment environment,IConfiguration configuration,ILogger<MailWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(environment.IsEnvironment("Testing")||!configuration.GetValue("MEGA:MailAgentEnabled",true))return;using var timer=new PeriodicTimer(TimeSpan.FromSeconds(10));
        try{while(await timer.WaitForNextTickAsync(stoppingToken)){try{await dispatcher.RunOnceAsync();}catch(Exception error){logger.LogError(error,"El agente de correo no pudo completar su ciclo; revisar pendientes.");}}}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){}
    }
}
