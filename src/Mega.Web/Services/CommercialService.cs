using System.Globalization;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;
public sealed record PromotionChange(long? Id,long Version,string Name,string Type,string Value,string Start,string End,bool Active,long Priority,List<long> Presentations);
public sealed record TaxChange(long? Id,string? Hash,string Name,string Rate,string Start,string? End,bool Exempt,string? ExemptionReason,bool Active,List<long> Products);
public sealed record AppliedPromotion(long Id,long Version,string Name,string Type,long Value,long Scale,long Priority,long Amount);
public sealed record AppliedTax(long Id,string Name,long Rate,long Scale,long Base,bool Exempt,string? ExemptionReason,long Amount,string Start,string? End);
public sealed record CommercialLine(long Discount,long Tax,long Total,AppliedPromotion? Promotion,List<AppliedTax> Taxes);

public sealed class CommercialService(MegaDbContext db,AccessControl access,Operations operations)
{
    public static string TaxHash(Impuesto t,IEnumerable<ProductoImpuesto> links)=>Formats.Hash(Formats.Json(new{t.Id,t.Nombre,t.TasaEscalada,t.EscalaTasa,t.VigenteDesde,t.VigenteHasta,t.EsExencion,t.MotivoExencion,t.Activo,productos=links.OrderBy(x=>x.ProductoId).ThenBy(x=>x.VigenteDesde).Select(x=>new{x.ProductoId,x.VigenteDesde,x.VigenteHasta})}));
    public async Task<long> SavePromotionAsync(long userId,PromotionChange input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);
        if(input.Type is not("porcentaje" or "importe_fijo")||input.Priority<0)throw new BusinessException("Tipo o prioridad de promoción no válidos.");
        var value=Formats.Parse(input.Value);if(input.Type=="porcentaje"&&value>100)throw new BusinessException("El porcentaje debe estar entre 0 y 100.");
        var scale=input.Type=="porcentaje"?6:2;var amount=Formats.Scaled(value,scale);var start=Utc(input.Start);var end=Utc(input.End);
        if(string.CompareOrdinal(end,start)<=0)throw new BusinessException("El fin debe ser posterior al inicio.");
        var ids=input.Presentations.Distinct().ToList();if(ids.Count is 0 or >500||await db.Set<Presentacion>().CountAsync(x=>ids.Contains(x.Id))!=ids.Count)throw new BusinessException("Selecciona presentaciones válidas.");
        var p=input.Id is null?new Promocion{CreadoPor=userId,CreadoEn=Formats.Now(),Version=1}:await db.Set<Promocion>().SingleOrDefaultAsync(x=>x.Id==input.Id)??throw new BusinessException("Promoción no encontrada.");
        object? before=null;
        if(input.Id is not null){AccessControl.Version(p.Version,input.Version);before=new{p.Nombre,p.Tipo,p.ValorEscalado,p.EscalaValor,p.Inicio,p.Fin,p.Activa,p.Version};p.Version++;db.RemoveRange(await db.Set<PromocionProducto>().Where(x=>x.PromocionId==p.Id).ToListAsync());await db.SaveChangesAsync();}
        p.Nombre=AccessControl.Text(input.Name,"Nombre");p.Tipo=input.Type;p.ValorEscalado=amount;p.EscalaValor=scale;p.Inicio=start;p.Fin=end;p.Activa=input.Active?1:0;
        if(input.Id is null)db.Add(p);await db.SaveChangesAsync();foreach(var id in ids)db.Add(new PromocionProducto{PromocionId=p.Id,PresentacionId=id,ReglaAplicacion="exclusiva",Prioridad=input.Priority});
        operations.Audit(userId,"Guardar promoción","Promocion",p.Id.ToString(),Guid.NewGuid().ToString(),new{antes=before,despues=new{p.Nombre,p.Tipo,p.ValorEscalado,p.EscalaValor,p.Inicio,p.Fin,p.Activa,p.Version,presentaciones=ids,prioridad=input.Priority}});
        await db.SaveChangesAsync();await tx.CommitAsync();return p.Id;
    }
    public async Task<long> SaveTaxAsync(long userId,TaxChange input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);
        var rate=Formats.Parse(input.Rate);if(rate>100)throw new BusinessException("La tasa configurada admite entre 0 y 100 por ciento.");
        var scaled=Formats.Scaled(rate,6);var reason=input.Exempt?AccessControl.Text(input.ExemptionReason,"Motivo de exención",1000):null;
        if(input.Exempt&&scaled!=0)throw new BusinessException("Una exención debe tener tasa cero y motivo; no equivale a una tasa ordinaria de cero.");
        var start=Utc(input.Start);var end=string.IsNullOrWhiteSpace(input.End)?null:Utc(input.End);if(end is not null&&string.CompareOrdinal(end,start)<=0)throw new BusinessException("El fin debe ser posterior al inicio.");
        var ids=input.Products.Distinct().ToList();if(ids.Count is 0 or >500||await db.Set<Producto>().CountAsync(x=>ids.Contains(x.Id))!=ids.Count)throw new BusinessException("Selecciona productos válidos.");
        var t=input.Id is null?new Impuesto{CreadoPor=userId,CreadoEn=Formats.Now()}:await db.Set<Impuesto>().SingleOrDefaultAsync(x=>x.Id==input.Id)??throw new BusinessException("Impuesto no encontrado.");
        object? before=null;
        if(input.Id is not null)
        {
            var links=await db.Set<ProductoImpuesto>().Where(x=>x.ImpuestoId==t.Id).ToListAsync();if(TaxHash(t,links)!=input.Hash)throw new BusinessException("La condición cambió. Recarga antes de guardar.");
            before=new{hash=input.Hash,t.Nombre,t.TasaEscalada,t.EscalaTasa,t.VigenteDesde,t.VigenteHasta,t.EsExencion,t.MotivoExencion,t.Activo};db.RemoveRange(links);await db.SaveChangesAsync();
        }
        t.Nombre=AccessControl.Text(input.Name,"Nombre");t.TasaEscalada=scaled;t.EscalaTasa=6;t.VigenteDesde=start;t.VigenteHasta=end;t.EsExencion=input.Exempt?1:0;t.MotivoExencion=reason;t.Activo=input.Active?1:0;
        if(input.Id is null)db.Add(t);await db.SaveChangesAsync();foreach(var id in ids)db.Add(new ProductoImpuesto{ProductoId=id,ImpuestoId=t.Id,VigenteDesde=start,VigenteHasta=end});
        operations.Audit(userId,"Guardar condición de impuesto","Impuesto",t.Id.ToString(),Guid.NewGuid().ToString(),new{antes=before,despues=new{t.Nombre,t.TasaEscalada,t.EscalaTasa,t.VigenteDesde,t.VigenteHasta,t.EsExencion,t.MotivoExencion,t.Activo,productos=ids},acreditaValidezFiscal=false});
        await db.SaveChangesAsync();await tx.CommitAsync();return t.Id;
    }
    public static string Utc(string text)
    {
        if(!DateTime.TryParseExact(text,["yyyy-MM-ddTHH:mm","yyyy-MM-ddTHH:mm:ss"],CultureInfo.InvariantCulture,DateTimeStyles.None,out var value))throw new BusinessException("Fecha/hora no válida.");
        return new DateTimeOffset(value,TimeSpan.FromHours(-6)).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'",CultureInfo.InvariantCulture);
    }
    public static string Local(string utc)=>DateTimeOffset.Parse(utc,CultureInfo.InvariantCulture).ToOffset(TimeSpan.FromHours(-6)).ToString("yyyy-MM-ddTHH:mm:ss",CultureInfo.InvariantCulture);
}

public sealed class CommercialCalculator(MegaDbContext db)
{
    public async Task<CommercialLine> CalculateAsync(long presentationId,long productId,decimal quantity,long subtotal,string now)
    {
        var candidates=await (from link in db.Set<PromocionProducto>().AsNoTracking() join p in db.Set<Promocion>().AsNoTracking() on link.PromocionId equals p.Id
            where link.PresentacionId==presentationId&&p.Activa==1&&string.Compare(p.Inicio,now)<=0&&string.Compare(p.Fin,now)>0 orderby link.Prioridad,p.Id select new{Promo=p,link.Prioridad,link.ReglaAplicacion}).ToListAsync();
        AppliedPromotion? promotion=null;long discount=0;
        if(candidates.FirstOrDefault() is {} selected)
        {
            var p=selected.Promo;if(selected.ReglaAplicacion!="exclusiva"||p.Tipo is not("porcentaje" or "importe_fijo"))throw new BusinessException("La promoción requiere revisar su regla de aplicación.");
            var value=p.ValorEscalado/ReceivingService.Power(p.EscalaValor);if(p.Tipo=="porcentaje"&&value>100)throw new BusinessException("La promoción contiene un porcentaje inválido.");
            var raw=p.Tipo=="porcentaje"?subtotal*value/100:quantity*value*100;
            discount=checked((long)Math.Min(subtotal,decimal.Round(raw,0,MidpointRounding.AwayFromZero)));promotion=new(p.Id,p.Version,p.Nombre,p.Tipo,p.ValorEscalado,p.EscalaValor,selected.Prioridad,discount);
        }
        var baseAmount=subtotal-discount;
        var conditions=await (from link in db.Set<ProductoImpuesto>().AsNoTracking() join t in db.Set<Impuesto>().AsNoTracking() on link.ImpuestoId equals t.Id
            where link.ProductoId==productId&&t.Activo==1&&string.Compare(link.VigenteDesde,now)<=0&&(link.VigenteHasta==null||string.Compare(link.VigenteHasta,now)>0)&&string.Compare(t.VigenteDesde,now)<=0&&(t.VigenteHasta==null||string.Compare(t.VigenteHasta,now)>0)
            orderby t.Id select t).ToListAsync();
        conditions=conditions.DistinctBy(t=>t.Id).ToList();if(conditions.Any(x=>x.EsExencion==1)&&conditions.Count!=1)throw new BusinessException("El producto tiene exención y otras condiciones simultáneas. Corrige las vigencias antes de vender.");
        List<AppliedTax> taxes=[];long tax=0;
        foreach(var t in conditions)
        {
            var rate=t.TasaEscalada/ReceivingService.Power(t.EscalaTasa);if(rate>100||t.EsExencion==1&&(rate!=0||string.IsNullOrWhiteSpace(t.MotivoExencion)))throw new BusinessException("La condición de impuesto o exención no es válida.");
            var amount=t.EsExencion==1?0:checked((long)decimal.Round(baseAmount*rate/100,0,MidpointRounding.AwayFromZero));tax=checked(tax+amount);
            taxes.Add(new(t.Id,t.Nombre,t.TasaEscalada,t.EscalaTasa,baseAmount,t.EsExencion==1,t.MotivoExencion,amount,t.VigenteDesde,t.VigenteHasta));
        }
        return new(discount,tax,checked(baseAmount+tax),promotion,taxes);
    }
}
