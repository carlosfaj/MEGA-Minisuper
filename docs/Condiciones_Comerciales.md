# Promociones e impuestos integrados

Gestión accede a **Promociones e impuestos**. Las condiciones vigentes se calculan en el servidor, aparecen antes de cobrar y quedan congeladas en el detalle y comprobante. Modificar condiciones después de cotizar exige revisar nuevamente; ventas anteriores no cambian.

## Promociones

Se configura porcentaje de 0 a 100 o importe fijo C$ por presentación vendida, con inicio/fin locales y presentaciones seleccionadas. La regla es exclusiva: se elige una promoción por línea, primero por menor prioridad numérica y después por identidad. No se acumulan. El importe fijo se multiplica por la cantidad de presentaciones y el descuento se limita al subtotal. Una compra cuyo total sea cero permanece deshabilitada.

## Impuestos y exenciones

No hay tasas fiscales predeterminadas. Gestión introduce la tasa porcentual y selecciona productos, vigencias y estado. Una exención exige tasa cero y motivo; una tasa ordinaria de cero conserva su identidad distinta. Una exención simultánea con otras condiciones del producto bloquea la venta hasta corregir la configuración.

El precio es la base anterior a estos impuestos adicionales. La base imponible es subtotal menos descuento. Cada condición ordinaria se calcula sobre esa misma base, sin cascada; se redondea por línea a centavos, mitades hacia arriba. Configurar una tasa no acredita el régimen ni validez fiscal del comprobante, que conserva su aviso. La fiscalidad real sigue pendiente de confirmar con el negocio.

Las instantáneas conservan identidad, nombre, tasa/escala, base, exención/motivo y monto; la promoción conserva condición, versión y cálculo. Sin impuestos se guarda una lista vacía y sin promoción se guarda NULL.

## Evidencia

145 pruebas aprobadas en `evidencias/comercial/comercial.trx`: exclusividad, prioridad, importe fijo, vigencias, exención, tasa cero, conflictos, permisos, ediciones/cotizaciones obsoletas e historia. `tools/commercial-check.cjs` verificó promoción ficticia de 10% e impuesto ficticio de 15%: subtotal C$38.00, descuento C$3.80, impuesto C$5.13, total C$39.33; al recibir C$50.00, cambio C$10.67. Tres capturas y JSON en la misma carpeta. Es una prueba de cálculo con datos ficticios, no validación fiscal real.
