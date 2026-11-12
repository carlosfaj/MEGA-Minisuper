using System.Globalization;
using System.Net;
using ClosedXML.Excel;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharp.Pdf.IO;

namespace Mega.Tests;
public sealed class ReportTests
{
    [Fact]
    public async Task initial_report_load_uses_defaults_but_explicit_invalid_dates_are_rejected()
    {
        using var f=new MegaFactory();using var client=f.Client();await HttpTestSupport.LoginAsync(client,"inventario");var html=await client.GetStringAsync("/Reportes");Assert.Contains("<th>Existencia</th>",html);Assert.DoesNotContain("field is required",html);var invalid=await client.GetAsync("/Reportes?handler=Export&format=pdf&Kind=inventario&From=&To=2026-12-31");Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
    }
    private static ReportFilter Filter(string kind,long? product=null)=>new(kind,"2026-01-01","2026-12-31",product);
    private static string Metric(ReportSnapshot r,string label)=>r.Metrics.Single(x=>x.Label==label).Value;
    private static async Task<long> Sale(IServiceProvider services,long presentation=2,string quantity="1")
    {var op=services.GetRequiredService<Operations>();if((await op.CashAsync(2)).Shift is null)await op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(presentation,"unidad",quantity)];var quote=await op.QuoteAsync(2,lines);return await op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),quote.Hash,"500",false,lines));}
    [Theory]
    [InlineData("10","40.00","30.00")]
    [InlineData("0","60.00","50.00")]
    public async Task T11_historical_cost_profit_and_expenses_reconcile_including_known_zero(string cost,string margin,string result)
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var catalog=s.ServiceProvider.GetRequiredService<CatalogService>();var receiving=s.ServiceProvider.GetRequiredService<ReceivingService>();
        var product=await catalog.SaveProductAsync(1,new(null,0,"Producto rentabilidad",1,"unidad","unidad","0",false,true));var presentation=await catalog.SavePresentationAsync(1,new(null,product,1,0,"Unidad","1",true));var version=(await db.Set<Producto>().AsNoTracking().SingleAsync(x=>x.Id==product)).Version;await catalog.SetPriceAsync(1,product,version,presentation,"30",null);var supplier=await receiving.SaveSupplierAsync(1,new(null,0,"Proveedor prueba",null,null,null,null,true));
        async Task Receive(string price){var id=await receiving.SaveAsync(3,new(null,0,Guid.NewGuid().ToString(),"REC-"+Guid.NewGuid(),supplier,"DOC-"+Guid.NewGuid(),"2026-11-12",null,[new(presentation,"10","10",price,null,null,null)]));var a=await receiving.SubmitAsync(3,id,1);var review=await db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==a);await receiving.DecideAsync(1,a,1,review.ContenidoHash,true,"Revisado");}
        await Receive(cost);await Sale(s.ServiceProvider,presentation,"2");await Receive("20");var expenses=s.ServiceProvider.GetRequiredService<ExpenseService>();await expenses.SaveAsync(1,new(Guid.NewGuid().ToString(),"2026-01-01","Gasto ficticio","10",null));var report=await s.ServiceProvider.GetRequiredService<ReportService>().RunAsync(1,Filter("resultados"));
        Assert.Equal("C$ 60.00",Metric(report,"Ingreso sin impuesto"));Assert.Equal("C$ "+margin,Metric(report,"Margen bruto"));Assert.Equal("C$ "+result,Metric(report,"Resultado después de gastos"));Assert.Equal("0",Metric(report,"Líneas con costo no calculable"));Assert.Equal(56000,(await s.ServiceProvider.GetRequiredService<Operations>().CashAsync(2)).Expected);
    }
    [Fact]
    public async Task unknown_cost_never_becomes_zero_or_fabricated_profit()
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();await Sale(s.ServiceProvider);var service=s.ServiceProvider.GetRequiredService<ReportService>();var report=await service.RunAsync(1,Filter("resultados"));Assert.Equal("No calculable",Metric(report,"Margen bruto"));Assert.Equal("No calculable",Metric(report,"Resultado después de gastos"));Assert.Equal("1",Metric(report,"Líneas con costo no calculable"));Assert.Equal("C$ 38.00",Metric(report,"Ingreso sin impuesto"));Assert.Equal("No calculable",report.Tables[0].Rows[0][7].Text);
    }
    [Fact]
    public async Task T11_inventory_role_never_receives_costs_in_data_excel_or_tampered_export()
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();var service=s.ServiceProvider.GetRequiredService<ReportService>();var report=await service.RunAsync(3,Filter("inventario"));Assert.Equal(5,report.Tables.Single().Headers.Length);Assert.Single(report.Metrics);Assert.DoesNotContain("Costo",Formats.Json(report),StringComparison.OrdinalIgnoreCase);using var book=new XLWorkbook(new MemoryStream(ReportExport.Excel(report)));Assert.Equal(5,book.Worksheet("Existencias").LastColumnUsed()!.ColumnNumber());Assert.Equal(2,book.Worksheets.Count);
        await Assert.ThrowsAsync<BusinessException>(()=>service.RunAsync(3,Filter("resultados")));await Assert.ThrowsAsync<BusinessException>(()=>service.RunAsync(2,Filter("ventas")));using var client=f.Client();await HttpTestSupport.LoginAsync(client,"inventario");var response=await client.GetAsync("/Reportes?handler=Export&format=xlsx&Kind=resultados&From=2026-01-01&To=2026-12-31");Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.DoesNotContain("No calculable",await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task date_product_and_operator_filters_do_not_mix_scopes()
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();await Sale(s.ServiceProvider);var service=s.ServiceProvider.GetRequiredService<ReportService>();Assert.Empty((await service.RunAsync(1,Filter("ventas",1))).Tables.Single().Rows);Assert.Single((await service.RunAsync(1,Filter("ventas",2))).Tables.Single().Rows);
        var future=DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);Assert.Empty((await service.RunAsync(1,new("ventas",future,future))).Tables.Single().Rows);await Assert.ThrowsAsync<BusinessException>(()=>service.RunAsync(1,new("resultados","2026-01-01","2026-12-31",2)));await Assert.ThrowsAsync<BusinessException>(()=>service.RunAsync(1,new("ventas","2026-12-31","2026-01-01")));Assert.Empty((await service.RunAsync(1,Filter("caja") with{OperatorId=4})).Tables[0].Rows);
    }
    [Fact]
    public async Task cancelled_sale_is_excluded_but_cash_keeps_original_close_and_real_later_refund()
    {
        using var f=new MegaFactory();using var s=f.Services.CreateScope();var sale=await Sale(s.ServiceProvider);var op=s.ServiceProvider.GetRequiredService<Operations>();var old=(await op.CashAsync(2)).Shift!;await op.CloseCashAsync(2,old.Id,old.Version,"538",null);var cancel=s.ServiceProvider.GetRequiredService<CancellationService>();var id=await cancel.SaveAsync(2,new(null,0,Guid.NewGuid().ToString(),sale,"AN-REP","Motivo ficticio","Acta ficticia",false,false));var authorization=await cancel.SubmitAsync(2,id,1);var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();var review=await db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==authorization);await cancel.DecideAsync(1,authorization,1,review.ContenidoHash,true,"Revisado");await op.OpenCashAsync(2,"500",null);await cancel.RefundAsync(2,id,1,true,"Entrega ficticia");var service=s.ServiceProvider.GetRequiredService<ReportService>();Assert.Empty((await service.RunAsync(1,Filter("ventas"))).Tables.Single().Rows);var cash=await service.RunAsync(1,Filter("caja"));Assert.Equal("C$ 0.00",Metric(cash,"Neto de movimientos"));Assert.Equal(2,cash.Tables[0].Rows.Count);Assert.Equal("538.00",cash.Tables[1].Rows[0][3].Text);Assert.Equal("462.00",cash.Tables[1].Rows[1][3].Text);
    }
    [Fact]
    public async Task pdf_paginates_and_excel_preserves_numbers_and_untrusted_text_without_formulas()
    {
        var text="=HYPERLINK(\"https://example.invalid\",\"Texto\")";var report=new ReportSnapshot("Ensayo Unicode áéíóú","2026-01-01 a 2026-12-31",Formats.Now(),"Datos ficticios","Sin aceptación real",[new("Total","C$ 123.45")],[new("Detalle",["Texto","Importe NIO"],Enumerable.Range(0,100).Select(i=>new ReportCell[]{text,ReportCell.Value(123.45m)}).ToList())]);
        var pdf=ReportExport.Pdf(report);Assert.StartsWith("%PDF",System.Text.Encoding.ASCII.GetString(pdf[..4]));using var doc=PdfReader.Open(new MemoryStream(pdf),PdfDocumentOpenMode.Import);Assert.True(doc.PageCount>=3);using var book=new XLWorkbook(new MemoryStream(ReportExport.Excel(report)));var cell=book.Worksheet("Detalle").Cell(7,1);Assert.Equal(text,cell.GetString());Assert.False(cell.HasFormula);Assert.Equal(123.45,book.Worksheet("Detalle").Cell(7,2).GetDouble());
    }
    [Fact]
    public async Task successful_http_exports_are_audited_and_do_not_change_operations()
    {
        using var f=new MegaFactory();using var client=f.Client();await HttpTestSupport.LoginAsync(client,"admin");foreach(var format in new[]{"pdf","xlsx"}){var response=await client.GetAsync("/Reportes?handler=Export&Kind=inventario&From=2026-01-01&To=2026-12-31&format="+format);Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.True((await response.Content.ReadAsByteArrayAsync()).Length>1000);}
        using var s=f.Services.CreateScope();var db=s.ServiceProvider.GetRequiredService<MegaDbContext>();Assert.Equal(2,await db.Set<EventoAuditoria>().CountAsync(x=>x.Accion=="Exportar reporte"));Assert.Empty(await db.Set<Venta>().ToListAsync());
    }
}
