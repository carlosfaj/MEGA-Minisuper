using Mega.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Data.Sqlite;
namespace Mega.Web.Services;
public sealed class DatabaseInitializer(MegaDbContext db, IWebHostEnvironment environment, IConfiguration config, IPasswordHasher<Usuario> hasher,
    DatabaseSchema schema, DatabaseInspector inspector, DatabaseLocation location)
{
    public async Task InitializeAsync()
    {
        var demo = config.GetValue<bool>("MEGA:DemoData");
        if (demo && !environment.IsDevelopment() && !environment.IsEnvironment("Testing")) throw new InvalidOperationException("Los datos de demostración solo se permiten en Development o Testing.");
        Directory.CreateDirectory(Path.GetDirectoryName(location.Path)!);
        await db.Database.OpenConnectionAsync();
        try
        {
            await schema.EnsureAsync(demo);
            if (!await db.Set<Usuario>().AnyAsync())
            {
                var password = demo ? "MegaDemo2026!" : config["MEGA_BOOTSTRAP_PASSWORD"];
                if (string.IsNullOrWhiteSpace(password) || password.Length < 12) throw new InvalidOperationException("Define MEGA_BOOTSTRAP_PASSWORD de al menos 12 caracteres para inicializar una base sin demostración.");
                await SeedAsync(password, demo);
            }
            var connection = (SqliteConnection)db.Database.GetDbConnection();
            await DatabaseSchema.ValidateModeAsync(connection, demo);
            if (demo) await EnsureDemoOwnerAsync();
            DatabaseInspector.RequireHealthy(await inspector.InspectAsync(connection, db.Model));
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
    private async Task SeedAsync(string password, bool demo)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(); var now = Formats.Now();
        if (await db.Set<Usuario>().AnyAsync()) return;
        db.AddRange(new Rol { Id=1, Nombre="Propietario" }, new Rol { Id=2, Nombre="Administrador" }, new Rol { Id=3, Nombre="Cajero" }, new Rol { Id=4, Nombre="Inventario" });
        await db.SaveChangesAsync();
        Usuario User(long id, string name, string visible, long role, long cash)
        {
            var u = new Usuario { Id=id, NombreCuenta=name, NombreNormalizado=name.ToUpperInvariant(), NombreVisible=visible, Activo=1, TipoCuenta="comercial", RolId=role, PermisoOperativoCaja=cash, DelegacionCuentas=0, CreadoEn=now, CredencialesVersion=1 };
            u.PasswordHash = hasher.HashPassword(u, password); return u;
        }
        db.Add(User(1,"admin",demo ? "Administración · demo" : "Propietario",demo ? 2 : 1,1));
        if (demo) db.AddRange(User(2,"cajero","Caja · demo",3,1), User(3,"inventario","Inventario · demo",4,0), User(4,"cajero2","Segunda caja · demo",3,1), User(5,"propietario","Propietario · demo",1,0));
        await db.SaveChangesAsync();
        db.Add(new ConfiguracionNegocio { Id=1, Nombre=demo ? "Minisúper MEGA · demostración" : "Minisúper MEGA", Direccion=demo ? "Managua · dirección ficticia para pruebas" : "Pendiente de configurar", Moneda="NIO", FiscalidadValidada=0, FondoHabitualCentavos=50000, ImpresionHabilitada=0, EfectivoHabilitado=1, TransferenciaHabilitada=0, TarjetaHabilitada=0, Version=1, VigenteDesde=now, CambiadoPor=1 });
        await db.SaveChangesAsync();
        if (demo)
        {
            db.Add(new Categoria { Id=1, Nombre="Productos de demostración", Activo=1 }); await db.SaveChangesAsync();
            var products = new[] { ("Arroz blanco","Libra","lb","peso",2400L,50000L), ("Leche entera","Unidad","unidad","unidad",3800L,30L), ("Frijol rojo","Libra","lb","peso",3200L,40000L), ("Azúcar","Libra","lb","peso",1800L,35000L), ("Aceite vegetal","Botella","unidad","unidad",6800L,24L), ("Jabón","Unidad","unidad","unidad",2500L,20L) };
            for (var i=0; i<products.Length; i++)
            {
                var (name,presentation,unit,control,price,stock)=products[i]; var id=i+1L; var scale=control=="peso" ? 3 : 0;
                db.Add(new Producto { Id=id,CategoriaId=1,Nombre=name,UnidadControl=control,UnidadBase=unit,EscalaCantidad=scale,MinimoEscalado=control=="peso" ? 5000 : 5,Activo=1,Version=1 }); await db.SaveChangesAsync();
                db.Add(new Presentacion { Id=id,ProductoId=id,Nombre=presentation,FactorEscalado=1,EscalaFactor=0,Activo=1,Version=1 }); await db.SaveChangesAsync();
                db.Add(new CodigoProducto { PresentacionId=id,Codigo=$"DEMO{id:000}",Activo=1 });
                db.Add(new PrecioProducto { PresentacionId=id,PrecioCentavos=price,VigenteDesde=now,CreadoPor=1,CreadoEn=now });
            }
            await db.SaveChangesAsync();
            var request=new SolicitudInventario { Referencia="DEMO-CARGA-001",Tipo="carga",Estado="Pendiente",Motivo="Inventario ficticio para pruebas académicas",SolicitadoPor=3,CreadoEn=now,EnviadoEn=now,Version=1,ClaveSolicitud="demo-carga-001",EvidenciaReferencia="Datos ficticios; no corresponden a un conteo real" };
            db.Add(request); await db.SaveChangesAsync();
            var snapshot=Formats.Json(new {versionFormato=1,solicitudId=request.Id,version=1,productos=products.Select((p,i)=>new {productoId=i+1,cantidad=p.Item6,escala=p.Item4=="peso"?3:0,estadoCosto="desconocido"})});
            var auth=new Autorizacion { SolicitudInventarioId=request.Id,SolicitanteId=3,AutorizadorId=1,FechaSolicitud=now,FechaDecision=now,Decision="autorizada",Modalidad="segunda_cuenta",ObjetoVersion=1,ContenidoSnapshot=snapshot,ContenidoHash=Formats.Hash(snapshot),Motivo="Aprobación ficticia de carga demo" };
            db.Add(auth); await db.SaveChangesAsync();
            var operation=new OperacionInventario { Tipo="carga",Fecha=now,ConfirmadoEn=now,SecuenciaConfirmacion=1,CreadoPor=3,EjecutadoPor=1,AutorizacionId=auth.Id,ClaveOperacion="demo-carga-001",Motivo=request.Motivo };
            db.Add(operation); await db.SaveChangesAsync(); request.Estado="Autorizada"; request.ResueltoEn=now; request.OperacionId=operation.Id;
            for(var i=0;i<products.Length;i++)
            {
                var item=new SolicitudInventarioDetalle {SolicitudId=request.Id,NumeroLinea=i+1,ProductoId=i+1,CantidadEscalada=products[i].Item6,Escala=products[i].Item4=="peso"?3:0,EstadoCosto="desconocido"};
                db.Add(item); await db.SaveChangesAsync();
                db.Add(new MovimientoInventario {OperacionId=operation.Id,ProductoId=i+1,Secuencia=i+1,SolicitudInventarioDetalleId=item.Id,CantidadEscalada=item.CantidadEscalada,Escala=item.Escala,EstadoCosto="desconocido"});
            }
        }
        db.Add(new EventoAuditoria {Fecha=now,Origen="tarea_automatica",Accion="Inicializar base",Entidad="Sistema",ClaveOperacion="inicializacion",Resultado="exito",DatosCambio=Formats.Json(new {versionFormato=1,demostracion=demo,servicio="Inicializador"}),VersionFormato=1});
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }
    private async Task EnsureDemoOwnerAsync()
    {
        // Ampliación de las bases ficticias anteriores; nunca modifica cuentas existentes.
        await using var tx = await db.Database.BeginTransactionAsync();
        var ownerRole = await db.Set<Rol>().Where(x => x.Nombre == "Propietario").Select(x => x.Id).SingleAsync();
        if (await db.Set<Usuario>().AnyAsync(x => x.RolId == ownerRole || x.NombreNormalizado == "PROPIETARIO")) return;
        var u = new Usuario { NombreCuenta = "propietario", NombreNormalizado = "PROPIETARIO", NombreVisible = "Propietario · demo", Activo = 1, TipoCuenta = "comercial", RolId = ownerRole, CreadoEn = Formats.Now(), CredencialesVersion = 1 };
        u.PasswordHash = hasher.HashPassword(u, "MegaDemo2026!"); db.Add(u); await db.SaveChangesAsync();
        db.Add(new EventoAuditoria { Fecha = Formats.Now(), Origen = "tarea_automatica", Accion = "Agregar Propietario de demostración", Entidad = "Usuario", EntidadId = u.Id.ToString(), ClaveOperacion = "demo-propietario", Resultado = "exito", DatosCambio = Formats.Json(new { demostracion = true, cuenta = u.NombreCuenta }), VersionFormato = 1 });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
