# Manual de usuario de MEGA

Ventas y control del minisúper

Versión oficial 0.23.2 | Edición del 18 de noviembre de 2026

Este manual te guía para vender, controlar existencias y administrar el minisúper. Busca la tarea que necesitas y sigue los pasos con tu propia cuenta. Los nombres en **negrita** corresponden a opciones de la aplicación.

## Encuentra tu tarea

[CONTENIDO]

## Qué puedes hacer con tu cuenta

| Cuenta | Trabajo principal |
|---|---|
| Cajero con permiso de caja | Abrir su turno, vender, consultar ventas y cerrar su caja. |
| Inventario | Consultar existencias y lotes; preparar recepciones, conteos y solicitudes. |
| Administrador | Gestionar catálogo, revisar solicitudes de otra cuenta, consultar resultados y generar respaldos. La gestión de cuentas exige delegación. |
| Propietario | Administrar cuentas, delegaciones, respaldo y recuperación. También necesita permiso adicional para operar caja. |

En esta guía, **gestión** significa Administrador o Propietario.

**Alcance.** La entrega funciona en Windows con datos ficticios y cobro en efectivo. Transferencia y tarjeta están deshabilitadas. La impresión física y la aceptación del negocio siguen pendientes. Usa el Manual técnico para instalar o actualizar el programa.

Equipo Frijol Devs | Grupo 3M1-SIS-S

[SALTO]

## Entrar y preparar la jornada

### Iniciar la demostración

1. Esta entrega documental incluye un **enlace GitHub**, sin ejecutable. Pide al responsable técnico descargar la versión oficial **0.23.2** y preparar el **SDK .NET 10.0.403** siguiendo el Manual técnico.
2. Desde la raíz de las fuentes, el responsable inicia el modo local con **powershell.exe -NoProfile -File tools/start.ps1**. Conserva la ventana abierta y entra en **https://localhost:7248**.
3. Comprueba la dirección antes de continuar ante el aviso de certificado de desarrollo. Escribe tu usuario y contraseña y selecciona **Entrar a MEGA**.

El primer inicio crea datos ficticios si no existe la base de demostración; los siguientes conservan su estado. No borres la base para reiniciar una jornada.

**Solo si dispones del paquete ejecutable amplio:** usa su iniciador en **03_Base_Datos_y_Ejecucion/Ejecutable_Oficial**. Su opción **08_Demostracion_Funcional** crea ensayos separados. Esas carpetas no forman parte de esta entrega documental.

**Cuentas de demostración:** cajero, cajero2, inventario, admin y propietario. Contraseña ficticia común: **MegaDemo2026!**. No las uses para una operación comercial.

### Orientarte en Inicio

El inicio de Inventario muestra mínimos, vencimientos y trabajo propio. Administrador ve revisiones y caja; Propietario, resultados del día y excepciones. Selecciona un pendiente para abrir su detalle. Recarga la página para actualizar la información.

### Abrir tu turno de caja

1. Comprueba fecha, estación, precios y avisos. Los importes se expresan en córdobas **C$**; la hora usa UTC-6.
2. Abre **Mi caja** y registra el **Fondo real recibido (C$)**. Explica cualquier diferencia con el fondo habitual.
3. Selecciona **Abrir mi turno** y comprueba el fondo guardado.

Solo puede existir un turno abierto en la estación. Si aparece otro, identifica al responsable y completa su cierre. El fondo real será la base del efectivo esperado.

**Al terminar:** usa **Salir**. En **Mi contraseña** puedes cambiar la clave; debe tener entre 12 y 200 caracteres y exige volver a entrar.

[SALTO]

## Vender y cerrar caja

### Confirmar una venta

**Antes:** debes tener permiso de caja y un turno propio abierto.

1. Abre **Nueva venta** y busca por nombre o código. Comprueba producto, presentación y precio.
2. Indica cantidad, peso manual o importe, según la modalidad que admita el producto. Selecciona **Añadir a la compra**.
3. Revisa las líneas y selecciona **Continuar al cobro**. Comprueba los precios, descuentos, impuestos y total que muestra el sistema.
4. Introduce el **Efectivo recibido (C$)**. Revisa el cambio y selecciona **Confirmar venta** una vez.
5. Comprueba el folio y el comprobante. Entrega el cambio y revisa el estado de impresión si corresponde.

**Resultado:** la venta confirmada registra el cobro y descuenta existencias. La impresión se gestiona por separado.

Escribe **38.50**, sin C$ ni separadores de miles. Las unidades indivisibles no admiten fracciones. Si cambió un precio o saldo, actualiza la preparación y vuelve a revisar. Ante una discrepancia de inventario, comprueba físicamente lo solicitado antes de declararlo.

**Si no recibes respuesta al confirmar:** selecciona **Recuperar resultado de la venta** en la misma preparación. Conserva esa operación hasta conocer su resultado. Empezar otra venta puede duplicar el cobro.

### Consultar y cerrar el turno

1. En **Ventas del turno**, abre la venta que necesitas y conserva su folio.
2. En **Mi caja**, revisa el efectivo esperado y cuenta el dinero disponible.
3. Registra el efectivo contado. Si hay diferencia, explica el motivo y confirma el cierre.

Si hubo movimientos después de abrir el cierre, recarga y cuenta de nuevo. El cierre conserva sus importes históricos.

**Ejemplo con datos iniciales:** fondo C$480 más una venta de C$86 produce efectivo esperado de C$566. Si recibes C$100, entregas C$14 de cambio. Un conteo final de C$560 genera diferencia de -C$6. El fondo no es ingreso por ventas.

[SALTO]

## Imprimir y entregar comprobantes

La venta queda guardada aunque falle la impresora. Abre **Impresión y entregas** y comprueba el estado antes de solicitar otra salida.

| Estado | Qué hacer |
|---|---|
| Pendiente | Revisar que el equipo y la impresión estén habilitados. Esperar antes de crear otra solicitud. |
| Aceptada | Comprobar el papel. Registrar entrega física solo cuando haya ocurrido. |
| Fallida | Avisar a gestión para comprobar la causa y autorizar un reintento cuando corresponda. |
| Incierta | Comprobar papel y conexión; revisar la cola si se usó un controlador de Windows. Solicitar una resolución de gestión. |
| En_cuarentena | Esperar la conciliación de los trabajos recuperados. |
| Resuelta | Leer la decisión registrada antes de emitir otra copia. |

**Copia de un comprobante.** Conserva el folio y los importes, añade número de copia y no vuelve a cobrar ni descontar inventario. Primero resuelve los trabajos pendientes o inciertos del mismo comprobante. Gestión puede cerrar sin reenviar, emitir una copia expresa o registrar una contingencia ya entregada, con motivo y evidencia.

### Configurar una impresora

Administrador o Propietario abre **Configuración del negocio**, elige el destino disponible, marca **Habilitar impresión automática de nuevos comprobantes** y guarda. Un cambio de destino no redirige los trabajos anteriores.

Para la **MP58-16 Bluetooth**, empareja la impresora con la computadora donde se ejecuta MEGA. Identifica su puerto COM **saliente** en Windows y elige el destino, por ejemplo **MP58-16 Bluetooth (COM7)**. COM7 es un ejemplo; usa el puerto real.

La conexión necesita Bluetooth clásico SPP y ESC/POS. Si no aparece un puerto compatible, consulta al responsable técnico. La unidad física del negocio todavía requiere prueba de papel, acentos, original y copia.

**Comprueba el resultado.** Una aceptación de Windows o del puerto Bluetooth no demuestra que salió papel. Microsoft Print to PDF genera un archivo. Abrir MEGA en un celular no conecta su impresora al servidor.

[SALTO]

## Recibir mercadería y consultar existencias

### Registrar una recepción

**Responsables:** Inventario o gestión prepara; otra cuenta Administrador o Propietario revisa. Gestión debe haber registrado el proveedor activo.

1. Abre **Recepciones** e indica referencia interna única, proveedor, documento y fecha.
2. Añade presentación, cantidad recibida y aceptada, costo por unidad base y, si corresponde, lote y vencimiento.
3. Selecciona **Guardar preparación**. Revisa equivalencias y explica los rechazos parciales. Una cantidad aceptada exige costo conocido; vacío no equivale a cero. Rechaza mercadería vencida.
4. Selecciona **Enviar versión guardada a revisión**.
5. Otra cuenta de gestión revisa la versión exacta y decide con motivo. Comprueba la confirmación y la entrada por la cantidad aceptada.

**Resultado:** guardar un borrador no aumenta existencias; aprobar la recepción registra la entrada. Recibir mercadería no registra un pago de caja.

Solo el preparador puede editar o enviar su recepción. Si cambia la información o queda desactualizada, guarda y envía una versión nueva.

### Consultar existencias y lotes

En **Existencias**, revisa cantidad, unidad base, mínimo y alertas. En **Lotes y vencimientos**, consulta saldo, fecha y procedencia. Si falta el umbral de vencimiento, gestión debe configurarlo.

Los lotes disponibles se consumen primero por vencimiento más próximo. El sistema impide vender lotes vencidos. Consultar una alerta no retira mercadería ni cambia existencias.

**Si hay saldo sin lote en un producto que controla vencimiento:** solicita conciliación. No inventes un lote para ocultar la diferencia. Inventario consulta cantidades sin costos ni márgenes.

Para corregir diferencias físicas, usa el procedimiento de conteos y solicitudes de la página siguiente.

[SALTO]

## Contar y solicitar movimientos de inventario

### Registrar un conteo físico

1. Abre **Conteos físicos** e indica referencia única y producto.
2. Elige el alcance: total del producto o un lote concreto. Registra la cantidad observada y la observación.
3. Declara la comprobación física únicamente si la realizaste. Guarda y finaliza el conteo completo.

**Resultado:** el conteo queda disponible para revisión; guardar o finalizar no ajusta existencias. Si hubo movimientos después de contar, vuelve a comprobar, guardar y finalizar.

No mezcles total y lotes del mismo producto ni dupliques el alcance. Si el producto está distribuido en lotes, el ajuste requiere conteos por lote.

### Preparar una solicitud

En **Ajustes y salidas**, elige el movimiento que corresponde.

| Tipo | Información necesaria | Efecto al aprobar |
|---|---|---|
| Carga | Cantidad, costo conocido o desconocido, motivo, evidencia y lote cuando aplique. | Entrada en unidad base. |
| Ajuste | Conteo finalizado y vigente. El sistema calcula la diferencia. | Regularización del alcance contado. |
| Salida justificada | Cantidad, causa, evidencia y proveedor si es devolución. | Salida autorizada. |

1. Guarda la solicitud y envía su versión a revisión.
2. Otra cuenta de gestión comprueba cantidades, saldos, lotes, costos y motivo.
3. Consulta el resultado. Si la información cambió, corrige y vuelve a enviar.

**Reglas de revisión.** Nadie aprueba su propia solicitud. Preparar, enviar, rechazar o cancelar no mueve existencias. La aprobación no admite saldo negativo y no registra pagos de caja.

En una solicitud manual, dejar lote vacío indica saldo no asignado, no el total del producto. La evidencia es una referencia a un documento o fotografía: el formulario no guarda ese archivo adjunto; el responsable debe custodiarlo.

[SALTO]

## Mantener el catálogo y anular ventas

### Mantener productos y configuración

Administrador o Propietario abre **Catálogo**. Crea el producto, su presentación y código; después registra un precio positivo con vigencia. Dar de alta un producto no crea existencias.

Conserva los ceros iniciales de los códigos. Un código desactivado no se reasigna. Si un producto o presentación ya tiene movimientos, sus unidades y equivalencias conservan la historia; utiliza otra presentación o producto para una unidad distinta.

En **Promociones e impuestos**, registra condiciones y vigencias y comprueba el resultado antes de confirmar una venta. En **Configuración**, revisa datos del negocio, fondo habitual y umbral de vencimiento. Cambiar el fondo habitual afecta aperturas futuras, no turnos existentes.

Las tasas y los datos del emisor deben validarse con el negocio. El comprobante generado no acredita validez fiscal. Si otro usuario guardó antes, recarga y revisa la información vigente.

### Solicitar la anulación de una venta

1. Identifica la venta por su folio e inicia la solicitud de anulación.
2. Registra motivo y evidencia. Indica si corresponde reincorporar mercadería y declara la comprobación física.
3. Envía a otra cuenta de gestión para revisión. Comprueba la decisión registrada.

**Resultado:** una anulación aprobada conserva el comprobante original y aplica las cantidades, lotes y costos de esa venta. Sin reincorporación no se crea una entrada de mercadería.

### Registrar la devolución de efectivo

La aprobación de la anulación deja el dinero pendiente hasta registrar su entrega. El solicitante o gestión con permiso de caja debe tener turno propio abierto, saldo suficiente y evidencia de la devolución.

Registra el importe **pagado por la venta**, no el billete recibido antes de dar cambio. Si el turno original cerró, utiliza el turno actual de quien entrega el dinero; el cierre anterior se conserva. Consulta la regularización hasta resolver el pendiente.

[SALTO]

## Registrar gastos y consultar reportes

### Registrar un gasto

Gestión abre **Gastos operativos**, indica fecha, concepto, importe positivo y referencia cuando corresponda, y guarda. El registro alimenta resultados; no representa una retirada de efectivo de caja.

Una corrección queda enlazada con el registro anterior. Para anular, indica motivo. Anular una corrección no reactiva el gasto sustituido.

### Consultar y descargar un reporte

1. Abre **Reportes**, selecciona el tipo y define los filtros.
2. Comprueba periodo y alcance antes de interpretar las cifras.
3. Descarga en PDF o Excel. Si excede el límite, reduce los filtros.

| Reporte | Cómo interpretarlo |
|---|---|
| Ventas | Usa condiciones históricas y excluye ventas anuladas. |
| Inventario | Muestra existencias actuales al generar. Inventario no accede a costos ni márgenes. |
| Caja | Refleja cobros y devoluciones en su fecha efectiva; conserva cierres. |
| Resultados | Combina ingresos, costos y gastos. Si falta algún costo, el margen o resultado global es **No calculable**. |

No interpretes **No calculable** como cero. La pantalla muestra hasta 250 filas; las descargas admiten hasta 5000. Una anulación puede aparecer de forma distinta en ventas y caja por su fecha y efecto.

### Enviar un reporte por correo

Gestión genera el reporte, indica destinatario y solicita el envío en PDF o Excel. El adjunto conserva el archivo exacto generado. Antes del primer envío, configura **Correo y reportes** con servidor, puerto, remitente y TLS; introduce la credencial si se requiere.

**Aceptada_SMTP** indica que el servidor aceptó el envío, sin demostrar recepción o lectura. Ante **Incierta**, comprueba con servidor y destinatario antes de resolver o solicitar otro envío expreso. Un fallo confirmado anterior a la aceptación puede admitir reintento.

Si cambias servidor, usuario o perfil de Windows, revisa la credencial. Sin correo configurado, las ventas y el inventario local siguen disponibles.

[SALTO]

## Administrar cuentas y proteger los datos

### Administrar accesos

Propietario abre **Cuentas** para crear o desactivar accesos, asignar permisos y delegar gestión de cuentas a un Administrador. Desactivar conserva la historia. Antes de retirar permisos de caja, completa el turno abierto.

El soporte temporal necesita motivo y duración y solo permite diagnóstico de integridad de lectura. No autoriza restauración. En **Bitácora**, gestión consulta quién realizó una acción y su resultado.

### Generar un respaldo

1. Propietario abre **Respaldos**, autoriza una carpeta existente fuera del almacén de operación e identifica el medio y la pauta.
2. Gestión selecciona **Generar y comprobar respaldo**.
3. Revisa el estado **Comprobada**. Si falla, conecta el medio y selecciona **Comprobar de nuevo**. Conserva la copia anterior.

Una carpeta del mismo disco sirve para ensayo; para proteger los datos se necesita un medio externo y una pauta acordada. Custodia las copias con acceso restringido.

### Recuperar una copia

**Solo Propietario.** La recuperación reemplaza el estado actual por el punto elegido; las novedades posteriores deben conciliarse.

1. En **Recuperación**, elige una copia comprobada y selecciona **Comprobar y revisar punto**.
2. Revisa fecha y novedades que quedarían fuera. Indica motivo y contraseña actual y reconoce la revisión.
3. Selecciona **Autorizar y recuperar copia**. Espera la pausa, preservación y comprobación.
4. Vuelve a entrar como Propietario. Descarga el estado anterior preservado y revisa las novedades posteriores; no se reproducen automáticamente.
5. Comprueba inventario, caja, cobros, devoluciones, cuentas y trabajos externos. Documenta los pendientes y completa las cuatro declaraciones de conciliación.
6. Selecciona **Autorizar reapertura**. Revisa y reautoriza impresora, correo y pauta; resuelve los trabajos retenidos individualmente.

La reapertura conserva la cuarentena. Si aparece una pausa por revisión técnica o no puedes completar la conciliación, conserva los archivos y avisa al responsable técnico.

[SALTO]

## Resolver problemas frecuentes

| Situación | Acción inmediata |
|---|---|
| MEGA no abre | Comprueba que la ventana del programa siga abierta y que usas la dirección indicada. Conserva el mensaje y avisa al responsable técnico si persiste. |
| Usuario bloqueado temporalmente | Espera el minuto indicado antes de volver a intentar. No compartas otra cuenta. |
| La venta no muestra resultado | Usa **Recuperar resultado de la venta** en la misma preparación. |
| Precio o saldo desactualizado | Recarga, revisa la información vigente y prepara de nuevo. |
| Hay otra caja abierta | Identifica al responsable y completa el cierre. |
| Impresión o correo inciertos | Comprueba el resultado real y pide resolución de gestión antes de otro envío. |
| No aparece el medio de respaldo | Conecta el medio correcto y comprueba la copia anterior. |
| El sistema está pausado | Propietario debe continuar la conciliación o pedir revisión técnica. |

### Información para pedir ayuda

Conserva la hora, el mensaje exacto, la cuenta utilizada, la tarea en curso y el folio o solicitud si existe. Si tomas una captura, evita incluir contraseñas o información personal innecesaria.

**Durante una caída:** utiliza la contingencia acordada con el negocio y conserva referencias de ventas, cobros, mercadería y comprobantes. Al volver, comprueba qué quedó registrado antes de ingresar una operación otra vez.

### Acceso desde celular

En las fuentes, **Iniciar_MEGA.cmd** muestra la dirección HTTPS para el mismo Wi-Fi. Usa esa dirección; si cambia, consulta al responsable técnico. La impresión se realiza desde Windows en el servidor.

Esta entrega no incluye ejecutable. El paquete ejecutable opcional solo abre localmente. No sustituyas localhost por una IP sin preparar el acceso.

**Vigencia:** esta guía corresponde a la versión oficial 0.23.2. La propuesta visual 0.24.2 permanece en una rama alternativa. Para instalación, mantenimiento y recuperación técnica, consulta el Manual técnico.
