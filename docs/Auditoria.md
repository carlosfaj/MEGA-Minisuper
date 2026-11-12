# Auditoría de operaciones y rechazos

Desde 0.14.0 la bitácora incluye rechazos HTTP de reglas de negocio, formularios inválidos, permisos, sesión, antifalsificación y límite de acceso. Un fallo inesperado genera un evento de fallo con correlación para consultar el registro del servicio. Los eventos de operaciones exitosas permanecen dentro de su transacción, con sus referencias y autorizaciones.

El rechazo se registra en un contexto separado después de revertir la operación. No se vuelve a ejecutar venta, pago, caja ni inventario. La escritura de estos eventos se serializa y reintenta únicamente ante bloqueo temporal de SQLite. Las pruebas simultáneas encontraron un bloqueo entre escrituras independientes; se corrigió sin relajar la comprobación de los dos eventos.

Se conserva cuenta autenticada, fecha, acción, ruta, resultado y clave de correlación. El contexto desconocido se identifica como tarea automática. No se guarda cuerpo, consulta, contraseña, cookies ni credenciales. Los accesos fallidos de una cuenta conocida conservan su evento específico sin duplicarlo; salir o cambiar correctamente la contraseña no se etiqueta como rechazo.

Gestión puede filtrar la bitácora por resultado y buscar acción, entidad o clave. Cada fila permite consultar el detalle, referencia y autorización. Se muestran los últimos 100 eventos del filtro; el historial completo se conserva y sus restricciones impiden editarlo o borrarlo.

## Verificación y límites

162 pruebas acumuladas aprobadas, incluidas venta rechazada sin efectos, permisos, antifalsificación, formulario y cuenta desconocida sin secretos, solicitudes simultáneas con responsables separados y fallo tardío con reversión completa. `evidencias/auditoria/auditoria.trx` contiene los resultados individuales. El recorrido `tools/audit-check.cjs` comprobó rechazo visible, filtro y detalle en navegador sobre base aislada; captura y JSON en la misma carpeta.

Si el almacenamiento está indisponible y agota los reintentos, el servicio registra un error visible con correlación. No se afirma que una base averiada pueda guardar eventos. La aceptación de negocio y los ensayos reales de recuperación continúan pendientes en el kanban; T13 se verifica aquí para los módulos implementados y se amplía con cada nuevo módulo.
