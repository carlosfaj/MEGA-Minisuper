-- Modelo de datos propuesto de MEGA, revisión académica 2026-11-11.
-- Este DDL concreta tipos, PK, FK, CHECK e índices; las reglas entre tablas
-- y los permisos requieren el servicio transaccional documentado en el Word.
PRAGMA foreign_keys = ON;
BEGIN;

CREATE TABLE "Categoria" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "nombre" TEXT NOT NULL CHECK (length(trim(nombre))>0),
  "activo" INTEGER NOT NULL CHECK (activo IN (0,1)),
  UNIQUE ("nombre")
 ) STRICT;

CREATE TABLE "Producto" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "categoriaId" INTEGER NOT NULL REFERENCES "Categoria"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "nombre" TEXT NOT NULL CHECK (length(trim(nombre))>0),
  "unidadControl" TEXT NOT NULL CHECK (unidadControl IN ('unidad', 'peso')),
  "minimoEscalado" INTEGER NOT NULL CHECK (minimoEscalado>=0),
  "unidadBase" TEXT NOT NULL CHECK (length(trim(unidadBase))>0),
  "escalaCantidad" INTEGER NOT NULL CHECK (escalaCantidad>=0) CHECK (escalaCantidad BETWEEN 0 AND 6),
  "activo" INTEGER NOT NULL CHECK (activo IN (0,1)),
  "controlVencimiento" INTEGER NOT NULL CHECK (controlVencimiento IN (0,1)),
  "version" INTEGER NOT NULL CHECK (version>0),
  CHECK (escalaCantidad IN (0,3)),
  CHECK (unidadControl<>'unidad' OR escalaCantidad=0)
 ) STRICT;

CREATE TABLE "Presentacion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "productoId" INTEGER NOT NULL REFERENCES "Producto"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "nombre" TEXT NOT NULL CHECK (length(trim(nombre))>0),
  "factorEscalado" INTEGER NOT NULL CHECK (factorEscalado>0),
  "escalaFactor" INTEGER NOT NULL CHECK (escalaFactor>=0) CHECK (escalaFactor BETWEEN 0 AND 6),
  "activo" INTEGER NOT NULL CHECK (activo IN (0,1)),
  "version" INTEGER NOT NULL CHECK (version>0),
  UNIQUE ("productoId", "nombre"),
  CHECK (escalaFactor BETWEEN 0 AND 6)
 ) STRICT;

CREATE TABLE "CodigoProducto" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "presentacionId" INTEGER NOT NULL REFERENCES "Presentacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "codigo" TEXT NOT NULL CHECK (length(trim(codigo))>0),
  "activo" INTEGER NOT NULL CHECK (activo IN (0,1)),
  UNIQUE ("codigo")
 ) STRICT;

CREATE TABLE "PrecioProducto" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "presentacionId" INTEGER NOT NULL REFERENCES "Presentacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "precioCentavos" INTEGER NOT NULL CHECK (precioCentavos>=0),
  "vigenteDesde" TEXT NOT NULL,
  "vigenteHasta" TEXT,
  "creadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "creadoEn" TEXT NOT NULL,
  CHECK (vigenteHasta IS NULL OR vigenteHasta>vigenteDesde)
 ) STRICT;

CREATE TABLE "Promocion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "nombre" TEXT NOT NULL CHECK (length(trim(nombre))>0),
  "tipo" TEXT NOT NULL CHECK (tipo IN ('porcentaje', 'importe_fijo')),
  "valorEscalado" INTEGER NOT NULL CHECK (valorEscalado>=0),
  "escalaValor" INTEGER NOT NULL CHECK (escalaValor>=0) CHECK (escalaValor BETWEEN 0 AND 6),
  "inicio" TEXT NOT NULL,
  "fin" TEXT NOT NULL,
  "activa" INTEGER NOT NULL CHECK (activa IN (0,1)),
  "creadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "creadoEn" TEXT NOT NULL,
  "version" INTEGER NOT NULL CHECK (version>0),
  CHECK (fin>inicio),
  CHECK (escalaValor BETWEEN 0 AND 6)
 ) STRICT;

CREATE TABLE "PromocionProducto" (
  "promocionId" INTEGER NOT NULL REFERENCES "Promocion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "presentacionId" INTEGER NOT NULL REFERENCES "Presentacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "reglaAplicacion" TEXT NOT NULL CHECK (reglaAplicacion IN ('exclusiva')),
  "prioridad" INTEGER NOT NULL CHECK (prioridad>=0),
  PRIMARY KEY ("promocionId", "presentacionId"),
  UNIQUE ("promocionId", "presentacionId")
 ) STRICT;

CREATE TABLE "Impuesto" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "nombre" TEXT NOT NULL CHECK (length(trim(nombre))>0),
  "tasaEscalada" INTEGER NOT NULL CHECK (tasaEscalada>=0),
  "escalaTasa" INTEGER NOT NULL CHECK (escalaTasa>=0) CHECK (escalaTasa BETWEEN 0 AND 6),
  "vigenteDesde" TEXT NOT NULL,
  "vigenteHasta" TEXT,
  "esExencion" INTEGER NOT NULL CHECK (esExencion IN (0,1)),
  "motivoExencion" TEXT CHECK (length(trim(motivoExencion))>0),
  "activo" INTEGER NOT NULL CHECK (activo IN (0,1)),
  "creadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "creadoEn" TEXT NOT NULL,
  CHECK (escalaTasa BETWEEN 0 AND 6),
  CHECK (vigenteHasta IS NULL OR vigenteHasta>vigenteDesde),
  CHECK (esExencion=0 OR motivoExencion IS NOT NULL)
 ) STRICT;

CREATE TABLE "ProductoImpuesto" (
  "productoId" INTEGER NOT NULL REFERENCES "Producto"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "impuestoId" INTEGER NOT NULL REFERENCES "Impuesto"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "vigenteDesde" TEXT NOT NULL,
  "vigenteHasta" TEXT,
  PRIMARY KEY ("productoId", "impuestoId", "vigenteDesde"),
  UNIQUE ("productoId", "impuestoId", "vigenteDesde"),
  CHECK (vigenteHasta IS NULL OR vigenteHasta>vigenteDesde)
 ) STRICT;

CREATE TABLE "ConfiguracionNegocio" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "nombre" TEXT NOT NULL CHECK (length(trim(nombre))>0),
  "direccion" TEXT NOT NULL CHECK (length(trim(direccion))>0),
  "telefono" TEXT,
  "moneda" TEXT NOT NULL CHECK (length(trim(moneda))>0),
  "emisor" TEXT CHECK (length(trim(emisor))>0),
  "identificacionEmisor" TEXT CHECK (length(trim(identificacionEmisor))>0),
  "regimenFiscal" TEXT CHECK (length(trim(regimenFiscal))>0),
  "fiscalidadValidada" INTEGER NOT NULL CHECK (fiscalidadValidada IN (0,1)),
  "fondoHabitualCentavos" INTEGER NOT NULL CHECK (fondoHabitualCentavos>=0),
  "impresoraNombre" TEXT CHECK (length(trim(impresoraNombre))>0),
  "impresionHabilitada" INTEGER NOT NULL CHECK (impresionHabilitada IN (0,1)),
  "efectivoHabilitado" INTEGER NOT NULL CHECK (efectivoHabilitado IN (0,1)),
  "transferenciaHabilitada" INTEGER NOT NULL CHECK (transferenciaHabilitada IN (0,1)),
  "tarjetaHabilitada" INTEGER NOT NULL CHECK (tarjetaHabilitada IN (0,1)),
  "procedimientoTransferencia" TEXT CHECK (length(trim(procedimientoTransferencia))>0),
  "smtpHost" TEXT CHECK (length(trim(smtpHost))>0),
  "smtpPuerto" INTEGER CHECK (smtpPuerto>0),
  "smtpSeguridad" TEXT CHECK (smtpSeguridad IN ('starttls', 'tls')),
  "correoRemitente" TEXT CHECK (length(trim(correoRemitente))>0),
  "smtpUsuario" TEXT CHECK (length(trim(smtpUsuario))>0),
  "smtpSecretoReferencia" TEXT CHECK (length(trim(smtpSecretoReferencia))>0),
  "version" INTEGER NOT NULL CHECK (version>0),
  "vigenteDesde" TEXT NOT NULL,
  "vigenteHasta" TEXT,
  "cambiadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "diasAlertaVencimiento" INTEGER CHECK (diasAlertaVencimiento>=0),
  "pautaRespaldo" TEXT CHECK (json_valid(pautaRespaldo)) CHECK (json_type(pautaRespaldo)='object'),
  "pautaAutorizadaPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "pautaAutorizadaEn" TEXT,
  UNIQUE ("version"),
  CHECK (length(moneda)=3),
  CHECK (vigenteHasta IS NULL OR vigenteHasta>vigenteDesde),
  CHECK (efectivoHabilitado=1),
  CHECK (tarjetaHabilitada=0),
  CHECK (transferenciaHabilitada=0 OR procedimientoTransferencia IS NOT NULL),
  CHECK (smtpPuerto IS NULL OR smtpPuerto BETWEEN 1 AND 65535),
  CHECK (impresionHabilitada=0 OR impresoraNombre IS NOT NULL),
  CHECK ((pautaRespaldo IS NULL AND pautaAutorizadaPor IS NULL AND pautaAutorizadaEn IS NULL) OR (pautaRespaldo IS NOT NULL AND pautaAutorizadaPor IS NOT NULL AND pautaAutorizadaEn IS NOT NULL))
 ) STRICT;

CREATE TABLE "Proveedor" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "identificacion" TEXT CHECK (length(trim(identificacion))>0),
  "nombre" TEXT NOT NULL CHECK (length(trim(nombre))>0),
  "telefono" TEXT,
  "direccion" TEXT,
  "contacto" TEXT,
  "activo" INTEGER NOT NULL CHECK (activo IN (0,1)),
  "version" INTEGER NOT NULL CHECK (version>0)
 ) STRICT;

CREATE TABLE "Recepcion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "proveedorId" INTEGER REFERENCES "Proveedor"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "operacionId" INTEGER REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "referencia" TEXT NOT NULL CHECK (length(trim(referencia))>0),
  "documento" TEXT CHECK (length(trim(documento))>0),
  "fechaDocumento" TEXT,
  "creadoEn" TEXT NOT NULL,
  "creadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "modificadoEn" TEXT NOT NULL,
  "estado" TEXT NOT NULL CHECK (estado IN ('Pendiente', 'Rechazada', 'Confirmada')),
  "observaciones" TEXT,
  "moneda" TEXT NOT NULL CHECK (length(trim(moneda))>0),
  "version" INTEGER NOT NULL CHECK (version>0),
  "claveSolicitud" TEXT NOT NULL CHECK (length(trim(claveSolicitud))>0),
  UNIQUE ("referencia"),
  UNIQUE ("claveSolicitud"),
  UNIQUE ("operacionId"),
  CHECK (estado<>'Confirmada' OR (proveedorId IS NOT NULL AND operacionId IS NOT NULL)),
  CHECK (estado='Confirmada' OR operacionId IS NULL)
 ) STRICT;

CREATE TABLE "RecepcionDetalle" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "recepcionId" INTEGER NOT NULL REFERENCES "Recepcion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "numeroLinea" INTEGER NOT NULL CHECK (numeroLinea>0),
  "presentacionId" INTEGER NOT NULL REFERENCES "Presentacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "cantidadRecibidaEscalada" INTEGER CHECK (cantidadRecibidaEscalada>=0),
  "cantidadAceptadaEscalada" INTEGER CHECK (cantidadAceptadaEscalada>=0),
  "escalaCantidad" INTEGER NOT NULL CHECK (escalaCantidad>=0) CHECK (escalaCantidad BETWEEN 0 AND 6),
  "factorHistoricoEscalado" INTEGER NOT NULL CHECK (factorHistoricoEscalado>0),
  "escalaFactor" INTEGER NOT NULL CHECK (escalaFactor>=0) CHECK (escalaFactor BETWEEN 0 AND 6),
  "cantidadBaseEscalada" INTEGER CHECK (cantidadBaseEscalada>=0),
  "costoUnitEscalado" INTEGER CHECK (costoUnitEscalado>=0),
  "escalaCosto" INTEGER CHECK (escalaCosto>=0) CHECK (escalaCosto BETWEEN 0 AND 6),
  "estadoCosto" TEXT NOT NULL CHECK (estadoCosto IN ('conocido', 'desconocido')),
  "vencimiento" TEXT,
  "loteReferencia" TEXT CHECK (length(trim(loteReferencia))>0),
  "observacion" TEXT,
  UNIQUE ("recepcionId", "numeroLinea"),
  CHECK (cantidadAceptadaEscalada IS NULL OR cantidadRecibidaEscalada IS NULL OR cantidadAceptadaEscalada<=cantidadRecibidaEscalada),
  CHECK ((estadoCosto='conocido' AND costoUnitEscalado IS NOT NULL AND escalaCosto IS NOT NULL) OR (estadoCosto='desconocido' AND costoUnitEscalado IS NULL AND escalaCosto IS NULL))
 ) STRICT;

CREATE TABLE "LoteExistencia" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "productoId" INTEGER NOT NULL REFERENCES "Producto"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "recepcionDetalleId" INTEGER REFERENCES "RecepcionDetalle"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "vencimiento" TEXT,
  "referencia" TEXT CHECK (length(trim(referencia))>0),
  "creadoEn" TEXT NOT NULL,
  UNIQUE ("recepcionDetalleId")
 ) STRICT;

CREATE TABLE "OperacionInventario" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "tipo" TEXT NOT NULL CHECK (tipo IN ('entrada_recepcion', 'carga', 'ajuste', 'salida_justificada', 'venta', 'reversion')),
  "fecha" TEXT NOT NULL,
  "confirmadoEn" TEXT NOT NULL,
  "secuenciaConfirmacion" INTEGER NOT NULL CHECK (secuenciaConfirmacion>0),
  "creadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "ejecutadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "autorizacionId" INTEGER REFERENCES "Autorizacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "motivo" TEXT,
  "claveOperacion" TEXT NOT NULL CHECK (length(trim(claveOperacion))>0),
  "reversionDeId" INTEGER REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  UNIQUE ("claveOperacion"),
  UNIQUE ("reversionDeId"),
  UNIQUE ("autorizacionId"),
  UNIQUE ("secuenciaConfirmacion"),
  CHECK (reversionDeId IS NULL OR reversionDeId<>id),
  CHECK ((tipo='reversion')=(reversionDeId IS NOT NULL))
 ) STRICT;

CREATE TABLE "MovimientoInventario" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "operacionId" INTEGER NOT NULL REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "productoId" INTEGER NOT NULL REFERENCES "Producto"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "loteId" INTEGER REFERENCES "LoteExistencia"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "secuencia" INTEGER NOT NULL CHECK (secuencia>0),
  "ventaDetalleId" INTEGER REFERENCES "VentaDetalle"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "recepcionDetalleId" INTEGER REFERENCES "RecepcionDetalle"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "solicitudInventarioDetalleId" INTEGER REFERENCES "SolicitudInventarioDetalle"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "movimientoRevertidoId" INTEGER REFERENCES "MovimientoInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "cantidadEscalada" INTEGER NOT NULL,
  "escala" INTEGER NOT NULL CHECK (escala>=0) CHECK (escala BETWEEN 0 AND 6),
  "costoUnitEscalado" INTEGER CHECK (costoUnitEscalado>=0),
  "escalaCosto" INTEGER CHECK (escalaCosto>=0) CHECK (escalaCosto BETWEEN 0 AND 6),
  "estadoCosto" TEXT NOT NULL CHECK (estadoCosto IN ('conocido', 'desconocido')),
  "costoReferenciaId" INTEGER REFERENCES "MovimientoInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  UNIQUE ("operacionId", "secuencia"),
  UNIQUE ("movimientoRevertidoId"),
  CHECK (cantidadEscalada<>0),
  CHECK ((ventaDetalleId IS NOT NULL)+(recepcionDetalleId IS NOT NULL)+(solicitudInventarioDetalleId IS NOT NULL)+(movimientoRevertidoId IS NOT NULL)=1),
  CHECK ((estadoCosto='conocido' AND costoUnitEscalado IS NOT NULL AND escalaCosto IS NOT NULL) OR (estadoCosto='desconocido' AND costoUnitEscalado IS NULL AND escalaCosto IS NULL)),
  CHECK (costoReferenciaId IS NULL OR costoReferenciaId<>id)
 ) STRICT;

CREATE TABLE "Conteo" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "referencia" TEXT NOT NULL CHECK (length(trim(referencia))>0),
  "creadoEn" TEXT NOT NULL,
  "fechaConteo" TEXT NOT NULL,
  "registradoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "finalizadoEn" TEXT,
  "estado" TEXT NOT NULL CHECK (estado IN ('En_registro', 'Finalizado')),
  "operacionReferenciaId" INTEGER REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "observaciones" TEXT,
  "version" INTEGER NOT NULL CHECK (version>0),
  UNIQUE ("referencia"),
  CHECK ((estado='Finalizado')=(finalizadoEn IS NOT NULL))
 ) STRICT;

CREATE TABLE "ConteoDetalle" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "conteoId" INTEGER NOT NULL REFERENCES "Conteo"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "productoId" INTEGER NOT NULL REFERENCES "Producto"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "loteId" INTEGER REFERENCES "LoteExistencia"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "cantidadFisicaEscalada" INTEGER NOT NULL CHECK (cantidadFisicaEscalada>=0),
  "escala" INTEGER NOT NULL CHECK (escala>=0) CHECK (escala BETWEEN 0 AND 6),
  "saldoReferenciaEscalado" INTEGER NOT NULL,
  "observacion" TEXT
 ) STRICT;

CREATE TABLE "SolicitudInventario" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "referencia" TEXT NOT NULL CHECK (length(trim(referencia))>0),
  "tipo" TEXT NOT NULL CHECK (tipo IN ('carga', 'ajuste', 'salida_justificada')),
  "estado" TEXT NOT NULL CHECK (estado IN ('En_preparacion', 'Pendiente', 'Autorizada', 'Rechazada', 'Cancelada')),
  "motivo" TEXT CHECK (length(trim(motivo))>0),
  "conteoId" INTEGER REFERENCES "Conteo"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "solicitadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "creadoEn" TEXT NOT NULL,
  "enviadoEn" TEXT,
  "resueltoEn" TEXT,
  "operacionId" INTEGER REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "version" INTEGER NOT NULL CHECK (version>0),
  "claveSolicitud" TEXT NOT NULL CHECK (length(trim(claveSolicitud))>0),
  "evidenciaReferencia" TEXT,
  UNIQUE ("referencia"),
  UNIQUE ("claveSolicitud"),
  UNIQUE ("operacionId"),
  CHECK (estado<>'Autorizada' OR operacionId IS NOT NULL),
  CHECK (estado='Autorizada' OR operacionId IS NULL),
  CHECK (estado IN ('En_preparacion','Cancelada') OR (enviadoEn IS NOT NULL AND motivo IS NOT NULL))
 ) STRICT;

CREATE TABLE "SolicitudInventarioDetalle" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "solicitudId" INTEGER NOT NULL REFERENCES "SolicitudInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "numeroLinea" INTEGER NOT NULL CHECK (numeroLinea>0),
  "productoId" INTEGER NOT NULL REFERENCES "Producto"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "loteId" INTEGER REFERENCES "LoteExistencia"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "cantidadEscalada" INTEGER NOT NULL,
  "escala" INTEGER NOT NULL CHECK (escala>=0) CHECK (escala BETWEEN 0 AND 6),
  "causa" TEXT CHECK (causa IN ('devolucion_proveedor', 'robo', 'dano', 'vencimiento', 'perdida', 'consumo_interno')),
  "proveedorDevolucionId" INTEGER REFERENCES "Proveedor"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "evidenciaReferencia" TEXT CHECK (length(trim(evidenciaReferencia))>0),
  "costoUnitEscalado" INTEGER CHECK (costoUnitEscalado>=0),
  "escalaCosto" INTEGER CHECK (escalaCosto>=0) CHECK (escalaCosto BETWEEN 0 AND 6),
  "estadoCosto" TEXT NOT NULL CHECK (estadoCosto IN ('conocido', 'desconocido')),
  "costoReferenciaId" INTEGER REFERENCES "MovimientoInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  UNIQUE ("solicitudId", "numeroLinea"),
  CHECK (cantidadEscalada<>0),
  CHECK ((estadoCosto='conocido' AND costoUnitEscalado IS NOT NULL AND escalaCosto IS NOT NULL) OR (estadoCosto='desconocido' AND costoUnitEscalado IS NULL AND escalaCosto IS NULL)),
  CHECK (causa<>'devolucion_proveedor' OR proveedorDevolucionId IS NOT NULL)
 ) STRICT;

CREATE TABLE "Venta" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "folio" TEXT NOT NULL CHECK (length(trim(folio))>0),
  "fecha" TEXT NOT NULL,
  "turnoId" INTEGER NOT NULL REFERENCES "TurnoCaja"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "operacionId" INTEGER NOT NULL REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "configuracionId" INTEGER NOT NULL REFERENCES "ConfiguracionNegocio"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "estado" TEXT NOT NULL CHECK (estado IN ('Confirmada', 'Anulada')),
  "subtotalCentavos" INTEGER NOT NULL CHECK (subtotalCentavos>=0),
  "impuestoCentavos" INTEGER NOT NULL CHECK (impuestoCentavos>=0),
  "descuentoCentavos" INTEGER NOT NULL CHECK (descuentoCentavos>=0),
  "totalCentavos" INTEGER NOT NULL CHECK (totalCentavos>=0),
  "moneda" TEXT NOT NULL CHECK (length(trim(moneda))>0),
  "claveOperacion" TEXT NOT NULL CHECK (length(trim(claveOperacion))>0),
  "comprobanteSnapshot" TEXT NOT NULL CHECK (json_valid(comprobanteSnapshot)) CHECK (json_type(comprobanteSnapshot)='object'),
  "comprobanteHash" TEXT NOT NULL CHECK (length(trim(comprobanteHash))>0) CHECK (length(comprobanteHash)=64 AND comprobanteHash NOT GLOB '*[^0-9a-f]*'),
  "version" INTEGER NOT NULL CHECK (version>0),
  UNIQUE ("folio"),
  UNIQUE ("claveOperacion"),
  UNIQUE ("operacionId"),
  CHECK (totalCentavos=subtotalCentavos+impuestoCentavos-descuentoCentavos),
  CHECK (totalCentavos>0)
 ) STRICT;

CREATE TABLE "VentaDetalle" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "ventaId" INTEGER NOT NULL REFERENCES "Venta"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "numeroLinea" INTEGER NOT NULL CHECK (numeroLinea>0),
  "presentacionId" INTEGER NOT NULL REFERENCES "Presentacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "productoId" INTEGER NOT NULL REFERENCES "Producto"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "descripcionHistorica" TEXT NOT NULL CHECK (length(trim(descripcionHistorica))>0),
  "presentacionHistorica" TEXT NOT NULL CHECK (length(trim(presentacionHistorica))>0),
  "unidadBaseHistorica" TEXT NOT NULL CHECK (length(trim(unidadBaseHistorica))>0),
  "cantidadEscalada" INTEGER NOT NULL CHECK (cantidadEscalada>0),
  "escalaCantidad" INTEGER NOT NULL CHECK (escalaCantidad>=0) CHECK (escalaCantidad BETWEEN 0 AND 6),
  "factorHistoricoEscalado" INTEGER NOT NULL CHECK (factorHistoricoEscalado>0),
  "escalaFactor" INTEGER NOT NULL CHECK (escalaFactor>=0) CHECK (escalaFactor BETWEEN 0 AND 6),
  "cantidadBaseEscalada" INTEGER NOT NULL CHECK (cantidadBaseEscalada>0),
  "modalidadVenta" TEXT NOT NULL CHECK (modalidadVenta IN ('unidad', 'peso_manual', 'importe')),
  "valorSolicitadoEscalado" INTEGER NOT NULL CHECK (valorSolicitadoEscalado>0),
  "escalaValorSolicitado" INTEGER NOT NULL CHECK (escalaValorSolicitado>=0) CHECK (escalaValorSolicitado BETWEEN 0 AND 6),
  "precioUnitEscalado" INTEGER NOT NULL CHECK (precioUnitEscalado>=0),
  "escalaPrecio" INTEGER NOT NULL CHECK (escalaPrecio>=0) CHECK (escalaPrecio BETWEEN 0 AND 6),
  "costoUnitEscalado" INTEGER CHECK (costoUnitEscalado>=0),
  "escalaCosto" INTEGER CHECK (escalaCosto>=0) CHECK (escalaCosto BETWEEN 0 AND 6),
  "estadoCosto" TEXT NOT NULL CHECK (estadoCosto IN ('conocido', 'desconocido')),
  "costoReferenciaId" INTEGER REFERENCES "MovimientoInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "subtotalCentavos" INTEGER NOT NULL CHECK (subtotalCentavos>=0),
  "impuestoCentavos" INTEGER NOT NULL CHECK (impuestoCentavos>=0),
  "descuentoCentavos" INTEGER NOT NULL CHECK (descuentoCentavos>=0),
  "totalCentavos" INTEGER NOT NULL CHECK (totalCentavos>=0),
  "impuestosSnapshot" TEXT NOT NULL CHECK (json_valid(impuestosSnapshot)) CHECK (json_type(impuestosSnapshot)='array'),
  "promocionSnapshot" TEXT CHECK (json_valid(promocionSnapshot)) CHECK (json_type(promocionSnapshot)='object'),
  UNIQUE ("ventaId", "numeroLinea"),
  CHECK (totalCentavos=subtotalCentavos+impuestoCentavos-descuentoCentavos),
  CHECK ((estadoCosto='conocido' AND costoUnitEscalado IS NOT NULL AND escalaCosto IS NOT NULL) OR (estadoCosto='desconocido' AND costoUnitEscalado IS NULL AND escalaCosto IS NULL))
 ) STRICT;

CREATE TABLE "Pago" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "ventaId" INTEGER NOT NULL REFERENCES "Venta"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "secuencia" INTEGER NOT NULL CHECK (secuencia>0),
  "medio" TEXT NOT NULL CHECK (medio IN ('efectivo', 'transferencia', 'tarjeta')),
  "importeCentavos" INTEGER NOT NULL CHECK (importeCentavos>0),
  "recibidoCentavos" INTEGER CHECK (recibidoCentavos>=0),
  "cambioCentavos" INTEGER CHECK (cambioCentavos>=0),
  "evidenciaReferencia" TEXT CHECK (length(trim(evidenciaReferencia))>0),
  "referenciaExterna" TEXT CHECK (length(trim(referenciaExterna))>0),
  "estadoVerificacion" TEXT NOT NULL CHECK (estadoVerificacion IN ('verificado')),
  "verificadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "verificadoEn" TEXT NOT NULL,
  UNIQUE ("ventaId", "secuencia"),
  CHECK ((medio='efectivo' AND recibidoCentavos IS NOT NULL AND cambioCentavos IS NOT NULL AND recibidoCentavos>=importeCentavos AND cambioCentavos=recibidoCentavos-importeCentavos) OR (medio<>'efectivo' AND recibidoCentavos IS NULL AND cambioCentavos IS NULL AND evidenciaReferencia IS NOT NULL AND referenciaExterna IS NOT NULL)),
  CHECK (medio<>'tarjeta')
 ) STRICT;

CREATE TABLE "TurnoCaja" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "operadorId" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "estacionId" TEXT NOT NULL CHECK (length(trim(estacionId))>0),
  "estado" TEXT NOT NULL CHECK (estado IN ('Abierto', 'Cerrado')),
  "apertura" TEXT NOT NULL,
  "cierre" TEXT,
  "configuracionId" INTEGER NOT NULL REFERENCES "ConfiguracionNegocio"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "fondoHabitualCentavos" INTEGER NOT NULL CHECK (fondoHabitualCentavos>=0),
  "fondoRealCentavos" INTEGER NOT NULL CHECK (fondoRealCentavos>=0),
  "motivoDiferenciaFondo" TEXT CHECK (length(trim(motivoDiferenciaFondo))>0),
  "efectivoEsperadoCierreCentavos" INTEGER,
  "efectivoContadoCentavos" INTEGER CHECK (efectivoContadoCentavos>=0),
  "diferenciaCentavos" INTEGER,
  "motivoDiferenciaCierre" TEXT CHECK (length(trim(motivoDiferenciaCierre))>0),
  "version" INTEGER NOT NULL CHECK (version>0),
  CHECK (cierre IS NULL OR cierre>=apertura),
  CHECK (fondoHabitualCentavos=fondoRealCentavos OR motivoDiferenciaFondo IS NOT NULL),
  CHECK (estado<>'Cerrado' OR (cierre IS NOT NULL AND efectivoEsperadoCierreCentavos IS NOT NULL AND efectivoContadoCentavos IS NOT NULL AND diferenciaCentavos IS NOT NULL AND diferenciaCentavos=efectivoContadoCentavos-efectivoEsperadoCierreCentavos)),
  CHECK (diferenciaCentavos IS NULL OR diferenciaCentavos=0 OR motivoDiferenciaCierre IS NOT NULL),
  CHECK (estado='Cerrado' OR (cierre IS NULL AND efectivoEsperadoCierreCentavos IS NULL AND efectivoContadoCentavos IS NULL AND diferenciaCentavos IS NULL AND motivoDiferenciaCierre IS NULL))
 ) STRICT;

CREATE TABLE "MovimientoCaja" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "turnoId" INTEGER NOT NULL REFERENCES "TurnoCaja"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "ventaId" INTEGER NOT NULL REFERENCES "Venta"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "operacionId" INTEGER NOT NULL REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "solicitudAnulacionId" INTEGER REFERENCES "SolicitudAnulacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "tipo" TEXT NOT NULL CHECK (tipo IN ('cobro_efectivo', 'devolucion_efectivo')),
  "importeCentavos" INTEGER NOT NULL,
  "registradoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "fecha" TEXT NOT NULL,
  "evidenciaReferencia" TEXT CHECK (length(trim(evidenciaReferencia))>0),
  UNIQUE ("operacionId", "tipo"),
  CHECK ((tipo='cobro_efectivo' AND importeCentavos>0 AND solicitudAnulacionId IS NULL) OR (tipo='devolucion_efectivo' AND importeCentavos<0 AND solicitudAnulacionId IS NOT NULL AND evidenciaReferencia IS NOT NULL))
 ) STRICT;

CREATE TABLE "GastoOperativo" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "fecha" TEXT NOT NULL,
  "creadoEn" TEXT NOT NULL,
  "concepto" TEXT NOT NULL CHECK (length(trim(concepto))>0),
  "importeCentavos" INTEGER NOT NULL CHECK (importeCentavos>0),
  "moneda" TEXT NOT NULL CHECK (length(trim(moneda))>0),
  "registradoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "comprobanteReferencia" TEXT CHECK (length(trim(comprobanteReferencia))>0),
  "estado" TEXT NOT NULL CHECK (estado IN ('Registrado', 'Corregido', 'Anulado')),
  "corrigeAId" INTEGER REFERENCES "GastoOperativo"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "motivoCorreccion" TEXT CHECK (length(trim(motivoCorreccion))>0),
  "anuladoPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "anuladoEn" TEXT,
  "motivoAnulacion" TEXT CHECK (length(trim(motivoAnulacion))>0),
  "claveOperacion" TEXT NOT NULL CHECK (length(trim(claveOperacion))>0),
  UNIQUE ("claveOperacion"),
  UNIQUE ("corrigeAId"),
  CHECK (corrigeAId IS NULL OR (corrigeAId<>id AND motivoCorreccion IS NOT NULL)),
  CHECK (estado<>'Anulado' OR (anuladoPor IS NOT NULL AND anuladoEn IS NOT NULL AND motivoAnulacion IS NOT NULL))
 ) STRICT;

CREATE TABLE "SolicitudAnulacion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "ventaId" INTEGER NOT NULL REFERENCES "Venta"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "referencia" TEXT NOT NULL CHECK (length(trim(referencia))>0),
  "solicitadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "creadoEn" TEXT NOT NULL,
  "enviadoEn" TEXT,
  "estado" TEXT NOT NULL CHECK (estado IN ('En_preparacion', 'Pendiente', 'Autorizada', 'Rechazada', 'Cancelada')),
  "motivo" TEXT CHECK (length(trim(motivo))>0),
  "evidenciaReferencia" TEXT CHECK (length(trim(evidenciaReferencia))>0),
  "version" INTEGER NOT NULL CHECK (version>0),
  "claveSolicitud" TEXT NOT NULL CHECK (length(trim(claveSolicitud))>0),
  "operacionId" INTEGER REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "reincorporacionEstado" TEXT NOT NULL CHECK (reincorporacionEstado IN ('No_verificada', 'No_reincorporar', 'Verificada')),
  "reincorporacionDetalle" TEXT NOT NULL CHECK (json_valid(reincorporacionDetalle)) CHECK (json_type(reincorporacionDetalle)='array'),
  "efectivoADevolverCentavos" INTEGER NOT NULL CHECK (efectivoADevolverCentavos>=0),
  "efectivoDevueltoCentavos" INTEGER NOT NULL CHECK (efectivoDevueltoCentavos>=0),
  "fondosExternosARegularizarCentavos" INTEGER NOT NULL CHECK (fondosExternosARegularizarCentavos>=0),
  "fondosExternosRegularizadosCentavos" INTEGER NOT NULL CHECK (fondosExternosRegularizadosCentavos>=0),
  "regularizacionEstado" TEXT NOT NULL CHECK (regularizacionEstado IN ('No_requerida', 'Pendiente', 'Completada')),
  "regularizacionDetalle" TEXT NOT NULL CHECK (json_valid(regularizacionDetalle)) CHECK (json_type(regularizacionDetalle)='array'),
  "regularizadoPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "regularizadoEn" TEXT,
  UNIQUE ("referencia"),
  UNIQUE ("claveSolicitud"),
  UNIQUE ("operacionId"),
  CHECK (efectivoDevueltoCentavos<=efectivoADevolverCentavos),
  CHECK (fondosExternosRegularizadosCentavos<=fondosExternosARegularizarCentavos),
  CHECK (estado<>'Autorizada' OR operacionId IS NOT NULL),
  CHECK (estado='Autorizada' OR operacionId IS NULL),
  CHECK (regularizacionEstado<>'Completada' OR (efectivoDevueltoCentavos=efectivoADevolverCentavos AND fondosExternosRegularizadosCentavos=fondosExternosARegularizarCentavos AND regularizadoPor IS NOT NULL AND regularizadoEn IS NOT NULL)),
  CHECK (regularizacionEstado<>'No_requerida' OR (efectivoADevolverCentavos=0 AND fondosExternosARegularizarCentavos=0 AND efectivoDevueltoCentavos=0 AND fondosExternosRegularizadosCentavos=0)),
  CHECK (efectivoDevueltoCentavos=0 OR efectivoDevueltoCentavos=efectivoADevolverCentavos)
 ) STRICT;

CREATE TABLE "Usuario" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "nombreCuenta" TEXT NOT NULL CHECK (length(trim(nombreCuenta))>0),
  "nombreNormalizado" TEXT NOT NULL CHECK (length(trim(nombreNormalizado))>0),
  "nombreVisible" TEXT NOT NULL CHECK (length(trim(nombreVisible))>0),
  "passwordHash" TEXT NOT NULL CHECK (length(trim(passwordHash))>0),
  "activo" INTEGER NOT NULL CHECK (activo IN (0,1)),
  "tipoCuenta" TEXT NOT NULL CHECK (tipoCuenta IN ('comercial', 'soporte_temporal')),
  "rolId" INTEGER REFERENCES "Rol"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "permisoOperativoCaja" INTEGER NOT NULL CHECK (permisoOperativoCaja IN (0,1)),
  "delegacionCuentas" INTEGER NOT NULL CHECK (delegacionCuentas IN (0,1)),
  "creadoEn" TEXT NOT NULL,
  "credencialesVersion" INTEGER NOT NULL CHECK (credencialesVersion>0),
  "finAcceso" TEXT,
  UNIQUE ("nombreNormalizado"),
  CHECK ((tipoCuenta='comercial' AND rolId IS NOT NULL) OR (tipoCuenta='soporte_temporal' AND rolId IS NULL AND finAcceso IS NOT NULL AND permisoOperativoCaja=0 AND delegacionCuentas=0))
 ) STRICT;

CREATE TABLE "Rol" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "nombre" TEXT NOT NULL CHECK (nombre IN ('Propietario', 'Administrador', 'Cajero', 'Inventario')),
  UNIQUE ("nombre")
 ) STRICT;

CREATE TABLE "Autorizacion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "recepcionId" INTEGER REFERENCES "Recepcion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "solicitudInventarioId" INTEGER REFERENCES "SolicitudInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "solicitudAnulacionId" INTEGER REFERENCES "SolicitudAnulacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "solicitanteId" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "autorizadorId" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "fechaSolicitud" TEXT NOT NULL,
  "fechaDecision" TEXT,
  "decision" TEXT NOT NULL CHECK (decision IN ('pendiente', 'autorizada', 'rechazada')),
  "modalidad" TEXT NOT NULL CHECK (modalidad IN ('segunda_cuenta', 'responsabilidad_propietario')),
  "motivo" TEXT,
  "objetoVersion" INTEGER NOT NULL CHECK (objetoVersion>0),
  "contenidoHash" TEXT NOT NULL CHECK (length(trim(contenidoHash))>0) CHECK (length(contenidoHash)=64 AND contenidoHash NOT GLOB '*[^0-9a-f]*'),
  "contenidoSnapshot" TEXT NOT NULL CHECK (json_valid(contenidoSnapshot)) CHECK (json_type(contenidoSnapshot)='object'),
  "invalidadaEn" TEXT,
  "motivoInvalidacion" TEXT CHECK (length(trim(motivoInvalidacion))>0),
  CHECK ((recepcionId IS NOT NULL)+(solicitudInventarioId IS NOT NULL)+(solicitudAnulacionId IS NOT NULL)=1),
  CHECK (decision='pendiente' OR (autorizadorId IS NOT NULL AND fechaDecision IS NOT NULL)),
  CHECK (fechaDecision IS NULL OR fechaDecision>=fechaSolicitud),
  CHECK (modalidad<>'segunda_cuenta' OR autorizadorId IS NULL OR solicitanteId<>autorizadorId),
  CHECK (invalidadaEn IS NULL OR motivoInvalidacion IS NOT NULL),
  CHECK (decision<>'pendiente' OR (autorizadorId IS NULL AND fechaDecision IS NULL)),
  CHECK (modalidad<>'responsabilidad_propietario' OR motivo IS NOT NULL)
 ) STRICT;

CREATE TABLE "AccesoSoporte" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "usuarioId" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "autorizadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "autorizadoEn" TEXT NOT NULL,
  "inicio" TEXT NOT NULL,
  "fin" TEXT NOT NULL,
  "alcance" TEXT NOT NULL CHECK (length(trim(alcance))>0),
  "motivo" TEXT NOT NULL CHECK (length(trim(motivo))>0),
  "revocadoEn" TEXT,
  "revocadoPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "motivoRevocacion" TEXT CHECK (length(trim(motivoRevocacion))>0),
  CHECK (fin>inicio),
  CHECK (revocadoEn IS NULL OR (revocadoPor IS NOT NULL AND motivoRevocacion IS NOT NULL))
 ) STRICT;

CREATE TABLE "EventoAuditoria" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "fecha" TEXT NOT NULL,
  "usuarioId" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "origen" TEXT NOT NULL CHECK (origen IN ('persona', 'tarea_automatica')),
  "accion" TEXT NOT NULL CHECK (length(trim(accion))>0),
  "entidad" TEXT NOT NULL CHECK (length(trim(entidad))>0),
  "entidadId" TEXT CHECK (length(trim(entidadId))>0),
  "claveOperacion" TEXT NOT NULL CHECK (length(trim(claveOperacion))>0),
  "resultado" TEXT NOT NULL CHECK (resultado IN ('exito', 'rechazado', 'fallo', 'pendiente', 'incierto', 'regularizado')),
  "eventoOrigenId" INTEGER REFERENCES "EventoAuditoria"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "autorizacionId" INTEGER REFERENCES "Autorizacion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "datosCambio" TEXT NOT NULL CHECK (json_valid(datosCambio)) CHECK (json_type(datosCambio)='object'),
  "versionFormato" INTEGER NOT NULL CHECK (versionFormato>0),
  CHECK (origen<>'persona' OR usuarioId IS NOT NULL),
  CHECK (eventoOrigenId IS NULL OR eventoOrigenId<>id)
 ) STRICT;

CREATE TABLE "SolicitudImpresion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "ventaId" INTEGER NOT NULL REFERENCES "Venta"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "solicitadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "tipo" TEXT NOT NULL CHECK (tipo IN ('original', 'copia')),
  "copiaNumero" INTEGER NOT NULL CHECK (copiaNumero>=0),
  "estado" TEXT NOT NULL CHECK (estado IN ('Pendiente', 'En_proceso', 'Aceptada', 'Fallida', 'Incierta', 'En_cuarentena', 'Resuelta')),
  "claveSolicitud" TEXT NOT NULL CHECK (length(trim(claveSolicitud))>0),
  "fecha" TEXT NOT NULL,
  "impresoraHistorica" TEXT NOT NULL CHECK (length(trim(impresoraHistorica))>0),
  "contenidoHash" TEXT NOT NULL CHECK (length(trim(contenidoHash))>0) CHECK (length(contenidoHash)=64 AND contenidoHash NOT GLOB '*[^0-9a-f]*'),
  "leaseToken" TEXT CHECK (length(trim(leaseToken))>0),
  "leaseHasta" TEXT,
  "proximoIntentoEn" TEXT,
  "ultimoError" TEXT,
  "aceptadaEn" TEXT,
  "restauracionId" INTEGER REFERENCES "Restauracion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "estadoAntesCuarentena" TEXT CHECK (length(trim(estadoAntesCuarentena))>0),
  "resueltoPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "resueltoEn" TEXT,
  "resolucion" TEXT CHECK (resolucion IN ('reintentar_mismo', 'no_reenviar', 'emitir_copia', 'contingencia_manual')),
  "motivoResolucion" TEXT CHECK (length(trim(motivoResolucion))>0),
  UNIQUE ("claveSolicitud"),
  UNIQUE ("ventaId", "tipo", "copiaNumero"),
  CHECK ((tipo='original' AND copiaNumero=0) OR (tipo='copia' AND copiaNumero>0)),
  CHECK ((leaseToken IS NULL)=(leaseHasta IS NULL)),
  CHECK (estado<>'En_cuarentena' OR restauracionId IS NOT NULL),
  CHECK (estado<>'Resuelta' OR (resueltoPor IS NOT NULL AND resueltoEn IS NOT NULL AND resolucion IS NOT NULL AND motivoResolucion IS NOT NULL)),
  CHECK (estado<>'En_proceso' OR (leaseToken IS NOT NULL AND leaseHasta IS NOT NULL)),
  CHECK (estado<>'Aceptada' OR aceptadaEn IS NOT NULL)
 ) STRICT;

CREATE TABLE "IntentoImpresion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "solicitudImpresionId" INTEGER NOT NULL REFERENCES "SolicitudImpresion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "numero" INTEGER NOT NULL CHECK (numero>0),
  "inicio" TEXT NOT NULL,
  "fin" TEXT,
  "resultado" TEXT NOT NULL CHECK (resultado IN ('en_curso', 'no_aceptada', 'aceptada_SO', 'incierta')),
  "idTrabajoSO" TEXT CHECK (length(trim(idTrabajoSO))>0),
  "impresora" TEXT NOT NULL CHECK (length(trim(impresora))>0),
  "agenteIdentidad" TEXT NOT NULL CHECK (length(trim(agenteIdentidad))>0),
  "leaseToken" TEXT NOT NULL CHECK (length(trim(leaseToken))>0),
  "evidencia" TEXT,
  "errorCodigo" TEXT CHECK (length(trim(errorCodigo))>0),
  UNIQUE ("solicitudImpresionId", "numero"),
  CHECK (fin IS NULL OR fin>=inicio),
  CHECK (resultado='en_curso' OR fin IS NOT NULL)
 ) STRICT;

CREATE TABLE "EntregaComprobante" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "ventaId" INTEGER NOT NULL REFERENCES "Venta"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "solicitudImpresionId" INTEGER REFERENCES "SolicitudImpresion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "tipo" TEXT NOT NULL CHECK (tipo IN ('impreso_original', 'copia', 'manual_contingencia')),
  "fecha" TEXT NOT NULL,
  "motivo" TEXT CHECK (length(trim(motivo))>0),
  "registradoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "autorizadoPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "evidenciaReferencia" TEXT CHECK (length(trim(evidenciaReferencia))>0),
  CHECK (tipo<>'manual_contingencia' OR (motivo IS NOT NULL AND autorizadoPor IS NOT NULL AND evidenciaReferencia IS NOT NULL))
 ) STRICT;

CREATE TABLE "SolicitudCorreo" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "solicitadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "fecha" TEXT NOT NULL,
  "destinatario" TEXT NOT NULL CHECK (length(trim(destinatario))>0),
  "reporteTipo" TEXT NOT NULL CHECK (length(trim(reporteTipo))>0),
  "parametrosReporte" TEXT NOT NULL CHECK (json_valid(parametrosReporte)) CHECK (json_type(parametrosReporte)='object'),
  "reporteReferencia" TEXT NOT NULL CHECK (length(trim(reporteReferencia))>0),
  "reporteHash" TEXT NOT NULL CHECK (length(trim(reporteHash))>0) CHECK (length(reporteHash)=64 AND reporteHash NOT GLOB '*[^0-9a-f]*'),
  "reporteGeneradoEn" TEXT NOT NULL,
  "estado" TEXT NOT NULL CHECK (estado IN ('Pendiente', 'En_transmision', 'Aceptada_SMTP', 'Incierta', 'Fallida', 'En_cuarentena', 'Resuelta')),
  "claveSolicitud" TEXT NOT NULL CHECK (length(trim(claveSolicitud))>0),
  "messageId" TEXT NOT NULL CHECK (length(trim(messageId))>0),
  "leaseToken" TEXT CHECK (length(trim(leaseToken))>0),
  "leaseHasta" TEXT,
  "proximoIntentoEn" TEXT,
  "ultimoError" TEXT,
  "aceptadoEn" TEXT,
  "restauracionId" INTEGER REFERENCES "Restauracion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "estadoAntesCuarentena" TEXT CHECK (length(trim(estadoAntesCuarentena))>0),
  "resueltoPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "resueltoEn" TEXT,
  "resolucion" TEXT CHECK (resolucion IN ('reintentar_mismo', 'no_reenviar', 'nueva_solicitud')),
  "motivoResolucion" TEXT CHECK (length(trim(motivoResolucion))>0),
  UNIQUE ("claveSolicitud"),
  UNIQUE ("messageId"),
  CHECK ((leaseToken IS NULL)=(leaseHasta IS NULL)),
  CHECK (estado<>'Aceptada_SMTP' OR aceptadoEn IS NOT NULL),
  CHECK (estado<>'En_cuarentena' OR restauracionId IS NOT NULL),
  CHECK (estado<>'Resuelta' OR (resueltoPor IS NOT NULL AND resueltoEn IS NOT NULL AND resolucion IS NOT NULL AND motivoResolucion IS NOT NULL)),
  CHECK (estado<>'En_transmision' OR (leaseToken IS NOT NULL AND leaseHasta IS NOT NULL))
 ) STRICT;

CREATE TABLE "IntentoCorreo" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "solicitudCorreoId" INTEGER NOT NULL REFERENCES "SolicitudCorreo"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "configuracionId" INTEGER NOT NULL REFERENCES "ConfiguracionNegocio"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "numero" INTEGER NOT NULL CHECK (numero>0),
  "inicio" TEXT NOT NULL,
  "fin" TEXT,
  "resultado" TEXT NOT NULL CHECK (resultado IN ('en_curso', 'no_aceptada', 'aceptada_SMTP', 'incierta')),
  "etapaSMTP" TEXT NOT NULL CHECK (etapaSMTP IN ('conexion', 'autenticacion', 'sobre', 'contenido', 'respuesta_final', 'registro_resultado')),
  "codigoSMTP" TEXT CHECK (length(trim(codigoSMTP))>0),
  "respuestaReferencia" TEXT,
  "messageId" TEXT NOT NULL CHECK (length(trim(messageId))>0),
  "leaseToken" TEXT NOT NULL CHECK (length(trim(leaseToken))>0),
  "evidencia" TEXT,
  "errorCodigo" TEXT CHECK (length(trim(errorCodigo))>0),
  UNIQUE ("solicitudCorreoId", "numero"),
  CHECK (fin IS NULL OR fin>=inicio),
  CHECK (resultado='en_curso' OR fin IS NOT NULL)
 ) STRICT;

CREATE TABLE "RespaldoEjecucion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "claveEjecucion" TEXT NOT NULL CHECK (length(trim(claveEjecucion))>0),
  "inicio" TEXT NOT NULL,
  "fin" TEXT,
  "solicitadoPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "ejecutadoPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "origen" TEXT NOT NULL CHECK (origen IN ('pauta_automatica', 'manual', 'preactualizacion', 'cierre')),
  "destino" TEXT NOT NULL CHECK (length(trim(destino))>0),
  "medioIdentificador" TEXT CHECK (length(trim(medioIdentificador))>0),
  "archivoReferencia" TEXT CHECK (length(trim(archivoReferencia))>0),
  "tamanoBytes" INTEGER CHECK (tamanoBytes>=0),
  "estado" TEXT NOT NULL CHECK (estado IN ('En_proceso', 'Comprobada', 'Fallida')),
  "checksum" TEXT CHECK (length(trim(checksum))>0) CHECK (length(checksum)=64 AND checksum NOT GLOB '*[^0-9a-f]*'),
  "manifiesto" TEXT CHECK (json_valid(manifiesto)) CHECK (json_type(manifiesto)='object'),
  "versionEsquema" TEXT CHECK (length(trim(versionEsquema))>0),
  "ultimoMovimientoId" INTEGER REFERENCES "MovimientoInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "ultimaOperacionId" INTEGER REFERENCES "OperacionInventario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "comprobadoEn" TEXT,
  "ultimoError" TEXT,
  "configuracionId" INTEGER NOT NULL REFERENCES "ConfiguracionNegocio"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  UNIQUE ("claveEjecucion"),
  CHECK (fin IS NULL OR fin>=inicio),
  CHECK (estado<>'Comprobada' OR (fin IS NOT NULL AND archivoReferencia IS NOT NULL AND tamanoBytes IS NOT NULL AND checksum IS NOT NULL AND manifiesto IS NOT NULL AND versionEsquema IS NOT NULL AND comprobadoEn IS NOT NULL)),
  CHECK (estado<>'Fallida' OR (fin IS NOT NULL AND ultimoError IS NOT NULL AND length(trim(ultimoError))>0)),
  CHECK (estado<>'En_proceso' OR fin IS NULL)
 ) STRICT;

CREATE TABLE "Restauracion" (
  "id" INTEGER PRIMARY KEY NOT NULL,
  "respaldoId" INTEGER REFERENCES "RespaldoEjecucion"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "respaldoReferencia" TEXT NOT NULL CHECK (length(trim(respaldoReferencia))>0),
  "respaldoChecksum" TEXT NOT NULL CHECK (length(trim(respaldoChecksum))>0) CHECK (length(respaldoChecksum)=64 AND respaldoChecksum NOT GLOB '*[^0-9a-f]*'),
  "versionEsquemaOrigen" TEXT NOT NULL CHECK (length(trim(versionEsquemaOrigen))>0),
  "autorizadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "autorizadoEn" TEXT NOT NULL,
  "ejecutadoPor" INTEGER NOT NULL REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "inicio" TEXT NOT NULL,
  "fin" TEXT,
  "estado" TEXT NOT NULL CHECK (estado IN ('En_proceso', 'Validada', 'Fallida')),
  "resultadoDetalle" TEXT,
  "motivo" TEXT NOT NULL CHECK (length(trim(motivo))>0),
  "preservacionActualReferencia" TEXT CHECK (length(trim(preservacionActualReferencia))>0),
  "puntoRecuperado" TEXT NOT NULL,
  "conciliacionEstado" TEXT NOT NULL CHECK (conciliacionEstado IN ('Pendiente', 'Completada', 'No_requerida')),
  "conciliacionDetalle" TEXT CHECK (json_valid(conciliacionDetalle)) CHECK (json_type(conciliacionDetalle)='object'),
  "nucleoReabiertoEn" TEXT,
  "reaperturaAutorizadaPor" INTEGER REFERENCES "Usuario"("id") ON DELETE RESTRICT ON UPDATE RESTRICT,
  "claveIntervencion" TEXT NOT NULL CHECK (length(trim(claveIntervencion))>0),
  UNIQUE ("claveIntervencion"),
  CHECK (fin IS NULL OR fin>=inicio),
  CHECK (nucleoReabiertoEn IS NULL OR (estado='Validada' AND reaperturaAutorizadaPor IS NOT NULL)),
  CHECK (estado='En_proceso' OR (fin IS NOT NULL AND resultadoDetalle IS NOT NULL AND length(trim(resultadoDetalle))>0)),
  CHECK (estado<>'En_proceso' OR fin IS NULL)
 ) STRICT;
CREATE INDEX "ix_Producto_categoriaId" ON "Producto" ("categoriaId");
CREATE INDEX "ix_Presentacion_productoId" ON "Presentacion" ("productoId");
CREATE INDEX "ix_CodigoProducto_presentacionId" ON "CodigoProducto" ("presentacionId");
CREATE INDEX "ix_PrecioProducto_presentacionId" ON "PrecioProducto" ("presentacionId");
CREATE INDEX "ix_PrecioProducto_creadoPor" ON "PrecioProducto" ("creadoPor");
CREATE INDEX "ix_Promocion_creadoPor" ON "Promocion" ("creadoPor");
CREATE INDEX "ix_PromocionProducto_promocionId" ON "PromocionProducto" ("promocionId");
CREATE INDEX "ix_PromocionProducto_presentacionId" ON "PromocionProducto" ("presentacionId");
CREATE INDEX "ix_Impuesto_creadoPor" ON "Impuesto" ("creadoPor");
CREATE INDEX "ix_ProductoImpuesto_productoId" ON "ProductoImpuesto" ("productoId");
CREATE INDEX "ix_ProductoImpuesto_impuestoId" ON "ProductoImpuesto" ("impuestoId");
CREATE INDEX "ix_ConfiguracionNegocio_cambiadoPor" ON "ConfiguracionNegocio" ("cambiadoPor");
CREATE INDEX "ix_ConfiguracionNegocio_pautaAutorizadaPor" ON "ConfiguracionNegocio" ("pautaAutorizadaPor");
CREATE UNIQUE INDEX "ux_Proveedor_0" ON "Proveedor" ("identificacion") WHERE identificacion IS NOT NULL;
CREATE UNIQUE INDEX "ux_Recepcion_2" ON "Recepcion" ("proveedorId", "documento") WHERE proveedorId IS NOT NULL AND documento IS NOT NULL;
CREATE INDEX "ix_Recepcion_proveedorId" ON "Recepcion" ("proveedorId");
CREATE INDEX "ix_Recepcion_operacionId" ON "Recepcion" ("operacionId");
CREATE INDEX "ix_Recepcion_creadoPor" ON "Recepcion" ("creadoPor");
CREATE INDEX "ix_RecepcionDetalle_recepcionId" ON "RecepcionDetalle" ("recepcionId");
CREATE INDEX "ix_RecepcionDetalle_presentacionId" ON "RecepcionDetalle" ("presentacionId");
CREATE INDEX "ix_LoteExistencia_productoId" ON "LoteExistencia" ("productoId");
CREATE INDEX "ix_LoteExistencia_recepcionDetalleId" ON "LoteExistencia" ("recepcionDetalleId");
CREATE INDEX "ix_OperacionInventario_creadoPor" ON "OperacionInventario" ("creadoPor");
CREATE INDEX "ix_OperacionInventario_ejecutadoPor" ON "OperacionInventario" ("ejecutadoPor");
CREATE INDEX "ix_OperacionInventario_autorizacionId" ON "OperacionInventario" ("autorizacionId");
CREATE INDEX "ix_OperacionInventario_reversionDeId" ON "OperacionInventario" ("reversionDeId");
CREATE INDEX "ix_MovimientoInventario_operacionId" ON "MovimientoInventario" ("operacionId");
CREATE INDEX "ix_MovimientoInventario_productoId" ON "MovimientoInventario" ("productoId");
CREATE INDEX "ix_MovimientoInventario_loteId" ON "MovimientoInventario" ("loteId");
CREATE INDEX "ix_MovimientoInventario_ventaDetalleId" ON "MovimientoInventario" ("ventaDetalleId");
CREATE INDEX "ix_MovimientoInventario_recepcionDetalleId" ON "MovimientoInventario" ("recepcionDetalleId");
CREATE INDEX "ix_MovimientoInventario_solicitudInventarioDetalleId" ON "MovimientoInventario" ("solicitudInventarioDetalleId");
CREATE INDEX "ix_MovimientoInventario_movimientoRevertidoId" ON "MovimientoInventario" ("movimientoRevertidoId");
CREATE INDEX "ix_MovimientoInventario_costoReferenciaId" ON "MovimientoInventario" ("costoReferenciaId");
CREATE INDEX "ix_Conteo_registradoPor" ON "Conteo" ("registradoPor");
CREATE INDEX "ix_Conteo_operacionReferenciaId" ON "Conteo" ("operacionReferenciaId");
CREATE UNIQUE INDEX "ux_ConteoDetalle_0" ON "ConteoDetalle" ("conteoId", "productoId") WHERE loteId IS NULL;
CREATE UNIQUE INDEX "ux_ConteoDetalle_1" ON "ConteoDetalle" ("conteoId", "productoId", "loteId") WHERE loteId IS NOT NULL;
CREATE INDEX "ix_ConteoDetalle_conteoId" ON "ConteoDetalle" ("conteoId");
CREATE INDEX "ix_ConteoDetalle_productoId" ON "ConteoDetalle" ("productoId");
CREATE INDEX "ix_ConteoDetalle_loteId" ON "ConteoDetalle" ("loteId");
CREATE INDEX "ix_SolicitudInventario_conteoId" ON "SolicitudInventario" ("conteoId");
CREATE INDEX "ix_SolicitudInventario_solicitadoPor" ON "SolicitudInventario" ("solicitadoPor");
CREATE INDEX "ix_SolicitudInventario_operacionId" ON "SolicitudInventario" ("operacionId");
CREATE INDEX "ix_SolicitudInventarioDetalle_solicitudId" ON "SolicitudInventarioDetalle" ("solicitudId");
CREATE INDEX "ix_SolicitudInventarioDetalle_productoId" ON "SolicitudInventarioDetalle" ("productoId");
CREATE INDEX "ix_SolicitudInventarioDetalle_loteId" ON "SolicitudInventarioDetalle" ("loteId");
CREATE INDEX "ix_SolicitudInventarioDetalle_proveedorDevolucionId" ON "SolicitudInventarioDetalle" ("proveedorDevolucionId");
CREATE INDEX "ix_SolicitudInventarioDetalle_costoReferenciaId" ON "SolicitudInventarioDetalle" ("costoReferenciaId");
CREATE INDEX "ix_Venta_turnoId" ON "Venta" ("turnoId");
CREATE INDEX "ix_Venta_operacionId" ON "Venta" ("operacionId");
CREATE INDEX "ix_Venta_configuracionId" ON "Venta" ("configuracionId");
CREATE INDEX "ix_VentaDetalle_ventaId" ON "VentaDetalle" ("ventaId");
CREATE INDEX "ix_VentaDetalle_presentacionId" ON "VentaDetalle" ("presentacionId");
CREATE INDEX "ix_VentaDetalle_productoId" ON "VentaDetalle" ("productoId");
CREATE INDEX "ix_VentaDetalle_costoReferenciaId" ON "VentaDetalle" ("costoReferenciaId");
CREATE INDEX "ix_Pago_ventaId" ON "Pago" ("ventaId");
CREATE INDEX "ix_Pago_verificadoPor" ON "Pago" ("verificadoPor");
CREATE UNIQUE INDEX "ux_TurnoCaja_0" ON "TurnoCaja" ("estacionId") WHERE estado='Abierto';
CREATE UNIQUE INDEX "ux_TurnoCaja_1" ON "TurnoCaja" ("operadorId") WHERE estado='Abierto';
CREATE INDEX "ix_TurnoCaja_operadorId" ON "TurnoCaja" ("operadorId");
CREATE INDEX "ix_TurnoCaja_configuracionId" ON "TurnoCaja" ("configuracionId");
CREATE INDEX "ix_MovimientoCaja_turnoId" ON "MovimientoCaja" ("turnoId");
CREATE INDEX "ix_MovimientoCaja_ventaId" ON "MovimientoCaja" ("ventaId");
CREATE INDEX "ix_MovimientoCaja_operacionId" ON "MovimientoCaja" ("operacionId");
CREATE INDEX "ix_MovimientoCaja_solicitudAnulacionId" ON "MovimientoCaja" ("solicitudAnulacionId");
CREATE INDEX "ix_MovimientoCaja_registradoPor" ON "MovimientoCaja" ("registradoPor");
CREATE INDEX "ix_GastoOperativo_registradoPor" ON "GastoOperativo" ("registradoPor");
CREATE INDEX "ix_GastoOperativo_corrigeAId" ON "GastoOperativo" ("corrigeAId");
CREATE INDEX "ix_GastoOperativo_anuladoPor" ON "GastoOperativo" ("anuladoPor");
CREATE UNIQUE INDEX "ux_SolicitudAnulacion_3" ON "SolicitudAnulacion" ("ventaId") WHERE estado IN ('En_preparacion','Pendiente','Autorizada');
CREATE INDEX "ix_SolicitudAnulacion_ventaId" ON "SolicitudAnulacion" ("ventaId");
CREATE INDEX "ix_SolicitudAnulacion_solicitadoPor" ON "SolicitudAnulacion" ("solicitadoPor");
CREATE INDEX "ix_SolicitudAnulacion_operacionId" ON "SolicitudAnulacion" ("operacionId");
CREATE INDEX "ix_SolicitudAnulacion_regularizadoPor" ON "SolicitudAnulacion" ("regularizadoPor");
CREATE INDEX "ix_Usuario_rolId" ON "Usuario" ("rolId");
CREATE UNIQUE INDEX "ux_Autorizacion_0" ON "Autorizacion" ("recepcionId", "objetoVersion") WHERE recepcionId IS NOT NULL AND invalidadaEn IS NULL AND decision IN ('pendiente','autorizada');
CREATE UNIQUE INDEX "ux_Autorizacion_1" ON "Autorizacion" ("solicitudInventarioId", "objetoVersion") WHERE solicitudInventarioId IS NOT NULL AND invalidadaEn IS NULL AND decision IN ('pendiente','autorizada');
CREATE UNIQUE INDEX "ux_Autorizacion_2" ON "Autorizacion" ("solicitudAnulacionId", "objetoVersion") WHERE solicitudAnulacionId IS NOT NULL AND invalidadaEn IS NULL AND decision IN ('pendiente','autorizada');
CREATE INDEX "ix_Autorizacion_recepcionId" ON "Autorizacion" ("recepcionId");
CREATE INDEX "ix_Autorizacion_solicitudInventarioId" ON "Autorizacion" ("solicitudInventarioId");
CREATE INDEX "ix_Autorizacion_solicitudAnulacionId" ON "Autorizacion" ("solicitudAnulacionId");
CREATE INDEX "ix_Autorizacion_solicitanteId" ON "Autorizacion" ("solicitanteId");
CREATE INDEX "ix_Autorizacion_autorizadorId" ON "Autorizacion" ("autorizadorId");
CREATE INDEX "ix_AccesoSoporte_usuarioId" ON "AccesoSoporte" ("usuarioId");
CREATE INDEX "ix_AccesoSoporte_autorizadoPor" ON "AccesoSoporte" ("autorizadoPor");
CREATE INDEX "ix_AccesoSoporte_revocadoPor" ON "AccesoSoporte" ("revocadoPor");
CREATE INDEX "ix_EventoAuditoria_usuarioId" ON "EventoAuditoria" ("usuarioId");
CREATE INDEX "ix_EventoAuditoria_eventoOrigenId" ON "EventoAuditoria" ("eventoOrigenId");
CREATE INDEX "ix_EventoAuditoria_autorizacionId" ON "EventoAuditoria" ("autorizacionId");
CREATE INDEX "ix_SolicitudImpresion_ventaId" ON "SolicitudImpresion" ("ventaId");
CREATE INDEX "ix_SolicitudImpresion_solicitadoPor" ON "SolicitudImpresion" ("solicitadoPor");
CREATE INDEX "ix_SolicitudImpresion_restauracionId" ON "SolicitudImpresion" ("restauracionId");
CREATE INDEX "ix_SolicitudImpresion_resueltoPor" ON "SolicitudImpresion" ("resueltoPor");
CREATE INDEX "ix_IntentoImpresion_solicitudImpresionId" ON "IntentoImpresion" ("solicitudImpresionId");
CREATE INDEX "ix_EntregaComprobante_ventaId" ON "EntregaComprobante" ("ventaId");
CREATE INDEX "ix_EntregaComprobante_solicitudImpresionId" ON "EntregaComprobante" ("solicitudImpresionId");
CREATE INDEX "ix_EntregaComprobante_registradoPor" ON "EntregaComprobante" ("registradoPor");
CREATE INDEX "ix_EntregaComprobante_autorizadoPor" ON "EntregaComprobante" ("autorizadoPor");
CREATE INDEX "ix_SolicitudCorreo_solicitadoPor" ON "SolicitudCorreo" ("solicitadoPor");
CREATE INDEX "ix_SolicitudCorreo_restauracionId" ON "SolicitudCorreo" ("restauracionId");
CREATE INDEX "ix_SolicitudCorreo_resueltoPor" ON "SolicitudCorreo" ("resueltoPor");
CREATE INDEX "ix_IntentoCorreo_solicitudCorreoId" ON "IntentoCorreo" ("solicitudCorreoId");
CREATE INDEX "ix_IntentoCorreo_configuracionId" ON "IntentoCorreo" ("configuracionId");
CREATE INDEX "ix_RespaldoEjecucion_solicitadoPor" ON "RespaldoEjecucion" ("solicitadoPor");
CREATE INDEX "ix_RespaldoEjecucion_ejecutadoPor" ON "RespaldoEjecucion" ("ejecutadoPor");
CREATE INDEX "ix_RespaldoEjecucion_ultimoMovimientoId" ON "RespaldoEjecucion" ("ultimoMovimientoId");
CREATE INDEX "ix_RespaldoEjecucion_ultimaOperacionId" ON "RespaldoEjecucion" ("ultimaOperacionId");
CREATE INDEX "ix_RespaldoEjecucion_configuracionId" ON "RespaldoEjecucion" ("configuracionId");
CREATE INDEX "ix_Restauracion_respaldoId" ON "Restauracion" ("respaldoId");
CREATE INDEX "ix_Restauracion_autorizadoPor" ON "Restauracion" ("autorizadoPor");
CREATE INDEX "ix_Restauracion_ejecutadoPor" ON "Restauracion" ("ejecutadoPor");
CREATE INDEX "ix_Restauracion_reaperturaAutorizadaPor" ON "Restauracion" ("reaperturaAutorizadaPor");

COMMIT;
PRAGMA foreign_key_check;
PRAGMA integrity_check;
