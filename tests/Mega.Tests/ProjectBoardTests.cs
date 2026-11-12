using System.Net;
using System.Text.Json;
using Mega.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;
public sealed class ProjectBoardTests
{
    [Fact]
    public void partial_card_cannot_be_completed_and_each_criterion_counts_once()
    {
        var card=new ProjectTask("X","Ejemplo","Software","pendiente",[],null,[new("1","Uno",true,"evidencia"),new("2","Dos",false,null)]);
        Assert.Equal("en_curso",card.Status);Assert.Equal(50m,card.Percent);
        var board=new ProjectBoard("MEGA","1.0.0",Formats.Now(),"Criterios",1,[card]);Assert.Equal(1,board.Done);Assert.Equal(2,board.Total);Assert.Equal(50m,board.Percent);
        Assert.Equal(0m,ProjectBoard.Percentage(0,0));Assert.Equal(100m,ProjectBoard.Percentage(2,2));Assert.True(ProjectBoard.Percentage(9999,10000)<100);
    }
    [Fact]
    public void real_plan_includes_software_quality_docs_delivery_and_business_validation()
    {
        using var factory=new MegaFactory();var b=factory.Services.GetRequiredService<ProjectBoardService>().Read();
        Assert.Contains(b.Tasks,x=>x.Id=="DOC");Assert.Contains(b.Tasks,x=>x.Id=="MANUAL");Assert.Contains(b.Tasks,x=>x.Id=="REPORTES");Assert.Contains(b.Tasks,x=>x.Id=="RESTORE");Assert.Contains(b.Tasks,x=>x.Id=="NEGOCIO");
        Assert.True(b.Total>b.Done);Assert.Equal(b.Done,b.Areas.Sum(x=>x.Done));Assert.Equal(b.Total,b.Areas.Sum(x=>x.Total));
        Assert.All(b.Tasks.Where(t=>t.Status=="completado"),t=>Assert.All(t.Criteria,c=>Assert.True(c.Done)));
        Assert.DoesNotContain(b.Tasks.Single(t=>t.Id=="DEMO").Criteria,c=>c.Label.Contains("Aceptación")&&c.Done);
    }
    [Fact]
    public async Task authenticated_board_and_refresh_are_available_to_commercial_accounts()
    {
        using var factory=new MegaFactory();using var client=factory.Client();Assert.Contains("Acceso",(await client.GetAsync("/Proyecto")).Headers.Location!.ToString());
        await HttpTestSupport.LoginAsync(client,"cajero");var response=await client.GetAsync("/Proyecto");Assert.Equal(HttpStatusCode.OK,response.StatusCode);var html=await response.Content.ReadAsStringAsync();Assert.Contains("id=\"project-percent\"",html);Assert.Contains("project.js",html);Assert.Contains("kanban-lane",html);
        response=await client.GetAsync("/Proyecto?handler=Estado");Assert.Equal(HttpStatusCode.OK,response.StatusCode);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Assert.Equal(64,json.RootElement.GetProperty("hash").GetString()!.Length);
    }
    [Fact]
    public void changing_the_tracking_source_refreshes_progress_without_restart()
    {
        using var factory=new MegaFactory();var env=factory.Services.GetRequiredService<IWebHostEnvironment>();var source=factory.Services.GetRequiredService<ProjectBoardService>().Read();
        var path=Path.Combine(Path.GetTempPath(),"mega-board-"+Guid.NewGuid()+".json");
        try
        {
            var options=new JsonSerializerOptions(JsonSerializerDefaults.Web);File.WriteAllText(path,JsonSerializer.Serialize(source,options));
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["MEGA:ProjectPlanPath"]=path}).Build();var service=new ProjectBoardService(env,config);var before=service.Read();
            var task=source.Tasks.First(x=>x.Criteria.Any(c=>!c.Done));var changed=task with{Criteria=task.Criteria.Select(x=>x with{Done=true,Evidence="docs/Valoracion.md"}).ToList()};source.Tasks[source.Tasks.IndexOf(task)]=changed;
            File.WriteAllText(path,JsonSerializer.Serialize(source,options));var after=service.Read();Assert.NotEqual(before.Hash,after.Hash);Assert.True(after.Percent>before.Percent);Assert.Equal("completado",after.Tasks.Single(x=>x.Id==task.Id).Status);
        }
        finally{File.Delete(path);}
    }
}
