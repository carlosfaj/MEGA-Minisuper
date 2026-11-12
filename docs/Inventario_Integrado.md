# Inventario integrado con valoración y lotes

Desde 0.10.0 se retiran las restricciones técnicas anteriores de regularización con costo conocido/lotes. Se mantiene la revisión por otra cuenta y la confirmación atómica descritas en `Solicitudes_Inventario.md`.

Las salidas y ajustes negativos conservan el promedio ponderado móvil vigente, escala, estado y referencia de costo. Si cambia entre preparar y enviar, se exige guardar una versión nueva. La huella de revisión incluye historial de valoración, lote, vencimiento y saldo de ese alcance; cambios posteriores bloquean la aprobación anterior.

Las cargas admiten lote existente o creación de uno nuevo con referencia y vencimiento. Crear el lote no genera existencias; la cantidad entra al aprobar. Un producto con control de vencimiento requiere identificación y fecha. No se carga mercadería vencida. Una salida justificada sí puede retirar un lote vencido con causa y evidencia.

En solicitudes manuales, sin lote significa saldo no asignado, no total del producto. Para ajustar productos distribuidos en lotes se exige un conteo por lote; un conteo total no identifica cuál difiere. Se impide repetir producto/lote y dejar saldo negativo por alcance o producto. Los ajustes positivos sin costo verificable permanecen desconocidos; el impacto se muestra en la revisión y no se fabrica un costo.

Los movimientos anteriores, ventas y cierres permanecen intactos. Las autorizaciones pendientes de formatos anteriores deben guardarse y revisarse nuevamente. Las decisiones ya aplicadas conservan su recuperación sin repetir efectos.

132 pruebas aprobadas en `evidencias/inventario_integrado/inventario-integrado.trx`. Incluyen salidas conocidas, cambios de promedio, ajuste físico por lote, alcance ambiguo, lote incorrecto y capacidad. El recorrido `tools/integrated-inventory-check.cjs` conserva ocho comprobaciones y seis capturas en esa carpeta. Base aislada y datos ficticios; aceptación operativa real pendiente.
