// Generado por tools/generate_model.py. DDL conservado en Schema.sql.
using Microsoft.EntityFrameworkCore;
namespace Mega.Web.Data;

public sealed partial class MegaDbContext(DbContextOptions<MegaDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Categoria>(e =>
        {
            e.ToTable("Categoria");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Activo).HasColumnName("activo");
        });
        model.Entity<Producto>(e =>
        {
            e.ToTable("Producto");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CategoriaId).HasColumnName("categoriaId");
            e.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.UnidadControl).HasColumnName("unidadControl");
            e.Property(x => x.MinimoEscalado).HasColumnName("minimoEscalado");
            e.Property(x => x.UnidadBase).HasColumnName("unidadBase");
            e.Property(x => x.EscalaCantidad).HasColumnName("escalaCantidad");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.ControlVencimiento).HasColumnName("controlVencimiento");
            e.Property(x => x.Version).HasColumnName("version");
        });
        model.Entity<Presentacion>(e =>
        {
            e.ToTable("Presentacion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProductoId).HasColumnName("productoId");
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.FactorEscalado).HasColumnName("factorEscalado");
            e.Property(x => x.EscalaFactor).HasColumnName("escalaFactor");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.Version).HasColumnName("version");
        });
        model.Entity<CodigoProducto>(e =>
        {
            e.ToTable("CodigoProducto");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PresentacionId).HasColumnName("presentacionId");
            e.HasOne<Presentacion>().WithMany().HasForeignKey(x => x.PresentacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Codigo).HasColumnName("codigo");
            e.Property(x => x.Activo).HasColumnName("activo");
        });
        model.Entity<PrecioProducto>(e =>
        {
            e.ToTable("PrecioProducto");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PresentacionId).HasColumnName("presentacionId");
            e.HasOne<Presentacion>().WithMany().HasForeignKey(x => x.PresentacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.PrecioCentavos).HasColumnName("precioCentavos");
            e.Property(x => x.VigenteDesde).HasColumnName("vigenteDesde");
            e.Property(x => x.VigenteHasta).HasColumnName("vigenteHasta");
            e.Property(x => x.CreadoPor).HasColumnName("creadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
        });
        model.Entity<Promocion>(e =>
        {
            e.ToTable("Promocion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Tipo).HasColumnName("tipo");
            e.Property(x => x.ValorEscalado).HasColumnName("valorEscalado");
            e.Property(x => x.EscalaValor).HasColumnName("escalaValor");
            e.Property(x => x.Inicio).HasColumnName("inicio");
            e.Property(x => x.Fin).HasColumnName("fin");
            e.Property(x => x.Activa).HasColumnName("activa");
            e.Property(x => x.CreadoPor).HasColumnName("creadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
            e.Property(x => x.Version).HasColumnName("version");
        });
        model.Entity<PromocionProducto>(e =>
        {
            e.ToTable("PromocionProducto");
            e.HasKey(x => new { x.PromocionId, x.PresentacionId });
            e.Property(x => x.PromocionId).HasColumnName("promocionId");
            e.Property(x => x.PromocionId).ValueGeneratedNever();
            e.HasOne<Promocion>().WithMany().HasForeignKey(x => x.PromocionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.PresentacionId).HasColumnName("presentacionId");
            e.Property(x => x.PresentacionId).ValueGeneratedNever();
            e.HasOne<Presentacion>().WithMany().HasForeignKey(x => x.PresentacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ReglaAplicacion).HasColumnName("reglaAplicacion");
            e.Property(x => x.Prioridad).HasColumnName("prioridad");
        });
        model.Entity<Impuesto>(e =>
        {
            e.ToTable("Impuesto");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.TasaEscalada).HasColumnName("tasaEscalada");
            e.Property(x => x.EscalaTasa).HasColumnName("escalaTasa");
            e.Property(x => x.VigenteDesde).HasColumnName("vigenteDesde");
            e.Property(x => x.VigenteHasta).HasColumnName("vigenteHasta");
            e.Property(x => x.EsExencion).HasColumnName("esExencion");
            e.Property(x => x.MotivoExencion).HasColumnName("motivoExencion");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.CreadoPor).HasColumnName("creadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
        });
        model.Entity<ProductoImpuesto>(e =>
        {
            e.ToTable("ProductoImpuesto");
            e.HasKey(x => new { x.ProductoId, x.ImpuestoId, x.VigenteDesde });
            e.Property(x => x.ProductoId).HasColumnName("productoId");
            e.Property(x => x.ProductoId).ValueGeneratedNever();
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ImpuestoId).HasColumnName("impuestoId");
            e.Property(x => x.ImpuestoId).ValueGeneratedNever();
            e.HasOne<Impuesto>().WithMany().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.VigenteDesde).HasColumnName("vigenteDesde");
            e.Property(x => x.VigenteDesde).ValueGeneratedNever();
            e.Property(x => x.VigenteHasta).HasColumnName("vigenteHasta");
        });
        model.Entity<ConfiguracionNegocio>(e =>
        {
            e.ToTable("ConfiguracionNegocio");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Direccion).HasColumnName("direccion");
            e.Property(x => x.Telefono).HasColumnName("telefono");
            e.Property(x => x.Moneda).HasColumnName("moneda");
            e.Property(x => x.Emisor).HasColumnName("emisor");
            e.Property(x => x.IdentificacionEmisor).HasColumnName("identificacionEmisor");
            e.Property(x => x.RegimenFiscal).HasColumnName("regimenFiscal");
            e.Property(x => x.FiscalidadValidada).HasColumnName("fiscalidadValidada");
            e.Property(x => x.FondoHabitualCentavos).HasColumnName("fondoHabitualCentavos");
            e.Property(x => x.ImpresoraNombre).HasColumnName("impresoraNombre");
            e.Property(x => x.ImpresionHabilitada).HasColumnName("impresionHabilitada");
            e.Property(x => x.EfectivoHabilitado).HasColumnName("efectivoHabilitado");
            e.Property(x => x.TransferenciaHabilitada).HasColumnName("transferenciaHabilitada");
            e.Property(x => x.TarjetaHabilitada).HasColumnName("tarjetaHabilitada");
            e.Property(x => x.ProcedimientoTransferencia).HasColumnName("procedimientoTransferencia");
            e.Property(x => x.SmtpHost).HasColumnName("smtpHost");
            e.Property(x => x.SmtpPuerto).HasColumnName("smtpPuerto");
            e.Property(x => x.SmtpSeguridad).HasColumnName("smtpSeguridad");
            e.Property(x => x.CorreoRemitente).HasColumnName("correoRemitente");
            e.Property(x => x.SmtpUsuario).HasColumnName("smtpUsuario");
            e.Property(x => x.SmtpSecretoReferencia).HasColumnName("smtpSecretoReferencia");
            e.Property(x => x.Version).HasColumnName("version");
            e.Property(x => x.VigenteDesde).HasColumnName("vigenteDesde");
            e.Property(x => x.VigenteHasta).HasColumnName("vigenteHasta");
            e.Property(x => x.CambiadoPor).HasColumnName("cambiadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CambiadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.DiasAlertaVencimiento).HasColumnName("diasAlertaVencimiento");
            e.Property(x => x.PautaRespaldo).HasColumnName("pautaRespaldo");
            e.Property(x => x.PautaAutorizadaPor).HasColumnName("pautaAutorizadaPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.PautaAutorizadaPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.PautaAutorizadaEn).HasColumnName("pautaAutorizadaEn");
        });
        model.Entity<Proveedor>(e =>
        {
            e.ToTable("Proveedor");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Identificacion).HasColumnName("identificacion");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Telefono).HasColumnName("telefono");
            e.Property(x => x.Direccion).HasColumnName("direccion");
            e.Property(x => x.Contacto).HasColumnName("contacto");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.Version).HasColumnName("version");
        });
        model.Entity<Recepcion>(e =>
        {
            e.ToTable("Recepcion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProveedorId).HasColumnName("proveedorId");
            e.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.OperacionId).HasColumnName("operacionId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.OperacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Referencia).HasColumnName("referencia");
            e.Property(x => x.Documento).HasColumnName("documento");
            e.Property(x => x.FechaDocumento).HasColumnName("fechaDocumento");
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
            e.Property(x => x.CreadoPor).HasColumnName("creadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ModificadoEn).HasColumnName("modificadoEn");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.Observaciones).HasColumnName("observaciones");
            e.Property(x => x.Moneda).HasColumnName("moneda");
            e.Property(x => x.Version).HasColumnName("version");
            e.Property(x => x.ClaveSolicitud).HasColumnName("claveSolicitud");
        });
        model.Entity<RecepcionDetalle>(e =>
        {
            e.ToTable("RecepcionDetalle");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RecepcionId).HasColumnName("recepcionId");
            e.HasOne<Recepcion>().WithMany().HasForeignKey(x => x.RecepcionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.NumeroLinea).HasColumnName("numeroLinea");
            e.Property(x => x.PresentacionId).HasColumnName("presentacionId");
            e.HasOne<Presentacion>().WithMany().HasForeignKey(x => x.PresentacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CantidadRecibidaEscalada).HasColumnName("cantidadRecibidaEscalada");
            e.Property(x => x.CantidadAceptadaEscalada).HasColumnName("cantidadAceptadaEscalada");
            e.Property(x => x.EscalaCantidad).HasColumnName("escalaCantidad");
            e.Property(x => x.FactorHistoricoEscalado).HasColumnName("factorHistoricoEscalado");
            e.Property(x => x.EscalaFactor).HasColumnName("escalaFactor");
            e.Property(x => x.CantidadBaseEscalada).HasColumnName("cantidadBaseEscalada");
            e.Property(x => x.CostoUnitEscalado).HasColumnName("costoUnitEscalado");
            e.Property(x => x.EscalaCosto).HasColumnName("escalaCosto");
            e.Property(x => x.EstadoCosto).HasColumnName("estadoCosto");
            e.Property(x => x.Vencimiento).HasColumnName("vencimiento");
            e.Property(x => x.LoteReferencia).HasColumnName("loteReferencia");
            e.Property(x => x.Observacion).HasColumnName("observacion");
        });
        model.Entity<LoteExistencia>(e =>
        {
            e.ToTable("LoteExistencia");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProductoId).HasColumnName("productoId");
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.RecepcionDetalleId).HasColumnName("recepcionDetalleId");
            e.HasOne<RecepcionDetalle>().WithMany().HasForeignKey(x => x.RecepcionDetalleId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Vencimiento).HasColumnName("vencimiento");
            e.Property(x => x.Referencia).HasColumnName("referencia");
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
        });
        model.Entity<OperacionInventario>(e =>
        {
            e.ToTable("OperacionInventario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Tipo).HasColumnName("tipo");
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.ConfirmadoEn).HasColumnName("confirmadoEn");
            e.Property(x => x.SecuenciaConfirmacion).HasColumnName("secuenciaConfirmacion");
            e.Property(x => x.CreadoPor).HasColumnName("creadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EjecutadoPor).HasColumnName("ejecutadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.EjecutadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AutorizacionId).HasColumnName("autorizacionId");
            e.HasOne<Autorizacion>().WithMany().HasForeignKey(x => x.AutorizacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Motivo).HasColumnName("motivo");
            e.Property(x => x.ClaveOperacion).HasColumnName("claveOperacion");
            e.Property(x => x.ReversionDeId).HasColumnName("reversionDeId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.ReversionDeId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("MovimientoInventario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OperacionId).HasColumnName("operacionId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.OperacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ProductoId).HasColumnName("productoId");
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.LoteId).HasColumnName("loteId");
            e.HasOne<LoteExistencia>().WithMany().HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Secuencia).HasColumnName("secuencia");
            e.Property(x => x.VentaDetalleId).HasColumnName("ventaDetalleId");
            e.HasOne<VentaDetalle>().WithMany().HasForeignKey(x => x.VentaDetalleId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.RecepcionDetalleId).HasColumnName("recepcionDetalleId");
            e.HasOne<RecepcionDetalle>().WithMany().HasForeignKey(x => x.RecepcionDetalleId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SolicitudInventarioDetalleId).HasColumnName("solicitudInventarioDetalleId");
            e.HasOne<SolicitudInventarioDetalle>().WithMany().HasForeignKey(x => x.SolicitudInventarioDetalleId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.MovimientoRevertidoId).HasColumnName("movimientoRevertidoId");
            e.HasOne<MovimientoInventario>().WithMany().HasForeignKey(x => x.MovimientoRevertidoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CantidadEscalada).HasColumnName("cantidadEscalada");
            e.Property(x => x.Escala).HasColumnName("escala");
            e.Property(x => x.CostoUnitEscalado).HasColumnName("costoUnitEscalado");
            e.Property(x => x.EscalaCosto).HasColumnName("escalaCosto");
            e.Property(x => x.EstadoCosto).HasColumnName("estadoCosto");
            e.Property(x => x.CostoReferenciaId).HasColumnName("costoReferenciaId");
            e.HasOne<MovimientoInventario>().WithMany().HasForeignKey(x => x.CostoReferenciaId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Conteo>(e =>
        {
            e.ToTable("Conteo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Referencia).HasColumnName("referencia");
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
            e.Property(x => x.FechaConteo).HasColumnName("fechaConteo");
            e.Property(x => x.RegistradoPor).HasColumnName("registradoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.FinalizadoEn).HasColumnName("finalizadoEn");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.OperacionReferenciaId).HasColumnName("operacionReferenciaId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.OperacionReferenciaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Observaciones).HasColumnName("observaciones");
            e.Property(x => x.Version).HasColumnName("version");
        });
        model.Entity<ConteoDetalle>(e =>
        {
            e.ToTable("ConteoDetalle");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ConteoId).HasColumnName("conteoId");
            e.HasOne<Conteo>().WithMany().HasForeignKey(x => x.ConteoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ProductoId).HasColumnName("productoId");
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.LoteId).HasColumnName("loteId");
            e.HasOne<LoteExistencia>().WithMany().HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CantidadFisicaEscalada).HasColumnName("cantidadFisicaEscalada");
            e.Property(x => x.Escala).HasColumnName("escala");
            e.Property(x => x.SaldoReferenciaEscalado).HasColumnName("saldoReferenciaEscalado");
            e.Property(x => x.Observacion).HasColumnName("observacion");
        });
        model.Entity<SolicitudInventario>(e =>
        {
            e.ToTable("SolicitudInventario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Referencia).HasColumnName("referencia");
            e.Property(x => x.Tipo).HasColumnName("tipo");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.Motivo).HasColumnName("motivo");
            e.Property(x => x.ConteoId).HasColumnName("conteoId");
            e.HasOne<Conteo>().WithMany().HasForeignKey(x => x.ConteoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SolicitadoPor).HasColumnName("solicitadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SolicitadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
            e.Property(x => x.EnviadoEn).HasColumnName("enviadoEn");
            e.Property(x => x.ResueltoEn).HasColumnName("resueltoEn");
            e.Property(x => x.OperacionId).HasColumnName("operacionId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.OperacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Version).HasColumnName("version");
            e.Property(x => x.ClaveSolicitud).HasColumnName("claveSolicitud");
            e.Property(x => x.EvidenciaReferencia).HasColumnName("evidenciaReferencia");
        });
        model.Entity<SolicitudInventarioDetalle>(e =>
        {
            e.ToTable("SolicitudInventarioDetalle");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SolicitudId).HasColumnName("solicitudId");
            e.HasOne<SolicitudInventario>().WithMany().HasForeignKey(x => x.SolicitudId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.NumeroLinea).HasColumnName("numeroLinea");
            e.Property(x => x.ProductoId).HasColumnName("productoId");
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.LoteId).HasColumnName("loteId");
            e.HasOne<LoteExistencia>().WithMany().HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CantidadEscalada).HasColumnName("cantidadEscalada");
            e.Property(x => x.Escala).HasColumnName("escala");
            e.Property(x => x.Causa).HasColumnName("causa");
            e.Property(x => x.ProveedorDevolucionId).HasColumnName("proveedorDevolucionId");
            e.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorDevolucionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EvidenciaReferencia).HasColumnName("evidenciaReferencia");
            e.Property(x => x.CostoUnitEscalado).HasColumnName("costoUnitEscalado");
            e.Property(x => x.EscalaCosto).HasColumnName("escalaCosto");
            e.Property(x => x.EstadoCosto).HasColumnName("estadoCosto");
            e.Property(x => x.CostoReferenciaId).HasColumnName("costoReferenciaId");
            e.HasOne<MovimientoInventario>().WithMany().HasForeignKey(x => x.CostoReferenciaId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Venta>(e =>
        {
            e.ToTable("Venta");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Folio).HasColumnName("folio");
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.TurnoId).HasColumnName("turnoId");
            e.HasOne<TurnoCaja>().WithMany().HasForeignKey(x => x.TurnoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.OperacionId).HasColumnName("operacionId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.OperacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ConfiguracionId).HasColumnName("configuracionId");
            e.HasOne<ConfiguracionNegocio>().WithMany().HasForeignKey(x => x.ConfiguracionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.SubtotalCentavos).HasColumnName("subtotalCentavos");
            e.Property(x => x.ImpuestoCentavos).HasColumnName("impuestoCentavos");
            e.Property(x => x.DescuentoCentavos).HasColumnName("descuentoCentavos");
            e.Property(x => x.TotalCentavos).HasColumnName("totalCentavos");
            e.Property(x => x.Moneda).HasColumnName("moneda");
            e.Property(x => x.ClaveOperacion).HasColumnName("claveOperacion");
            e.Property(x => x.ComprobanteSnapshot).HasColumnName("comprobanteSnapshot");
            e.Property(x => x.ComprobanteHash).HasColumnName("comprobanteHash");
            e.Property(x => x.Version).HasColumnName("version");
        });
        model.Entity<VentaDetalle>(e =>
        {
            e.ToTable("VentaDetalle");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.VentaId).HasColumnName("ventaId");
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.NumeroLinea).HasColumnName("numeroLinea");
            e.Property(x => x.PresentacionId).HasColumnName("presentacionId");
            e.HasOne<Presentacion>().WithMany().HasForeignKey(x => x.PresentacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ProductoId).HasColumnName("productoId");
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.DescripcionHistorica).HasColumnName("descripcionHistorica");
            e.Property(x => x.PresentacionHistorica).HasColumnName("presentacionHistorica");
            e.Property(x => x.UnidadBaseHistorica).HasColumnName("unidadBaseHistorica");
            e.Property(x => x.CantidadEscalada).HasColumnName("cantidadEscalada");
            e.Property(x => x.EscalaCantidad).HasColumnName("escalaCantidad");
            e.Property(x => x.FactorHistoricoEscalado).HasColumnName("factorHistoricoEscalado");
            e.Property(x => x.EscalaFactor).HasColumnName("escalaFactor");
            e.Property(x => x.CantidadBaseEscalada).HasColumnName("cantidadBaseEscalada");
            e.Property(x => x.ModalidadVenta).HasColumnName("modalidadVenta");
            e.Property(x => x.ValorSolicitadoEscalado).HasColumnName("valorSolicitadoEscalado");
            e.Property(x => x.EscalaValorSolicitado).HasColumnName("escalaValorSolicitado");
            e.Property(x => x.PrecioUnitEscalado).HasColumnName("precioUnitEscalado");
            e.Property(x => x.EscalaPrecio).HasColumnName("escalaPrecio");
            e.Property(x => x.CostoUnitEscalado).HasColumnName("costoUnitEscalado");
            e.Property(x => x.EscalaCosto).HasColumnName("escalaCosto");
            e.Property(x => x.EstadoCosto).HasColumnName("estadoCosto");
            e.Property(x => x.CostoReferenciaId).HasColumnName("costoReferenciaId");
            e.HasOne<MovimientoInventario>().WithMany().HasForeignKey(x => x.CostoReferenciaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SubtotalCentavos).HasColumnName("subtotalCentavos");
            e.Property(x => x.ImpuestoCentavos).HasColumnName("impuestoCentavos");
            e.Property(x => x.DescuentoCentavos).HasColumnName("descuentoCentavos");
            e.Property(x => x.TotalCentavos).HasColumnName("totalCentavos");
            e.Property(x => x.ImpuestosSnapshot).HasColumnName("impuestosSnapshot");
            e.Property(x => x.PromocionSnapshot).HasColumnName("promocionSnapshot");
        });
        model.Entity<Pago>(e =>
        {
            e.ToTable("Pago");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.VentaId).HasColumnName("ventaId");
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Secuencia).HasColumnName("secuencia");
            e.Property(x => x.Medio).HasColumnName("medio");
            e.Property(x => x.ImporteCentavos).HasColumnName("importeCentavos");
            e.Property(x => x.RecibidoCentavos).HasColumnName("recibidoCentavos");
            e.Property(x => x.CambioCentavos).HasColumnName("cambioCentavos");
            e.Property(x => x.EvidenciaReferencia).HasColumnName("evidenciaReferencia");
            e.Property(x => x.ReferenciaExterna).HasColumnName("referenciaExterna");
            e.Property(x => x.EstadoVerificacion).HasColumnName("estadoVerificacion");
            e.Property(x => x.VerificadoPor).HasColumnName("verificadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.VerificadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.VerificadoEn).HasColumnName("verificadoEn");
        });
        model.Entity<TurnoCaja>(e =>
        {
            e.ToTable("TurnoCaja");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OperadorId).HasColumnName("operadorId");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.OperadorId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EstacionId).HasColumnName("estacionId");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.Apertura).HasColumnName("apertura");
            e.Property(x => x.Cierre).HasColumnName("cierre");
            e.Property(x => x.ConfiguracionId).HasColumnName("configuracionId");
            e.HasOne<ConfiguracionNegocio>().WithMany().HasForeignKey(x => x.ConfiguracionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.FondoHabitualCentavos).HasColumnName("fondoHabitualCentavos");
            e.Property(x => x.FondoRealCentavos).HasColumnName("fondoRealCentavos");
            e.Property(x => x.MotivoDiferenciaFondo).HasColumnName("motivoDiferenciaFondo");
            e.Property(x => x.EfectivoEsperadoCierreCentavos).HasColumnName("efectivoEsperadoCierreCentavos");
            e.Property(x => x.EfectivoContadoCentavos).HasColumnName("efectivoContadoCentavos");
            e.Property(x => x.DiferenciaCentavos).HasColumnName("diferenciaCentavos");
            e.Property(x => x.MotivoDiferenciaCierre).HasColumnName("motivoDiferenciaCierre");
            e.Property(x => x.Version).HasColumnName("version");
        });
        model.Entity<MovimientoCaja>(e =>
        {
            e.ToTable("MovimientoCaja");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TurnoId).HasColumnName("turnoId");
            e.HasOne<TurnoCaja>().WithMany().HasForeignKey(x => x.TurnoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.VentaId).HasColumnName("ventaId");
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.OperacionId).HasColumnName("operacionId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.OperacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SolicitudAnulacionId).HasColumnName("solicitudAnulacionId");
            e.HasOne<SolicitudAnulacion>().WithMany().HasForeignKey(x => x.SolicitudAnulacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Tipo).HasColumnName("tipo");
            e.Property(x => x.ImporteCentavos).HasColumnName("importeCentavos");
            e.Property(x => x.RegistradoPor).HasColumnName("registradoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.EvidenciaReferencia).HasColumnName("evidenciaReferencia");
        });
        model.Entity<GastoOperativo>(e =>
        {
            e.ToTable("GastoOperativo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
            e.Property(x => x.Concepto).HasColumnName("concepto");
            e.Property(x => x.ImporteCentavos).HasColumnName("importeCentavos");
            e.Property(x => x.Moneda).HasColumnName("moneda");
            e.Property(x => x.RegistradoPor).HasColumnName("registradoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ComprobanteReferencia).HasColumnName("comprobanteReferencia");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.CorrigeAId).HasColumnName("corrigeAId");
            e.HasOne<GastoOperativo>().WithMany().HasForeignKey(x => x.CorrigeAId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.MotivoCorreccion).HasColumnName("motivoCorreccion");
            e.Property(x => x.AnuladoPor).HasColumnName("anuladoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AnuladoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AnuladoEn).HasColumnName("anuladoEn");
            e.Property(x => x.MotivoAnulacion).HasColumnName("motivoAnulacion");
            e.Property(x => x.ClaveOperacion).HasColumnName("claveOperacion");
        });
        model.Entity<SolicitudAnulacion>(e =>
        {
            e.ToTable("SolicitudAnulacion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.VentaId).HasColumnName("ventaId");
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Referencia).HasColumnName("referencia");
            e.Property(x => x.SolicitadoPor).HasColumnName("solicitadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SolicitadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
            e.Property(x => x.EnviadoEn).HasColumnName("enviadoEn");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.Motivo).HasColumnName("motivo");
            e.Property(x => x.EvidenciaReferencia).HasColumnName("evidenciaReferencia");
            e.Property(x => x.Version).HasColumnName("version");
            e.Property(x => x.ClaveSolicitud).HasColumnName("claveSolicitud");
            e.Property(x => x.OperacionId).HasColumnName("operacionId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.OperacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ReincorporacionEstado).HasColumnName("reincorporacionEstado");
            e.Property(x => x.ReincorporacionDetalle).HasColumnName("reincorporacionDetalle");
            e.Property(x => x.EfectivoADevolverCentavos).HasColumnName("efectivoADevolverCentavos");
            e.Property(x => x.EfectivoDevueltoCentavos).HasColumnName("efectivoDevueltoCentavos");
            e.Property(x => x.FondosExternosARegularizarCentavos).HasColumnName("fondosExternosARegularizarCentavos");
            e.Property(x => x.FondosExternosRegularizadosCentavos).HasColumnName("fondosExternosRegularizadosCentavos");
            e.Property(x => x.RegularizacionEstado).HasColumnName("regularizacionEstado");
            e.Property(x => x.RegularizacionDetalle).HasColumnName("regularizacionDetalle");
            e.Property(x => x.RegularizadoPor).HasColumnName("regularizadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegularizadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.RegularizadoEn).HasColumnName("regularizadoEn");
        });
        model.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.NombreCuenta).HasColumnName("nombreCuenta");
            e.Property(x => x.NombreNormalizado).HasColumnName("nombreNormalizado");
            e.Property(x => x.NombreVisible).HasColumnName("nombreVisible");
            e.Property(x => x.PasswordHash).HasColumnName("passwordHash");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.TipoCuenta).HasColumnName("tipoCuenta");
            e.Property(x => x.RolId).HasColumnName("rolId");
            e.HasOne<Rol>().WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.PermisoOperativoCaja).HasColumnName("permisoOperativoCaja");
            e.Property(x => x.DelegacionCuentas).HasColumnName("delegacionCuentas");
            e.Property(x => x.CreadoEn).HasColumnName("creadoEn");
            e.Property(x => x.CredencialesVersion).HasColumnName("credencialesVersion");
            e.Property(x => x.FinAcceso).HasColumnName("finAcceso");
        });
        model.Entity<Rol>(e =>
        {
            e.ToTable("Rol");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
        });
        model.Entity<Autorizacion>(e =>
        {
            e.ToTable("Autorizacion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RecepcionId).HasColumnName("recepcionId");
            e.HasOne<Recepcion>().WithMany().HasForeignKey(x => x.RecepcionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SolicitudInventarioId).HasColumnName("solicitudInventarioId");
            e.HasOne<SolicitudInventario>().WithMany().HasForeignKey(x => x.SolicitudInventarioId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SolicitudAnulacionId).HasColumnName("solicitudAnulacionId");
            e.HasOne<SolicitudAnulacion>().WithMany().HasForeignKey(x => x.SolicitudAnulacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SolicitanteId).HasColumnName("solicitanteId");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SolicitanteId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AutorizadorId).HasColumnName("autorizadorId");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AutorizadorId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.FechaSolicitud).HasColumnName("fechaSolicitud");
            e.Property(x => x.FechaDecision).HasColumnName("fechaDecision");
            e.Property(x => x.Decision).HasColumnName("decision");
            e.Property(x => x.Modalidad).HasColumnName("modalidad");
            e.Property(x => x.Motivo).HasColumnName("motivo");
            e.Property(x => x.ObjetoVersion).HasColumnName("objetoVersion");
            e.Property(x => x.ContenidoHash).HasColumnName("contenidoHash");
            e.Property(x => x.ContenidoSnapshot).HasColumnName("contenidoSnapshot");
            e.Property(x => x.InvalidadaEn).HasColumnName("invalidadaEn");
            e.Property(x => x.MotivoInvalidacion).HasColumnName("motivoInvalidacion");
        });
        model.Entity<AccesoSoporte>(e =>
        {
            e.ToTable("AccesoSoporte");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UsuarioId).HasColumnName("usuarioId");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AutorizadoPor).HasColumnName("autorizadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AutorizadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AutorizadoEn).HasColumnName("autorizadoEn");
            e.Property(x => x.Inicio).HasColumnName("inicio");
            e.Property(x => x.Fin).HasColumnName("fin");
            e.Property(x => x.Alcance).HasColumnName("alcance");
            e.Property(x => x.Motivo).HasColumnName("motivo");
            e.Property(x => x.RevocadoEn).HasColumnName("revocadoEn");
            e.Property(x => x.RevocadoPor).HasColumnName("revocadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RevocadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.MotivoRevocacion).HasColumnName("motivoRevocacion");
        });
        model.Entity<EventoAuditoria>(e =>
        {
            e.ToTable("EventoAuditoria");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.UsuarioId).HasColumnName("usuarioId");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Origen).HasColumnName("origen");
            e.Property(x => x.Accion).HasColumnName("accion");
            e.Property(x => x.Entidad).HasColumnName("entidad");
            e.Property(x => x.EntidadId).HasColumnName("entidadId");
            e.Property(x => x.ClaveOperacion).HasColumnName("claveOperacion");
            e.Property(x => x.Resultado).HasColumnName("resultado");
            e.Property(x => x.EventoOrigenId).HasColumnName("eventoOrigenId");
            e.HasOne<EventoAuditoria>().WithMany().HasForeignKey(x => x.EventoOrigenId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AutorizacionId).HasColumnName("autorizacionId");
            e.HasOne<Autorizacion>().WithMany().HasForeignKey(x => x.AutorizacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.DatosCambio).HasColumnName("datosCambio");
            e.Property(x => x.VersionFormato).HasColumnName("versionFormato");
        });
        model.Entity<SolicitudImpresion>(e =>
        {
            e.ToTable("SolicitudImpresion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.VentaId).HasColumnName("ventaId");
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SolicitadoPor).HasColumnName("solicitadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SolicitadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Tipo).HasColumnName("tipo");
            e.Property(x => x.CopiaNumero).HasColumnName("copiaNumero");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.ClaveSolicitud).HasColumnName("claveSolicitud");
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.ImpresoraHistorica).HasColumnName("impresoraHistorica");
            e.Property(x => x.ContenidoHash).HasColumnName("contenidoHash");
            e.Property(x => x.LeaseToken).HasColumnName("leaseToken");
            e.Property(x => x.LeaseHasta).HasColumnName("leaseHasta");
            e.Property(x => x.ProximoIntentoEn).HasColumnName("proximoIntentoEn");
            e.Property(x => x.UltimoError).HasColumnName("ultimoError");
            e.Property(x => x.AceptadaEn).HasColumnName("aceptadaEn");
            e.Property(x => x.RestauracionId).HasColumnName("restauracionId");
            e.HasOne<Restauracion>().WithMany().HasForeignKey(x => x.RestauracionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EstadoAntesCuarentena).HasColumnName("estadoAntesCuarentena");
            e.Property(x => x.ResueltoPor).HasColumnName("resueltoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.ResueltoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ResueltoEn).HasColumnName("resueltoEn");
            e.Property(x => x.Resolucion).HasColumnName("resolucion");
            e.Property(x => x.MotivoResolucion).HasColumnName("motivoResolucion");
        });
        model.Entity<IntentoImpresion>(e =>
        {
            e.ToTable("IntentoImpresion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SolicitudImpresionId).HasColumnName("solicitudImpresionId");
            e.HasOne<SolicitudImpresion>().WithMany().HasForeignKey(x => x.SolicitudImpresionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Numero).HasColumnName("numero");
            e.Property(x => x.Inicio).HasColumnName("inicio");
            e.Property(x => x.Fin).HasColumnName("fin");
            e.Property(x => x.Resultado).HasColumnName("resultado");
            e.Property(x => x.IdTrabajoSO).HasColumnName("idTrabajoSO");
            e.Property(x => x.Impresora).HasColumnName("impresora");
            e.Property(x => x.AgenteIdentidad).HasColumnName("agenteIdentidad");
            e.Property(x => x.LeaseToken).HasColumnName("leaseToken");
            e.Property(x => x.Evidencia).HasColumnName("evidencia");
            e.Property(x => x.ErrorCodigo).HasColumnName("errorCodigo");
        });
        model.Entity<EntregaComprobante>(e =>
        {
            e.ToTable("EntregaComprobante");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.VentaId).HasColumnName("ventaId");
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.SolicitudImpresionId).HasColumnName("solicitudImpresionId");
            e.HasOne<SolicitudImpresion>().WithMany().HasForeignKey(x => x.SolicitudImpresionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Tipo).HasColumnName("tipo");
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.Motivo).HasColumnName("motivo");
            e.Property(x => x.RegistradoPor).HasColumnName("registradoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AutorizadoPor).HasColumnName("autorizadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AutorizadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EvidenciaReferencia).HasColumnName("evidenciaReferencia");
        });
        model.Entity<SolicitudCorreo>(e =>
        {
            e.ToTable("SolicitudCorreo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SolicitadoPor).HasColumnName("solicitadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SolicitadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.Destinatario).HasColumnName("destinatario");
            e.Property(x => x.ReporteTipo).HasColumnName("reporteTipo");
            e.Property(x => x.ParametrosReporte).HasColumnName("parametrosReporte");
            e.Property(x => x.ReporteReferencia).HasColumnName("reporteReferencia");
            e.Property(x => x.ReporteHash).HasColumnName("reporteHash");
            e.Property(x => x.ReporteGeneradoEn).HasColumnName("reporteGeneradoEn");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.ClaveSolicitud).HasColumnName("claveSolicitud");
            e.Property(x => x.MessageId).HasColumnName("messageId");
            e.Property(x => x.LeaseToken).HasColumnName("leaseToken");
            e.Property(x => x.LeaseHasta).HasColumnName("leaseHasta");
            e.Property(x => x.ProximoIntentoEn).HasColumnName("proximoIntentoEn");
            e.Property(x => x.UltimoError).HasColumnName("ultimoError");
            e.Property(x => x.AceptadoEn).HasColumnName("aceptadoEn");
            e.Property(x => x.RestauracionId).HasColumnName("restauracionId");
            e.HasOne<Restauracion>().WithMany().HasForeignKey(x => x.RestauracionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EstadoAntesCuarentena).HasColumnName("estadoAntesCuarentena");
            e.Property(x => x.ResueltoPor).HasColumnName("resueltoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.ResueltoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ResueltoEn).HasColumnName("resueltoEn");
            e.Property(x => x.Resolucion).HasColumnName("resolucion");
            e.Property(x => x.MotivoResolucion).HasColumnName("motivoResolucion");
        });
        model.Entity<IntentoCorreo>(e =>
        {
            e.ToTable("IntentoCorreo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SolicitudCorreoId).HasColumnName("solicitudCorreoId");
            e.HasOne<SolicitudCorreo>().WithMany().HasForeignKey(x => x.SolicitudCorreoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ConfiguracionId).HasColumnName("configuracionId");
            e.HasOne<ConfiguracionNegocio>().WithMany().HasForeignKey(x => x.ConfiguracionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Numero).HasColumnName("numero");
            e.Property(x => x.Inicio).HasColumnName("inicio");
            e.Property(x => x.Fin).HasColumnName("fin");
            e.Property(x => x.Resultado).HasColumnName("resultado");
            e.Property(x => x.EtapaSMTP).HasColumnName("etapaSMTP");
            e.Property(x => x.CodigoSMTP).HasColumnName("codigoSMTP");
            e.Property(x => x.RespuestaReferencia).HasColumnName("respuestaReferencia");
            e.Property(x => x.MessageId).HasColumnName("messageId");
            e.Property(x => x.LeaseToken).HasColumnName("leaseToken");
            e.Property(x => x.Evidencia).HasColumnName("evidencia");
            e.Property(x => x.ErrorCodigo).HasColumnName("errorCodigo");
        });
        model.Entity<RespaldoEjecucion>(e =>
        {
            e.ToTable("RespaldoEjecucion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ClaveEjecucion).HasColumnName("claveEjecucion");
            e.Property(x => x.Inicio).HasColumnName("inicio");
            e.Property(x => x.Fin).HasColumnName("fin");
            e.Property(x => x.SolicitadoPor).HasColumnName("solicitadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SolicitadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EjecutadoPor).HasColumnName("ejecutadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.EjecutadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Origen).HasColumnName("origen");
            e.Property(x => x.Destino).HasColumnName("destino");
            e.Property(x => x.MedioIdentificador).HasColumnName("medioIdentificador");
            e.Property(x => x.ArchivoReferencia).HasColumnName("archivoReferencia");
            e.Property(x => x.TamanoBytes).HasColumnName("tamanoBytes");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.Checksum).HasColumnName("checksum");
            e.Property(x => x.Manifiesto).HasColumnName("manifiesto");
            e.Property(x => x.VersionEsquema).HasColumnName("versionEsquema");
            e.Property(x => x.UltimoMovimientoId).HasColumnName("ultimoMovimientoId");
            e.HasOne<MovimientoInventario>().WithMany().HasForeignKey(x => x.UltimoMovimientoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.UltimaOperacionId).HasColumnName("ultimaOperacionId");
            e.HasOne<OperacionInventario>().WithMany().HasForeignKey(x => x.UltimaOperacionId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ComprobadoEn).HasColumnName("comprobadoEn");
            e.Property(x => x.UltimoError).HasColumnName("ultimoError");
            e.Property(x => x.ConfiguracionId).HasColumnName("configuracionId");
            e.HasOne<ConfiguracionNegocio>().WithMany().HasForeignKey(x => x.ConfiguracionId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Restauracion>(e =>
        {
            e.ToTable("Restauracion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RespaldoId).HasColumnName("respaldoId");
            e.HasOne<RespaldoEjecucion>().WithMany().HasForeignKey(x => x.RespaldoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.RespaldoReferencia).HasColumnName("respaldoReferencia");
            e.Property(x => x.RespaldoChecksum).HasColumnName("respaldoChecksum");
            e.Property(x => x.VersionEsquemaOrigen).HasColumnName("versionEsquemaOrigen");
            e.Property(x => x.AutorizadoPor).HasColumnName("autorizadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AutorizadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AutorizadoEn).HasColumnName("autorizadoEn");
            e.Property(x => x.EjecutadoPor).HasColumnName("ejecutadoPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.EjecutadoPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Inicio).HasColumnName("inicio");
            e.Property(x => x.Fin).HasColumnName("fin");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.ResultadoDetalle).HasColumnName("resultadoDetalle");
            e.Property(x => x.Motivo).HasColumnName("motivo");
            e.Property(x => x.PreservacionActualReferencia).HasColumnName("preservacionActualReferencia");
            e.Property(x => x.PuntoRecuperado).HasColumnName("puntoRecuperado");
            e.Property(x => x.ConciliacionEstado).HasColumnName("conciliacionEstado");
            e.Property(x => x.ConciliacionDetalle).HasColumnName("conciliacionDetalle");
            e.Property(x => x.NucleoReabiertoEn).HasColumnName("nucleoReabiertoEn");
            e.Property(x => x.ReaperturaAutorizadaPor).HasColumnName("reaperturaAutorizadaPor");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.ReaperturaAutorizadaPor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ClaveIntervencion).HasColumnName("claveIntervencion");
        });
        ConfigureViews(model);
    }
    partial void ConfigureViews(ModelBuilder model);
}
