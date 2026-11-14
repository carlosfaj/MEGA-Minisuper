# Conteos físicos y lotes

Inventario, Administrador y Propietario pueden consultar **Conteos físicos** y registrar observaciones. Solo quien registra un conteo puede modificarlo y finalizarlo. Se elige el total de un producto o un lote concreto, con cantidad física en unidad base y observación. No se permite repetir producto/lote ni mezclar total y lotes de un mismo producto.

Cada guardado exige declarar una comprobación física y conserva, dentro de la misma transacción, el saldo de referencia y la última operación de inventario confirmada. Permite reabrir el registro y revisar la diferencia (físico menos registrado). Vacío no se convierte en cero y una unidad indivisible no admite fracciones. Los cambios de unidades de productos ya contados se bloquean.

**Guardar y finalizar no ajustan existencias.** Si hay movimientos posteriores de los productos/lotes contados, la finalización se rechaza: se debe volver a contar físicamente y guardar una versión nueva. Los movimientos de otros productos no invalidan el conteo. Finalizar conserva el contenido; repetir la finalización no repite efectos. La auditoría mantiene versiones y responsable. Desde 0.8.0, el ajuste se prepara como [solicitud independiente](Solicitudes_Inventario.md) y requiere aprobación por otra cuenta; conserva las restricciones de valoración/lotes descritas allí.

## Lotes y vencimientos

La consulta presenta saldos por lote y unidad base, fecha, estado y enlace a su recepción de procedencia. El umbral próximo a vencer se obtiene de Configuración; si no está definido se informa, sin inventar un número de días. Se distinguen vencidos, próximos, disponibles, saldos negativos, fechas faltantes y agotados. Los lotes sin saldo se incluyen con un filtro.

Los productos con control de vencimiento y saldo sin lote se muestran por separado para conciliación; no se altera la carga histórica ni se inventan lotes para ella. Consultar no hace salidas automáticas. Inventario no recibe costos ni márgenes en esta vista. La [venta con lotes y valoración](Valoracion.md) está integrada desde 0.9.0, y las [regularizaciones por lote](Inventario_Integrado.md) desde 0.10.0. Un ajuste de producto distribuido en lotes requiere conteo por lote; el total no identifica cuál difiere. La conciliación física real sigue pendiente.

## Verificación

108 pruebas automatizadas aprobadas en `evidencias/conteos/conteos.trx` para 0.7.0. Comprueban diferencias sin ajustes, movimientos posteriores, unidades, alcances, lote/producto, permisos, autor, edición obsoleta, finalización repetida, formularios y alertas. La solicitud de ajuste autorizada y las 120 pruebas acumuladas de 0.8.0 se documentan en `Solicitudes_Inventario.md`.

`tools/counting-check.cjs` ejecuta un recorrido en una base aislada, con datos ficticios:

- `01_diferencia_documentada.png`: físico 28 frente a registrado 30.
- `02_conteo_finalizado.png`: diferencia conservada y edición cerrada; existencia todavía 30.
- `03_lote_y_procedencia.png`: entrada independiente de tres unidades con lote y recepción; consulta sin costos/márgenes.

Capturas y `resultado.json` en `evidencias/conteos`. La declaración de comprobación en las pruebas es ficticia; no acredita un conteo ni aceptación reales del negocio.
