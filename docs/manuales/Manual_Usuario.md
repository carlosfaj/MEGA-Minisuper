# Manual de usuario de MEGA

Este manual explica cómo operar la versión 0.21.0 de MEGA en una computadora local. Está dirigido a las personas que realizan ventas, controlan inventario o administran el minisúper. Cada procedimiento indica qué revisar antes de confirmar y qué resultado debe quedar registrado.

Versión 0.21.0. Equipo Frijol Devs. Grupo 3M1-SIS-S. Edición del 14 de noviembre de 2026.

La demostración utiliza productos, importes y comprobaciones ficticios. Las pruebas técnicas no acreditan aceptación del personal, fiscalidad, impresión en papel ni recuperación con medios reales. El cobro habilitado es efectivo; transferencia y tarjeta permanecen deshabilitadas.

[TOC]

## Acceso y responsabilidades

Abre https://localhost:7248 cuando el servicio local esté iniciado. Escribe tu usuario y contraseña y selecciona **Entrar a MEGA**. Cada persona debe usar su propia cuenta para conservar la responsabilidad de los cambios. El menú muestra las funciones permitidas; escribir otra dirección no concede permisos.

| Cuenta o función | Operaciones permitidas | Restricción principal |
|---|---|---|
| Cajero con permiso de caja | Abrir y cerrar su turno, vender, consultar sus ventas y gestionar comprobantes de su turno abierto | No administra catálogo ni aprueba solicitudes |
| Inventario | Consultar existencias y lotes, preparar recepciones, conteos y solicitudes | No consulta costos o márgenes en reportes ni autoriza sus solicitudes |
| Administrador | Gestión de catálogo, configuración, revisiones, gastos, reportes, correo y copias | Cuentas requieren delegación; no restaura ni reabre el núcleo |
| Propietario | Gestión, cuentas, delegación, soporte temporal, pauta de respaldo y recuperación | Caja requiere permiso adicional; tampoco aprueba una solicitud propia |
| Soporte temporal | Diagnóstico de integridad de lectura durante el periodo autorizado | No tiene rol comercial ni acceso a restauración |

En una demostración nueva existen **cajero**, **cajero2**, **inventario**, **admin** y **propietario**; la contraseña ficticia común es **MegaDemo2026!**. No son credenciales para una operación comercial. La cuenta Propietario de la demostración no tiene caja habilitada inicialmente.

Para cambiar tu contraseña, abre **Mi contraseña**, comprueba la actual y confirma la nueva. La nueva debe tener entre 12 y 200 caracteres. El cambio invalida las sesiones anteriores y exige entrar nuevamente. Usa **Salir** al terminar; no compartas la sesión con otra persona. Si se alcanza el límite de acceso, espera el minuto indicado antes de intentarlo de nuevo.

## Inicio de la jornada

1. Comprueba que la estación, fecha y hora mostradas correspondan al trabajo. La aplicación usa un desplazamiento provisional UTC-6 y moneda NIO, indicada con C$.
2. Revisa avisos de existencias y vencimientos y confirma que los precios y condiciones estén vigentes.
3. Abre **Mi caja**, registra el **Fondo real recibido (C$)** y explica cualquier diferencia respecto del fondo habitual.
4. Selecciona **Abrir mi turno**. Revisa el fondo guardado antes de comenzar a vender.

Solo puede existir un turno abierto en la estación. El fondo habitual es una referencia; el efectivo esperado parte del fondo real que registraste. Si aparece otra caja abierta, identifica a su responsable y completa el cierre correspondiente; no abras otra cuenta para evadirlo.

## Preparar y confirmar una venta

1. Abre **Nueva venta** y busca el producto por nombre o código. Comprueba la presentación y el precio.
2. Indica cantidad por unidad, peso manual o importe únicamente cuando el producto admita esa modalidad. Revisa la cantidad resultante y selecciona **Añadir a la compra**.
3. Revisa todas las líneas y selecciona **Continuar al cobro**. El servidor calcula precios, promociones, impuestos configurados, lotes y disponibilidad.
4. En **Efectivo recibido (C$)** registra lo entregado por el cliente. Comprueba total y cambio antes de seleccionar **Confirmar venta**.
5. Verifica el folio y el comprobante. Confirma la entrega del cambio y gestiona la impresión según el procedimiento de comprobantes.

Escribe los números con punto decimal, sin símbolo C$ ni separador de miles; por ejemplo, 38.50. Las unidades indivisibles no admiten fracciones. Un precio o saldo que cambió después de revisar obliga a actualizar la preparación. La asignación por lote utiliza primero el vencimiento más próximo disponible y no permite consumir lotes vencidos. Una advertencia de discrepancia exige comprobar físicamente y documentar lo solicitado; no se debe confirmar una existencia que no se comprobó.

**Ejemplo de demostración nueva.** Con fondo C$500, una leche de C$38 y recibido C$50, el cambio es C$12 y el esperado de caja queda C$538. Este ejemplo supone catálogo inicial, sin promociones ni impuestos adicionales. Las condiciones que muestre la revisión de la venta prevalecen si la configuración cambió.

Si la respuesta se pierde después de confirmar, utiliza **Recuperar resultado de la venta** en la misma preparación. Conserva esa operación hasta conocer su resultado. El sistema recupera la venta ya guardada con su misma clave; volver a comenzar una venta diferente puede registrar otro cobro.

## Cierre y consulta de ventas

Desde **Ventas del turno**, abre el comprobante requerido y conserva su folio para cualquier consulta posterior. Sus datos corresponden al momento de confirmación; un cambio posterior de precio o del negocio no los sustituye.

Para cerrar, abre **Mi caja**, revisa el esperado y registra el efectivo realmente contado. Si existe diferencia, escribe su motivo y confirma el cierre. Una pantalla de cierre desactualizada se rechaza cuando hubo nuevos movimientos; revisa y cuenta otra vez. El turno cerrado no se vuelve a calcular con operaciones posteriores.

**Ejemplo de diferencia.** Fondo real C$480 más venta neta C$86 produce esperado C$566. Si el conteo es C$560, la diferencia es -C$6 y requiere motivo. El fondo inicial no es ingreso por ventas.

## Comprobantes y fallos de impresión

La venta queda guardada aunque falle la impresora. Consulta **Impresión y entregas** antes de decidir otra salida de papel. Gestión configura el nombre exacto de la impresora y habilita el agente desde Configuración; cambiar el nombre no convierte automáticamente un trabajo histórico en un trabajo para otro equipo.

| Estado | Qué significa | Qué hacer |
|---|---|---|
| Pendiente | Solicitud guardada que todavía no se aceptó | Revisar habilitación y equipo; evitar crear otra solicitud por impaciencia |
| Aceptada | Windows aceptó el trabajo | Comprobar papel y registrar entrega física solo si ocurrió |
| Fallida | Existe un fallo identificado | Gestión comprueba la causa y decide si corresponde reintentar |
| Incierta | No se conoce el resultado final | Comprobar cola y papel; no reenviar ciegamente |
| En_cuarentena | Trabajo recuperado y retenido | Esperar conciliación y resolver individualmente |
| Resuelta | Gestión registró una decisión y su evidencia | Consultar la resolución antes de emitir otra copia |

La copia conserva folio e importes y agrega un número de copia; no genera otra venta ni salida de inventario. Antes de solicitarla, resuelve los trabajos pendientes o inciertos del mismo comprobante. Gestión puede cerrar sin reenviar, emitir una copia expresa o registrar una contingencia manual ya entregada, con motivo y evidencia. La aceptación de Windows no demuestra entrega de papel ni validez fiscal.

[PAGEBREAK]

## Comprobante de la venta de ejemplo

La Figura 1 muestra el comprobante de la venta utilizada en el ensayo de distribución. El total y el cambio deben revisarse antes de entregar el comprobante al cliente.

![Figura 1 Detalle del comprobante de una leche por C$38 con recibido C$50 y cambio C$12. Fuente captura del ensayo de distribución 0.21.0 con datos ficticios.](../evidencias/distribucion/01_venta_paquete.png#xywh=270,257,711,653)

[PAGEBREAK]

## Catálogo y condiciones comerciales

Administrador o Propietario abre **Catálogo** para administrar categorías, productos, presentaciones, equivalencias, códigos y precios. Primero crea el producto, luego su presentación y código, y registra un precio positivo con su vigencia. Dar de alta un producto no crea existencias.

Los códigos conservan ceros iniciales y no se reasignan después de desactivarlos. Productos con historia conservan unidad y escala; una presentación utilizada conserva su equivalencia. Para otra unidad se requiere otro producto o presentación. Desactivar conserva la historia y evita nuevas operaciones que requieran una entidad activa.

En **Promociones e impuestos**, gestión define las condiciones admitidas y sus vigencias. Comprueba el resultado en la revisión del cobro antes de aplicarlo a una jornada. Las tasas y datos del emisor requieren validación del negocio; la aplicación no inventa una tasa ni acredita fiscalidad.

En **Configuración**, guarda datos del negocio, fondo habitual, umbral de vencimiento y equipo de impresión. El fondo nuevo se propone en aperturas futuras y no cambia el dinero de un turno existente. Si otro usuario guardó antes, recarga y revisa el formulario actualizado.

## Proveedores y recepciones

Gestión registra un proveedor activo. Inventario o gestión abre **Recepciones** y prepara referencia interna única, proveedor, documento y fecha. En cada línea indica presentación, recibido, aceptado, costo por unidad base y, cuando corresponda, lote y vencimiento.

1. Selecciona **Guardar preparación**. Guardar un borrador no aumenta existencias.
2. Revisa equivalencias, aceptado y motivos de rechazo parcial. Lo aceptado exige costo conocido; un campo vacío no significa costo cero. No aceptes mercadería vencida.
3. Selecciona **Enviar versión guardada a revisión**.
4. Otra cuenta Administrador o Propietario abre la revisión exacta, verifica su contenido y decide con motivo.
5. Comprueba que la recepción quede confirmada y que aparezca una sola entrada por la cantidad aceptada.

Solo el preparador edita o envía su recepción. Cambiar el documento, proveedor, producto o versión puede invalidar una revisión abierta. Corrige y vuelve a enviar cuando sea necesario; no apliques una autorización anterior a datos nuevos. Recibir mercadería no registra por sí mismo un pago de caja.

## Existencias y lotes

En **Existencias** consulta cantidad, unidad base, mínimo y alertas. En **Lotes y vencimientos** revisa fecha, saldo y procedencia. Un umbral próximo a vencer sin configurar se informa como pendiente; no se presume un número de días.

La consulta no hace ajustes ni retira mercadería vencida. Un producto con control de vencimiento y saldo sin lote requiere conciliación; no se debe inventar un lote para ocultar esa diferencia. Inventario consulta esta información sin costos ni márgenes.

La valoración técnica usa promedio ponderado móvil en unidad base. Costos anteriores desconocidos permanecen identificados y no se convierten en cero. Las ventas conservan sus costos históricos aunque se registren nuevas entradas. La confirmación técnica de la política no sustituye el ensayo con datos y responsables del negocio.

## Conteos y solicitudes de inventario

En **Conteos físicos**, registra referencia única, producto, alcance total o lote concreto, cantidad físicamente observada y observación. Declara la comprobación física únicamente si la realizaste. Guarda y luego finaliza el conteo cuando su contenido esté completo.

Guardar o finalizar un conteo no ajusta existencias. Si hubo movimientos de esos productos o lotes después de contar, vuelve a comprobar, guardar y finalizar la información vigente. No mezcles total y lotes del mismo producto ni dupliques el alcance.

Desde **Ajustes y salidas**, prepara el movimiento adecuado:

| Tipo | Información que debes comprobar | Efecto al aprobar |
|---|---|---|
| Carga | Cantidad, costo conocido o desconocido, motivo, evidencia y lote cuando aplique | Entrada en la unidad base |
| Ajuste | Conteo finalizado vigente; diferencias calculadas por el sistema | Regularización de los alcances contados |
| Salida justificada | Cantidad, causa, evidencia y proveedor si es devolución | Salida autorizada, con costo histórico correspondiente |

Guarda y envía la versión a otra cuenta de gestión. Preparar, enviar, rechazar o cancelar no mueve existencias. El aprobador debe revisar cantidades, saldos, lotes y estado de costo. Un cambio posterior obliga a revisar de nuevo. Las solicitudes no admiten saldo negativo ni registran pagos de caja.

Para productos distribuidos en lotes, un ajuste requiere conteos por lote. Sin lote en una solicitud manual significa saldo no asignado y no el total del producto. La evidencia es la referencia a un documento o fotografía que debe custodiar el responsable; el formulario no almacena ese archivo como adjunto.

## Anular y devolver efectivo

Una anulación conserva el comprobante original y requiere solicitud, motivo, evidencia y aprobación por otra cuenta de gestión. El solicitante debe indicar si corresponde reincorporar mercadería y declarar la comprobación física. La aprobación utiliza cantidades, lotes y costos de la venta original.

Autorizar una anulación no demuestra que se entregó dinero. La regularización de efectivo queda pendiente hasta que el solicitante o una cuenta de gestión con permiso de caja registre la entrega efectiva desde su turno actual abierto, con evidencia y saldo suficiente. Se devuelve el importe pagado por la venta, no el billete recibido antes de dar cambio.

Si el turno original ya cerró, conserva su cierre. La devolución posterior se registra en la caja actual de quien entrega el dinero. Consulta el estado de regularización y deja identificadas las partidas pendientes. Si no se reincorpora mercadería, no se crea una entrada ficticia.

## Gastos y reportes

Gestión registra en **Gastos operativos** fecha, concepto, importe positivo y referencia cuando corresponda. El registro alimenta resultados y no representa una retirada de efectivo de caja. Corregir genera un registro enlazado y conserva el anterior; anular requiere motivo. Anular una corrección no reactiva el gasto previo.

En **Reportes**, selecciona tipo y filtros. Ventas utiliza condiciones históricas de ventas confirmadas; inventario representa existencias actuales al generar. Caja utiliza la fecha efectiva de cobros y devoluciones y conserva los cierres. Por eso una anulación puede tener efectos distintos en un reporte comercial y en caja.

El margen y resultado global aparecen **No calculable** cuando falta costo de alguna línea. No los interpretes como cero. Descarga PDF o Excel con el alcance y momento de generación indicados. La pantalla muestra hasta 250 filas y la descarga admite hasta 5000; si se supera el límite, reduce filtros. Inventario solo puede consultar y exportar información de inventario sin importes de costo o margen.

## Correo de reportes

Gestión prepara un reporte, indica destinatario y solicita el envío en PDF o Excel. El sistema conserva el archivo exacto que se generó. Cambios posteriores no sustituyen el adjunto de esa solicitud.

La configuración de **Correo y reportes** requiere servidor, puerto, remitente y seguridad TLS admitida. Si se requiere autenticación, introduce su credencial. El valor conservado no se muestra de nuevo; cambiar servidor o usuario obliga a proporcionarlo otra vez. En otro perfil de Windows o equipo se debe revisar y configurar nuevamente.

Sin configuración o conexión, el núcleo local sigue disponible. **Aceptada_SMTP** indica aceptación por el servidor, sin acreditar lectura. Ante **Incierta**, gestión comprueba servidor y destinatario y decide cerrar sin reenviar o solicitar otro envío expreso. Un fallo confirmado no aceptado puede admitir reintento. No confundas una nueva solicitud con la recuperación de una solicitud existente.

## Respaldar y recuperar

El Propietario abre **Respaldos**, autoriza una carpeta existente fuera del almacén de operación, identifica el medio y decide qué opciones automáticas habilitar. El intervalo propuesto no es una política aprobada por sí mismo. Una carpeta del mismo disco sirve para ensayo y no sustituye un medio externo.

Gestión selecciona **Generar y comprobar respaldo** y revisa el resultado. Solo **Comprobada** identifica una copia que superó huellas, archivos e integridad. Un fallo no borra la copia anterior; conecta el medio correcto y utiliza **Comprobar de nuevo**. Custodia las copias y sus archivos asociados con acceso restringido.

La recuperación está reservada al Propietario:

1. En **Recuperación**, selecciona una copia comprobada y pulsa **Comprobar y revisar punto**.
2. Revisa momento, cantidades y novedades que quedarían fuera del punto. Registra motivo y contraseña actual y reconoce esa revisión.
3. Selecciona **Autorizar y recuperar copia**. El sistema pausa operaciones, espera procesos, preserva el estado anterior y comprueba la copia aplicada.
4. Inicia sesión otra vez como Propietario. Descarga el estado anterior conservado y revisa las novedades posteriores; no se vuelven a registrar automáticamente.
5. Comprueba inventario, caja, cobros, devoluciones, turnos, cuentas y trabajos externos. Documenta el tratamiento de pendientes y completa las cuatro declaraciones.
6. Selecciona **Autorizar reapertura**. Después revisa y reautoriza impresora, correo y pauta, y resuelve individualmente los trabajos retenidos.

La Figura 2 identifica las cuatro revisiones que el Propietario debe documentar antes de reabrir.

![Figura 2 Revisiones de inventario caja novedades y trabajos externos antes de la reapertura. Fuente detalle de captura del ensayo aislado de restauración 0.20.0 con datos ficticios.](../evidencias/restauracion/03_conciliacion_pendiente.png#xywh=295,563,700,152)

La reapertura conserva la cuarentena; no dispara todos los trabajos recuperados. Las sesiones previas quedan invalidadas, los accesos temporales se revocan y las revisiones pendientes requieren nuevo envío. Se conserva la numeración alcanzada para evitar reutilizar folios. No elimines controles de recuperación para forzar una reapertura. Si aparece pausa por revisión técnica, consulta **Estado de recuperación** y conserva los archivos para el responsable técnico.

## Administración y seguimiento

El Propietario administra **Cuentas**, puede delegar facultad de cuentas a un Administrador y autoriza soporte temporal con motivo y duración. Una cuenta con turno abierto conserva los permisos necesarios hasta cerrar; se protege al último Propietario activo. Desactivar no borra historia ni libera el nombre para reutilizarlo.

Gestión consulta **Bitácora** por cuenta, fecha, acción u objeto. Los registros explican decisiones y rechazos; no se editan desde la operación. En **Avance del proyecto**, consulta el porcentaje y las tareas por área. El porcentaje cuenta criterios terminados y no equivale a aceptación del negocio ni a tiempo restante.

## Atención de incidencias

| Situación | Acción inmediata | Información que debes conservar |
|---|---|---|
| Confirmación de venta sin respuesta | Recuperar la misma preparación | Folio si existe y resultado de recuperación |
| Impresión o correo inciertos | Consultar el resultado real antes de otro envío | Solicitud, intento, comprobación y decisión |
| Pantalla desactualizada | Recargar, revisar y enviar la versión vigente | Motivo del rechazo y cambios observados |
| Medio de respaldo ausente | Conectar el medio y comprobar la copia anterior | Ejecución y detalle del fallo |
| Núcleo pausado por recuperación | Continuar conciliación autorizada o revisión técnica | Intervención y preservación |
| Aplicación detenida | Conservar datos y avisar al responsable | Momento, mensaje y operación en curso |

El personal debe acordar la contingencia manual, los responsables y la posterior conciliación antes de operar comercialmente. Durante una caída, conserva referencias de ventas, cobros, mercadería y comprobantes conforme al procedimiento aprobado; no supongas que el sistema registró lo que no pudo confirmarse.

## Fuentes y vigencia

Versión documentada 0.21.0 del repositorio MEGA-Minisuper. Para instalación y mantenimiento consulta el Manual técnico. Las comprobaciones reales pendientes y su evidencia necesaria se identifican en la Matriz de validación.
