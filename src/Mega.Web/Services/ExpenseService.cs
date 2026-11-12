using System.Globalization;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;
public sealed record ExpenseChange(string Key,string Date,string Concept,string Amount,string? Receipt,long? Corrects=null,string? Reason=null,string? ReviewHash=null);
public sealed class ExpenseService(MegaDbContext db,AccessControl access,Operations operations)
{
    public static string ReviewHash(GastoOperativo row)=>Formats.Hash(Formats.Json(row));
    public async Task<long> SaveAsync(long userId,ExpenseChange input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);
        if(!Guid.TryParseExact(input.Key,"D",out _))throw new BusinessException("Recarga el formulario de gasto.");
        if(!DateOnly.TryParseExact(input.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)||date>DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6)))throw new BusinessException("Introduce una fecha válida que no sea futura.");
        var when=date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)+"T06:00:00.000Z";
        var concept=AccessControl.Text(input.Concept,"Concepto",500);var receipt=AccessControl.Optional(input.Receipt,"Comprobante",500);var amount=Formats.Scaled(Formats.Parse(input.Amount),2);
        if(amount<=0)throw new BusinessException("El importe del gasto debe ser mayor que cero.");
        var reason=input.Corrects is null?null:AccessControl.Text(input.Reason,"Motivo de corrección",1000);
        var existing=await db.Set<GastoOperativo>().AsNoTracking().SingleOrDefaultAsync(x=>x.ClaveOperacion==input.Key);
        if(existing is not null)
        {if(existing.RegistradoPor!=userId||existing.Fecha!=when||existing.Concepto!=concept||existing.ImporteCentavos!=amount||existing.ComprobanteReferencia!=receipt||existing.CorrigeAId!=input.Corrects||existing.MotivoCorreccion!=reason)throw new BusinessException("Esta clave corresponde a otro registro de gasto.");await tx.CommitAsync();return existing.Id;}
        GastoOperativo? previous=null;
        if(input.Corrects is long id)
        {
            previous=await db.Set<GastoOperativo>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("El gasto que quieres corregir no existe.");
            if(previous.Estado!="Registrado"||ReviewHash(previous)!=input.ReviewHash)throw new BusinessException("El gasto cambió. Recarga antes de corregir.");
            previous.Estado="Corregido";
        }
        var row=new GastoOperativo{Fecha=when,CreadoEn=Formats.Now(),Concepto=concept,ImporteCentavos=amount,Moneda="NIO",RegistradoPor=userId,ComprobanteReferencia=receipt,Estado="Registrado",CorrigeAId=input.Corrects,MotivoCorreccion=reason,ClaveOperacion=input.Key};
        db.Add(row);await db.SaveChangesAsync();operations.Audit(userId,previous is null?"Registrar gasto":"Corregir gasto","GastoOperativo",row.Id.ToString(),input.Key,new{anteriorId=previous?.Id,anteriorImporte=previous?.ImporteCentavos,registro=row,motivo=reason});
        await db.SaveChangesAsync();await tx.CommitAsync();return row.Id;
    }
    public async Task CancelAsync(long userId,long id,string hash,string reason)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await access.ManagerAsync(userId);
        var row=await db.Set<GastoOperativo>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("El gasto no existe.");
        if(row.Estado!="Registrado"||ReviewHash(row)!=hash)throw new BusinessException("El gasto cambió o ya no está vigente. Recarga antes de anular.");
        row.MotivoAnulacion=AccessControl.Text(reason,"Motivo de anulación",1000);row.Estado="Anulado";row.AnuladoPor=userId;row.AnuladoEn=Formats.Now();
        operations.Audit(userId,"Anular gasto","GastoOperativo",id.ToString(),Guid.NewGuid().ToString(),new{row.ImporteCentavos,row.MotivoAnulacion});await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
