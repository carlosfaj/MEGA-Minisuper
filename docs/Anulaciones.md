# Anulaciones y devolución de fondos

Cajero solicita sobre sus ventas; gestión puede preparar y consultar las del negocio. Otra cuenta Administrador/Propietario revisa la versión, huella, venta/pagos originales, motivo y evidencia. Se anula la venta completa; las devoluciones parciales no están habilitadas. Preparar, enviar, rechazar o cancelar no cambia inventario ni caja.

## Mercadería

Se declara reincorporación total verificada o no reincorporación con evidencia. Reincorporar exige confirmar la comprobación física. Al aprobar se crea una reversión que utiliza cada cantidad, lote y costo histórico del movimiento original, no el costo actual. Si no se reincorpora, no se inventa entrada. La venta conserva folio, importes, detalle, pagos y comprobante; solo cambia su estado/versionado y se agrega la reversión autorizada.

## Efectivo

La aprobación no inventa un egreso: el importe queda pendiente. El solicitante o una cuenta de gestión con permiso de caja registra la entrega efectiva desde su turno actual abierto, con evidencia, confirmación de entrega y saldo suficiente. Se devuelve el importe pagado, no el efectivo recibido antes de dar cambio. Registrar de nuevo recupera el egreso existente, incluso tras un reintento concurrente.

Si el turno original cerró, se conserva exactamente su cierre. La entrega posterior utiliza un turno nuevo del responsable y cambia su saldo esperado. El registro identifica caja, responsable, fecha, importe y evidencia. Los fondos externos, si existieran en historia, permanecen identificados para conciliación; el MVP actual solo habilita efectivo y no afirma un procedimiento bancario real.

Decisión, reversión, estado y auditoría se confirman juntos. La devolución también es atómica con estado de regularización y versión del turno. Fallos tardíos no dejan movimientos ni estados parciales. Las revisiones editadas/canceladas quedan invalidadas. La responsabilidad directa del Propietario sigue sin habilitarse.

## Evidencia

156 pruebas aprobadas en `evidencias/anulaciones/anulaciones.trx`: T06/T08/T09/T10, permisos, otra cuenta, versiones, costo histórico después de nuevas entradas, recuperación, concurrencia y fallos tardíos de reincorporación y devolución. El diagnóstico del esquema sigue sano después de los efectos.

`tools/cancellation-check.cjs` verificó en navegador una venta de C$38 y cierre original C$538; aprobación sin egreso ficticio; comprobante de C$38 conservado; nueva caja con fondo C$500 y devolución C$38, esperado C$462. Cuatro capturas y JSON en la misma carpeta. Todas las comprobaciones físicas/entregas declaradas son ficticias en una base aislada; no acreditan aceptación real del negocio.
