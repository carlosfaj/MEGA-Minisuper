using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record PrintLease(long RequestId,string Token,long AttemptId,PrintDocument Document);
public sealed class PrintQueue(MegaDbContext db,AccessControl access,Operations operations,SettingsService settings)
{
    public static string ReviewHash(SolicitudImpresion row)=>Formats.Hash(Formats.Json(row));
    public async Task<Venta> RequireSaleAsync(long userId,long saleId)
    {
        var user=await access.RequireAsync(userId,"Cajero","Administrador","Propietario");
        var sale=await db.Set<Venta>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==saleId)??throw new BusinessException("La venta no existe.");
        var manager=await db.Set<Rol>().AnyAsync(x=>x.Id==user.RolId&&(x.Nombre=="Administrador"||x.Nombre=="Propietario"));
        if(!manager&&(user.PermisoOperativoCaja!=1||!await db.Set<TurnoCaja>().AnyAsync(x=>x.Id==sale.TurnoId&&x.OperadorId==userId&&x.Estado=="Abierto")))throw new BusinessException("Solo puedes gestionar comprobantes de tu turno abierto.");
        return sale;
    }
    public async Task<long> CopyAsync(long userId,long saleId,string key,string reason)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var sale=await RequireSaleAsync(userId,saleId);
        if(!Guid.TryParseExact(key,"D",out _))throw new BusinessException("Recarga la solicitud de copia.");
        var prior=await db.Set<SolicitudImpresion>().SingleOrDefaultAsync(x=>x.ClaveSolicitud==key);
        if(prior is not null){if(prior.VentaId!=saleId||prior.SolicitadoPor!=userId||prior.Tipo!="copia")throw new BusinessException("La clave pertenece a otra solicitud.");return prior.Id;}
        if(sale.Estado!="Confirmada")throw new BusinessException("La venta anulada conserva su comprobante en pantalla; no admite nuevas impresiones.");
        if(await db.Set<SolicitudImpresion>().AnyAsync(x=>x.VentaId==saleId&&(x.Estado=="Pendiente"||x.Estado=="En_proceso"||x.Estado=="Incierta"||x.Estado=="En_cuarentena"||x.Estado=="Fallida")))
            throw new BusinessException("Hay un trabajo sin resolver. Revisa su estado antes de emitir una copia.");
        var row=await AddCopyAsync(userId,sale,key,AccessControl.Text(reason,"Motivo de copia",1000));
        await db.SaveChangesAsync();await tx.CommitAsync();return row.Id;
    }
    private async Task<SolicitudImpresion> AddCopyAsync(long userId,Venta sale,string key,string reason)
    {
        var config=await settings.CurrentAsync();
        if(string.IsNullOrWhiteSpace(config.ImpresoraNombre))throw new BusinessException("Configura la impresora antes de solicitar una copia.");
        var number=checked((await db.Set<SolicitudImpresion>().Where(x=>x.VentaId==sale.Id).MaxAsync(x=>(long?)x.CopiaNumero)??0)+1);
        var row=new SolicitudImpresion{VentaId=sale.Id,SolicitadoPor=userId,Tipo="copia",CopiaNumero=number,Estado="Pendiente",ClaveSolicitud=key,Fecha=Formats.Now(),ImpresoraHistorica=config.ImpresoraNombre,ContenidoHash=sale.ComprobanteHash};
        db.Add(row);await db.SaveChangesAsync();operations.Audit(userId,"Solicitar copia","SolicitudImpresion",row.Id.ToString(),key,new{sale.Folio,copiaNumero=number,motivo=reason,row.ContenidoHash,row.ImpresoraHistorica});return row;
    }
    public async Task ResolveAsync(long userId,long id,string hash,string resolution,string reason,string evidence,bool checkedQueue)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);
        var row=await db.Set<SolicitudImpresion>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("El trabajo no existe.");
        if(ReviewHash(row)!=hash)throw new BusinessException("El trabajo cambió. Revisa su estado actual.");
        if(row.Estado is not ("Fallida" or "Incierta" or "Pendiente"))throw new BusinessException("Este estado no admite resolución manual. Los trabajos en proceso deben finalizar o vencer; la cuarentena requiere conciliación.");
        if(!checkedQueue)throw new BusinessException("Comprueba la cola de Windows y el papel antes de resolver.");
        reason=AccessControl.Text(reason,"Motivo de resolución",1000);evidence=AccessControl.Text(evidence,"Evidencia",1000);
        if(resolution is not ("reintentar_mismo" or "no_reenviar" or "emitir_copia" or "contingencia_manual"))throw new BusinessException("Selecciona una resolución válida.");
        if(resolution=="reintentar_mismo"&&row.Estado!="Fallida")throw new BusinessException("Solo se reintenta el mismo trabajo cuando consta que no fue aceptado. Un resultado incierto necesita otra resolución explícita.");
        var sale=await db.Set<Venta>().SingleAsync(x=>x.Id==row.VentaId);
        if(sale.Estado!="Confirmada"&&resolution is "reintentar_mismo" or "emitir_copia")throw new BusinessException("Una venta anulada no admite envío a impresora.");
        var before=new{row.Estado,row.UltimoError};
        row.Resolucion=resolution;row.ResueltoPor=userId;row.ResueltoEn=Formats.Now();row.MotivoResolucion=reason;row.Estado=resolution=="reintentar_mismo"?"Pendiente":"Resuelta";
        row.LeaseToken=null;row.LeaseHasta=null;row.ProximoIntentoEn=null;
        if(resolution=="reintentar_mismo")row.UltimoError=null;
        await db.SaveChangesAsync();
        if(resolution=="emitir_copia")await AddCopyAsync(userId,sale,Guid.NewGuid().ToString(),reason+"; evidencia: "+evidence);
        if(resolution=="contingencia_manual")db.Add(new EntregaComprobante{VentaId=row.VentaId,SolicitudImpresionId=row.Id,Tipo="manual_contingencia",Fecha=Formats.Now(),Motivo=reason,RegistradoPor=userId,AutorizadoPor=userId,EvidenciaReferencia=evidence});
        operations.Audit(userId,"Resolver impresión","SolicitudImpresion",id.ToString(),Guid.NewGuid().ToString(),new{antes=before,despues=row.Estado,resolucion=resolution,motivo=reason,evidencia=evidence,colaYPapelComprobados=checkedQueue});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<long> DeliverAsync(long userId,long id,string evidence,bool handedOver)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        var row=await db.Set<SolicitudImpresion>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("El trabajo no existe.");await RequireSaleAsync(userId,row.VentaId);
        if(row.Estado!="Aceptada")throw new BusinessException("La entrega impresa requiere un trabajo aceptado por Windows.");
        if(!handedOver)throw new BusinessException("Confirma que se entregó el comprobante físico.");
        var existing=await db.Set<EntregaComprobante>().SingleOrDefaultAsync(x=>x.SolicitudImpresionId==id&&x.Tipo!="manual_contingencia");if(existing is not null)return existing.Id;
        var delivery=new EntregaComprobante{VentaId=row.VentaId,SolicitudImpresionId=id,Tipo=row.Tipo=="original"?"impreso_original":"copia",Fecha=Formats.Now(),RegistradoPor=userId,EvidenciaReferencia=AccessControl.Text(evidence,"Evidencia de entrega",1000)};
        db.Add(delivery);await db.SaveChangesAsync();operations.Audit(userId,"Registrar entrega de comprobante","EntregaComprobante",delivery.Id.ToString(),Guid.NewGuid().ToString(),new{solicitudId=id,delivery.Tipo,delivery.EvidenciaReferencia});await db.SaveChangesAsync();await tx.CommitAsync();return delivery.Id;
    }
    public async Task<PrintLease?> ClaimAsync()
    {
        await using var tx=await db.Database.BeginTransactionAsync();var now=Formats.Now();
        var expired=await db.Set<SolicitudImpresion>().Where(x=>x.Estado=="En_proceso"&&string.Compare(x.LeaseHasta,now)<=0).ToListAsync();
        foreach(var old in expired)
        {
            var attempt=await db.Set<IntentoImpresion>().SingleOrDefaultAsync(x=>x.SolicitudImpresionId==old.Id&&x.LeaseToken==old.LeaseToken&&x.Resultado=="en_curso");
            if(attempt is not null){attempt.Fin=now;attempt.Resultado="incierta";attempt.ErrorCodigo="Reserva vencida sin resultado confirmado.";}
            old.Estado="Incierta";old.UltimoError="La reserva venció. Verifica la cola y el papel; no se reenviará automáticamente.";old.LeaseToken=null;old.LeaseHasta=null;AutoAudit(old.Id,"Reserva de impresión vencida","incierto",new{intentoId=attempt?.Id});
        }
        await db.SaveChangesAsync();var config=await settings.CurrentAsync();
        if(config.ImpresionHabilitada==0){await tx.CommitAsync();return null;}
        var candidates=await db.Set<SolicitudImpresion>().Where(x=>x.Estado=="Pendiente"&&(x.ProximoIntentoEn==null||string.Compare(x.ProximoIntentoEn,now)<=0)).OrderBy(x=>x.Id).Take(100).ToListAsync();
        foreach(var row in candidates)
        {
            var sale=await db.Set<Venta>().AsNoTracking().SingleAsync(x=>x.Id==row.VentaId);
            if(sale.Estado!="Confirmada"||Formats.Hash(sale.ComprobanteSnapshot)!=row.ContenidoHash||sale.ComprobanteHash!=row.ContenidoHash)
            {row.Estado="Fallida";row.UltimoError="Venta anulada o comprobante sin integridad. Requiere revisión de gestión.";AutoAudit(row.Id,"Pausar impresión","rechazado",new{row.UltimoError});continue;}
            if(row.ImpresoraHistorica!=config.ImpresoraNombre){row.UltimoError="La impresora histórica no coincide con la habilitada. Gestión debe resolver este trabajo.";continue;}
            var token=Guid.NewGuid().ToString();row.Estado="En_proceso";row.LeaseToken=token;row.LeaseHasta=DateTime.UtcNow.AddMinutes(3).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");row.UltimoError=null;
            var attempt=new IntentoImpresion{SolicitudImpresionId=row.Id,Numero=checked((await db.Set<IntentoImpresion>().Where(x=>x.SolicitudImpresionId==row.Id).MaxAsync(x=>(long?)x.Numero)??0)+1),Inicio=now,Resultado="en_curso",Impresora=row.ImpresoraHistorica,AgenteIdentidad=Environment.MachineName+"/MEGA",LeaseToken=token};
            db.Add(attempt);await db.SaveChangesAsync();AutoAudit(row.Id,"Reservar impresión","pendiente",new{intentoId=attempt.Id,attempt.Numero,row.LeaseHasta});await db.SaveChangesAsync();await tx.CommitAsync();
            return new(row.Id,token,attempt.Id,new(row.Id,row.ImpresoraHistorica,sale.Folio,row.CopiaNumero,ReceiptText.Build(sale.ComprobanteSnapshot,row.CopiaNumero)));
        }
        await db.SaveChangesAsync();await tx.CommitAsync();return null;
    }
    public async Task FinishAsync(PrintLease lease,PrintAcceptance? acceptance,PrintDispatchException? failure)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var row=await db.Set<SolicitudImpresion>().SingleAsync(x=>x.Id==lease.RequestId);
        var attempt=await db.Set<IntentoImpresion>().SingleAsync(x=>x.Id==lease.AttemptId&&x.LeaseToken==lease.Token);
        if(row.Estado!="En_proceso"||row.LeaseToken!=lease.Token)
        {
            if(acceptance is not null&&attempt.IdTrabajoSO is null)
            {
                attempt.IdTrabajoSO=acceptance.JobId;attempt.Evidencia=acceptance.Evidence+"; resultado recibido después de vencer la reserva, sin cambiar la resolución.";
                AutoAudit(row.Id,"Resultado tardío de impresión","incierto",new{intentoId=attempt.Id,attempt.IdTrabajoSO,estadoConservado=row.Estado});await db.SaveChangesAsync();
            }
            await tx.CommitAsync();return;
        }
        attempt.Fin=Formats.Now();attempt.IdTrabajoSO=acceptance?.JobId;attempt.Evidencia=acceptance?.Evidence;attempt.ErrorCodigo=failure?.Code;
        var uncertain=failure?.Uncertain==true||string.CompareOrdinal(row.LeaseHasta,Formats.Now())<=0;
        row.Estado=uncertain?"Incierta":acceptance is not null?"Aceptada":"Fallida";
        attempt.Resultado=uncertain?"incierta":acceptance is not null?"aceptada_SO":"no_aceptada";
        row.AceptadaEn=row.Estado=="Aceptada"?attempt.Fin:null;row.UltimoError=uncertain?"Resultado incierto. Comprueba la cola de Windows y el papel antes de resolver.":failure?.Code;
        row.LeaseToken=null;row.LeaseHasta=null;
        AutoAudit(row.Id,"Resultado de impresión",uncertain?"incierto":acceptance is not null?"exito":"fallo",new{intentoId=attempt.Id,attempt.Resultado,attempt.IdTrabajoSO,attempt.ErrorCodigo});await db.SaveChangesAsync();await tx.CommitAsync();
    }
    private void AutoAudit(long id,string action,string result,object data)=>db.Add(new EventoAuditoria{Fecha=Formats.Now(),Origen="tarea_automatica",Accion=action,Entidad="SolicitudImpresion",EntidadId=id.ToString(),ClaveOperacion=Guid.NewGuid().ToString(),Resultado=result,DatosCambio=Formats.Json(data),VersionFormato=1});
}

public sealed class PrintDispatcher(IServiceScopeFactory scopes,IPrintTransport transport)
{
    private readonly SemaphoreSlim gate=new(1,1);
    public async Task<bool> RunOnceAsync()
    {
        if(!await gate.WaitAsync(0))return false;
        try
        {
            PrintLease? lease;
            await using(var scope=scopes.CreateAsyncScope())lease=await scope.ServiceProvider.GetRequiredService<PrintQueue>().ClaimAsync();
            if(lease is null)return false;
            PrintAcceptance? accepted=null;PrintDispatchException? failure=null;
            try{accepted=await transport.SendAsync(lease.Document);}
            catch(PrintDispatchException error){failure=error;}
            catch{failure=new("El agente perdió el resultado del controlador.",true);}
            await using var finish=scopes.CreateAsyncScope();await finish.ServiceProvider.GetRequiredService<PrintQueue>().FinishAsync(lease,accepted,failure);return true;
        }
        finally{gate.Release();}
    }
}
public sealed class PrintWorker(PrintDispatcher dispatcher,IWebHostEnvironment environment,IConfiguration configuration,ILogger<PrintWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(environment.IsEnvironment("Testing")||!configuration.GetValue("MEGA:PrintingAgentEnabled",true))return;
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(5));
        try{while(await timer.WaitForNextTickAsync(stoppingToken)){try{await dispatcher.RunOnceAsync();}catch(Exception error){logger.LogError(error,"El agente de impresión no pudo completar su ciclo; revisar trabajos pendientes.");}}}
        catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){}
    }
}
