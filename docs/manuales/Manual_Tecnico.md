# Manual técnico de MEGA

Instalación y mantenimiento del sistema

Versión oficial 0.23.2 | Edición del 18 de noviembre de 2026

Esta guía permite obtener y ejecutar las fuentes oficiales en Windows, conservar sus datos, diagnosticar fallos y actualizar la aplicación. Está dirigida al responsable técnico. El Propietario autoriza cuentas, pauta de respaldo, recuperación y reapertura.

## Encuentra el procedimiento

[CONTENIDO]

## Arquitectura esencial

MEGA es una aplicación web con **ASP.NET Core y Razor Pages**. Los servicios validan permisos y reglas de negocio; **EF Core con SQLite** persiste los registros. Impresión, correo y respaldo se procesan mediante agentes locales con historial de intentos.

El ejecutable aloja el servicio en Windows y el navegador presenta la interfaz. El núcleo funciona sin Internet después de instalarse; SMTP requiere conectividad al servidor configurado.

**Entrega documentada:** versión oficial 0.23.2, esquema 2. La propuesta visual 0.24.2 permanece separada. La instalación en el equipo definitivo, los periféricos físicos y la aceptación comercial siguen pendientes.

Equipo Frijol Devs | Grupo 3M1-SIS-S

[SALTO]

## Obtener y preparar el sistema

### Requisitos

| Elemento | Requisito de ejecución |
|---|---|
| Sistema | Windows x64; Windows PowerShell 5.1. |
| Entorno | SDK .NET 10.0.403 para las fuentes de esta entrega. El ejecutable amplio opcional incluye su entorno. |
| Perfil y disco | Carpeta de datos escribible y espacio para base, archivos y copias. Usa el mismo perfil para conservar secretos protegidos. |
| Navegador y puerto | Edge fue el navegador ensayado. Puerto local 7248 libre, o uno configurado expresamente. |
| Equipo de referencia | Mínimo planificado: x64 de cuatro núcleos, 8 GB de RAM y SSD de 240 GB. Rendimiento pendiente en la estación definitiva. |

### Obtener las fuentes oficiales

1. Abre **02_Repositorio_GitHub.txt** y sigue el enlace al [repositorio MEGA](https://github.com/carlosfaj/MEGA-Minisuper). Esta entrega contiene documentos y enlaces; no incluye ejecutable ni base operativa.
2. Descarga las fuentes oficiales de la revisión **649ecd1372855557ccb40cc7d43b3adc217a9fe1**, o clona el repositorio y selecciona esa revisión. Comprueba **VERSION = 0.23.2**; la rama **feature/frontend-branded** es una alternativa distinta.
3. Extrae o clona en una carpeta local. Instala el **SDK .NET 10.0.403** fijado en **global.json**. La descarga de fuentes y dependencias requiere Internet; el núcleo puede operar localmente después.
4. Desde la raíz con **MEGA.sln**, completa la preparación y el arranque de la página siguiente. El primer inicio crea la base ficticia si no existe; conserva los datos en los siguientes arranques.

### Paquete ejecutable opcional

**Solo si dispones del paquete amplio por separado:** conserva **App**, **Licencias**, **paquete.json** e iniciadores y ejecuta **Iniciar_MEGA.cmd** desde **03_Base_Datos_y_Ejecucion/Ejecutable_Oficial**. Usa **%LOCALAPPDATA%\MEGA\Demostracion** y escucha en localhost; **08_Demostracion_Funcional** permite un ensayo nuevo aislado. Esas carpetas no están en esta entrega documental.

Su certificado localhost se protege con **DPAPI CurrentUser** y un PFX cifrado temporal. Ningún iniciador instala confianza en Windows. El aviso del navegador se limita a la demostración; una operación real requiere certificados y confianza adecuados.

[SALTO]

## Ejecutar desde fuentes y habilitar la red

### Preparar las fuentes

La entrega oficial parte del commit **649ecd1372855557ccb40cc7d43b3adc217a9fe1**. Comprueba **VERSION**, **global.json** y los archivos de bloqueo antes de reconstruir. Ejecuta desde la raíz del repositorio:

```powershell
dotnet restore MEGA.sln --locked-mode
dotnet build MEGA.sln -c Release --no-restore
dotnet test MEGA.sln -c Release --no-build
```

| Dependencia | Versión fijada |
|---|---|
| SDK de construcción | .NET 10.0.403 |
| ASP.NET Core y entorno distribuido | .NET 10.0.14 |
| Persistencia | EF Core SQLite 10.0.14 |
| Documentos | PDFsharp 6.2.4 y ClosedXML 0.105.0 |
| Correo | MailKit 5.0.0 |

### Elegir el modo de acceso

**Inicio local desde fuentes**, sin depender de una red Wi-Fi: ejecuta desde la raíz **powershell.exe -NoProfile -File tools/start.ps1** y abre **https://localhost:7248**. Conserva la ventana abierta; **Ctrl+C** detiene el servicio.

**Inicio en LAN desde fuentes:** ejecuta **Iniciar_MEGA.cmd**. Usa **tools/start-lan.ps1**, detecta IPv4 con puerta de enlace y muestra la dirección HTTPS para el mismo Wi-Fi. Limita acceso a la subred privada y conserva autenticación y antifalsificación. Si no hay red activa, usa el modo local anterior.

Si cambia la IP, reinicia. Revisa permiso de Windows para la red privada y certificado del servidor; el iniciador no configura reglas de Windows ni confianza. No publiques el servicio en Internet ni compartas SQLite entre equipos.

**Configuración de base:** **MEGA__DatabasePath** define la ruta completa y **MEGA__DemoData** el modo. Una base nueva sin demostración exige **MEGA_BOOTSTRAP_PASSWORD** de al menos 12 caracteres; crea Propietario sin productos ficticios. No cambia el modo de una base existente. Evita guardar credenciales en scripts o historial.

[SALTO]

## Conservar datos y controlar accesos

### Archivos que forman el estado

| Elemento | Tratamiento |
|---|---|
| Base configurada, normalmente mega-demo.db | Respaldar con el mecanismo consistente de SQLite. |
| Carpeta de la base con sufijo .archivos | Conservar reportes, secretos protegidos, PDF y preservaciones junto con la base. |
| Recuperacion.json | Conservar: controla fase, sesiones y continuidad de numeración. |
| Preservaciones | Custodiar ZIP y huellas del estado anterior a una recuperación. |
| Carpeta HTTPS del iniciador | Mantener vinculada al perfil de Windows que protege el certificado. |
| Programa y paquete.json | Conservar completos y separados de los datos. |

**No copies solo el .db mientras el servicio escribe.** WAL puede contener cambios confirmados. No borres WAL, SHM ni Recuperacion.json para quitar un bloqueo.

Las credenciales SMTP se protegen fuera de la base, que conserva su referencia. Tras cambiar de perfil o equipo, revisa su disponibilidad y configura nuevamente cuando corresponda. Los reportes enviados conservan archivo exacto y SHA-256.

### Esquema y seguridad

El modelo de procedencia contiene **39 tablas, 499 columnas y 103 claves externas**. El esquema operativo es **2**. DDL y JSON originales se conservan; las modificaciones de esquema se realizan mediante migraciones explícitas.

La autorización se verifica en el servidor. Caja requiere permiso adicional; Administrador necesita delegación para cuentas. Soporte temporal solo dispone de diagnóstico de lectura. Las solicitudes propias requieren otra cuenta de gestión para aprobarse.

Las sesiones usan cookies Secure, HttpOnly y SameSite Strict y protección antifalsificación. Cambios de contraseña, permisos o credenciales invalidan sesiones anteriores. La bitácora conserva decisiones y rechazos sin guardar contraseñas ni cuerpos completos de solicitudes.

Antes del uso real, valida cuentas, unidades, equivalencias, costos, precios, NIO, UTC-6, datos del negocio y respaldo. El programa no determina validez fiscal ni habilita cobros por tarjeta o transferencia.

[SALTO]

## Verificar operaciones y servicios externos

### Reglas que deben conservarse

La confirmación de venta registra detalle, pago, caja, inventario, costos, auditoría y solicitud original de impresión en una transacción. La clave de operación recupera una venta confirmada; una respuesta incierta se resuelve recuperando esa operación.

Recepciones y solicitudes no cambian saldos antes de aprobarse. La anulación conserva venta y cierre originales; reincorporar mercadería y devolver efectivo son efectos separados. Los gastos alimentan resultados sin crear una salida de caja.

La valoración usa **promedio ponderado móvil** por producto y unidad base, con seis decimales de costo. Los costos de venta quedan históricos. Costo desconocido no equivale a cero; un costo faltante impide calcular el resultado global. Los lotes se asignan por vencimiento más próximo e identidad, sin consumir vencidos.

### Impresión y correo

| Servicio | Configuración y control |
|---|---|
| Impresora Windows | Nombre exacto instalado y habilitación expresa. Microsoft Print to PDF crea archivo; OneNote no es destino automático. |
| MP58-16 Bluetooth | SPP, COM saliente, ESC/POS y PC850. Perfil de 58 mm y 32 columnas; prueba física pendiente. |
| Correo SMTP | STARTTLS o TLS con certificado válido; archivo, huella y Message-ID conservados. |

Impresión consulta cada 5 segundos; correo, cada 10. Ambos registran reserva de 3 minutos antes del envío. Una reserva vencida o transmisión sin resultado queda **Incierta** y necesita revisión; no se reenvía automáticamente.

Bluetooth directo no aparece en la cola de impresión de Windows. Una aceptación del sistema o del puerto no acredita papel entregado. **Aceptada_SMTP** no acredita recepción ni lectura. Cambiar de impresora no redirige trabajos históricos.

SMTP admite hasta tres intentos ante no aceptación conocida o rechazo temporal explícito, con esperas de dos y cuatro minutos. Un cambio de servidor o usuario requiere nueva credencial. Para ensayos aislados, usa **MEGA__PrintAgentEnabled**, **MEGA__MailAgentEnabled** y **MEGA__BackupAgentEnabled** según el escenario.

[SALTO]

## Generar y comprobar respaldos

### Preparar la pauta

Propietario autoriza un destino existente fuera del almacén operativo e identifica el medio. Decide las opciones periódica, al cierre o previa a actualización. El intervalo propuesto no activa una política sin autorización.

1. Confirma medio conectado, permisos y espacio disponible.
2. En **Respaldos**, gestión selecciona **Generar y comprobar respaldo**.
3. Revisa el registro final y exige estado **Comprobada**.
4. Custodia ZIP, manifiesto y huellas. Comprueba nuevamente la copia si cambia el medio o aparece una alerta.

**Contenido:** instantánea consistente de SQLite, reportes congelados, secretos protegidos, PDF administrados referenciados y manifiesto con versión, esquema, fecha, registros, identificadores, tamaños y SHA-256.

La comprobación revisa ZIP, rutas, archivos, huellas, integridad física, relaciones y punto recuperable. El formato 2 conserva continuidad de numeración y preservaciones anteriores; se admite lectura del formato 1.

### Si falla una copia

Conserva la última copia comprobada. Revisa conexión del medio, permisos, espacio y detalle del fallo. Usa **Comprobar de nuevo** cuando corresponda. No sobrescribas una copia válida para ocultar el problema.

El agente revisa pauta y cierres cada 30 segundos y serializa la ejecución. El sistema no elimina automáticamente copias anteriores. Una carpeta del mismo disco permite ensayo, pero no protege frente a pérdida de ese disco.

**Detalle del registro:** dentro de su propia instantánea, la ejecución puede figurar **En_proceso** porque el ZIP precede al resultado final. La recuperación concilia el manifiesto con el registro externo comprobado; no edites el ZIP para cambiarlo.

Acuerda medios alternos, retención y una prueba de restauración con el negocio. Los tiempos y la pérdida de datos admisibles dependen de ese ensayo; no se dan por verificados con datos ficticios.

[SALTO]

## Recuperar y reabrir la operación

**Requisitos:** Propietario activo, contraseña actual, copia registrada comprobada e identidad coincidente en ella. La base actual debe ser legible y coherente. Daño físico, pérdida del almacén o ausencia de identidad requieren intervención supervisada.

### Aplicar una copia

1. Selecciona **Comprobar y revisar punto**. Revisa fecha y novedades posteriores. Si cambió el estado, repite la revisión.
2. Registra motivo, contraseña y reconocimiento; selecciona **Autorizar y recuperar copia**.
3. El servicio bloquea solicitudes y espera a peticiones y agentes. Si no obtiene pausa en dos minutos, no aplica la copia.
4. Conserva una preservación comprobada del estado actual; prepara y valida la copia fuera de la base activa y la instala mediante la API de respaldo de SQLite.
5. Invalida sesiones y deja la operación pausada para conciliación. Las novedades posteriores al punto permanecen en la preservación; no se reproducen solas.

### Conciliar y reabrir

Propietario vuelve a entrar, descarga la preservación y documenta inventario, caja, novedades y trabajos externos. Completa las declaraciones y selecciona **Autorizar reapertura**. Impresión, correo y pauta requieren nueva autorización; los trabajos recuperados conservan cuarentena y resolución individual.

Las cuentas coincidentes mantienen contraseñas, activación y permisos actuales. Las no coincidentes quedan inactivas; soporte temporal recuperado se revoca y las revisiones pendientes se invalidan.

**Numeración:** Recuperacion.json conserva máximos y secuencia de folios; los saltos no demuestran una venta perdida. No elimines ese control ni ejecutes versiones anteriores a 0.20.0 después de restaurar.

**Si se interrumpe:** al reiniciar se reconoce la intervención y se intenta la reversión comprobada. Si no puede comprobarse, el núcleo permanece pausado. Conserva original, copias y preservación; no fuerces acceso modificando archivos. **/Recuperacion/Estado** permite consultar la pausa sin acceder a la base.

[SALTO]

## Diagnosticar y actualizar

### Diagnóstico de lectura

Desde las fuentes, utiliza **Revisar_Base_Datos.cmd** con el SDK disponible. También puedes ejecutar el programa en modo **--db-check** con ruta y modo correctos. El iniciador de diagnóstico puede comprobar la base con MEGA abierto: compila en una carpeta temporal independiente y no reemplaza el ejecutable en uso.

Revisa integridad física, claves externas, contrato, saldos y huellas. **Healthy** indica integridad; **UpToDate**, esquema vigente. Un resultado no sustituye al otro. Las migraciones se aplican explícitamente al iniciar o con **--db-upgrade**, con copia previa cuando corresponda.

### Actualizar el programa

1. Comprueba versión de origen, compatibilidad de esquema y ausencia de trabajos en curso.
2. Genera y comprueba el respaldo autorizado. Detén el servicio y conserva programa y datos anteriores.
3. Extrae el programa nuevo en otra carpeta. Inicia con el mismo perfil y la misma carpeta de datos.
4. Comprueba acceso, integridad, permisos y consultas antes de reanudar.

Si falla el inicio, detén el servicio y vuelve a una versión compatible. No restaures la base solo para cambiar el programa; una migración exige su procedimiento de reversión.

| Síntoma | Intervención |
|---|---|
| Puerto ocupado o segunda instancia | Detener la instancia identificada o elegir puerto libre. No abrir la misma base dos veces. |
| Certificado no recuperable | Conservar HTTPS y revisar perfil de Windows; no borrar datos para regenerarlo. |
| Reporte alterado | Conservar archivo y comprobar huella, origen y copia. |
| Estado externo incierto | Verificar equipo o destinatario y registrar resolución. |
| Base dañada o recuperación pausada | Custodiar originales y copias; intervención supervisada. |

Supervisa espacio, permisos, colas, última copia comprobada y bitácora. No borres auditoría o archivos referenciados como mantenimiento ordinario.

[SALTO]

## Consultar referencias y evidencia

### Ubicaciones del código

| Ruta desde la raíz | Contenido |
|---|---|
| src/Mega.Web/Pages | Pantallas y manejadores Razor. |
| src/Mega.Web/Services | Reglas comerciales, reportes y continuidad. |
| src/Mega.Web/Data | Modelo, contrato, esquema y migraciones. |
| tests/Mega.Tests | Pruebas con bases separadas. |
| tools | Inicio, diagnóstico, construcción y distribución. |

Para preparar un paquete desde una revisión registrada, consulta **tools/package.ps1** y **tools/build-distribution.ps1**. Los manifiestos registran versión, commit, entorno y huellas; SHA-256 detecta alteraciones y no equivale a firma digital del editor. Conserva licencias y avisos con la distribución.

### Evidencia de esta entrega

El registro oficial conservado contiene **272 pruebas aprobadas**. La entrega comprobó integridad de la base inicial, cero ventas y acceso HTTPS local con cuenta ficticia. Son resultados históricos documentados, no pruebas reejecutadas durante esta revisión editorial.

El acceso móvil del código fuente se comprobó en el Wi-Fi de desarrollo. El iniciador distribuido sigue siendo local. No se acredita con ello instalación en la estación definitiva, impresión física ni aceptación comercial.

En esta entrega, consulta **06_Matriz_de_Pruebas_y_Evidencias.pdf** y el documento consolidado. En el repositorio, **docs/Base_Datos.md** explica la base y su procedencia. Las carpetas **06_Pruebas_y_Evidencias** y **03_Base_Datos_y_Ejecucion** solo pertenecen al paquete amplio opcional.

### Referencias técnicas

- [Repositorio oficial MEGA](https://github.com/carlosfaj/MEGA-Minisuper) y sus guías de Distribución, Acceso celular, Impresión Bluetooth, Respaldos y Restauración.
- [Microsoft sobre publicación de aplicaciones .NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/).
- [Microsoft sobre respaldo de SQLite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup).

**Control documental:** manual técnico de la versión oficial 0.23.2, edición del 18 de noviembre de 2026. Actualiza esta guía cuando cambien inicio, rutas, esquema o recuperación.
