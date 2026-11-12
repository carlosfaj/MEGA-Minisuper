using System.Globalization;
using System.Text.Json;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;
namespace Mega.Web.Services;

public sealed class Operations(MegaDbContext db, IConfiguration configuration)
{
    public string Station => configuration["MEGA:Station"] ?? "MEGA-LOCAL-01";
    public async Task<Usuario> RequireCashAsync(long userId)
    {
        var user=await db.Set<Usuario>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==userId);
        var role=user?.RolId is long id ? await db.Set<Rol>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id) : null;
        if(user is null || user.Activo!=1 || user.TipoCuenta!="comercial" || user.PermisoOperativoCaja!=1 || role?.Nombre is not ("Cajero" or "Administrador" or "Propietario"))
            throw new BusinessException("Tu cuenta no tiene permiso para operar caja.");
        return user;
    }
    private async Task<ConfiguracionNegocio> ConfigAsync()
    {
        var now=Formats.Now();
        var rows=await db.Set<ConfiguracionNegocio>().AsNoTracking().Where(x=>x.VigenteHasta==null && string.Compare(x.VigenteDesde,now)<=0).ToListAsync();
        if(rows.Count!=1) throw new BusinessException("Debe existir una configuración vigente del negocio.");
        return rows[0];
    }
    public async Task<CashSummary> CashAsync(long userId)
    {
        await RequireCashAsync(userId);
        var shift=await db.Set<TurnoCaja>().AsNoTracking().SingleOrDefaultAsync(x=>x.OperadorId==userId && x.Estado=="Abierto" && x.EstacionId==Station);
        var net=shift is null ? 0 : await db.Set<MovimientoCaja>().Where(x=>x.TurnoId==shift.Id).SumAsync(x=>(long?)x.ImporteCentavos) ?? 0;
        var config=await ConfigAsync();
        return new(shift,net,checked((shift?.FondoRealCentavos??0)+net),config.FondoHabitualCentavos,
            await db.Set<TurnoCaja>().AnyAsync(x=>x.EstacionId==Station && x.Estado=="Abierto" && x.OperadorId!=userId));
    }
    public void Audit(long userId,string action,string entity,string? id,string key,object changes,string result="exito") =>
        db.Add(new EventoAuditoria {Fecha=Formats.Now(),UsuarioId=userId,Origen="persona",Accion=action,Entidad=entity,EntidadId=id,
            ClaveOperacion=key,Resultado=result,DatosCambio=Formats.Json(changes),VersionFormato=1});
    public async Task<long> OpenCashAsync(long userId,string fund,string? reason)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        await RequireCashAsync(userId); var cents=Formats.Scaled(Formats.Parse(fund),2); var config=await ConfigAsync();
        if(await db.Set<TurnoCaja>().AnyAsync(x=>x.Estado=="Abierto" && (x.EstacionId==Station || x.OperadorId==userId)))
            throw new BusinessException("Ya existe un turno abierto en esta estación o para esta cuenta.");
        if(cents!=config.FondoHabitualCentavos && string.IsNullOrWhiteSpace(reason)) throw new BusinessException("Explica la diferencia entre el fondo habitual y el recibido.");
        if(reason?.Length>1000) throw new BusinessException("El motivo admite hasta 1000 caracteres.");
        var shift=new TurnoCaja {OperadorId=userId,EstacionId=Station,Estado="Abierto",Apertura=Formats.Now(),ConfiguracionId=config.Id,
            FondoHabitualCentavos=config.FondoHabitualCentavos,FondoRealCentavos=cents,MotivoDiferenciaFondo=string.IsNullOrWhiteSpace(reason)?null:reason.Trim(),Version=1};
        db.Add(shift); await db.SaveChangesAsync();
        Audit(userId,"Abrir turno","TurnoCaja",shift.Id.ToString(),Guid.NewGuid().ToString(),new {fondoRealCentavos=cents,shift.MotivoDiferenciaFondo});
        await db.SaveChangesAsync(); await tx.CommitAsync(); return shift.Id;
    }
    public async Task CloseCashAsync(long userId,long shiftId,long version,string counted,string? reason)
    {
        await using var tx=await db.Database.BeginTransactionAsync(); await RequireCashAsync(userId);
        var shift=await db.Set<TurnoCaja>().SingleOrDefaultAsync(x=>x.Id==shiftId && x.OperadorId==userId && x.EstacionId==Station && x.Estado=="Abierto");
        if(shift is null || shift.Version!=version) throw new BusinessException("El turno cambió o ya se cerró. Recarga Mi caja.");
        var cents=Formats.Scaled(Formats.Parse(counted),2);
        var expected=checked(shift.FondoRealCentavos+(await db.Set<MovimientoCaja>().Where(x=>x.TurnoId==shift.Id).SumAsync(x=>(long?)x.ImporteCentavos)??0));
        var difference=checked(cents-expected);
        if(difference!=0 && string.IsNullOrWhiteSpace(reason)) throw new BusinessException("Explica la diferencia entre el efectivo contado y el esperado.");
        if(reason?.Length>1000) throw new BusinessException("El motivo admite hasta 1000 caracteres.");
        shift.Estado="Cerrado"; shift.Cierre=Formats.Now(); shift.EfectivoEsperadoCierreCentavos=expected;
        shift.EfectivoContadoCentavos=cents; shift.DiferenciaCentavos=difference;
        shift.MotivoDiferenciaCierre=string.IsNullOrWhiteSpace(reason)?null:reason.Trim(); shift.Version++;
        Audit(userId,"Cerrar turno","TurnoCaja",shift.Id.ToString(),Guid.NewGuid().ToString(),new {esperadoCentavos=expected,contadoCentavos=cents,diferenciaCentavos=difference,shift.MotivoDiferenciaCierre});
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task<Dictionary<long,decimal>> StocksAsync()
    {
        var rows=await db.Set<MovimientoInventario>().AsNoTracking().Select(x=>new {x.ProductoId,x.CantidadEscalada,x.Escala}).ToListAsync();
        return rows.GroupBy(x=>x.ProductoId).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.CantidadEscalada/(decimal)Math.Pow(10,x.Escala)));
    }
    public async Task<List<ProductOption>> ProductsAsync()
    {
        var now=Formats.Now(); var products=await db.Set<Producto>().AsNoTracking().Where(x=>x.Activo==1).ToDictionaryAsync(x=>x.Id);
        var presentations=await db.Set<Presentacion>().AsNoTracking().Where(x=>x.Activo==1).ToListAsync();
        var prices=await db.Set<PrecioProducto>().AsNoTracking().Where(x=>string.Compare(x.VigenteDesde,now)<=0 && (x.VigenteHasta==null || string.Compare(x.VigenteHasta,now)>0)).ToListAsync();
        var codes=await db.Set<CodigoProducto>().AsNoTracking().Where(x=>x.Activo==1).ToListAsync(); var stock=await StocksAsync();
        return presentations.Where(x=>products.ContainsKey(x.ProductoId)).Select(p=>
        {
            var product=products[p.ProductoId]; var price=prices.Where(x=>x.PresentacionId==p.Id).ToList();
            return price.Count!=1 ? null : new ProductOption(p.Id,product.Nombre,p.Nombre,codes.FirstOrDefault(x=>x.PresentacionId==p.Id)?.Codigo??"",product.UnidadBase,
                product.UnidadControl,product.EscalaCantidad,price[0].PrecioCentavos,stock.GetValueOrDefault(product.Id));
        }).Where(x=>x!=null).Select(x=>x!).OrderBy(x=>x.Name).ToList();
    }
    public async Task<Quote> QuoteAsync(long userId,List<LineInput>? inputs)
    {
        await RequireCashAsync(userId);
        if(!await db.Set<TurnoCaja>().AnyAsync(x=>x.OperadorId==userId && x.Estado=="Abierto" && x.EstacionId==Station)) throw new BusinessException("Abre tu turno en Mi caja antes de preparar una venta.");
        if(inputs is null || inputs.Count is 0 or >100) throw new BusinessException("La venta debe contener entre 1 y 100 líneas.");
        var config=await ConfigAsync(); var now=Formats.Now(); var stocks=await StocksAsync(); var lines=new List<QuoteLine>(); long total=0;
        foreach(var input in inputs)
        {
            var p=await db.Set<Presentacion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==input.PresentationId && x.Activo==1);
            var product=p is null?null:await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==p.ProductoId && x.Activo==1);
            if(p is null || product is null) throw new BusinessException("Un producto o presentación dejó de estar disponible. Retíralo de la compra.");
            // La primera entrega no procesa lotes, promociones ni condiciones tributarias.
            if(product.ControlVencimiento==1 || await db.Set<LoteExistencia>().AnyAsync(x=>x.ProductoId==product.Id)) throw new BusinessException("Este producto requiere asignación por lotes, pendiente en este avance.");
            if(await db.Set<MovimientoInventario>().AnyAsync(x=>x.ProductoId==product.Id && x.EstadoCosto=="conocido")) throw new BusinessException("Este producto requiere valoración de costo, pendiente en este avance.");
            if(await db.Set<ProductoImpuesto>().AnyAsync(x=>x.ProductoId==product.Id) || await db.Set<PromocionProducto>().AnyAsync(x=>x.PresentacionId==p.Id)) throw new BusinessException("Las condiciones de impuestos o promociones de este producto requieren el módulo pendiente.");
            var prices=await db.Set<PrecioProducto>().AsNoTracking().Where(x=>x.PresentacionId==p.Id && string.Compare(x.VigenteDesde,now)<=0 && (x.VigenteHasta==null || string.Compare(x.VigenteHasta,now)>0)).ToListAsync();
            if(prices.Count!=1 || prices[0].PrecioCentavos<=0) throw new BusinessException("El producto necesita un único precio vigente mayor que cero.");
            var price=prices[0]; var value=Formats.Parse(input.Value); if(value<=0) throw new BusinessException("La cantidad o importe debe ser mayor que cero.");
            var quantityScale=product.UnidadControl=="unidad"?0:3;
            if(product.EscalaCantidad!=quantityScale) throw new BusinessException("La escala de este producto requiere revisar su configuración.");
            if(input.Mode is not ("unidad" or "peso_manual" or "importe") || (product.UnidadControl=="unidad" && input.Mode!="unidad") || (product.UnidadControl=="peso" && input.Mode=="unidad"))
                throw new BusinessException("La modalidad no corresponde al producto.");
            var requestedScale=input.Mode=="importe"?2:quantityScale; var requested=Formats.Scaled(value,requestedScale);
            var quantity=input.Mode=="importe"?decimal.Round(value*100/price.PrecioCentavos,3,MidpointRounding.AwayFromZero):value;
            var scaled=Formats.Scaled(quantity,quantityScale); if(scaled<=0) throw new BusinessException("El importe resulta en una cantidad menor que la precisión admitida.");
            var factor=p.FactorEscalado/(decimal)Math.Pow(10,p.EscalaFactor);
            var baseQuantity=Formats.Scaled(quantity*factor,(int)product.EscalaCantidad);
            var subtotal=checked((long)decimal.Round(quantity*price.PrecioCentavos,0,MidpointRounding.AwayFromZero));
            if(subtotal<=0) throw new BusinessException("El importe de la línea debe ser mayor que cero.");
            total=checked(total+subtotal);
            lines.Add(new(p.Id,product.Id,product.Nombre,p.Nombre,product.UnidadBase,input.Mode,scaled,quantityScale,baseQuantity,p.FactorEscalado,p.EscalaFactor,
                requested,requestedScale,price.PrecioCentavos,subtotal,product.Version,p.Version,price.Id,stocks.GetValueOrDefault(product.Id)));
        }
        var needsCheck=lines.GroupBy(x=>x.ProductId).Any(g=>g.Sum(x=>x.BaseQuantity/(decimal)Math.Pow(10,x.QuantityScale))>stocks.GetValueOrDefault(g.Key));
        var hash=Formats.Hash(Formats.Json(new {versionFormato=1,configuracionId=config.Id,config.Version,lineas=lines.Select(x=>x with {Stock=0}),total}));
        return new(lines,total,hash,config.Id,needsCheck);
    }
    public async Task<long> ConfirmAsync(long userId,SaleInput input)
    {
        if(!Guid.TryParseExact(input.Key,"D",out _)) throw new BusinessException("La preparación no tiene una clave válida. Recarga Nueva venta.");
        var intentHash=Formats.Hash(Formats.Json(input));
        await using var tx=await db.Database.BeginTransactionAsync(); var user=await RequireCashAsync(userId);
        var existing=await db.Set<Venta>().AsNoTracking().SingleOrDefaultAsync(x=>x.ClaveOperacion==input.Key);
        if(existing is not null)
        {
            var owner=await db.Set<TurnoCaja>().AsNoTracking().SingleAsync(x=>x.Id==existing.TurnoId);
            using var saved=JsonDocument.Parse(existing.ComprobanteSnapshot);
            if(owner.OperadorId!=userId || saved.RootElement.GetProperty("intentHash").GetString()!=intentHash) throw new BusinessException("Esta clave pertenece a otra confirmación. Recupera su comprobante antes de continuar.");
            await tx.CommitAsync(); return existing.Id;
        }
        Quote quote;
        try { quote=await QuoteAsync(userId,input.Lines); }
        catch(OverflowException) { throw new BusinessException("La cantidad o total supera la capacidad de registro."); }
        if(quote.Hash!=input.QuoteHash) throw new BusinessException("Los precios o condiciones cambiaron. Revisa de nuevo la compra antes de cobrar.");
        var received=Formats.Scaled(Formats.Parse(input.Received),2);
        if(received<quote.Total) throw new BusinessException("El efectivo recibido es insuficiente para confirmar la venta.");
        if(quote.NeedsPhysicalCheck && !input.PhysicalStockChecked) throw new BusinessException("Comprueba la disponibilidad física y confirma la discrepancia de existencias.");
        var shift=await db.Set<TurnoCaja>().SingleAsync(x=>x.OperadorId==userId && x.EstacionId==Station && x.Estado=="Abierto");
        var config=await ConfigAsync(); var now=Formats.Now();
        var operation=new OperacionInventario {Tipo="venta",Fecha=now,ConfirmadoEn=now,SecuenciaConfirmacion=checked((await db.Set<OperacionInventario>().MaxAsync(x=>(long?)x.SecuenciaConfirmacion)??0)+1),CreadoPor=userId,EjecutadoPor=userId,ClaveOperacion=input.Key};
        db.Add(operation); await db.SaveChangesAsync();
        var folio=$"MEGA-{operation.SecuenciaConfirmacion:00000000}";
        var snapshot=Formats.Json(new {versionFormato=1,intentHash,folio,fecha=now,negocio=config.Nombre,direccion=config.Direccion,telefono=config.Telefono,
            emisor=config.Emisor,identificacionEmisor=config.IdentificacionEmisor,moneda=config.Moneda,operador=user.NombreVisible,configuracionId=config.Id,
            fiscalidadValidada=config.FiscalidadValidada==1,lineas=quote.Lines.Select(x=>new {x.Name,x.Presentation,x.Unit,x.Quantity,x.QuantityScale,x.Factor,x.FactorScale,x.BaseQuantity,x.Price,x.Subtotal,impuestos=Array.Empty<object>(),promocion=(object?)null}),
            subtotalCentavos=quote.Total,impuestoCentavos=0,descuentoCentavos=0,totalCentavos=quote.Total,
            pagos=new[]{new {medio="efectivo",importeCentavos=quote.Total,recibidoCentavos=received,cambioCentavos=received-quote.Total}}});
        var sale=new Venta {Folio=folio,Fecha=now,TurnoId=shift.Id,OperacionId=operation.Id,ConfiguracionId=config.Id,Estado="Confirmada",SubtotalCentavos=quote.Total,
            TotalCentavos=quote.Total,Moneda=config.Moneda,ClaveOperacion=input.Key,ComprobanteSnapshot=snapshot,ComprobanteHash=Formats.Hash(snapshot),Version=1};
        db.Add(sale); await db.SaveChangesAsync(); var number=0;
        foreach(var line in quote.Lines)
        {
            // Hasta implementar valoración, costo desconocido permanece explícito.
            var detail=new VentaDetalle {VentaId=sale.Id,NumeroLinea=++number,PresentacionId=line.PresentationId,ProductoId=line.ProductId,
                DescripcionHistorica=line.Name,PresentacionHistorica=line.Presentation,UnidadBaseHistorica=line.Unit,CantidadEscalada=line.Quantity,
                EscalaCantidad=line.QuantityScale,FactorHistoricoEscalado=line.Factor,EscalaFactor=line.FactorScale,CantidadBaseEscalada=line.BaseQuantity,
                ModalidadVenta=line.Mode,ValorSolicitadoEscalado=line.Requested,EscalaValorSolicitado=line.RequestedScale,PrecioUnitEscalado=line.Price,
                EscalaPrecio=2,EstadoCosto="desconocido",SubtotalCentavos=line.Subtotal,TotalCentavos=line.Subtotal,ImpuestosSnapshot="[]"};
            db.Add(detail); await db.SaveChangesAsync();
            db.Add(new MovimientoInventario {OperacionId=operation.Id,ProductoId=line.ProductId,Secuencia=number,VentaDetalleId=detail.Id,
                CantidadEscalada=-line.BaseQuantity,Escala=line.QuantityScale,EstadoCosto="desconocido"});
        }
        db.Add(new Pago {VentaId=sale.Id,Secuencia=1,Medio="efectivo",ImporteCentavos=quote.Total,RecibidoCentavos=received,CambioCentavos=received-quote.Total,EstadoVerificacion="verificado",VerificadoPor=userId,VerificadoEn=now});
        db.Add(new MovimientoCaja {TurnoId=shift.Id,VentaId=sale.Id,OperacionId=operation.Id,Tipo="cobro_efectivo",ImporteCentavos=quote.Total,RegistradoPor=userId,Fecha=now});
        db.Add(new SolicitudImpresion {VentaId=sale.Id,SolicitadoPor=userId,Tipo="original",CopiaNumero=0,Estado="Pendiente",ClaveSolicitud="original-"+input.Key,
            Fecha=now,ImpresoraHistorica=config.ImpresoraNombre??"Sin impresora configurada",ContenidoHash=sale.ComprobanteHash,UltimoError=config.ImpresionHabilitada==0?"Impresión automática pendiente de integrar en este avance.":null});
        shift.Version++;
        Audit(userId,"Confirmar venta","Venta",sale.Id.ToString(),input.Key,new {sale.Folio,totalCentavos=quote.Total,turnoId=shift.Id,comprobanteHash=sale.ComprobanteHash,discrepanciaFisicaConfirmada=quote.NeedsPhysicalCheck && input.PhysicalStockChecked});
        await db.SaveChangesAsync(); await tx.CommitAsync(); return sale.Id;
    }
}
