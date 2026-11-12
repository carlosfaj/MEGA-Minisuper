using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;
public sealed class CommercialTests
{
    private sealed class Fixture:IDisposable
    {
        public MegaFactory Factory{get;}=new();public IServiceScope Scope{get;}
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();public CommercialService Service=>Scope.ServiceProvider.GetRequiredService<CommercialService>();public Operations Op=>Scope.ServiceProvider.GetRequiredService<Operations>();
        public Fixture()=>Scope=Factory.Services.CreateScope();
        public PromotionChange Promo(string value="10",string type="porcentaje",long priority=0)=>new(null,0,"Promoción ficticia",type,value,"2026-01-01T00:00","2030-01-01T00:00",true,priority,[2]);
        public TaxChange Tax(string rate="15",bool exempt=false)=>new(null,null,"Tasa ficticia de prueba",rate,"2026-01-01T00:00",null,exempt,exempt?"Motivo ficticio":null,true,[2]);
        public async Task<long> Sale(List<LineInput>? lines=null)
        {await Op.OpenCashAsync(2,"500",null);lines??=[new(2,"unidad","1")];var q=await Op.QuoteAsync(2,lines);return await Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),q.Hash,"100",false,lines));}
        public void Dispose(){Scope.Dispose();Factory.Dispose();}
    }
    [Fact]
    public async Task discount_then_tax_are_visible_and_frozen_in_sale_and_receipt()
    {
        using var f=new Fixture();await f.Service.SavePromotionAsync(1,f.Promo());var tax=await f.Service.SaveTaxAsync(1,f.Tax());var sale=await f.Sale();
        var v=await f.Db.Set<Venta>().SingleAsync(x=>x.Id==sale);Assert.Equal(3800,v.SubtotalCentavos);Assert.Equal(380,v.DescuentoCentavos);Assert.Equal(513,v.ImpuestoCentavos);Assert.Equal(3933,v.TotalCentavos);
        var d=await f.Db.Set<VentaDetalle>().SingleAsync();Assert.Contains("Promoci",d.PromocionSnapshot);Assert.Contains("\"rate\":15000000",d.ImpuestosSnapshot);var before=v.ComprobanteSnapshot;
        var t=await f.Db.Set<Impuesto>().SingleAsync(x=>x.Id==tax);var links=await f.Db.Set<ProductoImpuesto>().Where(x=>x.ImpuestoId==tax).ToListAsync();await f.Service.SaveTaxAsync(1,f.Tax("20") with{Id=tax,Hash=CommercialService.TaxHash(t,links)});f.Db.ChangeTracker.Clear();Assert.Equal(before,(await f.Db.Set<Venta>().SingleAsync()).ComprobanteSnapshot);
        Assert.Equal(4104,(await f.Op.QuoteAsync(2,[new(2,"unidad","1")])).Total);
    }
    [Fact]
    public async Task promotions_are_exclusive_priority_is_stable_and_fixed_discount_is_per_presentation()
    {
        using var f=new Fixture();await f.Service.SavePromotionAsync(1,f.Promo("50",priority:2));await f.Service.SavePromotionAsync(1,f.Promo("2","importe_fijo",1));await f.Op.OpenCashAsync(2,"500",null);
        var q=await f.Op.QuoteAsync(2,[new(2,"unidad","2")]);Assert.Equal(400,q.Discount);Assert.Equal(7200,q.Total);Assert.Single(q.Lines);Assert.Equal("importe_fijo",q.Lines[0].Promotion!.Type);
    }
    [Fact]
    public async Task exemptions_are_distinct_from_zero_rate_and_conflicts_block_checkout()
    {
        using var f=new Fixture();await f.Service.SaveTaxAsync(1,f.Tax("0",true));await f.Op.OpenCashAsync(2,"500",null);var q=await f.Op.QuoteAsync(2,[new(2,"unidad","1")]);Assert.True(q.Lines[0].Taxes.Single().Exempt);Assert.Equal(0,q.Tax);
        await f.Service.SaveTaxAsync(1,f.Tax("0"));await Assert.ThrowsAsync<BusinessException>(()=>f.Op.QuoteAsync(2,[new(2,"unidad","1")]));Assert.Empty(await f.Db.Set<Venta>().ToListAsync());
    }
    [Fact]
    public async Task future_inactive_and_expired_conditions_do_not_change_sale()
    {
        using var f=new Fixture();await f.Service.SavePromotionAsync(1,f.Promo() with{Start="2031-01-01T00:00",End="2032-01-01T00:00"});await f.Service.SaveTaxAsync(1,f.Tax() with{End="2026-02-01T00:00"});await f.Service.SaveTaxAsync(1,f.Tax() with{Active=false});await f.Op.OpenCashAsync(2,"500",null);
        var q=await f.Op.QuoteAsync(2,[new(2,"unidad","1")]);Assert.Equal(3800,q.Total);Assert.Null(q.Lines[0].Promotion);Assert.Empty(q.Lines[0].Taxes);
    }
    [Fact]
    public async Task zero_total_and_invalid_percent_exemption_or_dates_are_rejected()
    {
        using var f=new Fixture();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SavePromotionAsync(1,f.Promo("101")));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SavePromotionAsync(1,f.Promo() with{End="2020-01-01T00:00"}));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveTaxAsync(1,f.Tax("15",true)));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveTaxAsync(1,f.Tax("0",true) with{ExemptionReason=null}));
        await f.Service.SavePromotionAsync(1,f.Promo("100"));await f.Op.OpenCashAsync(2,"500",null);await Assert.ThrowsAsync<BusinessException>(()=>f.Op.QuoteAsync(2,[new(2,"unidad","1")]));
    }
    [Fact]
    public async Task stale_edits_and_changed_quote_require_a_new_review_without_effects()
    {
        using var f=new Fixture();var id=await f.Service.SavePromotionAsync(1,f.Promo());await f.Op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(2,"unidad","1")];var q=await f.Op.QuoteAsync(2,lines);
        await f.Service.SavePromotionAsync(1,f.Promo("20") with{Id=id,Version=1});await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SavePromotionAsync(1,f.Promo() with{Id=id,Version=1}));await Assert.ThrowsAsync<BusinessException>(()=>f.Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),q.Hash,"50",false,lines)));Assert.Empty(await f.Db.Set<Venta>().ToListAsync());
    }
    [Theory]
    [InlineData(2)] [InlineData(3)]
    public async Task only_management_changes_commercial_conditions(long user)
    {
        using var f=new Fixture();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SavePromotionAsync(user,f.Promo()));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveTaxAsync(user,f.Tax()));
    }
    [Fact]
    public async Task management_forms_and_receipt_render_applied_conditions()
    {
        using var f=new Fixture();using var client=f.Factory.Client();await HttpTestSupport.LoginAsync(client);
        var response=await HttpTestSupport.PostAsync(client,"/Comercial/Promociones",new(){["Input.Name"]="Promoción formulario",["Input.Type"]="porcentaje",["Input.Value"]="10",["Input.Start"]="2026-01-01T00:00",["Input.End"]="2030-01-01T00:00",["Input.Active"]="true",["Input.Presentations"]="2"});Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        response=await HttpTestSupport.PostAsync(client,"/Comercial/Impuestos",new(){["Input.Name"]="Tasa ficticia",["Input.Rate"]="15",["Input.Start"]="2026-01-01T00:00",["Input.Active"]="true",["Input.Products"]="2"});Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        var sale=await f.Sale();var receipt=await client.GetAsync("/Comprobante/"+sale);Assert.Equal(HttpStatusCode.OK,receipt.StatusCode);Assert.Contains("39.33",await receipt.Content.ReadAsStringAsync());
    }
}
