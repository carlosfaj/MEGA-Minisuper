# Reportes y exportaciones

Desde 0.17.0 Gestión consulta ventas, inventario actual, caja y resultados. Inventario consulta exclusivamente existencias, mínimos y estado: no recibe costos, valoración monetaria ni márgenes en pantalla, PDF o Excel. Las comprobaciones de permisos se realizan también en el servidor al manipular la dirección de descarga.

Se filtra por periodo local UTC−6, producto en ventas/inventario y operador en caja. Los periodos incluyen ambos días completos, hasta 367 días. Inventario siempre representa existencias actuales al generar, no un saldo histórico de las fechas seleccionadas. Cada lectura se obtiene en una transacción consistente; se limita a 5000 filas y la pantalla presenta 250 con descargas completas del filtro. Si el resultado excede la capacidad numérica o de filas, se solicita reducir el alcance sin publicar un total parcial como completo.

## Cálculos y conciliación

Ventas usa líneas históricas de ventas actualmente confirmadas y excluye las anuladas, aunque la anulación sea posterior al periodo. El ingreso sin impuesto es subtotal menos descuento. Los costos se calculan desde cantidad base y costo histórico congelados por línea, redondeados a centavos. Costo conocido cero conserva su significado; costo desconocido o fuera de capacidad no se sustituye por cero.

Resultados muestra ingresos, costo de líneas conocidas, número de líneas no calculables, gastos vigentes, margen bruto y resultado después de gastos. Si falta un costo, el margen global y el resultado son **No calculable**. Los gastos Corregidos/Anulados no participan ni se reactivan al anular una corrección. Esto es gestión operativa, no un estado contable o fiscal acreditado.

Caja usa la fecha efectiva de cada cobro/devolución. El fondo no es ingreso. Los turnos relacionados se incluyen por apertura, cierre o movimiento en el periodo; sus saldos corresponden al turno completo. Un cierre original conserva lo guardado y una devolución posterior figura en su caja real. Por eso el informe comercial de ventas anuladas y el informe de caja tienen alcances diferentes, explicados en cada salida.

## Archivos

PDF y Excel incluyen periodo, alcance, momento de generación, moneda, criterios y detalle. El PDF pagina con encabezados repetidos y fuente Noto Sans incluida para los acentos. Excel conserva números ordinarios como números y valores de más de 14 dígitos como texto para no perder precisión; el texto de origen no se convierte en fórmula. Los archivos son una consulta estática; volver a descargar regenera con los datos actuales. La auditoría registra filtros, formato y huella SHA-256 del archivo.

## Evidencias

189 pruebas acumuladas aprobadas en `evidencias/reportes/reportes.trx`; regresión dirigida adicional en la misma carpeta. T11 comprueba costo histórico después de nuevas recepciones, costo cero/desconocido, filtros, permisos, anulación y cierres conservados, paginación PDF, tipos de datos Excel y texto que comienza con signo igual sin ejecución como fórmula. También se comprobó la apertura inicial sin filtros y rechazo de fechas explícitas inválidas.

El navegador registró en base aislada 10 unidades a costo C$10, vendió dos a C$30 y registró gasto C$10: ingreso C$60, costo C$20, margen C$40 y resultado C$30. Se descargaron `resultados.pdf`, `resultados.xlsx` e inventario limitado en ambos formatos. Se revisaron contenido, importes, acentos, paginación, vistas de las hojas y formato móvil sin desbordamiento. Las capturas y `resultado.json` registran el recorrido. Los datos son ficticios; no representan aceptación del negocio ni validación fiscal.
