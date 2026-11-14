# Valoración e integración de ventas

Se implementa el promedio ponderado móvil por producto en unidad base propuesto en el tercer corte y confirmado por el usuario el 12 de noviembre de 2026. El orden es secuencia de confirmación de operación, secuencia del movimiento e identidad; la fecha del documento no reordena el costo. Precisión de costo: seis decimales, mitades hacia arriba.

Una entrada conocida sobre saldo cero establece la base. Con saldo positivo y costo previo conocido se ponderan saldo y entrada. Si falta la base, entra costo desconocido o el saldo previo es negativo, permanece desconocido; no equivale a cero. Un costo cero declarado permanece conocido. Agotar el saldo permite que una entrada posterior conocida establezca una base nueva.

La venta conserva costo unitario, escala, estado y referencia anteriores a su salida. Entradas nuevas no modifican ventas ni movimientos anteriores. El margen de salidas desconocidas sigue siendo no calculable.

## Lotes

El servidor asigna los lotes disponibles por vencimiento más próximo y después por identidad. No asigna unidades vencidas, fechas inválidas ni saldos negativos de lote. Un producto con control de vencimiento requiere lotes identificados con fecha. Las líneas repetidas comparten disponibilidad. Una línea con varios lotes genera varios movimientos y un solo detalle/pago.

Productos sin control pueden consumir saldo sin lote. La contingencia física de saldo negativo se conserva solo en ventas básicas sin historial de lotes. La cotización incluye huella de valoración y distribución; cambiarlas exige revisar antes de cobrar. El reintento de una venta confirmada recupera el resultado original.

## Evidencia

127 pruebas aprobadas en `evidencias/valoracion/valoracion.trx`: promedio, desconocido, cero, saldo negativo, orden, capacidad, costo histórico, lotes múltiples, vencimiento, disponibilidad compartida y cotización obsoleta. `tools/valuation-check.cjs` verificó una recepción con costo/lote y venta antes restringida, con saldo del lote 3 a 2; dos capturas y JSON en la misma carpeta. Base aislada, datos ficticios.

Las regularizaciones por lotes/costo conocido están integradas desde 0.10.0 en [Inventario integrado](Inventario_Integrado.md). La política técnica confirmada no acredita aceptación comercial, fiscal ni de periféricos reales.
