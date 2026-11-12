// Generado por tools/generate_model.py. No editar a mano.
#nullable enable
namespace Mega.Web.Data;

public sealed class Categoria
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
    public long Activo { get; set; }
}

public sealed class Producto
{
    public long Id { get; set; }
    public long CategoriaId { get; set; }
    public string Nombre { get; set; } = "";
    public string UnidadControl { get; set; } = "";
    public long MinimoEscalado { get; set; }
    public string UnidadBase { get; set; } = "";
    public long EscalaCantidad { get; set; }
    public long Activo { get; set; }
    public long ControlVencimiento { get; set; }
    public long Version { get; set; }
}

public sealed class Presentacion
{
    public long Id { get; set; }
    public long ProductoId { get; set; }
    public string Nombre { get; set; } = "";
    public long FactorEscalado { get; set; }
    public long EscalaFactor { get; set; }
    public long Activo { get; set; }
    public long Version { get; set; }
}

public sealed class CodigoProducto
{
    public long Id { get; set; }
    public long PresentacionId { get; set; }
    public string Codigo { get; set; } = "";
    public long Activo { get; set; }
}

public sealed class PrecioProducto
{
    public long Id { get; set; }
    public long PresentacionId { get; set; }
    public long PrecioCentavos { get; set; }
    public string VigenteDesde { get; set; } = "";
    public string? VigenteHasta { get; set; }
    public long CreadoPor { get; set; }
    public string CreadoEn { get; set; } = "";
}

public sealed class Promocion
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public long ValorEscalado { get; set; }
    public long EscalaValor { get; set; }
    public string Inicio { get; set; } = "";
    public string Fin { get; set; } = "";
    public long Activa { get; set; }
    public long CreadoPor { get; set; }
    public string CreadoEn { get; set; } = "";
    public long Version { get; set; }
}

public sealed class PromocionProducto
{
    public long PromocionId { get; set; }
    public long PresentacionId { get; set; }
    public string ReglaAplicacion { get; set; } = "";
    public long Prioridad { get; set; }
}

public sealed class Impuesto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
    public long TasaEscalada { get; set; }
    public long EscalaTasa { get; set; }
    public string VigenteDesde { get; set; } = "";
    public string? VigenteHasta { get; set; }
    public long EsExencion { get; set; }
    public string? MotivoExencion { get; set; }
    public long Activo { get; set; }
    public long CreadoPor { get; set; }
    public string CreadoEn { get; set; } = "";
}

public sealed class ProductoImpuesto
{
    public long ProductoId { get; set; }
    public long ImpuestoId { get; set; }
    public string VigenteDesde { get; set; } = "";
    public string? VigenteHasta { get; set; }
}

public sealed class ConfiguracionNegocio
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string? Telefono { get; set; }
    public string Moneda { get; set; } = "";
    public string? Emisor { get; set; }
    public string? IdentificacionEmisor { get; set; }
    public string? RegimenFiscal { get; set; }
    public long FiscalidadValidada { get; set; }
    public long FondoHabitualCentavos { get; set; }
    public string? ImpresoraNombre { get; set; }
    public long ImpresionHabilitada { get; set; }
    public long EfectivoHabilitado { get; set; }
    public long TransferenciaHabilitada { get; set; }
    public long TarjetaHabilitada { get; set; }
    public string? ProcedimientoTransferencia { get; set; }
    public string? SmtpHost { get; set; }
    public long? SmtpPuerto { get; set; }
    public string? SmtpSeguridad { get; set; }
    public string? CorreoRemitente { get; set; }
    public string? SmtpUsuario { get; set; }
    public string? SmtpSecretoReferencia { get; set; }
    public long Version { get; set; }
    public string VigenteDesde { get; set; } = "";
    public string? VigenteHasta { get; set; }
    public long CambiadoPor { get; set; }
    public long? DiasAlertaVencimiento { get; set; }
    public string? PautaRespaldo { get; set; }
    public long? PautaAutorizadaPor { get; set; }
    public string? PautaAutorizadaEn { get; set; }
}

public sealed class Proveedor
{
    public long Id { get; set; }
    public string? Identificacion { get; set; }
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }
    public long Activo { get; set; }
    public long Version { get; set; }
}

public sealed class Recepcion
{
    public long Id { get; set; }
    public long? ProveedorId { get; set; }
    public long? OperacionId { get; set; }
    public string Referencia { get; set; } = "";
    public string? Documento { get; set; }
    public string? FechaDocumento { get; set; }
    public string CreadoEn { get; set; } = "";
    public long CreadoPor { get; set; }
    public string ModificadoEn { get; set; } = "";
    public string Estado { get; set; } = "";
    public string? Observaciones { get; set; }
    public string Moneda { get; set; } = "";
    public long Version { get; set; }
    public string ClaveSolicitud { get; set; } = "";
}

public sealed class RecepcionDetalle
{
    public long Id { get; set; }
    public long RecepcionId { get; set; }
    public long NumeroLinea { get; set; }
    public long PresentacionId { get; set; }
    public long? CantidadRecibidaEscalada { get; set; }
    public long? CantidadAceptadaEscalada { get; set; }
    public long EscalaCantidad { get; set; }
    public long FactorHistoricoEscalado { get; set; }
    public long EscalaFactor { get; set; }
    public long? CantidadBaseEscalada { get; set; }
    public long? CostoUnitEscalado { get; set; }
    public long? EscalaCosto { get; set; }
    public string EstadoCosto { get; set; } = "";
    public string? Vencimiento { get; set; }
    public string? LoteReferencia { get; set; }
    public string? Observacion { get; set; }
}

public sealed class LoteExistencia
{
    public long Id { get; set; }
    public long ProductoId { get; set; }
    public long? RecepcionDetalleId { get; set; }
    public string? Vencimiento { get; set; }
    public string? Referencia { get; set; }
    public string CreadoEn { get; set; } = "";
}

public sealed class OperacionInventario
{
    public long Id { get; set; }
    public string Tipo { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string ConfirmadoEn { get; set; } = "";
    public long SecuenciaConfirmacion { get; set; }
    public long CreadoPor { get; set; }
    public long EjecutadoPor { get; set; }
    public long? AutorizacionId { get; set; }
    public string? Motivo { get; set; }
    public string ClaveOperacion { get; set; } = "";
    public long? ReversionDeId { get; set; }
}

public sealed class MovimientoInventario
{
    public long Id { get; set; }
    public long OperacionId { get; set; }
    public long ProductoId { get; set; }
    public long? LoteId { get; set; }
    public long Secuencia { get; set; }
    public long? VentaDetalleId { get; set; }
    public long? RecepcionDetalleId { get; set; }
    public long? SolicitudInventarioDetalleId { get; set; }
    public long? MovimientoRevertidoId { get; set; }
    public long CantidadEscalada { get; set; }
    public long Escala { get; set; }
    public long? CostoUnitEscalado { get; set; }
    public long? EscalaCosto { get; set; }
    public string EstadoCosto { get; set; } = "";
    public long? CostoReferenciaId { get; set; }
}

public sealed class Conteo
{
    public long Id { get; set; }
    public string Referencia { get; set; } = "";
    public string CreadoEn { get; set; } = "";
    public string FechaConteo { get; set; } = "";
    public long RegistradoPor { get; set; }
    public string? FinalizadoEn { get; set; }
    public string Estado { get; set; } = "";
    public long? OperacionReferenciaId { get; set; }
    public string? Observaciones { get; set; }
    public long Version { get; set; }
}

public sealed class ConteoDetalle
{
    public long Id { get; set; }
    public long ConteoId { get; set; }
    public long ProductoId { get; set; }
    public long? LoteId { get; set; }
    public long CantidadFisicaEscalada { get; set; }
    public long Escala { get; set; }
    public long SaldoReferenciaEscalado { get; set; }
    public string? Observacion { get; set; }
}

public sealed class SolicitudInventario
{
    public long Id { get; set; }
    public string Referencia { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Estado { get; set; } = "";
    public string? Motivo { get; set; }
    public long? ConteoId { get; set; }
    public long SolicitadoPor { get; set; }
    public string CreadoEn { get; set; } = "";
    public string? EnviadoEn { get; set; }
    public string? ResueltoEn { get; set; }
    public long? OperacionId { get; set; }
    public long Version { get; set; }
    public string ClaveSolicitud { get; set; } = "";
    public string? EvidenciaReferencia { get; set; }
}

public sealed class SolicitudInventarioDetalle
{
    public long Id { get; set; }
    public long SolicitudId { get; set; }
    public long NumeroLinea { get; set; }
    public long ProductoId { get; set; }
    public long? LoteId { get; set; }
    public long CantidadEscalada { get; set; }
    public long Escala { get; set; }
    public string? Causa { get; set; }
    public long? ProveedorDevolucionId { get; set; }
    public string? EvidenciaReferencia { get; set; }
    public long? CostoUnitEscalado { get; set; }
    public long? EscalaCosto { get; set; }
    public string EstadoCosto { get; set; } = "";
    public long? CostoReferenciaId { get; set; }
}

public sealed class Venta
{
    public long Id { get; set; }
    public string Folio { get; set; } = "";
    public string Fecha { get; set; } = "";
    public long TurnoId { get; set; }
    public long OperacionId { get; set; }
    public long ConfiguracionId { get; set; }
    public string Estado { get; set; } = "";
    public long SubtotalCentavos { get; set; }
    public long ImpuestoCentavos { get; set; }
    public long DescuentoCentavos { get; set; }
    public long TotalCentavos { get; set; }
    public string Moneda { get; set; } = "";
    public string ClaveOperacion { get; set; } = "";
    public string ComprobanteSnapshot { get; set; } = "";
    public string ComprobanteHash { get; set; } = "";
    public long Version { get; set; }
}

public sealed class VentaDetalle
{
    public long Id { get; set; }
    public long VentaId { get; set; }
    public long NumeroLinea { get; set; }
    public long PresentacionId { get; set; }
    public long ProductoId { get; set; }
    public string DescripcionHistorica { get; set; } = "";
    public string PresentacionHistorica { get; set; } = "";
    public string UnidadBaseHistorica { get; set; } = "";
    public long CantidadEscalada { get; set; }
    public long EscalaCantidad { get; set; }
    public long FactorHistoricoEscalado { get; set; }
    public long EscalaFactor { get; set; }
    public long CantidadBaseEscalada { get; set; }
    public string ModalidadVenta { get; set; } = "";
    public long ValorSolicitadoEscalado { get; set; }
    public long EscalaValorSolicitado { get; set; }
    public long PrecioUnitEscalado { get; set; }
    public long EscalaPrecio { get; set; }
    public long? CostoUnitEscalado { get; set; }
    public long? EscalaCosto { get; set; }
    public string EstadoCosto { get; set; } = "";
    public long? CostoReferenciaId { get; set; }
    public long SubtotalCentavos { get; set; }
    public long ImpuestoCentavos { get; set; }
    public long DescuentoCentavos { get; set; }
    public long TotalCentavos { get; set; }
    public string ImpuestosSnapshot { get; set; } = "";
    public string? PromocionSnapshot { get; set; }
}

public sealed class Pago
{
    public long Id { get; set; }
    public long VentaId { get; set; }
    public long Secuencia { get; set; }
    public string Medio { get; set; } = "";
    public long ImporteCentavos { get; set; }
    public long? RecibidoCentavos { get; set; }
    public long? CambioCentavos { get; set; }
    public string? EvidenciaReferencia { get; set; }
    public string? ReferenciaExterna { get; set; }
    public string EstadoVerificacion { get; set; } = "";
    public long VerificadoPor { get; set; }
    public string VerificadoEn { get; set; } = "";
}

public sealed class TurnoCaja
{
    public long Id { get; set; }
    public long OperadorId { get; set; }
    public string EstacionId { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Apertura { get; set; } = "";
    public string? Cierre { get; set; }
    public long ConfiguracionId { get; set; }
    public long FondoHabitualCentavos { get; set; }
    public long FondoRealCentavos { get; set; }
    public string? MotivoDiferenciaFondo { get; set; }
    public long? EfectivoEsperadoCierreCentavos { get; set; }
    public long? EfectivoContadoCentavos { get; set; }
    public long? DiferenciaCentavos { get; set; }
    public string? MotivoDiferenciaCierre { get; set; }
    public long Version { get; set; }
}

public sealed class MovimientoCaja
{
    public long Id { get; set; }
    public long TurnoId { get; set; }
    public long VentaId { get; set; }
    public long OperacionId { get; set; }
    public long? SolicitudAnulacionId { get; set; }
    public string Tipo { get; set; } = "";
    public long ImporteCentavos { get; set; }
    public long RegistradoPor { get; set; }
    public string Fecha { get; set; } = "";
    public string? EvidenciaReferencia { get; set; }
}

public sealed class GastoOperativo
{
    public long Id { get; set; }
    public string Fecha { get; set; } = "";
    public string CreadoEn { get; set; } = "";
    public string Concepto { get; set; } = "";
    public long ImporteCentavos { get; set; }
    public string Moneda { get; set; } = "";
    public long RegistradoPor { get; set; }
    public string? ComprobanteReferencia { get; set; }
    public string Estado { get; set; } = "";
    public long? CorrigeAId { get; set; }
    public string? MotivoCorreccion { get; set; }
    public long? AnuladoPor { get; set; }
    public string? AnuladoEn { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string ClaveOperacion { get; set; } = "";
}

public sealed class SolicitudAnulacion
{
    public long Id { get; set; }
    public long VentaId { get; set; }
    public string Referencia { get; set; } = "";
    public long SolicitadoPor { get; set; }
    public string CreadoEn { get; set; } = "";
    public string? EnviadoEn { get; set; }
    public string Estado { get; set; } = "";
    public string? Motivo { get; set; }
    public string? EvidenciaReferencia { get; set; }
    public long Version { get; set; }
    public string ClaveSolicitud { get; set; } = "";
    public long? OperacionId { get; set; }
    public string ReincorporacionEstado { get; set; } = "";
    public string ReincorporacionDetalle { get; set; } = "";
    public long EfectivoADevolverCentavos { get; set; }
    public long EfectivoDevueltoCentavos { get; set; }
    public long FondosExternosARegularizarCentavos { get; set; }
    public long FondosExternosRegularizadosCentavos { get; set; }
    public string RegularizacionEstado { get; set; } = "";
    public string RegularizacionDetalle { get; set; } = "";
    public long? RegularizadoPor { get; set; }
    public string? RegularizadoEn { get; set; }
}

public sealed class Usuario
{
    public long Id { get; set; }
    public string NombreCuenta { get; set; } = "";
    public string NombreNormalizado { get; set; } = "";
    public string NombreVisible { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public long Activo { get; set; }
    public string TipoCuenta { get; set; } = "";
    public long? RolId { get; set; }
    public long PermisoOperativoCaja { get; set; }
    public long DelegacionCuentas { get; set; }
    public string CreadoEn { get; set; } = "";
    public long CredencialesVersion { get; set; }
    public string? FinAcceso { get; set; }
}

public sealed class Rol
{
    public long Id { get; set; }
    public string Nombre { get; set; } = "";
}

public sealed class Autorizacion
{
    public long Id { get; set; }
    public long? RecepcionId { get; set; }
    public long? SolicitudInventarioId { get; set; }
    public long? SolicitudAnulacionId { get; set; }
    public long SolicitanteId { get; set; }
    public long? AutorizadorId { get; set; }
    public string FechaSolicitud { get; set; } = "";
    public string? FechaDecision { get; set; }
    public string Decision { get; set; } = "";
    public string Modalidad { get; set; } = "";
    public string? Motivo { get; set; }
    public long ObjetoVersion { get; set; }
    public string ContenidoHash { get; set; } = "";
    public string ContenidoSnapshot { get; set; } = "";
    public string? InvalidadaEn { get; set; }
    public string? MotivoInvalidacion { get; set; }
}

public sealed class AccesoSoporte
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public long AutorizadoPor { get; set; }
    public string AutorizadoEn { get; set; } = "";
    public string Inicio { get; set; } = "";
    public string Fin { get; set; } = "";
    public string Alcance { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string? RevocadoEn { get; set; }
    public long? RevocadoPor { get; set; }
    public string? MotivoRevocacion { get; set; }
}

public sealed class EventoAuditoria
{
    public long Id { get; set; }
    public string Fecha { get; set; } = "";
    public long? UsuarioId { get; set; }
    public string Origen { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string? EntidadId { get; set; }
    public string ClaveOperacion { get; set; } = "";
    public string Resultado { get; set; } = "";
    public long? EventoOrigenId { get; set; }
    public long? AutorizacionId { get; set; }
    public string DatosCambio { get; set; } = "";
    public long VersionFormato { get; set; }
}

public sealed class SolicitudImpresion
{
    public long Id { get; set; }
    public long VentaId { get; set; }
    public long SolicitadoPor { get; set; }
    public string Tipo { get; set; } = "";
    public long CopiaNumero { get; set; }
    public string Estado { get; set; } = "";
    public string ClaveSolicitud { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string ImpresoraHistorica { get; set; } = "";
    public string ContenidoHash { get; set; } = "";
    public string? LeaseToken { get; set; }
    public string? LeaseHasta { get; set; }
    public string? ProximoIntentoEn { get; set; }
    public string? UltimoError { get; set; }
    public string? AceptadaEn { get; set; }
    public long? RestauracionId { get; set; }
    public string? EstadoAntesCuarentena { get; set; }
    public long? ResueltoPor { get; set; }
    public string? ResueltoEn { get; set; }
    public string? Resolucion { get; set; }
    public string? MotivoResolucion { get; set; }
}

public sealed class IntentoImpresion
{
    public long Id { get; set; }
    public long SolicitudImpresionId { get; set; }
    public long Numero { get; set; }
    public string Inicio { get; set; } = "";
    public string? Fin { get; set; }
    public string Resultado { get; set; } = "";
    public string? IdTrabajoSO { get; set; }
    public string Impresora { get; set; } = "";
    public string AgenteIdentidad { get; set; } = "";
    public string LeaseToken { get; set; } = "";
    public string? Evidencia { get; set; }
    public string? ErrorCodigo { get; set; }
}

public sealed class EntregaComprobante
{
    public long Id { get; set; }
    public long VentaId { get; set; }
    public long? SolicitudImpresionId { get; set; }
    public string Tipo { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string? Motivo { get; set; }
    public long RegistradoPor { get; set; }
    public long? AutorizadoPor { get; set; }
    public string? EvidenciaReferencia { get; set; }
}

public sealed class SolicitudCorreo
{
    public long Id { get; set; }
    public long SolicitadoPor { get; set; }
    public string Fecha { get; set; } = "";
    public string Destinatario { get; set; } = "";
    public string ReporteTipo { get; set; } = "";
    public string ParametrosReporte { get; set; } = "";
    public string ReporteReferencia { get; set; } = "";
    public string ReporteHash { get; set; } = "";
    public string ReporteGeneradoEn { get; set; } = "";
    public string Estado { get; set; } = "";
    public string ClaveSolicitud { get; set; } = "";
    public string MessageId { get; set; } = "";
    public string? LeaseToken { get; set; }
    public string? LeaseHasta { get; set; }
    public string? ProximoIntentoEn { get; set; }
    public string? UltimoError { get; set; }
    public string? AceptadoEn { get; set; }
    public long? RestauracionId { get; set; }
    public string? EstadoAntesCuarentena { get; set; }
    public long? ResueltoPor { get; set; }
    public string? ResueltoEn { get; set; }
    public string? Resolucion { get; set; }
    public string? MotivoResolucion { get; set; }
}

public sealed class IntentoCorreo
{
    public long Id { get; set; }
    public long SolicitudCorreoId { get; set; }
    public long ConfiguracionId { get; set; }
    public long Numero { get; set; }
    public string Inicio { get; set; } = "";
    public string? Fin { get; set; }
    public string Resultado { get; set; } = "";
    public string EtapaSMTP { get; set; } = "";
    public string? CodigoSMTP { get; set; }
    public string? RespuestaReferencia { get; set; }
    public string MessageId { get; set; } = "";
    public string LeaseToken { get; set; } = "";
    public string? Evidencia { get; set; }
    public string? ErrorCodigo { get; set; }
}

public sealed class RespaldoEjecucion
{
    public long Id { get; set; }
    public string ClaveEjecucion { get; set; } = "";
    public string Inicio { get; set; } = "";
    public string? Fin { get; set; }
    public long? SolicitadoPor { get; set; }
    public long? EjecutadoPor { get; set; }
    public string Origen { get; set; } = "";
    public string Destino { get; set; } = "";
    public string? MedioIdentificador { get; set; }
    public string? ArchivoReferencia { get; set; }
    public long? TamanoBytes { get; set; }
    public string Estado { get; set; } = "";
    public string? Checksum { get; set; }
    public string? Manifiesto { get; set; }
    public string? VersionEsquema { get; set; }
    public long? UltimoMovimientoId { get; set; }
    public long? UltimaOperacionId { get; set; }
    public string? ComprobadoEn { get; set; }
    public string? UltimoError { get; set; }
    public long ConfiguracionId { get; set; }
}

public sealed class Restauracion
{
    public long Id { get; set; }
    public long? RespaldoId { get; set; }
    public string RespaldoReferencia { get; set; } = "";
    public string RespaldoChecksum { get; set; } = "";
    public string VersionEsquemaOrigen { get; set; } = "";
    public long AutorizadoPor { get; set; }
    public string AutorizadoEn { get; set; } = "";
    public long EjecutadoPor { get; set; }
    public string Inicio { get; set; } = "";
    public string? Fin { get; set; }
    public string Estado { get; set; } = "";
    public string? ResultadoDetalle { get; set; }
    public string Motivo { get; set; } = "";
    public string? PreservacionActualReferencia { get; set; }
    public string PuntoRecuperado { get; set; } = "";
    public string ConciliacionEstado { get; set; } = "";
    public string? ConciliacionDetalle { get; set; }
    public string? NucleoReabiertoEn { get; set; }
    public long? ReaperturaAutorizadaPor { get; set; }
    public string ClaveIntervencion { get; set; } = "";
}
