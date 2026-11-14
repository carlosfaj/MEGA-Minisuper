# Restauración y conciliación — 0.20.0

Implementación comprobada el 14 de noviembre de 2026. RF09/T15: recuperación con autorización expresa del Propietario, preservación previa, pausa de actividad, cuarentena y reapertura documentada. No se restauró la base de demostración habitual ni una base comercial.

## Procedimiento en pantalla

1. Iniciar sesión como **Propietario** y abrir **Recuperación**. Seleccionar una ejecución comprobada de Respaldos y mantener conectado su medio.
2. Pulsar **Comprobar y revisar punto**. Se comprueban SHA-256, manifiesto, archivos, esquema e integridad. Revisar el momento recuperado, los últimos identificadores y la comparación de registros. Los recuentos no describen por sí solos modificaciones de registros anteriores.
3. Registrar motivo, contraseña actual y reconocimiento de las novedades pendientes. La identidad activa del Propietario debe existir también en la copia. Si cambió la información desde la revisión, se exige comprobarla de nuevo.
4. Autorizar la recuperación. El sistema espera a que terminen solicitudes y agentes en curso, conserva el estado actual y prepara la copia fuera de la base activa. No solicita que se cierre manualmente un trabajo que ya está enviándose.
5. Iniciar sesión nuevamente como Propietario. La operación comercial permanece pausada. **Descargar estado anterior conservado** permite revisar el archivo comprobado de preservación; contiene información del sistema y debe custodiarse como un respaldo.
6. Conciliar inventario físico, caja, cobros/devoluciones/turnos y las novedades posteriores al punto. Registrar su tratamiento. No se vuelven a ejecutar ventas ni se fusionan automáticamente operaciones posteriores: permanecen en la preservación.
7. Confirmar las cuatro revisiones y **Autorizar reapertura**. La declaración queda vinculada al Propietario y a la intervención. No constituye aceptación del negocio cuando se trata de un ensayo ficticio.
8. Reautorizar la impresora, el correo y la pauta de respaldo según corresponda. Sus configuraciones de ejecución quedan desactivadas. Resolver los trabajos recuperados individualmente desde Impresiones/Correos, después de comprobar cola, papel, servidor y destinatario. La reapertura conserva su cuarentena.

Administrador puede generar/comprobar copias; la restauración y reapertura requieren Propietario en servidor. Soporte temporal conserva únicamente el diagnóstico de integridad de lectura y no puede restaurar por esta pantalla.

## Qué se conserva y qué cambia

- Se utilizan las tablas existentes `Restauracion`, colas, intentos, configuraciones y bitácora. No se modifica el DDL/JSON original ni el esquema 2.
- Se conserva un ZIP en `<base>.archivos/Preservaciones/<clave>.zip`, con huellas, base anterior y archivos asociados disponibles. La preservación admite advertencias explícitas por archivos previamente ausentes o alterados; conserva los bytes disponibles y **no se presenta como respaldo operativo apto para recuperación**. El respaldo seleccionado exige sus archivos válidos.
- Los respaldos nuevos usan manifiesto 2 e incluyen límites de numeración y preservaciones anteriores como archivos opacos. Se siguen leyendo respaldos de formato 1. Las preservaciones de intervención no incluyen otros ZIP de preservación, para evitar crecimiento recursivo. No se sobrescriben ZIP existentes.
- La copia activa se instala mediante la operación de respaldo de SQLite hacia la base destino, después de drenar procesos. No se borran archivos WAL ni se reemplaza una base abierta con una copia de archivos ordinaria.
- Los identificadores alcanzados antes de recuperar y la secuencia usada para folios se conservan fuera de la base y se reservan antes de insertar. Una venta posterior no reutiliza el identificador o folio de una venta preservada. Los fallos pueden dejar saltos legítimos. Esta protección exige conservar `Recuperacion.json` y usar esta versión o una posterior; no se admite ejecutar simultáneamente versiones anteriores sobre la misma base.
- Todas las sesiones anteriores quedan invalidadas. Para cuentas comerciales de identidad coincidente se conservan contraseña, activación y permisos actuales, evitando revivir credenciales o permisos revocados. Cuentas sin coincidencia quedan inactivas; altas posteriores a la copia no se recrean. Todos los accesos temporales de soporte recuperados se revocan. El Propietario revisa cuentas como parte de las novedades.
- Revisiones pendientes de recepción, inventario y anulación se invalidan; sus objetos reciben nueva versión y deben enviarse de nuevo. Las decisiones con efecto ya confirmado forman parte de la historia recuperada.
- Todos los trabajos de impresión/correo recuperados, incluidos los terminados, quedan `En_cuarentena`, con su estado anterior y vínculo a la restauración. Las reservas se eliminan; intentos en curso pasan a resultado incierto. No se reintenta el mismo trabajo retenido: una copia o solicitud nueva requiere decisión expresa y advierte del posible duplicado.
- La ejecución seleccionada se concilia con el manifiesto externo comprobado: su fila dentro de la copia puede estar aún `En_proceso`, porque el ZIP se generó antes del registro final. Otros respaldos interrumpidos quedan visibles como fallidos y requieren comprobación.

## Interrupciones y fallos

El control persistente `Recuperacion.json` registra fases y continuidad de numeración mediante escritura sincronizada y sustitución atómica de archivo. Los bloqueos breves de lectura de Windows se toleran con reintentos acotados; si persisten, se rechaza la reserva sin avanzar el contador ni insertar la operación. Dos pruebas usan bloqueos reales de archivo para comprobar ambos casos. Solo una instancia puede abrir esta base mediante la aplicación. Las solicitudes que usan base se registran antes de autenticar; los agentes mantienen su reserva durante todo el intento externo. Al recuperar se espera su finalización y se bloquea actividad nueva. Las solicitudes propias de recuperación también se reincorporan a ese control al terminar la fase exclusiva.

Si no se obtiene la pausa en dos minutos, no se aplica una copia. Si la preparación o instalación falla, se intenta revertir desde la preservación comprobada; se conserva el archivo y se registra el fallo. Una clave de intervención fallida no se vuelve a ejecutar ciegamente. Si la reversión no puede comprobarse, la aplicación permanece pausada y muestra `/Recuperacion/Estado` sin acceder a la base.

Al arrancar después de una interrupción durante la aplicación, se revierte desde la preservación bajo la autorización ya registrada. Una conciliación pendiente sigue pausada tras reiniciar. Si la reapertura ya quedó confirmada en la base antes de interrumpirse la escritura del control, se reconoce esa autorización confirmada. Si base y control no corresponden, o falta la base, se mantiene la pausa.

El recorrido requiere una base actual legible y coherente para autenticar y preservar. Una base físicamente dañada, identidad del Propietario ausente en la copia, pérdida del almacén o conflicto de preservaciones requiere intervención técnica supervisada; no se elimina información para forzar la reapertura. La protección frente a interrupciones de proceso no acredita un ensayo de corte eléctrico o fallo físico del disco.

## Evidencias y límites

- `tests/Mega.Tests/RecoveryTests.cs`: 15 casos dirigidos, incluidos estado recuperado, preservación, cuarentena, intentos en curso, numeración/folios, permisos/contraseña, revisión obsoleta, fallo de instalación, reversión al iniciar, falta de preservación, sesión, cuentas y revisiones pendientes.
- `tools/restore-check.cjs`: recorrido Edge con base aislada, puerto 7365 y confirmación `MEGA_RECOVERY_ISOLATED=1`. Conserva una venta, preserva la segunda, exige conciliación y emite la siguiente con identificador mayor. `tools/recovery-check.cjs` conserva su prueba original de respuesta perdida al confirmar venta.
- [Resultado y capturas](evidencias/restauracion/resultado.json), [suite completa](evidencias/restauracion/suite.trx) y [pruebas dirigidas](evidencias/restauracion/dirigidas.trx). Los ZIP/bases de ensayo y archivos protegidos permanecen fuera de Git.

El tiempo del recorrido ficticio se registra como observación local, no como cumplimiento del RTO de cuatro horas para datos y medios reales. Quedan pendientes volumen real, medio externo, corte eléctrico, equipo objetivo y conciliación/aceptación por responsables del negocio. No se enviaron mensajes a personas ni se imprimió papel durante este ensayo.
