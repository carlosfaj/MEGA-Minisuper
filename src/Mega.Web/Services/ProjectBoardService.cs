using System.Text.Json;

namespace Mega.Web.Services;

public sealed record ProjectCriterion(string Id,string Label,bool Done,string? Evidence);
public sealed record ProjectTask(string Id,string Title,string Area,string Lane,List<string> DependsOn,string? Blocker,List<ProjectCriterion> Criteria)
{
    public int Done=>Criteria.Count(x=>x.Done);
    public int Total=>Criteria.Count;
    public decimal Percent=>ProjectBoard.Percentage(Done,Total);
    public string Status=>Done==Total?"completado":Lane=="validacion"?"validacion":Done>0||Lane=="en_curso"?"en_curso":"pendiente";
}
public sealed record ProjectArea(string Name,int Done,int Total)
{
    public decimal Percent=>ProjectBoard.Percentage(Done,Total);
}
public sealed record ProjectBoard(string Name,string Version,string UpdatedAt,string Methodology,int TestsPassed,List<ProjectTask> Tasks)
{
    public int Done=>Tasks.Sum(x=>x.Done);
    public int Total=>Tasks.Sum(x=>x.Total);
    public decimal Percent=>Percentage(Done,Total);
    public string Hash {get;set;}="";
    public IEnumerable<ProjectArea> Areas=>Tasks.GroupBy(x=>x.Area).Select(g=>new ProjectArea(g.Key,g.Sum(x=>x.Done),g.Sum(x=>x.Total)));
    public static decimal Percentage(int done,int total)=>total==0?0:Math.Min(done==total?100m:99.9m,decimal.Round(done*100m/total,1,MidpointRounding.AwayFromZero));
}
public sealed class ProjectBoardService(IWebHostEnvironment environment,IConfiguration configuration)
{
    public ProjectBoard Read()
    {
        var path=configuration["MEGA:ProjectPlanPath"]??Path.Combine(environment.ContentRootPath,"Tracking","proyecto.json");
        var text=File.ReadAllText(path);var board=JsonSerializer.Deserialize<ProjectBoard>(text,new JsonSerializerOptions(JsonSerializerDefaults.Web))??throw new InvalidOperationException("Seguimiento del proyecto no disponible.");
        if(board.Tasks.Count==0||board.Tasks.Select(x=>x.Id).Distinct().Count()!=board.Tasks.Count||board.Tasks.Any(x=>x.Total==0||x.Criteria.Any(c=>c.Done&&string.IsNullOrWhiteSpace(c.Evidence))))throw new InvalidOperationException("El kanban requiere criterios únicos y evidencia para cada criterio completado.");
        board.Hash=Formats.Hash(text);return board;
    }
    public static string EvidenceUrl(string evidence)=>"https://github.com/carlosfaj/MEGA-Minisuper/blob/main/"+string.Join('/',evidence.Split('/').Select(Uri.EscapeDataString));
}
