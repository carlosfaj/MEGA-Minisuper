using System.Text.Json;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;
public sealed record CancellationChange(long? Id,long Version,string Key,long SaleId,string Reference,string Reason,string Evidence,bool Restock,bool PhysicalVerified);
public sealed record Reentry(long MovementId,long ProductId,string Product,long? LotId,long Quantity,long Scale,string CostState,long? Cost,long? CostScale,long? CostReferenceId);
public sealed record CancellationPayment(long Id,string Method,long Amount);
public sealed record CancellationReview(long RequestId,long Version,long SaleId,long SaleVersion,string Folio,string SaleHash,long Total,long Requester,string? Reason,string? Evidence,string RestockState,List<Reentry> Reentries,List<CancellationPayment> Payments,long Cash,long External);

public sealed class CancellationService(MegaDbContext db,AccessControl access,Operations operations)
{
    private async Task<bool> ManagerAsync(long id){var u=await access.RequireAsync(id,"Cajero","Administrador","Propietario");return await db.Set<Rol>().AnyAsync(x=>x.Id==u.RolId&&(x.Nombre=="Administrador"||x.Nombre=="Propietario"));}
    public async Task RequireReadAsync(long userId,long requestId)
    {
        var manager=await ManagerAsync(userId);var r=await db.Set<SolicitudAnulacion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==requestId)??throw new BusinessException("Solicitud no encontrada.");
        if(!manager&&r.SolicitadoPor!=userId&&!await db.Set<Venta>().AnyAsync(v=>v.Id==r.VentaId&&db.Set<TurnoCaja>().Any(t=>t.Id==v.TurnoId&&t.OperadorId==userId)))throw new BusinessException("Solo puedes consultar anulaciones de tus ventas o solicitudes.");
    }
    public async Task<long> SaveAsync(long userId,CancellationChange input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var manager=await ManagerAsync(userId);
        if(!Guid.TryParseExact(input.Key,"D",out _))throw new BusinessException("La preparación no tiene una clave válida.");
        var sale=await db.Set<Venta>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==input.SaleId)??throw new BusinessException("Venta no encontrada.");
        if(sale.Estado!="Confirmada")throw new BusinessException("La venta ya está anulada.");
        var shift=await db.Set<TurnoCaja>().AsNoTracking().SingleAsync(x=>x.Id==sale.TurnoId);if(!manager&&shift.OperadorId!=userId)throw new BusinessException("Solo puedes solicitar anulación de tus propias ventas.");
        if(Formats.Hash(sale.ComprobanteSnapshot)!=sale.ComprobanteHash)throw new BusinessException("El comprobante requiere verificar integridad.");
        var reference=AccessControl.Text(input.Reference,"Referencia",100);var reason=AccessControl.Text(input.Reason,"Motivo",1000);var evidence=AccessControl.Text(input.Evidence,"Evidencia",1000);
        if(await db.Set<SolicitudAnulacion>().AnyAsync(x=>x.Id!=input.Id&&(x.Referencia==reference||x.ClaveSolicitud==input.Key||x.VentaId==sale.Id&&(x.Estado=="En_preparacion"||x.Estado=="Pendiente"||x.Estado=="Autorizada"))))throw new BusinessException("La venta ya tiene una solicitud vigente, o se repitió su referencia/preparación.");
        var r=input.Id is null?new SolicitudAnulacion{VentaId=sale.Id,SolicitadoPor=userId,CreadoEn=Formats.Now(),Version=1,ClaveSolicitud=input.Key}:await db.Set<SolicitudAnulacion>().SingleOrDefaultAsync(x=>x.Id==input.Id)??throw new BusinessException("Solicitud no encontrada.");
        object? before=null;
        if(input.Id is not null)
        {
            if(r.SolicitadoPor!=userId||r.Estado is "Autorizada" or "Cancelada"||r.VentaId!=input.SaleId||r.ClaveSolicitud!=input.Key)throw new BusinessException("Esta preparación no puede modificarse.");
            AccessControl.Version(r.Version,input.Version);before=await ReviewAsync(r.Id);r.Version++;
            foreach(var a in await db.Set<Autorizacion>().Where(x=>x.SolicitudAnulacionId==r.Id&&x.InvalidadaEn==null&&x.Decision!="rechazada").ToListAsync()){a.InvalidadaEn=Formats.Now();a.MotivoInvalidacion="Solicitud editada; requiere revisión nueva.";}
        }
        if(input.Restock&&!input.PhysicalVerified)throw new BusinessException("Reincorporar exige comprobar físicamente toda la mercadería y dejar evidencia.");
        var original=await SourceReentries(sale.Id,input.Restock);
        var payments=await db.Set<Pago>().AsNoTracking().Where(x=>x.VentaId==sale.Id).ToListAsync();
        r.Referencia=reference;r.Motivo=reason;r.EvidenciaReferencia=evidence;r.Estado="En_preparacion";r.EnviadoEn=null;r.ReincorporacionEstado=input.Restock?"Verificada":"No_reincorporar";r.ReincorporacionDetalle=Formats.Json(original);
        r.EfectivoADevolverCentavos=payments.Where(x=>x.Medio=="efectivo").Sum(x=>x.ImporteCentavos);r.FondosExternosARegularizarCentavos=payments.Where(x=>x.Medio!="efectivo").Sum(x=>x.ImporteCentavos);
        r.RegularizacionEstado=r.EfectivoADevolverCentavos+r.FondosExternosARegularizarCentavos>0?"Pendiente":"No_requerida";r.RegularizacionDetalle="[]";
        if(input.Id is null)db.Add(r);await db.SaveChangesAsync();operations.Audit(userId,"Guardar solicitud de anulación","SolicitudAnulacion",r.Id.ToString(),Guid.NewGuid().ToString(),new{antes=before,despues=await ReviewAsync(r.Id),reincorporacionFisicaDeclarada=input.Restock&&input.PhysicalVerified,alteraExistenciasOCaja=false});
        await db.SaveChangesAsync();await tx.CommitAsync();return r.Id;
    }
    private async Task<List<Reentry>> SourceReentries(long saleId,bool restock)
    {
        var details=await db.Set<VentaDetalle>().AsNoTracking().Where(x=>x.VentaId==saleId).ToDictionaryAsync(x=>x.Id);
        var ids=details.Keys.ToList();var movements=await db.Set<MovimientoInventario>().AsNoTracking().Where(x=>x.VentaDetalleId!=null&&ids.Contains(x.VentaDetalleId.Value)).OrderBy(x=>x.Secuencia).ToListAsync();
        return movements.Select(m=>new Reentry(m.Id,m.ProductoId,details[m.VentaDetalleId!.Value].DescripcionHistorica,m.LoteId,restock?-m.CantidadEscalada:0,m.Escala,m.EstadoCosto,m.CostoUnitEscalado,m.EscalaCosto,m.CostoReferenciaId)).ToList();
    }
    public async Task<CancellationReview> ReviewAsync(long id)
    {
        var r=await db.Set<SolicitudAnulacion>().AsNoTracking().SingleAsync(x=>x.Id==id);var v=await db.Set<Venta>().AsNoTracking().SingleAsync(x=>x.Id==r.VentaId);
        var lines=JsonSerializer.Deserialize<List<Reentry>>(r.ReincorporacionDetalle,new JsonSerializerOptions(JsonSerializerDefaults.Web))??[];
        var payments=await db.Set<Pago>().AsNoTracking().Where(x=>x.VentaId==v.Id).OrderBy(x=>x.Id).Select(x=>new CancellationPayment(x.Id,x.Medio,x.ImporteCentavos)).ToListAsync();
        return new(r.Id,r.Version,v.Id,v.Version,v.Folio,v.ComprobanteHash,v.TotalCentavos,r.SolicitadoPor,r.Motivo,r.EvidenciaReferencia,r.ReincorporacionEstado,lines,payments,r.EfectivoADevolverCentavos,r.FondosExternosARegularizarCentavos);
    }
    private async Task ReadyAsync(CancellationReview review)
    {
        var sale=await db.Set<Venta>().AsNoTracking().SingleAsync(x=>x.Id==review.SaleId);
        if(sale.Estado!="Confirmada"||Formats.Hash(sale.ComprobanteSnapshot)!=review.SaleHash)throw new BusinessException("La venta cambió o su comprobante requiere revisión.");
        if(review.RestockState is not("Verificada" or "No_reincorporar"))throw new BusinessException("Define la reincorporación con evidencia antes de enviar.");
        var source=await SourceReentries(review.SaleId,review.RestockState=="Verificada");
        if(source.Count==0||Formats.Json(source)!=Formats.Json(review.Reentries))throw new BusinessException("La reincorporación no coincide con las cantidades, lotes y costos originales.");
        if(review.Payments.Sum(x=>x.Amount)!=review.Total||review.Cash!=review.Payments.Where(x=>x.Method=="efectivo").Sum(x=>x.Amount)||review.External!=review.Payments.Where(x=>x.Method!="efectivo").Sum(x=>x.Amount))throw new BusinessException("La regularización no coincide con los pagos originales.");
    }
    public async Task<long> SubmitAsync(long userId,long id,long version)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await ManagerAsync(userId);var r=await db.Set<SolicitudAnulacion>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Solicitud no encontrada.");
        if(r.SolicitadoPor!=userId||r.Estado is not("En_preparacion" or "Pendiente"))throw new BusinessException("Solo quien preparó la solicitud vigente puede enviarla.");AccessControl.Version(r.Version,version);
        var review=await ReviewAsync(id);await ReadyAsync(review);var snapshot=Formats.Json(review);var hash=Formats.Hash(snapshot);
        var existing=await db.Set<Autorizacion>().AsNoTracking().SingleOrDefaultAsync(x=>x.SolicitudAnulacionId==id&&x.ObjetoVersion==version&&x.InvalidadaEn==null&&x.Decision!="rechazada");
        if(existing is not null){if(existing.ContenidoHash!=hash)throw new BusinessException("El contenido cambió; guarda una versión nueva.");await tx.CommitAsync();return existing.Id;}
        r.Estado="Pendiente";r.EnviadoEn=Formats.Now();var a=new Autorizacion{SolicitudAnulacionId=id,SolicitanteId=userId,FechaSolicitud=r.EnviadoEn,Decision="pendiente",Modalidad="segunda_cuenta",ObjetoVersion=version,ContenidoSnapshot=snapshot,ContenidoHash=hash};db.Add(a);await db.SaveChangesAsync();operations.Audit(userId,"Enviar anulación a revisión","SolicitudAnulacion",id.ToString(),Guid.NewGuid().ToString(),new{version,hash},authorizationId:a.Id);await db.SaveChangesAsync();await tx.CommitAsync();return a.Id;
    }
    public async Task<long?> DecideAsync(long userId,long authorizationId,long version,string hash,bool approve,string reason)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);var a=await db.Set<Autorizacion>().SingleOrDefaultAsync(x=>x.Id==authorizationId&&x.SolicitudAnulacionId!=null)??throw new BusinessException("Revisión no encontrada.");
        if(a.SolicitanteId==userId)throw new BusinessException("Debe decidir otra cuenta de gestión. No se permite autoaprobar.");
        if(a.InvalidadaEn!=null||a.ObjetoVersion!=version||a.ContenidoHash!=hash||Formats.Hash(a.ContenidoSnapshot)!=hash)throw new BusinessException("La revisión está obsoleta. Abre la versión vigente.");
        var r=await db.Set<SolicitudAnulacion>().SingleAsync(x=>x.Id==a.SolicitudAnulacionId);
        if(approve&&r.Estado=="Autorizada"&&a.Decision=="autorizada"&&a.AutorizadorId==userId){await tx.CommitAsync();return r.OperacionId;}
        if(r.Estado!="Pendiente"||a.Decision!="pendiente")throw new BusinessException("La solicitud ya fue resuelta.");
        var review=await ReviewAsync(r.Id);if(r.Version!=version||Formats.Hash(Formats.Json(review))!=hash)throw new BusinessException("La solicitud o venta cambiaron. Guarda y revisa una versión nueva.");
        reason=AccessControl.Text(reason,"Motivo de decisión",1000);if(approve)await ReadyAsync(review);
        var now=Formats.Now();a.AutorizadorId=userId;a.FechaDecision=now;a.Decision=approve?"autorizada":"rechazada";a.Motivo=reason;
        if(!approve)r.Estado="Rechazada";
        else
        {
            foreach(var group in review.Reentries.Where(x=>x.Quantity>0).GroupBy(x=>x.ProductId))
            {try{var balance=await db.Set<MovimientoInventario>().Where(x=>x.ProductoId==group.Key).SumAsync(x=>(long?)x.CantidadEscalada)??0;foreach(var l in group)balance=checked(balance+l.Quantity);}catch(OverflowException){throw new BusinessException("La reincorporación supera la capacidad de existencias.");}}
            var sale=await db.Set<Venta>().SingleAsync(x=>x.Id==r.VentaId);var op=new OperacionInventario{Tipo="reversion",ReversionDeId=sale.OperacionId,Fecha=now,ConfirmadoEn=now,SecuenciaConfirmacion=checked((await db.Set<OperacionInventario>().MaxAsync(x=>(long?)x.SecuenciaConfirmacion)??0)+1),CreadoPor=r.SolicitadoPor,EjecutadoPor=userId,AutorizacionId=a.Id,ClaveOperacion="anulacion-"+r.ClaveSolicitud,Motivo=reason};db.Add(op);await db.SaveChangesAsync();
            r.OperacionId=op.Id;r.Estado="Autorizada";sale.Estado="Anulada";sale.Version++;var seq=0;
            foreach(var line in review.Reentries.Where(x=>x.Quantity>0))db.Add(new MovimientoInventario{OperacionId=op.Id,ProductoId=line.ProductId,LoteId=line.LotId,Secuencia=++seq,MovimientoRevertidoId=line.MovementId,CantidadEscalada=line.Quantity,Escala=line.Scale,EstadoCosto=line.CostState,CostoUnitEscalado=line.Cost,EscalaCosto=line.CostScale,CostoReferenciaId=line.MovementId});
        }
        operations.Audit(userId,approve?"Autorizar anulación":"Rechazar anulación","SolicitudAnulacion",r.Id.ToString(),"decision-anulacion-"+a.Id,new{version,hash,motivo=reason,r.ReincorporacionEstado,r.EfectivoADevolverCentavos,efectivoDevuelto=0,regularizacion=r.RegularizacionEstado},authorizationId:a.Id);
        await db.SaveChangesAsync();await tx.CommitAsync();return r.OperacionId;
    }
    public async Task<long> RefundAsync(long userId,long id,long version,bool handedOver,string evidence)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var manager=await ManagerAsync(userId);await operations.RequireCashAsync(userId);
        var r=await db.Set<SolicitudAnulacion>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Solicitud no encontrada.");
        if(!manager&&r.SolicitadoPor!=userId)throw new BusinessException("Esta devolución debe registrarla su solicitante o una cuenta de gestión con caja propia.");
        if(r.Estado!="Autorizada")throw new BusinessException("La anulación requiere aprobación antes de devolver efectivo.");
        var prior=await db.Set<MovimientoCaja>().AsNoTracking().SingleOrDefaultAsync(x=>x.SolicitudAnulacionId==id&&x.Tipo=="devolucion_efectivo");if(prior is not null){await tx.CommitAsync();return prior.Id;}
        AccessControl.Version(r.Version,version);if(!handedOver)throw new BusinessException("Confirma la entrega efectiva del dinero al cliente.");evidence=AccessControl.Text(evidence,"Evidencia de devolución",1000);
        if(r.EfectivoADevolverCentavos<=0)throw new BusinessException("No hay efectivo por devolver.");var cash=await operations.CashAsync(userId);if(cash.Shift is null)throw new BusinessException("Abre tu caja para registrar el egreso real.");if(cash.Expected<r.EfectivoADevolverCentavos)throw new BusinessException("Tu caja no tiene efectivo registrado suficiente. La regularización queda pendiente.");
        var movement=new MovimientoCaja{TurnoId=cash.Shift.Id,VentaId=r.VentaId,OperacionId=r.OperacionId!.Value,SolicitudAnulacionId=r.Id,Tipo="devolucion_efectivo",ImporteCentavos=-r.EfectivoADevolverCentavos,RegistradoPor=userId,Fecha=Formats.Now(),EvidenciaReferencia=evidence};db.Add(movement);await db.SaveChangesAsync();
        r.EfectivoDevueltoCentavos=r.EfectivoADevolverCentavos;r.Version++;r.RegularizacionDetalle=Formats.Json(new[]{new{tipo="efectivo",movement.Id,movement.TurnoId,importe=r.EfectivoDevueltoCentavos,evidencia=evidence,registradoPor=userId,fecha=movement.Fecha}});
        if(r.FondosExternosRegularizadosCentavos==r.FondosExternosARegularizarCentavos){r.RegularizacionEstado="Completada";r.RegularizadoPor=userId;r.RegularizadoEn=Formats.Now();}
        var shift=await db.Set<TurnoCaja>().SingleAsync(x=>x.Id==cash.Shift.Id);shift.Version++;
        operations.Audit(userId,"Registrar devolución efectiva","SolicitudAnulacion",id.ToString(),"devolucion-"+r.ClaveSolicitud,new{movimientoId=movement.Id,turnoId=movement.TurnoId,importe=r.EfectivoDevueltoCentavos,evidencia=evidence});await db.SaveChangesAsync();await tx.CommitAsync();return movement.Id;
    }
    public async Task CancelAsync(long userId,long id,long version,string reason)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await ManagerAsync(userId);var r=await db.Set<SolicitudAnulacion>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Solicitud no encontrada.");
        if(r.SolicitadoPor!=userId||r.Estado is "Autorizada" or "Cancelada")throw new BusinessException("Esta solicitud no puede cancelarse.");AccessControl.Version(r.Version,version);reason=AccessControl.Text(reason,"Motivo de cancelación",1000);
        foreach(var a in await db.Set<Autorizacion>().Where(x=>x.SolicitudAnulacionId==id&&x.Decision=="pendiente"&&x.InvalidadaEn==null).ToListAsync()){a.InvalidadaEn=Formats.Now();a.MotivoInvalidacion=reason;}r.Estado="Cancelada";r.Version++;
        operations.Audit(userId,"Cancelar solicitud de anulación","SolicitudAnulacion",id.ToString(),Guid.NewGuid().ToString(),new{motivo=reason});await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
