-- Esquema 2. Se aplica en una transacción por DatabaseSchema.
-- No reemplaza las comprobaciones de permisos, sumas y autorizaciones del servicio.
CREATE INDEX ix_Venta_turno_id ON Venta(turnoId, id DESC);
CREATE INDEX ix_MovimientoInventario_producto_costo ON MovimientoInventario(productoId, estadoCosto);
CREATE INDEX ix_MovimientoCaja_turno_importe ON MovimientoCaja(turnoId, importeCentavos);

CREATE VIEW vw_Existencias AS
SELECT p.id AS ProductoId, p.nombre AS Nombre, p.unidadBase AS UnidadBase,
       p.escalaCantidad AS EscalaCantidad, p.minimoEscalado AS MinimoEscalado,
       p.activo AS Activo, COALESCE(SUM(m.cantidadEscalada), 0) AS CantidadEscalada
FROM Producto p LEFT JOIN MovimientoInventario m ON m.productoId=p.id
GROUP BY p.id;

CREATE VIEW vw_ResumenCaja AS
SELECT t.id AS TurnoId, t.operadorId AS OperadorId, t.estado AS Estado,
       t.fondoRealCentavos AS FondoRealCentavos,
       COALESCE(SUM(m.importeCentavos), 0) AS NetoCentavos,
       CASE WHEN t.estado='Cerrado' THEN t.efectivoEsperadoCierreCentavos
            ELSE t.fondoRealCentavos + COALESCE(SUM(m.importeCentavos), 0)
       END AS EsperadoCentavos
FROM TurnoCaja t LEFT JOIN MovimientoCaja m ON m.turnoId=t.id
GROUP BY t.id;

CREATE TRIGGER tr_PrecioProducto_intervalo_insert
BEFORE INSERT ON PrecioProducto
WHEN EXISTS (
    SELECT 1 FROM PrecioProducto p WHERE p.presentacionId=NEW.presentacionId
      AND (NEW.vigenteHasta IS NULL OR p.vigenteDesde<NEW.vigenteHasta)
      AND (p.vigenteHasta IS NULL OR NEW.vigenteDesde<p.vigenteHasta)
)
BEGIN SELECT RAISE(ABORT, 'MEGA: vigencias de precio superpuestas'); END;

CREATE TRIGGER tr_PrecioProducto_intervalo_update
BEFORE UPDATE ON PrecioProducto
WHEN EXISTS (
    SELECT 1 FROM PrecioProducto p WHERE p.id<>OLD.id AND p.presentacionId=NEW.presentacionId
      AND (NEW.vigenteHasta IS NULL OR p.vigenteDesde<NEW.vigenteHasta)
      AND (p.vigenteHasta IS NULL OR NEW.vigenteDesde<p.vigenteHasta)
)
BEGIN SELECT RAISE(ABORT, 'MEGA: vigencias de precio superpuestas'); END;

CREATE TRIGGER tr_Venta_origen_insert
BEFORE INSERT ON Venta
WHEN NOT EXISTS (
    SELECT 1 FROM TurnoCaja t JOIN OperacionInventario o ON o.id=NEW.operacionId
      JOIN ConfiguracionNegocio c ON c.id=NEW.configuracionId
    WHERE t.id=NEW.turnoId AND t.estado='Abierto'
      AND t.operadorId=o.ejecutadoPor AND o.tipo='venta' AND c.moneda=NEW.moneda
)
BEGIN SELECT RAISE(ABORT, 'MEGA: venta incompatible con turno, operacion o moneda'); END;

CREATE TRIGGER tr_Venta_historia_update
BEFORE UPDATE ON Venta
WHEN OLD.id IS NOT NEW.id OR OLD.folio IS NOT NEW.folio OR OLD.fecha IS NOT NEW.fecha
  OR OLD.turnoId IS NOT NEW.turnoId OR OLD.operacionId IS NOT NEW.operacionId
  OR OLD.configuracionId IS NOT NEW.configuracionId OR OLD.moneda IS NOT NEW.moneda
  OR OLD.subtotalCentavos IS NOT NEW.subtotalCentavos OR OLD.impuestoCentavos IS NOT NEW.impuestoCentavos
  OR OLD.descuentoCentavos IS NOT NEW.descuentoCentavos OR OLD.totalCentavos IS NOT NEW.totalCentavos
  OR OLD.claveOperacion IS NOT NEW.claveOperacion OR OLD.comprobanteSnapshot IS NOT NEW.comprobanteSnapshot
  OR OLD.comprobanteHash IS NOT NEW.comprobanteHash
  OR (OLD.estado='Anulada' AND NEW.estado<>'Anulada')
BEGIN SELECT RAISE(ABORT, 'MEGA: conservar historia y comprobante de venta'); END;

CREATE TRIGGER tr_Venta_historia_delete BEFORE DELETE ON Venta
BEGIN SELECT RAISE(ABORT, 'MEGA: no borrar ventas confirmadas'); END;

CREATE TRIGGER tr_VentaDetalle_producto_insert
BEFORE INSERT ON VentaDetalle
WHEN NOT EXISTS (
    SELECT 1 FROM Presentacion pr JOIN Producto p ON p.id=pr.productoId
    WHERE pr.id=NEW.presentacionId AND p.id=NEW.productoId
      AND p.escalaCantidad=NEW.escalaCantidad AND p.unidadBase=NEW.unidadBaseHistorica
)
BEGIN SELECT RAISE(ABORT, 'MEGA: producto, presentacion o unidad de venta incompatibles'); END;

CREATE TRIGGER tr_VentaDetalle_historia_update BEFORE UPDATE ON VentaDetalle
BEGIN SELECT RAISE(ABORT, 'MEGA: no modificar detalle historico de venta'); END;
CREATE TRIGGER tr_VentaDetalle_historia_delete BEFORE DELETE ON VentaDetalle
BEGIN SELECT RAISE(ABORT, 'MEGA: no borrar detalle historico de venta'); END;

CREATE TRIGGER tr_MovimientoInventario_origen_insert
BEFORE INSERT ON MovimientoInventario
WHEN NOT EXISTS (SELECT 1 FROM Producto p WHERE p.id=NEW.productoId AND p.escalaCantidad=NEW.escala)
 OR (NEW.loteId IS NOT NULL AND NOT EXISTS (
     SELECT 1 FROM LoteExistencia l WHERE l.id=NEW.loteId AND l.productoId=NEW.productoId))
 OR (NEW.ventaDetalleId IS NOT NULL AND NOT EXISTS (
     SELECT 1 FROM VentaDetalle d JOIN Venta v ON v.id=d.ventaId JOIN OperacionInventario o ON o.id=v.operacionId
     WHERE d.id=NEW.ventaDetalleId AND d.productoId=NEW.productoId AND o.id=NEW.operacionId
       AND o.tipo='venta' AND NEW.cantidadEscalada<0))
 OR (NEW.recepcionDetalleId IS NOT NULL AND NOT EXISTS (
     SELECT 1 FROM RecepcionDetalle d JOIN Presentacion p ON p.id=d.presentacionId
       JOIN Recepcion r ON r.id=d.recepcionId JOIN OperacionInventario o ON o.id=r.operacionId
     WHERE d.id=NEW.recepcionDetalleId AND p.productoId=NEW.productoId AND o.id=NEW.operacionId
       AND o.tipo='entrada_recepcion' AND NEW.cantidadEscalada>0))
 OR (NEW.solicitudInventarioDetalleId IS NOT NULL AND NOT EXISTS (
     SELECT 1 FROM SolicitudInventarioDetalle d JOIN SolicitudInventario s ON s.id=d.solicitudId JOIN OperacionInventario o ON o.id=s.operacionId
     WHERE d.id=NEW.solicitudInventarioDetalleId AND d.productoId=NEW.productoId AND o.id=NEW.operacionId AND o.tipo=s.tipo
       AND ((o.tipo='carga' AND NEW.cantidadEscalada>0) OR o.tipo='ajuste' OR (o.tipo='salida_justificada' AND NEW.cantidadEscalada<0))))
 OR (NEW.movimientoRevertidoId IS NOT NULL AND NOT EXISTS (
     SELECT 1 FROM MovimientoInventario m JOIN OperacionInventario o ON o.id=NEW.operacionId
     WHERE m.id=NEW.movimientoRevertidoId AND m.productoId=NEW.productoId AND m.loteId IS NEW.loteId
       AND m.escala=NEW.escala AND o.tipo='reversion' AND o.reversionDeId=m.operacionId
       AND m.cantidadEscalada<0 AND NEW.cantidadEscalada>0 AND NEW.cantidadEscalada<=-m.cantidadEscalada))
BEGIN SELECT RAISE(ABORT, 'MEGA: movimiento de inventario incompatible con origen, producto o unidad'); END;

-- Las correcciones de un movimiento se registran como otro movimiento; no se edita su origen/cantidad.
-- Los campos de costo quedan disponibles para las pruebas y el futuro módulo de valoración.
CREATE TRIGGER tr_MovimientoInventario_identidad_update
BEFORE UPDATE ON MovimientoInventario
WHEN OLD.id IS NOT NEW.id OR OLD.operacionId IS NOT NEW.operacionId OR OLD.productoId IS NOT NEW.productoId
 OR OLD.loteId IS NOT NEW.loteId OR OLD.secuencia IS NOT NEW.secuencia
 OR OLD.ventaDetalleId IS NOT NEW.ventaDetalleId OR OLD.recepcionDetalleId IS NOT NEW.recepcionDetalleId
 OR OLD.solicitudInventarioDetalleId IS NOT NEW.solicitudInventarioDetalleId
 OR OLD.movimientoRevertidoId IS NOT NEW.movimientoRevertidoId
 OR OLD.cantidadEscalada IS NOT NEW.cantidadEscalada OR OLD.escala IS NOT NEW.escala
BEGIN SELECT RAISE(ABORT, 'MEGA: conservar origen y cantidad del movimiento'); END;
CREATE TRIGGER tr_MovimientoInventario_historia_delete BEFORE DELETE ON MovimientoInventario
BEGIN SELECT RAISE(ABORT, 'MEGA: no borrar movimientos de inventario'); END;

CREATE TRIGGER tr_Pago_cobertura_insert
BEFORE INSERT ON Pago
WHEN NOT EXISTS (
    SELECT 1 FROM Venta v JOIN TurnoCaja t ON t.id=v.turnoId
    WHERE v.id=NEW.ventaId AND v.estado='Confirmada' AND t.estado='Abierto' AND t.operadorId=NEW.verificadoPor
      AND COALESCE((SELECT SUM(p.importeCentavos) FROM Pago p WHERE p.ventaId=v.id), 0)+NEW.importeCentavos<=v.totalCentavos
)
BEGIN SELECT RAISE(ABORT, 'MEGA: pago incompatible con venta, operador o cobertura'); END;
CREATE TRIGGER tr_Pago_historia_update BEFORE UPDATE ON Pago
BEGIN SELECT RAISE(ABORT, 'MEGA: no modificar pagos historicos'); END;
CREATE TRIGGER tr_Pago_historia_delete BEFORE DELETE ON Pago
BEGIN SELECT RAISE(ABORT, 'MEGA: no borrar pagos historicos'); END;

CREATE TRIGGER tr_MovimientoCaja_origen_insert
BEFORE INSERT ON MovimientoCaja
WHEN NOT EXISTS (SELECT 1 FROM TurnoCaja t WHERE t.id=NEW.turnoId AND t.estado='Abierto' AND t.operadorId=NEW.registradoPor)
 OR (NEW.tipo='cobro_efectivo' AND NOT EXISTS (
    SELECT 1 FROM Venta v WHERE v.id=NEW.ventaId AND v.turnoId=NEW.turnoId AND v.operacionId=NEW.operacionId
      AND NEW.importeCentavos=(SELECT SUM(p.importeCentavos) FROM Pago p WHERE p.ventaId=v.id AND p.medio='efectivo')))
 OR (NEW.tipo='devolucion_efectivo' AND NOT EXISTS (
    SELECT 1 FROM SolicitudAnulacion s JOIN OperacionInventario o ON o.id=s.operacionId
    WHERE s.id=NEW.solicitudAnulacionId AND s.ventaId=NEW.ventaId AND s.operacionId=NEW.operacionId
      AND s.estado='Autorizada' AND o.tipo='reversion'))
BEGIN SELECT RAISE(ABORT, 'MEGA: movimiento de caja incompatible con turno u origen'); END;
CREATE TRIGGER tr_MovimientoCaja_historia_update BEFORE UPDATE ON MovimientoCaja
BEGIN SELECT RAISE(ABORT, 'MEGA: no modificar movimientos de caja'); END;
CREATE TRIGGER tr_MovimientoCaja_historia_delete BEFORE DELETE ON MovimientoCaja
BEGIN SELECT RAISE(ABORT, 'MEGA: no borrar movimientos de caja'); END;

CREATE TRIGGER tr_TurnoCaja_cerrado_update BEFORE UPDATE ON TurnoCaja WHEN OLD.estado='Cerrado'
BEGIN SELECT RAISE(ABORT, 'MEGA: un cierre de caja es historico'); END;
CREATE TRIGGER tr_TurnoCaja_cerrado_delete BEFORE DELETE ON TurnoCaja WHEN OLD.estado='Cerrado'
BEGIN SELECT RAISE(ABORT, 'MEGA: no borrar un turno cerrado'); END;

CREATE TRIGGER tr_EventoAuditoria_append_update BEFORE UPDATE ON EventoAuditoria
BEGIN SELECT RAISE(ABORT, 'MEGA: la auditoria solo admite nuevos eventos'); END;
CREATE TRIGGER tr_EventoAuditoria_append_delete BEFORE DELETE ON EventoAuditoria
BEGIN SELECT RAISE(ABORT, 'MEGA: no borrar auditoria'); END;
