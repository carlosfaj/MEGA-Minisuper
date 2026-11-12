# Correo de reportes

La versión 0.18.0 permite a Gestión preparar un reporte y solicitar su envío en PDF o Excel a una única dirección. Se guarda el archivo exacto, filtros, momento de generación, huella SHA-256 y Message-ID antes de enviarlo. Un reintento de la misma solicitud recupera la misma fila. Cambios posteriores en ventas, inventario o gastos no regeneran el adjunto solicitado; se puede descargar y verificar su huella.

## Configuración protegida

**Correo y reportes** contiene servidor, puerto, remitente, usuario y seguridad STARTTLS obligatorio o TLS desde conexión. No se admite una conexión sin TLS ni se omite la validación del certificado en operación. La configuración está versionada. Deshabilitarla deja los reportes pendientes y conserva la credencial protegida; el núcleo local no necesita Internet.

La credencial se cifra con DPAPI para el perfil actual de Windows. La base guarda una referencia, no su valor. No se devuelve al formulario ni se incluye en auditoría. Cambiar servidor o usuario exige introducirla de nuevo para impedir enviar una credencial conservada a otro destino. Al cambiar de equipo o perfil debe configurarse nuevamente; las referencias históricas se conservan.

Los adjuntos y secretos se guardan junto a la base en `<nombre-base>.archivos/Reportes` y `Secretos`, con referencias de nombre controladas. Esta ubicación permanece estable al actualizar el programa. Debe formar parte del respaldo y recuperación junto con la base. No se incluye en Git ni en paquetes de fuentes.

## Envío y estados

El agente consulta cada diez segundos, guarda reserva e intento antes de contactar SMTP y registra configuración usada, etapa y resultado. La reserva vence a los tres minutos; el transporte tiene tiempo límite. La aceptación SMTP significa aceptación por el servidor, no lectura ni entrega final.

- Conexión fallida antes de enviar o rechazo temporal explícito permiten hasta tres intentos, a los dos y cuatro minutos, con el mismo Message-ID y archivo. La falta de configuración conserva la solicitud sin contactar servidores.
- Rechazo permanente o credencial/TLS inválidos quedan Fallida para revisión. El archivo ausente o con huella distinta no se envía.
- Una interrupción durante envío, pérdida de respuesta final o reserva vencida quedan Incierta. No se reenvían automáticamente.
- Gestión revisa servidor/destinatario y deja motivo/evidencia. Puede cerrar sin reenviar, reintentar un fallo confirmado no aceptado o solicitar explícitamente un nuevo envío, advirtiendo posible duplicado. Este último conserva el archivo y recibe otro Message-ID, enlazado en auditoría. Cuarentena requiere conciliación de recuperación.

Un resultado tardío no sobrescribe una resolución manual. Preparación, decisiones e intentos no generan ventas, pagos ni movimientos de caja.

## Verificación y límites

200 pruebas acumuladas aprobadas en `evidencias/correo/correo.trx`. La regresión dirigida comprueba protección de credenciales, cambio de destino, archivo congelado, permisos, concurrencia, fallos tardíos, reintentos acotados, reserva vencida y resultado incierto. El transporte MailKit real se ejercitó contra un servidor TLS local de prueba: aceptación, rechazo 550 y desconexión tras DATA sin respuesta final; se decodificó y comparó el adjunto. El certificado temporal de esa prueba se admite únicamente mediante una fábrica de cliente del ensayo; no existe una opción de interfaz para omitir certificados en operación.

El navegador comprobó configuración ficticia sin exponer la credencial, deshabilitación, solicitud pendiente, descarga y huella idéntica después de un gasto nuevo, y móvil sin desbordamiento. Se mantuvo deshabilitado el agente de esa base de interfaz para no contactar servicios externos. Capturas y JSON en la misma carpeta. No se enviaron mensajes a personas ni se configura una cuenta SMTP real con estos ensayos; esa comprobación permanece pendiente con el negocio.
