# Manual técnico de MEGA

Este manual permite instalar, verificar, mantener y atender incidencias de la versión 0.21.0 de MEGA. Está dirigido al responsable técnico y al Propietario que autoriza cambios de operación. La aplicación es web local para una computadora; el servicio y los datos permanecen en ese equipo.

Versión 0.21.0. Equipo Frijol Devs. Grupo 3M1-SIS-S. Edición del 14 de noviembre de 2026.

La versión dispone de 223 pruebas automatizadas aprobadas y ensayos aislados de navegador. La demostración, los controladores virtuales y el servidor SMTP local de prueba no acreditan instalación en otro equipo, aceptación del negocio, impresión física, fiscalidad ni objetivos de recuperación con volumen real.

[TOC]

## Arquitectura y componentes

La presentación utiliza Razor Pages con recursos estáticos locales. Los servicios de aplicación validan permisos, versiones y reglas de negocio; EF Core persiste mediante SQLite. Venta, autorización y regularización confirman sus efectos comerciales junto con auditoría en transacciones. Los agentes de impresión, correo y respaldo trabajan después de las solicitudes que preparan su información. En la Tabla 1, las rutas de aplicación parten de src/Mega.Web; las de pruebas y herramientas parten de la raíz del repositorio.

| Componente | Responsabilidad | Ubicación de referencia |
|---|---|---|
| Páginas Razor | Formularios, consulta y protección antifalsificación | Pages |
| Servicios | Venta, valoración, recepciones, inventario, anulaciones, reportes y continuidad | Services |
| Persistencia | Modelo EF, contrato, diagnóstico, esquema y migraciones | Data |
| Pruebas | Casos automatizados con bases separadas | tests/Mega.Tests |
| Seguimiento | Criterios y avance calculado | Tracking/proyecto.json |
| Construcción | Publicación, inicio y empaquetado | tools y tools/distribution |

El modelo de procedencia conserva 39 tablas, 499 columnas y 103 claves externas. El esquema operativo es 2. Se conserva el DDL y JSON originales y se agregan restricciones y vistas mediante migración explícita; no se utiliza EnsureCreated para sustituir el contrato.

## Versiones y requisitos

| Elemento | Versión o condición de esta entrega |
|---|---|
| SDK de construcción | .NET 10.0.403 fijado en global.json |
| Aplicación y persistencia | ASP.NET Core 10 y EF Core SQLite 10.0.14 |
| Ejecución distribuida | Windows x64 con .NET 10.0.14 incluido |
| Inicio del paquete | Windows PowerShell 5.1 y perfil de usuario con carpeta de datos escribible |
| Navegador ensayado | Microsoft Edge en el equipo de desarrollo |
| PDF y Excel | PDFsharp 6.2.4 y ClosedXML 0.105.0 |
| Correo | MailKit 5.0.0 con validación TLS |
| Infraestructura local | Puerto disponible, espacio para datos y copias, y política de respaldo autorizada |

La ejecución desde fuentes requiere SDK y restaurar dependencias cuando no estén disponibles. El ZIP de ejecución incluye el entorno y se probó sin dotnet accesible en PATH y con DOTNET_ROOT inexistente. Las dependencias están fijadas en archivos de bloqueo. El tamaño observado del paquete de ensayo fue aproximadamente 61.4 MiB; la capacidad de datos y medios debe dimensionarse con el negocio.

El núcleo no descarga recursos de interfaz ni necesita Internet tras instalarse. SMTP y una futura descarga de dependencias son usos de red diferentes. La revisión de NuGet del 14 de noviembre de 2026 no encontró vulnerabilidades declaradas; esta consulta no garantiza ausencia de vulnerabilidades futuras.

## Instalación del paquete

1. Obtener el ZIP de una revisión registrada y su archivo SHA-256. Conservar la referencia del commit y comprobar la huella antes de extraer.
2. Extraer todo el contenido en una carpeta local. Conservar completa la carpeta App, que incluye Licencias, junto al iniciador y paquete.json.
3. Detener cualquier instancia anterior. Ejecutar **Iniciar_MEGA.cmd** con el mismo perfil de Windows que conservará los datos.
4. Esperar el aviso de servicio y abrir https://localhost:7248. El iniciador rechaza un puerto ocupado antes de crear datos.
5. Verificar acceso, permisos, estado de caja y consultas. En una demostración nueva, las existencias son ficticias y no existen ventas.

El paquete usa por defecto %LOCALAPPDATA%\MEGA\Demostracion. Cambiar de carpeta el programa no cambia esa ubicación. No importa automáticamente la base que utiliza el desarrollo ni una base de otro perfil. Las cuentas ficticias se describen en el Manual de usuario.

El iniciador crea un certificado para localhost y sus direcciones de bucle local. Protege el material con DPAPI CurrentUser y utiliza un PFX cifrado con contraseña aleatoria durante la ejecución. No instala confianza en el sistema; el navegador puede presentar su aviso local. Un cierre forzado puede dejar un PFX cifrado residual. Una credencial protegida no puede trasladarse sin revisión a otro perfil.

Para un ensayo aislado se admite el iniciador PowerShell con **-DataRoot RUTA_COMPLETA -Port 7366**. La ruta debe ser completa y el puerto admitido estar libre. No utilices una base de operación para ejecutar herramientas que preparan ventas o simulan restauraciones.

## Datos y archivos que deben conservarse

| Elemento | Función | Conservación |
|---|---|---|
| mega-demo.db o base configurada | Registros, historia y efectos comerciales | Mantener mediante copias consistentes de SQLite |
| carpeta con nombre de base y sufijo .archivos | Reportes, secretos protegidos, salidas PDF y preservaciones | Conservar junto a la base |
| Recuperacion.json | Fase de recuperación, sesiones y numeración | No borrar ni sustituir para forzar el acceso |
| Preservaciones | ZIP del estado anterior a recuperar | Custodiar con sus huellas; no sobrescribir |
| HTTPS en la carpeta del iniciador | Certificado local protegido por el perfil | Conservar o tratar mediante intervención supervisada |
| Programa y paquete.json | Archivos distribuibles y procedencia | Mantener completos y separados de datos |

Reportes y secretos usan referencias de nombre controladas. Las credenciales SMTP se guardan protegidas fuera de las columnas de configuración; la base contiene la referencia. Los reportes enviados conservan su archivo exacto y SHA-256. Las salidas de Microsoft Print to PDF administradas se ubican junto a la base.

No copiar únicamente un archivo .db mientras la aplicación escribe. Un archivo WAL puede contener cambios confirmados. No borrar WAL, SHM, la base ni el control de recuperación para resolver un bloqueo. El mecanismo de respaldo obtiene una instantánea consistente y conserva los archivos asociados.

## Ejecución desde las fuentes

Clona la revisión correspondiente y comprueba VERSION y global.json. Desde la raíz del repositorio, restaura con **dotnet restore MEGA.sln --locked-mode**, compila con **dotnet build MEGA.sln -c Release --no-restore** y ejecuta las pruebas antes de modificar una instalación.

El iniciador del repositorio utiliza dotnet run y la base de demostración del proyecto. El paquete tiene un iniciador diferente y almacena datos en el perfil; ambas ubicaciones no deben confundirse. Una segunda instancia sobre la misma base se rechaza aunque se intente utilizar otro puerto.

| Ajuste | Uso previsto |
|---|---|
| MEGA__DatabasePath | Ruta completa de base en configuración por entorno |
| MEGA__DemoData | Selección explícita de demostración; no cambiar el modo de una base existente |
| MEGA__ProjectPlanPath | Fuente del tablero cuando se mantiene externamente al programa |
| MEGA__PrintAgentEnabled | Control del agente en un ensayo aislado |
| MEGA__MailAgentEnabled | Control del agente para evitar contactos durante pruebas de interfaz |
| MEGA__BackupAgentEnabled | Control de la pauta automática en pruebas |

La aplicación no habilita una base con datos ficticios bajo el modo real ni permite demostración fuera de Development o Testing. Una base nueva sin demostración exige MEGA_BOOTSTRAP_PASSWORD de al menos 12 caracteres, crea un Propietario inicial y no agrega productos ni ventas ficticios. Ese modo requiere preparación y aceptación propias; el paquete entregado inicia demostración. No guardar una contraseña real en scripts versionados ni en el historial de consola.

## Configuración y control de acceso

Los roles comerciales son Propietario, Administrador, Cajero e Inventario. El permiso de caja es adicional y se verifica en el servidor. La administración de cuentas por Administrador exige delegación expresa; el Propietario conserva sus facultades reservadas. Soporte temporal solo dispone de diagnóstico de integridad de lectura y no sustituye autorización del Propietario.

Se utiliza cookie segura, HttpOnly y SameSite Strict, protección antifalsificación, contraseñas con hash y validación de credenciales en cada solicitud. El inicio de sesión limita intentos por dirección local. Cambiar contraseña, permisos o versión de credenciales invalida sesiones anteriores. El servicio rechaza conexiones de origen que no sean de bucle local y añade encabezados de seguridad.

Cada formulario relevante compara la versión o huella revisada. Una aprobación requiere otra cuenta facultada y el mismo contenido y saldo. No se habilita la responsabilidad directa del Propietario pendiente de validación. Los rechazos y fallos HTTP críticos se auditan después de revertir la operación, sin registrar cuerpos, contraseñas ni secretos.

Antes de una operación real, verificar cuentas, unidades, equivalencias, costos, configuración del negocio, fondo, vencimientos, periféricos y pauta. La moneda es NIO y el desplazamiento de hora es UTC-6 provisional. El sistema no define fiscalidad ni habilita transferencia o tarjeta por introducir datos del emisor.

## Integridad comercial y valoración

La confirmación de venta registra detalle, pago, caja, inventario, costos históricos, auditoría y solicitud original de impresión de manera atómica. La clave de operación recupera un resultado ya confirmado y rechaza reutilización con otro contenido. Después de una respuesta incierta, se recupera esa operación; no se ejecuta una venta nueva como reparación.

Las recepciones y solicitudes preparadas no afectan saldos antes de aprobar. Las anulaciones conservan el original y su cierre previo. La reversión de mercadería y la entrega efectiva de dinero son efectos separados y documentados. Los gastos alimentan resultados sin inventar una salida de caja.

La política confirmada es promedio ponderado móvil por producto en unidad base, ordenada por confirmación de operaciones. Usa seis decimales de costo y conserva estado conocido o desconocido. Un costo desconocido no se convierte en cero; costo cero declarado conserva significado. Los costos de venta quedan congelados antes de su salida. Los lotes disponibles se asignan por vencimiento más próximo y luego identidad, con restricciones de fecha y saldo.

Inventario no recibe valoración monetaria ni márgenes en consultas o exportaciones. Los reportes de ventas excluyen anuladas y los de caja conservan cobros y devoluciones efectivos; sus alcances deben explicarse al conciliar. Si falta costo, el resultado global no se publica como utilidad cero.

## Agentes y resultados externos

| Agente | Reserva y ejecución | Tratamiento de incertidumbre |
|---|---|---|
| Impresión | Consulta cada 5 segundos; guarda reserva e intento antes del controlador; reserva de 3 minutos | Una reserva vencida o envío sin resultado se vuelve incierto; no se reenvía automáticamente |
| Correo | Consulta cada 10 segundos; conserva adjunto, huella y Message-ID; reserva de 3 minutos | Pérdida de respuesta tras transmisión exige revisión, no reintento ciego |
| Respaldo | Revisa pauta y cierres cada 30 segundos; ejecución serializada | Registra fallo visible y conserva copias anteriores |

Impresión requiere nombre exacto de equipo instalado y habilitación. Un trabajo conserva su impresora histórica. Microsoft Print to PDF produce un archivo administrado y no prueba papel entregado; OneNote no se admite como destino automático. La aceptación del sistema operativo y la entrega física son registros diferentes.

SMTP exige STARTTLS o TLS desde la conexión con validación de certificado. No existe opción de interfaz para omitirla. Un fallo conocido previo al envío o rechazo temporal explícito admite como máximo tres intentos con esperas de dos y cuatro minutos. La falta de configuración conserva solicitudes. Un cambio de servidor o usuario exige nueva credencial.

Los resultados externos no forman parte de la transacción de venta. Si una respuesta llega después de una resolución manual, se conserva la decisión y se registra la referencia; no se sobrescribe para fingir una certeza que no existía. La aplicación no garantiza una única impresión física ni lectura de un correo; controla intentos y exige resolución de resultados inciertos.

## Respaldo operativo

El Propietario autoriza destino existente fuera del almacén operativo, identifica el medio y decide las opciones periódica, por cierre o previa a actualización. Gestión puede generar una copia manual. El intervalo propuesto no activa una política sin autorización y los objetivos de continuidad dependen de medios y ensayos reales.

La copia contiene SQLite, reportes congelados, secretos protegidos y salidas PDF administradas referenciadas. El manifiesto registra aplicación, esquema, momento, cantidades de filas, últimos identificadores, tamaños y SHA-256. Se comprueba el ZIP completo, sus archivos, rutas admitidas, integridad física, relaciones y punto recuperable antes de marcarlo Comprobada.

El formato 2 incorpora continuidad de numeración y preservaciones anteriores como archivos opacos; se admite lectura del formato 1. Se limita cantidad y tamaño de archivos y se rechazan rutas no admitidas, duplicados y contenido alterado. No se sobrescriben ni eliminan copias anteriores automáticamente. Si el medio falta, se conserva el último registro comprobado y se documenta el fallo actual.

La fila de ejecución dentro de su propia instantánea puede aparecer En_proceso porque el archivo precede al registro final. Durante restauración, el manifiesto y el registro externo comprobados concilian esa ejecución. No se modifica el ZIP para ocultar esa diferencia.

## Restauración y reapertura

La pantalla exige Propietario, contraseña actual, copia registrada comprobada e identidad activa coincidente en ella. La revisión compara el estado lógico actual; los cambios posteriores obligan a comprobar el punto de nuevo. Los conteos de registros no reemplazan el examen de novedades sobre registros existentes.

1. El sistema bloquea solicitudes nuevas y espera a que terminen peticiones y agentes. Si no obtiene la pausa en dos minutos, no aplica la copia.
2. Conserva una preservación comprobada del estado actual con autorización y huella. Puede advertir archivos previamente ausentes o alterados; conserva los disponibles y no se presenta como copia operativa apta para restaurar.
3. Prepara la copia fuera de la base activa, aplica controles y cuarentena y comprueba integridad. La instalación se realiza mediante la operación de respaldo de SQLite hacia la base destino.
4. Cierra sesiones anteriores y deja operación pausada para conciliación del Propietario. No reproduce automáticamente las operaciones posteriores al punto: permanecen en la preservación.
5. Tras documentar inventario, caja, novedades y trabajos externos, el Propietario autoriza reapertura. La cuarentena se conserva y periféricos/pauta requieren nueva autorización.

Los identificadores máximos y la secuencia de folios anteriores se conservan fuera de la base y se reservan antes de insertar. Los saltos por intentos fallidos son posibles y no prueban una venta perdida. Conservar Recuperacion.json es necesario para esa continuidad. No ejecutar versiones anteriores a 0.20.0 después de restaurar.

Las cuentas comerciales coincidentes conservan contraseña, activación y permisos actuales para evitar revivir credenciales o permisos revocados. Cuentas sin coincidencia quedan inactivas; accesos temporales recuperados se revocan y revisiones pendientes se invalidan. Todos los trabajos de impresión/correo recuperados quedan retenidos, incluso los anteriormente terminados, con su estado anterior y vínculo a la intervención.

## Fallos e intervención técnica

| Síntoma | Respuesta permitida | Acción que debe evitarse |
|---|---|---|
| Resultado externo incierto | Revisar equipo o destinatario y registrar resolución | Reenvío automático sin comprobar |
| Archivo de reporte alterado | Conservarlo y revisar huella, origen y copia | Sustituirlo silenciosamente por un reporte nuevo |
| Medio de copia ausente | Conectar medio, revisar permisos y comprobar copia | Borrar la última copia válida |
| Intervención interrumpida | Permitir reconocimiento y reversión comprobada al iniciar | Borrar Recuperacion.json o WAL |
| Conciliación pendiente | Continuar como Propietario y documentar novedades | Forzar el modo normal en archivos |
| Base dañada o identidad ausente | Custodiar original y copias para intervención supervisada | Sobrescribir para hacer funcionar la pantalla |

El control de continuidad se escribe y sincroniza antes de reemplazarse atómicamente. Tolera breves bloqueos de lectura de Windows con reintentos acotados; si persisten, rechaza la reserva y no inserta la operación. Una segunda instancia sobre la misma base se rechaza.

Si falla la preparación o instalación, se intenta volver a la preservación comprobada y se conserva la evidencia. Si no puede comprobarse la reversión, el núcleo permanece pausado. Al reiniciar una intervención en aplicación se reconoce la autorización ya registrada y se revierte; una conciliación pendiente sigue bloqueando operación. Una reapertura ya confirmada antes de interrumpir el control se reconoce como tal.

El acceso público **/Recuperacion/Estado** muestra la pausa sin consultar la base. La recuperación de interfaz requiere una base actual legible y coherente; una base físicamente dañada, pérdida del almacén o identidad ausente necesita tratamiento técnico supervisado. No se acredita recuperación frente a corte eléctrico o daño físico con los ensayos de interrupción de proceso.

## Diagnóstico y mantenimiento

Para una lectura sin modificar la base, utilizar **Revisar_Base_Datos.cmd** o el modo **--db-check** con la ruta y modo correctos. El diagnóstico revisa integridad física, claves externas, contrato del modelo, saldos y hashes. Healthy distingue integridad de UpToDate, que indica esquema vigente. Soporte temporal puede realizar el diagnóstico de lectura mediante su pantalla autorizada.

Las actualizaciones de esquema se ejecutan explícitamente al iniciar o con **--db-upgrade**, con copia previa cuando corresponde. No editar la base para simular una migración aprobada. En 0.21.0 no se cambia el esquema 2 ni se modifica el DDL/JSON de procedencia.

Supervisar fallos visibles de colas, copia más reciente comprobada, espacio, permisos del perfil y eventos de bitácora. No borrar auditoría, historial o credenciales referenciadas como limpieza ordinaria. La política de retención y medios alternados debe ser aceptada por el negocio.

## Construcción y actualización

Para preparar fuentes, usar **tools/package.ps1**. Solo empaqueta archivos versionados y rechaza rutas operativas. Para el paquete Windows usar **pwsh -File tools/build-distribution.ps1** desde una revisión registrada. Un árbol sin registrar solo admite ensayo privado bajo TestResults con AllowDirty, identificado como tal en el manifiesto.

El paquete registra commit, versión, SDK, entornos incluidos y huellas de archivos y añade SHA-256 externo. La huella permite detectar cambios respecto del archivo comprobado; no constituye una firma digital del editor. Las licencias y avisos del entorno y bibliotecas acompañan la distribución.

Antes de actualizar, confirmar ausencia de trabajos en curso, generar copia autorizada, detener el programa y conservar su carpeta. Extraer el nuevo programa aparte y utilizar el mismo perfil y carpeta de datos. Si no inicia, detener y volver a una versión compatible conservando los datos. No restaurar una base únicamente para reemplazar el programa. Los cambios futuros de esquema requieren su procedimiento específico.

GitHub comprueba dependencias fijadas, sintaxis PowerShell, auditoría de dependencias, kanban, compilación, 223 pruebas y preparación del paquete. Solo tras éxito registra la etiqueta si no existe; las etiquetas existentes no se mueven. Conserva artefactos de ejecución y resultados durante 30 días. El repositorio y las copias del responsable conservan la historia independiente de esa retención temporal.

## Referencias operativas

La fuente de esta edición es MEGA-Minisuper, código 0.21.0, junto con sus documentos de Base de datos, Valoración, Inventario integrado, Impresión, Correo, Respaldos, Restauración y Distribución. Los resultados acumulados se consultan en la Matriz de validación y los TRX conservados. El Manual de usuario contiene los procedimientos de operación.

La documentación oficial de publicación .NET está en https://learn.microsoft.com/en-us/dotnet/core/deploying/. La API de copia SQLite se describe en https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup. Estas referencias amplían decisiones técnicas; el código y los ensayos de la versión determinan el comportamiento aquí documentado.
