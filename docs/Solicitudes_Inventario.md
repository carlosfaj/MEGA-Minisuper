# Cargas, ajustes y salidas justificadas

Desde **Ajustes y salidas**, Inventario, Administrador y Propietario preparan solicitudes. Otra cuenta de gestión revisa la versión exacta, cantidades, costo, saldo y evidencia. Solo quien prepara puede editar, enviar o cancelar. Preparar, enviar, rechazar o cancelar no altera existencias.

- **Carga:** cantidad positiva en unidad base, motivo y evidencia. El costo por unidad base puede declararse conocido con hasta seis decimales; vacío permanece desconocido y no se convierte en cero.
- **Ajuste:** requiere un conteo finalizado. El servidor obtiene las diferencias físico menos registrado y omite diferencias cero. No acepta cantidades manuales. Si hubo movimientos posteriores sobre los productos contados, exige registrar un conteo nuevo. Un conteo no puede tener dos solicitudes vigentes o aplicadas.
- **Salida justificada:** cantidad positiva en el formulario, registrada como salida negativa tras aprobar; causa (devolución a proveedor, robo, daño, vencimiento, pérdida o consumo interno), evidencia y proveedor activo en una devolución. No se asigna costo de salida manualmente.

Después de guardar, envía la versión guardada a revisión. Otra cuenta Administrador o Propietario comprueba el contenido y decide con motivo. Las decisiones por cuenta propia se rechazan; no se habilita la excepción de responsabilidad directa del Propietario pendiente de validar.

Editar o cancelar invalida revisiones. Cambios del producto, proveedor de devolución o saldo posterior a la revisión también bloquean la decisión anterior. La aprobación registra autorización, operación, movimientos y auditoría en una transacción; un fallo tardío revierte todo. Repetir la aprobación recupera la misma operación. Las solicitudes aplicadas conservan su contenido. No se modifican caja ni pagos por estas solicitudes.

## Alcance y límites

El recorrido actual permite regularizaciones de productos sin lotes y con historia de costo desconocido. Se bloquean productos con lotes/control de vencimiento o movimientos de costo conocido hasta integrar la asignación y valoración de salidas. No se degrada su costo conocido ni se inventa un costo para permitir la operación. Una carga que declara costo conocido conserva esa entrada, y sus salidas posteriores requieren el módulo de valoración. La solicitud tampoco puede dejar existencias negativas.

La evidencia es una referencia al documento, fotografía o acta conservados por el responsable; este avance no almacena archivos adjuntos. La carga inicial ficticia continúa consultable sin reinterpretar su snapshot histórico. Las pruebas no acreditan evidencia física ni aceptación reales.

## Verificación y evidencia

120 pruebas automatizadas aprobadas en `evidencias/solicitudes/solicitudes.trx`. Incluyen T04/T10 para el alcance implementado, permisos, contenido obsoleto, saldo cambiado, conteo antiguo, costo conocido/desconocido, salida sin causa, cancelación, rechazo, fallo tardío, concurrencia y formularios con protección antifalsificación.

`tools/inventory-check.cjs` usa una base ficticia aislada. Se registraron cuatro capturas:

- `01_ajuste_revisado.png`: diferencia −2 procedente del conteo, abierta por otra cuenta.
- `02_ajuste_aplicado.png`: autorización aplicada una sola vez, de 30 a 28 unidades.
- `03_salida_justificada.png`: después de carga de 3, salida justificada de 1; saldo final 30.
- `04_cancelacion.png`: preparación cancelada sin efecto; la revisión anterior abierta rechaza aplicar.

Capturas y `resultado.json` en `evidencias/solicitudes`. Se mantienen pendientes valoración/lotes en regularizaciones, anulación de ventas y aceptación del negocio.
