# Impresión, copias y entregas

La versión 0.15.0 integra un agente local con la cola de Windows. Gestión consulta las impresoras instaladas en **Impresión y entregas**, escribe el nombre exacto en Configuración y habilita la impresión automática. La opción permanece deshabilitada en la base de trabajo hasta configurarla expresamente. Desde 0.19.0 Microsoft Print to PDF genera archivos únicos en `<nombre-base>.archivos/Impresiones`, junto a la base; OneNote no admite este envío automático.

Cada venta crea su solicitud original en la misma transacción comercial. El agente funciona cada cinco segundos y reserva un trabajo y su intento en la base antes de llamar al controlador. La reserva dura tres minutos. El comprobante se produce desde el contenido histórico y su huella, con texto de 40 columnas y páginas según el área imprimible; el tamaño de papel lo aporta la configuración de Windows.

Windows puede aceptar un trabajo sin que salga papel. Por eso se distingue Aceptada de la entrega física declarada. No se crean venta, pago, caja ni inventario al imprimir, reintentar o emitir copias. El comprobante interno sigue sin validez fiscal acreditada.

## Fallos y decisiones

- **Fallida:** consta que el controlador no aceptó el trabajo, o la revisión detectó venta anulada/huella inválida. Gestión puede revisar y reintentar el mismo trabajo cuando corresponde; no cambia su impresora histórica.
- **Incierta:** hubo interrupción después de iniciar el envío o venció la reserva sin resultado confirmado. No se reenvía automáticamente ni se permite reintentar ciegamente el mismo trabajo.
- **Resuelta:** gestión comprobó cola y papel y dejó motivo/evidencia. Puede cerrar sin reenviar, emitir una copia explícita advirtiendo riesgo de duplicado o registrar una contingencia manual ya entregada. Los trabajos en cuarentena requieren la conciliación de recuperación.

Una copia conserva folio, importes y huella y añade su número. Solicitarla de nuevo con la misma clave recupera el mismo trabajo. Los trabajos pendientes/inciertos deben resolverse primero. La entrega impresa requiere confirmación explícita y evidencia y se recupera sin duplicarla. La contingencia registra responsable y autorización de gestión. Cajero gestiona únicamente comprobantes de su turno abierto; Inventario no accede a esta función. Un resultado tardío conserva la resolución existente y agrega su referencia a la auditoría.

## Verificación

175 pruebas acumuladas, incluidas T07, concurrencia, fallo conocido, resultado incierto, reserva vencida, copia sin cobro, entrega no inventada, permisos y reversión completa de una contingencia ante fallo tardío. TRX en `evidencias/impresion/impresion.trx`.

`tools/printing-check.cjs` probó en base aislada configuración, venta de C$38, original y copia aceptados por el controlador real **Microsoft Print to PDF**, una sola venta/cobro y caja C$538. Los PDF `original.pdf` y `copia-1.pdf` conservan folio MEGA-00000002 y total C$38; se extrajo y revisó visualmente su contenido. Las capturas muestran aceptación sin una entrega física ficticia y el formato móvil sin desbordamiento.

La implementación utiliza las API oficiales de [enumeración](https://learn.microsoft.com/en-us/windows/win32/printdocs/enumprinters), [contexto de impresora](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-createdcw), [inicio de documento](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-startdocw) y [texto Unicode](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-textoutw). El envío ocurre fuera de la solicitud de venta. El equipo físico, ancho/papel, corte y entrega real requieren ensayo con la impresora del negocio; la prueba virtual no los acredita.
