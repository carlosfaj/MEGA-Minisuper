using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mega.Web.Pages;

[Authorize(Policy="Comercial")]
public sealed class ProyectoModel(ProjectBoardService service):ManagementPage
{
    public ProjectBoard Board {get;private set;}=null!;
    public void OnGet()=>Board=service.Read();
    public IActionResult OnGetEstado()=>new JsonResult(new{hash=service.Read().Hash});
    public static readonly (string Key,string Label)[] Lanes=[("pendiente","Pendiente"),("en_curso","En curso"),("validacion","Por validar"),("completado","Completado")];
}
