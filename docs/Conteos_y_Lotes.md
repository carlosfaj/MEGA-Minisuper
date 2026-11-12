# Conteos físicos y lotes

Inventario, Administrador y Propietario pueden consultar **Conteos físicos** y registrar observaciones. Solo quien registra un conteo puede modificarlo y finalizarlo. Se elige el total de un producto o un lote concreto, con cantidad física en unidad base y observación. No se permite repetir producto/lote ni mezclar total y lotes de un mismo producto.

Cada guardado exige declarar una comprobación física y conserva, dentro de la misma transacción, el saldo de referencia y la última operación de inventario confirmada. Permite reabrir el registro y revisar la diferencia (físico menos registrado). Vacío no se convierte en cero y una unidad indivisible no admite fracciones. Los cambios de unidades de productos ya contados se bloquean.

**Guardar y finalizar no ajustan existencias.** Si hay movimientos posteriores de los productos/lotes contados, la finalización se rechaza: se debe volver a contar físicamente y guardar una versión nueva. Los movimientos de otros productos no invalidan el conteo. Finalizar conserva el contenido; repetir la finalización no repite efectos. La auditoría mantiene versiones y responsable. La solicitud y aprobación de un eventual ajuste son operaciones independientes todavía pendientes.

## Lotes y vencimientos

La consulta presenta saldos por lote y unidad base, fecha, estado y enlace a su recepción de procedencia. El umbral próximo a vencer se obtiene de Configuración; si no está definido se informa, sin inventar un número de días. Se distinguen vencidos, próximos, disponibles, saldos negativos, fechas faltantes y agotados. Los lotes sin saldo se incluyen con un filtro.

Los productos con control de vencimiento y saldo sin lote se muestran por separado para conciliación; no se altera la carga histórica ni se inventan lotes para ella. Consultar no hace salidas automáticas. Inventario no recibe costos ni márgenes en esta vista. La salida por lotes, la valoración y las regularizaciones requieren los módulos y políticas pendientes.

## Verificación

108 pruebas automatizadas aprobadas en `evidencias/conteos/conteos.trx`. Las nuevas pruebas comprueban diferencias sin ajustes, movimientos posteriores, unidades, alcances, lote/producto, permisos, autor, edición obsoleta, finalización repetida, formularios y alertas. La parte de T04 referente a que un conteo no ajusta por sí mismo está comprobada; falta su solicitud de ajuste autorizada.

`tools/counting-check.cjs` ejecuta un recorrido en una base aislada, con datos ficticios:

- `01_diferencia_documentada.png`: físico 28 frente a registrado 30.
- `02_conteo_finalizado.png`: diferencia conservada y edición cerrada; existencia todavía 30.
- `03_lote_y_procedencia.png`: entrada independiente de tres unidades con lote y recepción; consulta sin costos/márgenes.

Capturas y `resultado.json` en `evidencias/conteos`. La declaración de comprobación en las pruebas es ficticia; no acredita un conteo ni aceptación reales del negocio.
