using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Data;

public sealed class ExistenciaVista
{
    public long ProductoId { get; set; }
    public string Nombre { get; set; } = "";
    public string UnidadBase { get; set; } = "";
    public long EscalaCantidad { get; set; }
    public long MinimoEscalado { get; set; }
    public long Activo { get; set; }
    public long CantidadEscalada { get; set; }
}

public sealed class ResumenCajaVista
{
    public long TurnoId { get; set; }
    public long OperadorId { get; set; }
    public string Estado { get; set; } = "";
    public long FondoRealCentavos { get; set; }
    public long NetoCentavos { get; set; }
    public long EsperadoCentavos { get; set; }
}

public sealed partial class MegaDbContext
{
    partial void ConfigureViews(ModelBuilder model)
    {
        model.Entity<ExistenciaVista>().HasNoKey().ToView("vw_Existencias");
        model.Entity<ResumenCajaVista>().HasNoKey().ToView("vw_ResumenCaja");
    }
}
