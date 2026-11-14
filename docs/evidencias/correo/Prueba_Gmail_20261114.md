# Prueba manual de correo con Gmail — 14 de noviembre de 2026

Complemento de los ensayos históricos de [Correo](../../Correo.md), sobre MEGA 0.21.0. El usuario configuró una cuenta Gmail para sus pruebas y solicitó reportes desde la aplicación. La comprobación posterior consultó la base local de demostración en una transacción SQLite de solo lectura y calculó SHA-256 de los adjuntos conservados. No generó nuevos envíos ni modificó la cola.

Fuente verificable: [registro extraído y comprobaciones](resultado-gmail-20261114.json). Se omiten direcciones de correo, credenciales, referencias de secretos y contenido de reportes para poder versionar la evidencia. La base y los adjuntos operativos permanecen fuera de Git.

## Configuración observada

Configuración vigente ID 3, versión 3: `smtp.gmail.com`, puerto 587, `starttls`, credencial configurada. El servidor está habilitado. La existencia de la referencia protegida se consultó como booleano; no se abrió ni descifró la credencial. La captura aportada por el usuario mostraba previamente los campos completos pero el agente sin habilitar; el estado persistido posterior acredita la configuración activa.

## Casos y resultados

Horas del 14 de noviembre de 2026 en Guatemala (UTC−06:00); el JSON conserva los tiempos originales UTC.

| Caso | Solicitud | Esperado | Obtenido | Estado |
|---|---|---|---|---|
| GM-01 Configuración real | Configuración 3 | SMTP habilitado con TLS y credencial protegida | Host Gmail, 587, STARTTLS y referencia de credencial presente | Comprobado |
| GM-02 Reporte de resultados pendiente | 1 | Procesar el PDF conservado al habilitar el servicio | Un intento; `Aceptada_SMTP` a las 09:14:05.795 | Aprobado a nivel SMTP |
| GM-03 Resolución y nueva solicitud de inventario | 2 → 3 | Conservar reporte y distinguir la nueva solicitud | Solicitud 2 `Resuelta`, resolución `nueva_solicitud` a las 09:12:42.146; solicitud 3 con misma huella/fecha de generación y distinto Message-ID; aceptada a las 09:14:15.029 | Comprobado; nueva solicitud aceptada por SMTP |
| GM-04 Primer reporte de caja | 4 | Enviar PDF conservado | Un intento; `Aceptada_SMTP` a las 09:19:15.945 | Aprobado a nivel SMTP |
| GM-05 Segundo reporte de caja | 5 | Enviar PDF conservado | Un intento; `Aceptada_SMTP` a las 09:21:05.302 | Aprobado a nivel SMTP |
| GM-06 Integridad de adjuntos | 1–5 | Archivo presente, cabecera PDF y SHA-256 igual al registrado | Cinco referencias comprobadas, cuatro archivos distintos; solicitudes 2 y 3 conservan el mismo reporte | Aprobado |

Los cuatro intentos registrados terminan en `respuesta_final`, con resultado `aceptada_SMTP` y sin error registrado. La solicitud 2 no tiene intento SMTP; su resolución no se presenta como rechazo, fallo de autenticación ni envío incierto comprobado.

## Límites y pendientes

- Se acredita comunicación con SMTP externo real y aceptación registrada por MEGA. No se conservó un código numérico de respuesta SMTP; no se infiere uno.
- No hay confirmación de recepción en bandeja, carpeta de spam, lectura ni apertura del adjunto recibido. Esas comprobaciones corresponden al destinatario.
- Todos los adjuntos observados en esta prueba son PDF. El envío real de Excel queda pendiente; su implementación y ensayos anteriores no equivalen a esta comprobación externa.
- No se ejecutó nuevamente la suite automatizada. El recuento histórico permanece en 223; estos casos manuales no se suman como nuevas pruebas automatizadas.
- Es una prueba personal con datos de demostración, no aceptación comercial ni validación del personal del negocio.

## Repetición manual

Con MEGA abierto y conexión a Internet, Gestión genera un reporte, selecciona PDF o Excel, revisa el destinatario y solicita el envío. En **Correo y reportes** revisa el intento y su resultado después del ciclo del agente (cada diez segundos). El destinatario comprueba recepción y abre el adjunto para registrar ese resultado por separado. Ante `Incierta`, comprobar antes servidor y destinatario y resolver desde la interfaz; no asumir que repetir garantiza un único mensaje.
