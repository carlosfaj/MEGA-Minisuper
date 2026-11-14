# Cargas, ajustes y salidas justificadas

Desde **Ajustes y salidas**, Inventario, Administrador y Propietario preparan solicitudes. Otra cuenta de gestión revisa la versión exacta, cantidades, costo, saldo y evidencia. Solo quien prepara puede editar, enviar o cancelar. Preparar, enviar, rechazar o cancelar no altera existencias.

- **Carga:** cantidad positiva en unidad base, motivo y evidencia. El costo por unidad base puede declararse conocido con hasta seis decimales; vacío permanece desconocido y no se convierte en cero.
- **Ajuste:** requiere un conteo finalizado. El servidor obtiene las diferencias físico menos registrado y omite diferencias cero. No acepta cantidades manuales. Si hubo movimientos posteriores sobre los productos contados, exige registrar un conteo nuevo. Un conteo no puede tener dos solicitudes vigentes o aplicadas.
- **Salida justificada:** cantidad positiva en el formulario, registrada como salida negativa tras aprobar; causa (devolución a proveedor, robo, daño, vencimiento, pérdida o consumo interno), evidencia y proveedor activo en una devolución. No se asigna costo de salida manualmente.

Después de guardar, envía la versión guardada a revisión. Otra cuenta Administrador o Propietario comprueba el contenido y decide con motivo. Las decisiones por cuenta propia se rechazan; no se habilita la excepción de responsabilidad directa del Propietario pendiente de validar.

Editar o cancelar invalida revisiones. Cambios del producto, proveedor de devolución o saldo posterior a la revisión también bloquean la decisión anterior. La aprobación registra autorización, operación, movimientos y auditoría en una transacción; un fallo tardío revierte todo. Repetir la aprobación recupera la misma operación. Las solicitudes aplicadas conservan su contenido. No se modifican caja ni pagos por estas solicitudes.

## Alcance y límites

Desde 0.10.0, las regularizaciones admiten costo conocido y lotes según [Inventario integrado](Inventario_Integrado.md). Las salidas conservan el promedio ponderado móvil vigente; no se inventa un costo para permitir la operación. Las cargas pueden identificar un lote existente o preparar uno nuevo, cuya existencia solo cambia al aprobar. Sin lote identifica saldo no asignado, y el ajuste de producto distribuido en lotes exige conteo por lote. La solicitud no puede dejar existencias negativas por producto ni por alcance. La revisión incorpora valoración, fecha y saldo; un cambio posterior exige nueva revisión.

La evidencia es una referencia al documento, fotografía o acta conservados por el responsable; este avance no almacena archivos adjuntos. La carga inicial ficticia continúa consultable sin reinterpretar su snapshot histórico. Las pruebas no acreditan evidencia física ni aceptación reales.

## Verificación y evidencia

El registro histórico de 0.8.0 conserva 120 pruebas automatizadas aprobadas en `evidencias/solicitudes/solicitudes.trx`. Incluyen T04/T10 para ese alcance, permisos, contenido obsoleto, saldo cambiado, conteo antiguo, costo conocido/desconocido, salida sin causa, cancelación, rechazo, fallo tardío, concurrencia y formularios con protección antifalsificación. La integración posterior y la regresión acumulada se documentan en [Inventario integrado](Inventario_Integrado.md) y [Manuales y validación](Manuales_y_Validacion.md).

`tools/inventory-check.cjs` usa una base ficticia aislada. Se registraron cuatro capturas:

- `01_ajuste_revisado.png`: diferencia −2 procedente del conteo, abierta por otra cuenta.
- `02_ajuste_aplicado.png`: autorización aplicada una sola vez, de 30 a 28 unidades.
- `03_salida_justificada.png`: después de carga de 3, salida justificada de 1; saldo final 30.
- `04_cancelacion.png`: preparación cancelada sin efecto; la revisión anterior abierta rechaza aplicar.

Capturas y `resultado.json` en `evidencias/solicitudes`. Las regularizaciones con valoración/lotes y las [anulaciones](Anulaciones.md) ya están integradas; la aceptación del negocio y las evidencias físicas siguen pendientes.
