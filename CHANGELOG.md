# Historial de versiones de MEGA

## 0.15.0 — 12 de noviembre de 2026

- Agente local de impresión con habilitación explícita, cola persistente, reserva previa e intentos verificables; aceptación de Windows separada de entrega física.
- Fallo conocido permite reintento revisado; reserva vencida o resultado incierto bloquean reenvío automático. Resolución de gestión, copia del mismo folio y contingencia documentada.
- Comprobante inmutable, sin nuevo cobro por imprimir; venta anulada o huella inválida se pausan. Resultado tardío conserva la resolución y su evidencia.
- 175 pruebas y navegador de escritorio/móvil; controlador Microsoft Print to PDF produjo original y copia comprobados. Impresora física y aceptación del negocio pendientes.

## 0.14.0 — 12 de noviembre de 2026

- Rechazos críticos y fallos HTTP con cuenta, ruta y correlación, sin guardar contraseñas ni cuerpos de solicitud.
- Escritura independiente de la operación revertida; serialización y reintento acotado ante bloqueo temporal, sin repetir efectos comerciales.
- Filtro, búsqueda y consulta de detalles en la bitácora. Salida y cambio de contraseña exitosos no se registran como rechazos.
- 162 pruebas acumuladas y recorrido de navegador con datos ficticios; T13 para los módulos implementados.

## 0.13.0 — 12 de noviembre de 2026

- Anulación completa por solicitud, motivo/evidencia y revisión exacta de otra cuenta de gestión; edición, rechazo y cancelación sin efectos anticipados.
- Reincorporación solo con comprobación física declarada; cantidades, lotes y costos históricos originales. Venta, comprobante y cierre anterior conservados.
- Aprobación comercial separada de devolución efectiva: queda pendiente hasta entregar y registrar dinero en caja propia abierta, con evidencia y saldo suficiente.
- Regularización en turno nuevo sin modificar el cierre original. Decisiones y devoluciones concurrentes no repiten movimientos; fallos tardíos revierten todo.
- 156 pruebas y navegador con cierre original 538 conservado y devolución 38 en turno nuevo de fondo 500, esperado 462.

## 0.12.0 — 12 de noviembre de 2026

- Promociones de porcentaje/importe fijo por presentación, vigencias, prioridad y exclusividad; condiciones configurables sobre múltiples presentaciones.
- Tasas y exenciones por producto con vigencias y motivo; sin tasas predeterminadas ni atribución de validez fiscal. Base después del descuento, sin cascada y redondeo por línea.
- Carrito y comprobante muestran subtotal, descuento, impuesto y total. Se congelan las condiciones aplicadas; editar o desactivar no altera ventas anteriores.
- Cambios posteriores a cotizar exigen revisión. Conflictos de exención, porcentajes inválidos y total cero se rechazan.
- 145 pruebas aprobadas y navegador con datos fiscales ficticios: 38 − 3.80 + 5.13 = 39.33; cambio 10.67 al recibir 50.

## 0.11.0 — 12 de noviembre de 2026

- Kanban de todo el proyecto: software, calidad, documentación, entrega, decisiones del negocio y periféricos, con cuatro columnas y filtros.
- Porcentaje global, por área y por tarjeta calculado con criterios completados; implementación y aceptación real se mantienen separadas.
- Fuente de seguimiento versionada y vista Markdown en GitHub; actualización automática de la pantalla al cambiar la fuente.
- Herramienta para registrar criterios con evidencia y resultados TRX; GitHub Actions comprueba coherencia de versión y tablero.
- 136 pruebas aprobadas y navegador de escritorio/móvil con filtros sin alterar el porcentaje global.

## 0.10.0 — 12 de noviembre de 2026

- Cargas, ajustes y salidas integrados con costos y lotes; costos de salida tomados del promedio vigente y conservados con referencia histórica.
- Lotes nuevos para carga sin afectar stock hasta aprobar; conteos por lote para ajustar sin adivinar la distribución de un total.
- Revisión vinculada también a valoración y metadatos de lote; costo cambiado exige guardar otra versión. Control de saldos por lote y producto.
- Costo desconocido sigue explícito; no se transforma en cero ni se altera la historia. Las revisiones pendientes de formatos anteriores requieren guardado/revisión nuevos.
- 132 pruebas aprobadas y ocho comprobaciones en navegador, incluidas carga con lote nuevo y salida del lote de 3 a 2.

## 0.9.0 — 12 de noviembre de 2026

- Ventas integradas con promedio ponderado móvil, política confirmada por el usuario, y asignación de lotes por vencimiento más próximo e identidad estable.
- Costos conocidos, desconocidos y cero diferenciados; saldo negativo o base incompleta no inventan promedio. Orden por secuencia de confirmación.
- Salidas con costo y procedencia históricos; entradas posteriores no revalúan ventas anteriores. Una línea puede descontar varios lotes sin duplicar cobro.
- Revisión obligatoria si cambian valoración o lotes; rechazo de lotes vencidos/no identificados y sobreasignación. Guardas de capacidad de inventario/caja.
- 127 pruebas aprobadas y navegador: recepción con costo/lote, venta antes restringida y descuento del lote de 3 a 2.

## 0.8.0 — 12 de noviembre de 2026

- Solicitudes de carga, ajuste desde conteo finalizado y salida justificada, con motivo/evidencia, versiones, cancelación e historia.
- Los ajustes toman exclusivamente las diferencias del conteo; movimientos posteriores exigen un conteo nuevo. Preparar/enviar/cancelar no modifica existencias.
- Revisión de contenido, versión y saldo por otra cuenta de gestión; aprobación transaccional con movimiento y autorización, sin duplicar efectos por reintento concurrente.
- Costos desconocidos conservados como NULL; costo de carga conocido solo si se introduce explícitamente. Regularizaciones de productos con costo conocido o lotes bloqueadas hasta integrar valoración.
- Causas de salida y devolución a proveedor validadas; rechazo de cantidades que dejarían saldo negativo; consulta de la carga histórica demo preservada.
- 120 pruebas aprobadas. Recorrido de navegador con ajuste 30→28, carga 28→31, salida 31→30 y cancelación que invalida la revisión abierta; cuatro capturas sin errores JavaScript.

## 0.7.0 — 12 de noviembre de 2026

- Conteos físicos por total de producto o lote: cantidades declaradas, referencia consistente de existencias, diferencias, versiones e historia auditada.
- Guardar y finalizar no generan ajustes. Movimientos posteriores de los productos contados impiden finalizar hasta comprobar físicamente y guardar una versión nueva.
- Protección de conteos finalizados, autor del registro, precisión, alcances superpuestos y unidades de productos ya contados.
- Lotes con saldo, procedencia y alertas de vencido/próximo según umbral configurado; detección de existencias controladas sin lote y filtro de agotados. Inventario consulta cantidades sin costos/márgenes.
- 108 pruebas automatizadas y recorrido de navegador con tres capturas; los ajustes autorizados y la valoración continúan pendientes.

## 0.6.0 — 12 de noviembre de 2026

- Proveedores con mantenimiento por gestión, desactivación, versión y auditoría; consulta de Inventario.
- Recepciones pendientes con documento, cantidades recibidas/aceptadas, equivalencias históricas, costo explícito por unidad base y lotes/vencimientos.
- Revisión inmutable de contenido y versión por otra cuenta de gestión. Cambiar la recepción invalida revisiones; cambios de catálogo/proveedor también bloquean una revisión anterior.
- Aprobación y entrada única en una transacción con trazabilidad de autorización, detalle, lote y costo. Reintentos concurrentes recuperan la misma operación.
- Rechazos y borradores no alteran existencias. Costo vacío sigue desconocido y bloquea confirmación ordinaria de lo aceptado; aceptación parcial exige motivo.
- 99 pruebas aprobadas, incluidos T02/T10, fallo tardío, concurrencia, permisos, lotes y capacidad numérica. Recorrido de navegador con cinco capturas y rechazo real de revisión obsoleta.
- La salida por venta de productos con costo conocido o lotes sigue pendiente de integrar con valoración; se mantiene su bloqueo explícito.

## 0.5.0 — 12 de noviembre de 2026

- Cuentas comerciales: alta, edición, desactivación, roles, permiso de caja y restablecimiento de contraseña; no se eliminan identidades históricas.
- Gestión del Propietario y delegación explícita al Administrador, sin facultad delegada para modificar Propietarios o volver a delegar.
- Protección del último Propietario activo, del operador con turno abierto y de formularios desactualizados. Cada edición revoca sesiones anteriores.
- Cambio de contraseña propia con comprobación de la actual y cierre de sesiones; auditoría sin contraseñas ni hashes.
- Acceso temporal autorizado/revocable por Propietario, limitado a diagnóstico de integridad de lectura, sin rol comercial; vigencia comprobada en cada acceso.
- Quinta cuenta ficticia `propietario`; incorporación conservadora a las bases demo anteriores. El modo sin demostración conserva su inicialización privada.
- 83 pruebas aprobadas; seis comprobaciones en navegador sin errores JavaScript, con tres capturas.

## 0.4.0 — 12 de noviembre de 2026

- Configuración versionada de nombre, dirección, contacto, fondo habitual, alerta de vencimiento y datos de emisor/impresora previstos.
- Los cambios conservan comprobantes y fondos de turnos abiertos; se aplican a nuevas operaciones.
- Ediciones obsoletas rechazadas, auditoría transaccional y permisos de gestión comprobados.
- 71 pruebas automatizadas aprobadas; recorrido de navegador sobre una base aislada con cinco comprobaciones y tres capturas.
- Fiscalidad, impresión física y medios adicionales siguen sujetos a integración y validación.

## 0.3.0 — 12 de noviembre de 2026

- Catálogo operativo para gestión: categorías, productos, desactivación, presentaciones/equivalencias, códigos y precios con vigencias.
- Códigos con ceros iniciales y rechazo de reasignación de códigos desactivados; unidades y equivalencias usadas se conservan.
- Control de ediciones desactualizadas y auditoría de cambios en la misma transacción.
- Precios inmediatos o programados, intervalos adyacentes y comprobantes anteriores conservados (T01).
- Límite de acceso aplicado solo a intentos de contraseña, sin consumir intentos al consultar el formulario. Mensaje y plazo de reintento ante 429.
- 65 pruebas automatizadas y recorrido de navegador aislado: alta completa, venta, cambio de precio, historia conservada y nuevo precio efectivo.
- Promociones, impuestos y venta con lotes/costos conocidos permanecen en los hitos posteriores.

## 0.2.0 — 12 de noviembre de 2026

### Base de datos

- Corrección del mapeo de claves compuestas de PromocionProducto y ProductoImpuesto, con persistencia y recuperación de múltiples asociaciones.
- Creación atómica y migración del esquema 1 al 2, copia consistente previa y conservación de datos. No se recrean bases existentes ni se sobrescriben versiones desconocidas.
- Contrato de estructura y mapeo de las 39 tablas, 499 columnas y 103 FK; comprobación física, relacional, de importes y de hashes de comprobantes.
- Dos vistas de lectura para existencias y resumen de caja; saldos en enteros escalados/centavos sin tablas mutables de saldo.
- 21 triggers para vigencias de precios, orígenes compatibles y protección de comprobantes, detalles, pagos, caja, cierres, auditoría y cantidades de inventario. Tres índices adicionales.
- Diagnóstico de solo lectura con `--db-check`, actualización explícita con `--db-upgrade` e iniciador Revisar_Base_Datos.cmd.
- Inicialización sin demostración con contraseña suministrada y cuenta inicial de Propietario; sin catálogo ni ventas ficticias.

### Validación

- 50 pruebas de integración y base de datos, incluidos migración con ventas existentes, venta posterior a la migración, reversión de migración fallida, claves compuestas y conservación de historia.
- Base local actualizada al esquema 2. Se verificaron por resumen criptográfico los registros anteriores de sus 39 tablas; solo se añadió el evento de actualización.
- Comprobación de una distribución publicada e informes en docs/evidencias/base_datos.

### Alcance

La copia previa de migración es un control técnico local. El módulo de respaldo externo, restauración autorizada y cuarentena de colas de RF09 continúa pendiente. Los permisos, autorizaciones/versiones, cálculos de negocio y reglas entre múltiples registros siguen requiriendo el servicio transaccional.

## 0.1.0 — 12 de noviembre de 2026

Primera versión registrada en GitHub, correspondiente al avance funcional iniciado el 11 de noviembre.

### Funcionalidades

- Acceso local con cuentas, roles, permiso operativo e invalidación de sesiones.
- Apertura y cierre de caja propia con fondo real, esperado, contado y motivos de diferencias.
- Venta en efectivo por unidad, peso manual e importe, cotización autoritativa y revisión ante cambios.
- Confirmación transaccional de venta, pago, caja, inventario, auditoría y solicitud de impresión.
- Recuperación de una confirmación incierta y protección ante reintentos concurrentes sin duplicar cobros.
- Ventas del turno, comprobantes conservados, consulta de existencias y bitácora por rol.
- Modelo de 39 tablas, documentación del avance, matriz y capturas reales con datos ficticios.

### Registro y comprobación

- Git y repositorio remoto carlosfaj/MEGA-Minisuper.
- Política persistente de commit/push por funcionalidad terminada y por versión estable.
- Compilación y pruebas en GitHub Actions; etiquetado de versiones aprobadas en main.
- 23 pruebas automatizadas de integración/validación y dos recorridos de navegador aprobados en el avance inicial. La comprobación de publicación se registra en GitHub Actions.

### Alcance

Versión estable para el recorrido implementado y su demostración académica. Recepciones, autorizaciones, anulaciones, impresión física, valoración, gestión completa y continuidad permanecen pendientes. No acredita aceptación por el negocio ni implantación comercial.
