# Proveedores, recepciones y revisiones

Desde **Recepciones**, Inventario, Administrador y Propietario pueden consultar y preparar entradas. Gestión mantiene proveedores; Inventario los consulta. Desactivar un proveedor conserva su historia y bloquea su uso para nuevas entradas. Los formularios desactualizados se rechazan.

## Recorrido

1. Gestión registra un proveedor activo.
2. Inventario o gestión prepara una recepción con referencia única, proveedor, documento y fecha. Puede guardar un borrador incompleto sin afectar existencias.
3. En cada línea registra presentación, recibido y aceptado; el servidor conserva equivalencia y calcula cantidad base sin redondear silenciosamente. El costo se introduce en C$ por unidad base, hasta seis decimales. Una casilla vacía queda desconocida; nunca se convierte automáticamente en cero.
4. Completa lotes/vencimientos requeridos y explica rechazos parciales. Lo aceptado en una recepción ordinaria exige costo conocido. La mercadería vencida no se acepta: puede documentarse con aceptación cero. Guarda antes de enviar.
5. Envía la versión guardada. Otra cuenta Administrador o Propietario abre el contenido exacto, con versión y huella, y decide con motivo. La modalidad de responsabilidad directa del Propietario permanece deshabilitada hasta su validación.
6. Aprobar registra autorización, operación de inventario, cantidades aceptadas, procedencia, costos y lotes en una sola transacción. Rechazar no mueve existencias. La recepción confirmada queda sin edición.

Editar invalida revisiones pendientes. Cambiar datos del proveedor o del catálogo también impide aplicar una revisión cuyo contenido ya no coincide. Las revisiones conservan su contenido anterior. Una recepción rechazada puede corregirse por su preparador y enviarse como versión nueva.

Solo el preparador edita/envía su recepción. Otra cuenta de gestión decide; Inventario y Cajero no aprueban. No se duplica el documento de un mismo proveedor ni se elimina una recepción confirmada. Repetir la misma aprobación, incluso en concurrencia, recupera su operación sin repetir entradas. No se registra un pago de caja por recibir mercadería.

## Integración vigente

El costo de entrada se conserva como fue declarado, con escala y estado. Desde 0.9.0, la venta aplica el [promedio ponderado móvil y la asignación de lotes](Valoracion.md); desde 0.10.0 también están integradas las [regularizaciones de inventario](Inventario_Integrado.md). La recepción autorizada aporta existencias, procedencia y valoración; vender exige además disponibilidad admisible y condiciones comerciales vigentes. La aceptación del negocio sobre unidades, costos y fechas sigue pendiente.

## Evidencias y trazabilidad

RF02, RF03, RF06 y RF08; criterios T02 y T10 comprobados para recepciones. Las autorizaciones de [inventario](Solicitudes_Inventario.md) y [anulaciones](Anulaciones.md) cuentan con sus módulos. El registro histórico de 0.6.0 conserva 99 pruebas aprobadas en `evidencias/abastecimiento/abastecimiento.trx`, con comprobaciones de atomicidad ante fallo tardío, reintento concurrente, permisos, costo desconocido, precisión/equivalencias, lote, rechazo parcial, documento duplicado y versiones obsoletas. La regresión acumulada se consulta en [Manuales y validación](Manuales_y_Validacion.md).

`tools/receiving-check.cjs` usa una base ficticia aislada. Capturas reales:

- `01_preparacion.png`: preparación guardada sin entrada.
- `02_revision.png`: contenido de la versión 1 abierto por otra cuenta.
- `03_revision_obsoleta_rechazada.png`: se modificó la recepción mientras la revisión estaba abierta; aprobar fue rechazado.
- `04_entrada_confirmada.png`: aprobación de la nueva revisión.
- `05_existencias.png`: leche de 30 a 40 unidades por una sola entrada aceptada de 10.

Las capturas y `resultado.json` están en `evidencias/abastecimiento`. No acreditan aceptación del negocio.
