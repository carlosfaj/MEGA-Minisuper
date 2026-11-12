using System.Globalization;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;
public sealed record ReportFilter(string Kind,string From,string To,long? ProductId=null,long? OperatorId=null);
public sealed record ReportCell(string Text,decimal? Number=null)
{
    public static ReportCell Value(decimal value,string format="0.00")=>new(value.ToString(format,CultureInfo.InvariantCulture),value);
    public static implicit operator ReportCell(string text)=>new(text);
}
public sealed record ReportTable(string Title,string[] Headers,List<ReportCell[]> Rows);
public sealed record ReportMetric(string Label,string Value);
public sealed record ReportSnapshot(string Title,string Period,string GeneratedAt,string Scope,string Note,List<ReportMetric> Metrics,List<ReportTable> Tables);
public sealed class ReportService(MegaDbContext db,AccessControl access)
{
    public static readonly IReadOnlyDictionary<string,string> Kinds=new Dictionary<string,string>{{"ventas","Ventas"},{"inventario","Inventario actual"},{"caja","Caja y movimientos"},{"resultados","Costos, gastos y resultados"}};
    public async Task<ReportSnapshot> RunAsync(long userId,ReportFilter filter)
    {
        try{return await BuildAsync(userId,filter);}
        catch(OverflowException){throw new BusinessException("El reporte supera la capacidad numérica. Reduce el periodo o filtra un producto y revisa los valores.");}
    }
    private async Task<ReportSnapshot> BuildAsync(long userId,ReportFilter filter)
    {
        var user=await access.RequireAsync(userId,"Inventario","Administrador","Propietario");var manager=await db.Set<Rol>().AnyAsync(x=>x.Id==user.RolId&&(x.Nombre=="Administrador"||x.Nombre=="Propietario"));
        if(!Kinds.ContainsKey(filter.Kind)||!manager&&filter.Kind!="inventario")throw new BusinessException("Tu cuenta solo puede consultar los reportes autorizados para su función.");
        if(!DateOnly.TryParseExact(filter.From,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var from)||!DateOnly.TryParseExact(filter.To,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var to)||to<from||to.DayNumber-from.DayNumber>366||to.Year>=9999)throw new BusinessException("Selecciona un periodo válido de hasta 367 días.");
        if(filter.ProductId is not null&&filter.Kind is not ("ventas" or "inventario")||filter.OperatorId is not null&&filter.Kind!="caja")throw new BusinessException("El filtro de producto corresponde a ventas/inventario y el de operador a caja.");
        var begin=from.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)+"T06:00:00.000Z";var end=to.AddDays(1).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)+"T06:00:00.000Z";
        await using var tx=await db.Database.BeginTransactionAsync();
        var scope=filter.ProductId is long productId?"Producto #"+productId:filter.OperatorId is long operatorId?"Operador #"+operatorId:"Todos los registros autorizados";
        var report=filter.Kind switch
        {
            "inventario"=>await InventoryAsync(filter,manager),
            "caja"=>await CashAsync(filter,begin,end),
            _=>await SalesAsync(filter,begin,end)
        };
        if(report.Tables.Sum(x=>x.Rows.Count)>5000)throw new BusinessException("El reporte supera 5000 filas. Reduce el periodo o utiliza un filtro.");
        await tx.CommitAsync();return new(Kinds[filter.Kind],filter.Kind=="inventario"?"Existencias actuales al generar":$"{filter.From} a {filter.To} · hora UTC−6",Formats.Now(),scope,report.Note,report.Metrics,report.Tables);
    }
    private async Task<(string Note,List<ReportMetric> Metrics,List<ReportTable> Tables)> InventoryAsync(ReportFilter filter,bool manager)
    {
        var products=await db.Set<Producto>().AsNoTracking().Where(x=>filter.ProductId==null||x.Id==filter.ProductId).OrderBy(x=>x.Nombre).ToListAsync();
        var ids=products.Select(x=>x.Id).ToArray();var moves=await (from m in db.Set<MovimientoInventario>().AsNoTracking() join o in db.Set<OperacionInventario>() on m.OperacionId equals o.Id where ids.Contains(m.ProductoId) select new{m.ProductoId,Move=new ValuationMovement(m.Id,m.CantidadEscalada,m.Escala,m.EstadoCosto,m.CostoUnitEscalado,m.EscalaCosto,o.SecuenciaConfirmacion,m.Secuencia)}).ToListAsync();
        List<ReportCell[]> rows=[];var unknown=0;decimal knownValue=0;
        foreach(var p in products)
        {
            var history=moves.Where(x=>x.ProductoId==p.Id).Select(x=>x.Move).ToList();
            var state=manager?ValuationService.Calculate(p.EscalaCantidad,history):new ValuationState(checked((long)history.Sum(x=>(decimal)x.Quantity)),p.EscalaCantidad,"desconocido",null,null,"");var quantity=state.Balance/ReceivingService.Power(p.EscalaCantidad);
            List<ReportCell> row=[p.Nombre,p.UnidadBase,ReportCell.Value(quantity,"0.###"),ReportCell.Value(p.MinimoEscalado/ReceivingService.Power(p.EscalaCantidad),"0.###"),p.Activo==1?"Activo":"Inactivo"];
            if(manager)
            {
                decimal? value=null;try{if(state.Cost is long cost)value=decimal.Round(quantity*cost/1000000m,2,MidpointRounding.AwayFromZero);}catch(OverflowException){}
                if(value is null)unknown++;else knownValue+=value.Value;
                row.Add(state.Cost is long c?ReportCell.Value(c/1000000m,"0.######"):"Desconocido");row.Add(value is decimal v?ReportCell.Value(v):"No calculable");
            }
            rows.Add(row.ToArray());
        }
        string[] headers=manager?["Producto","Unidad","Existencia","Mínimo","Estado","Costo unitario NIO","Valor NIO"]:["Producto","Unidad","Existencia","Mínimo","Estado"];
        List<ReportMetric> metrics=[new("Productos",products.Count.ToString())];if(manager){metrics.Add(new("Valor de existencias con costo conocido",Money(knownValue)));metrics.Add(new("Productos con valor no calculable",unknown.ToString()));}
        return("Existencias actuales, incluidos productos inactivos; no es un inventario histórico a la fecha del filtro."+(manager?" Costo desconocido o fuera de capacidad no se trata como cero.":" Incluye cantidades, mínimos y estado."),metrics,[new("Existencias",headers,rows)]);
    }
    private async Task<(string Note,List<ReportMetric> Metrics,List<ReportTable> Tables)> SalesAsync(ReportFilter filter,string begin,string end)
    {
        var lines=await (from d in db.Set<VentaDetalle>().AsNoTracking() join s in db.Set<Venta>().AsNoTracking() on d.VentaId equals s.Id where s.Estado=="Confirmada"&&string.Compare(s.Fecha,begin)>=0&&string.Compare(s.Fecha,end)<0&&(filter.ProductId==null||d.ProductoId==filter.ProductId) orderby s.Fecha,s.Id,d.NumeroLinea select new{Sale=s,Line=d}).ToListAsync();
        decimal gross=0,net=0,tax=0,discount=0,knownCost=0;var missing=0;List<ReportCell[]> rows=[];
        foreach(var item in lines)
        {
            var l=item.Line;gross+=l.TotalCentavos/100m;net+=(l.SubtotalCentavos-l.DescuentoCentavos)/100m;tax+=l.ImpuestoCentavos/100m;discount+=l.DescuentoCentavos/100m;
            decimal? cost=null;if(l.EstadoCosto=="conocido"&&l.CostoUnitEscalado is long c&&l.EscalaCosto is long scale)
            {try{cost=decimal.Round(l.CantidadBaseEscalada/ReceivingService.Power(l.EscalaCantidad)*c/ReceivingService.Power(scale),2,MidpointRounding.AwayFromZero);}catch(OverflowException){}}
            if(cost is null)missing++;else knownCost+=cost.Value;
            List<ReportCell> row=[item.Sale.Folio,Formats.Date(item.Sale.Fecha),l.DescripcionHistorica+" · "+l.PresentacionHistorica,ReportCell.Value(l.CantidadEscalada/ReceivingService.Power(l.EscalaCantidad),"0.###"),ReportCell.Value((l.SubtotalCentavos-l.DescuentoCentavos)/100m),ReportCell.Value(l.ImpuestoCentavos/100m),ReportCell.Value(l.TotalCentavos/100m)];
            if(filter.Kind=="resultados"){row.Add(cost is decimal v?ReportCell.Value(v):"No calculable");row.Add(cost is decimal known?ReportCell.Value((l.SubtotalCentavos-l.DescuentoCentavos)/100m-known):"No calculable");}rows.Add(row.ToArray());
        }
        List<ReportMetric> metrics=[new("Ventas incluidas",lines.Select(x=>x.Sale.Id).Distinct().Count().ToString()),new("Cobro de líneas incluidas",Money(gross)),new("Ingreso sin impuesto",Money(net)),new("Impuesto aplicado",Money(tax)),new("Descuentos",Money(discount))];
        string[] headers=filter.Kind=="resultados"?["Folio","Fecha local","Producto","Cantidad","Ingreso NIO","Impuesto NIO","Total NIO","Costo NIO","Margen NIO"]:["Folio","Fecha local","Producto","Cantidad","Ingreso NIO","Impuesto NIO","Total NIO"];
        List<ReportTable> tables=[new("Líneas de ventas vigentes",headers,rows)];
        if(filter.Kind=="resultados")
        {
            var expenses=await db.Set<GastoOperativo>().AsNoTracking().Where(x=>x.Estado=="Registrado"&&string.Compare(x.Fecha,begin)>=0&&string.Compare(x.Fecha,end)<0).OrderBy(x=>x.Fecha).ThenBy(x=>x.Id).ToListAsync();var expenseTotal=expenses.Sum(x=>x.ImporteCentavos/100m);
            metrics.AddRange([new("Costo de líneas con valor conocido",Money(knownCost)),new("Líneas con costo no calculable",missing.ToString()),new("Margen bruto",missing==0?Money(net-knownCost):"No calculable"),new("Gastos vigentes",Money(expenseTotal)),new("Resultado después de gastos",missing==0?Money(net-knownCost-expenseTotal):"No calculable")]);
            tables.Add(new("Gastos vigentes",["Referencia","Fecha local","Concepto","Comprobante","Importe NIO"],expenses.Select(x=>new ReportCell[]{"#"+x.Id,Formats.Date(x.Fecha),x.Concepto,x.ComprobanteReferencia??"Sin referencia",ReportCell.Value(x.ImporteCentavos/100m)}).ToList()));
        }
        return("Estado actual de ventas fechadas en el periodo: las anuladas se excluyen aunque su anulación sea posterior. No sustituye el cierre histórico de caja ni un estado contable/fiscal. Ingreso = subtotal − descuento, sin impuesto; costo histórico por línea redondeado a centavos. Si falta un costo, el margen global y el resultado son no calculables.",metrics,tables);
    }
    private async Task<(string Note,List<ReportMetric> Metrics,List<ReportTable> Tables)> CashAsync(ReportFilter filter,string begin,string end)
    {
        var movements=await (from m in db.Set<MovimientoCaja>().AsNoTracking() join t in db.Set<TurnoCaja>() on m.TurnoId equals t.Id join u in db.Set<Usuario>() on m.RegistradoPor equals u.Id where string.Compare(m.Fecha,begin)>=0&&string.Compare(m.Fecha,end)<0&&(filter.OperatorId==null||t.OperadorId==filter.OperatorId) orderby m.Fecha,m.Id select new{Movement=m,Account=u.NombreCuenta}).ToListAsync();
        var turnIds=movements.Select(x=>x.Movement.TurnoId).ToArray();var turns=await db.Set<TurnoCaja>().AsNoTracking().Where(t=>(filter.OperatorId==null||t.OperadorId==filter.OperatorId)&&(turnIds.Contains(t.Id)||string.Compare(t.Apertura,begin)>=0&&string.Compare(t.Apertura,end)<0||t.Cierre!=null&&string.Compare(t.Cierre,begin)>=0&&string.Compare(t.Cierre,end)<0)).OrderBy(t=>t.Id).ToListAsync();
        var ids=turns.Select(x=>x.Id).ToArray();var summaries=await db.Set<ResumenCajaVista>().AsNoTracking().Where(x=>ids.Contains(x.TurnoId)).ToDictionaryAsync(x=>x.TurnoId);
        List<ReportTable> tables=[new("Movimientos del periodo",["Referencia","Fecha local","Turno","Responsable","Tipo","Importe NIO"],movements.Select(x=>new ReportCell[]{"#"+x.Movement.Id,Formats.Date(x.Movement.Fecha),"#"+x.Movement.TurnoId,x.Account,x.Movement.Tipo,ReportCell.Value(x.Movement.ImporteCentavos/100m)}).ToList()),
            new("Turnos relacionados",["Turno","Estado","Fondo NIO","Esperado NIO","Contado NIO","Diferencia NIO"],turns.Select(t=>new ReportCell[]{"#"+t.Id,t.Estado,ReportCell.Value(t.FondoRealCentavos/100m),ReportCell.Value(summaries[t.Id].EsperadoCentavos/100m),t.EfectivoContadoCentavos is long counted?ReportCell.Value(counted/100m):"Sin cierre",t.DiferenciaCentavos is long difference?ReportCell.Value(difference/100m):"Sin cierre"}).ToList())];
        return("Movimientos según su fecha real, incluidas devoluciones posteriores a una venta. El fondo no se cuenta como ingreso. Turnos relacionados = abiertos, cerrados o con movimientos en el periodo; su saldo comprende el turno completo y conserva los valores de cierres anteriores.",[new("Ingresos del periodo",Money(movements.Where(x=>x.Movement.ImporteCentavos>0).Sum(x=>x.Movement.ImporteCentavos/100m))),new("Egresos del periodo",Money(-movements.Where(x=>x.Movement.ImporteCentavos<0).Sum(x=>x.Movement.ImporteCentavos/100m))),new("Neto de movimientos",Money(movements.Sum(x=>x.Movement.ImporteCentavos/100m)))],tables);
    }
    private static string Money(decimal value)=>"C$ "+value.ToString("N2",CultureInfo.GetCultureInfo("es-NI"));
}
