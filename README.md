# Desarrollo de MEGA para el cuarto corte

Primer avance funcional del 11 de noviembre de 2026. La aplicación utiliza ASP.NET Core 10, Razor Pages, EF Core y SQLite, siguiendo la arquitectura del tercer corte corregido. Funciona en esta computadora y guarda los datos entre ejecuciones. Todos los recursos de interfaz se sirven localmente.

## Versiones en GitHub

Repositorio: [carlosfaj/MEGA-Minisuper](https://github.com/carlosfaj/MEGA-Minisuper). La versión actual es **0.13.0**, con anulación autorizada y devolución efectiva sin alterar cierres históricos. `main` conserva avances funcionales comprobados y cada funcionalidad terminada o corrección se registra con commit y push. [CHANGELOG.md](CHANGELOG.md) explica cada versión y `VERSION` declara la actual. [AGENTS.md](AGENTS.md) conserva la autorización y el flujo acordados con el usuario.

GitHub Actions restaura dependencias fijadas, compila y ejecuta las pruebas en cada push y PR configurados. Cuando un push a main aprueba la comprobación, registra `v<VERSION>` si esa etiqueta todavía no existe. Una etiqueta existente conserva su commit; no se sobrescribe. Los resultados de cada ejecución se consultan en [Actions](https://github.com/carlosfaj/MEGA-Minisuper/actions).

El [kanban con porcentaje de todo el proyecto](docs/KANBAN.md) se consulta en **Avance del proyecto** y se actualiza con evidencia en cada versión. El [roadmap pendiente](docs/ROADMAP.md) ordena las próximas funcionalidades, sus criterios de cierre y los entregables académicos.

## Abrir la aplicación

1. Ejecutar **Iniciar_MEGA.cmd** desde esta carpeta y conservar su ventana abierta.
2. Abrir **https://localhost:7248**. El certificado HTTPS de desarrollo ya existe en esta computadora; todavía no está marcado como confiable. Si aparece el aviso del navegador, corresponde al certificado local de desarrollo. El iniciador no cambia certificados de confianza del sistema.
3. Ingresar con **cajero** y **MegaDemo2026!**.
4. Abrir **Mi caja**, registrar el fondo recibido y comenzar una venta.

También existen las cuentas **admin**, **inventario**, **cajero2** y **propietario**, con la misma contraseña de demostración. Las cuentas pertenecen exclusivamente a la base ficticia y sus claves se guardan como hashes, no como texto en la base. Otra cuenta no puede abrir caja mientras exista un turno abierto en la misma estación.

La primera ejecución necesita Internet para descargar dependencias si no están disponibles en la computadora. Después de restaurar y compilar, el núcleo no necesita conexión. Se requiere SDK .NET 10; esta computadora tiene 10.0.403.

## Qué se puede probar

- Inicio y cierre de sesión, navegación por rol e invalidación de sesiones al cambiar la versión de credenciales.
- Apertura de caja propia, motivo de diferencia del fondo y exclusión de turnos simultáneos.
- Búsqueda de productos por nombre o código, venta por unidad, peso manual o importe para productos compatibles.
- Cálculo en el servidor, cobro en efectivo, cambio, pago insuficiente y revisión ante cambio de precio.
- Confirmación transaccional de venta, detalle, pago, caja, inventario, auditoría y solicitud de impresión pendiente.
- Recuperación de la misma confirmación sin repetir efectos, incluidos reintentos concurrentes. Una confirmación incierta se conserva en la pestaña para recuperarla con la misma clave.
- Consulta de ventas del turno y comprobante histórico; cierre con efectivo esperado, contado y diferencia.
- Consulta de existencias para Inventario/gestión y bitácora para gestión.

Ejemplo: abrir con **480.00**, motivo **Fondo recibido C$20 menor**, vender **2.000 lb de arroz** y **1 leche**, recibir **100.00**. El total es **C$86.00**, cambio **C$14.00**, caja esperada **C$566.00**. Cerrar contando **560.00** exige explicar una diferencia de **−C$6.00**. En la copia de trabajo de esta computadora, una venta de ese ejemplo ya quedó registrada por la comprobación de interfaz y su turno está cerrado; las existencias actuales de arroz/leche son 48 lb y 29 unidades. Una segunda prueba registró un jabón por C$25 para comprobar recuperación de respuesta perdida; también cerró su turno. El ZIP de fuentes excluye la base de trabajo: al extraerlo en otra ubicación, la inicialización crea datos ficticios sin ventas, con 50 lb de arroz y 30 unidades de leche.

## Estructura y continuidad

Módulos incorporados y sus instrucciones/evidencias:

- [Catálogo y precios](docs/Catalogo.md), [configuración](docs/Configuracion.md) y [cuentas/delegación/soporte](docs/Cuentas.md).
- [Proveedores, recepciones y revisiones](docs/Abastecimiento.md).
- [Conteos y lotes](docs/Conteos_y_Lotes.md) y [cargas, ajustes y salidas autorizadas](docs/Solicitudes_Inventario.md).

La suite actual contiene **120 pruebas aprobadas**. Cada módulo incluye recorridos de navegador con bases aisladas. Los parámetros de negocio y periféricos reales siguen pendientes de aceptación.

- `src/Mega.Web`: aplicación y copia del esquema corregido de 39 tablas.
- `src/Mega.Web/App_Data/mega-demo.db`: base persistente de demostración, creada automáticamente; no se incluye en control de versiones.
- `tests/Mega.Tests`: pruebas con bases efímeras separadas de la demostración.
- `docs/Estado_Cuarto_Corte.md`: avance por requisito y trabajo restante.
- `docs/Matriz_Pruebas.md`: resultados ejecutados, vinculados a RF y criterios.
- `docs/Evidencias_Avance01.md`: capturas reales explicadas.
- `docs/Documento_Final_Estructura.md`: organización del documento consolidado.
- `docs/Decisiones_y_Pendientes.md`: decisiones técnicas y validaciones del negocio.
- `docs/Base_Datos.md`: estructura, migraciones, diagnóstico y controles del esquema 2.
- `docs/Catalogo.md`: operación de productos, categorías, presentaciones, códigos y precios.

La fuente SQL y el modelo JSON se copiaron de `../../00_OFICIALES/Tercer_Corte/Correccion_20261111/`; los originales se conservan con sus hashes. El esquema actual es **2**: se crea de forma atómica o se migra desde el esquema 1 con copia previa, sin recrear ni borrar datos. EF mapea las 39 tablas, incluidas las claves compuestas, y dos vistas de lectura. No se usa EnsureCreated. El generador `tools/generate_model.py` permite regenerar clases y mapeos desde el JSON incluido.

Para consultar el estado sin modificar la base, ejecutar **Revisar_Base_Datos.cmd**. También puede utilizarse `dotnet run --project src/Mega.Web --configuration Release -- --db-check`. La actualización se aplica al arrancar la nueva aplicación, o explícitamente con `--db-upgrade`. El diagnóstico distingue base íntegra (`Healthy`) de esquema actualizado (`UpToDate`). Detalles en [Base_Datos.md](docs/Base_Datos.md).

## Validación

```powershell
dotnet build MEGA.sln
dotnet test MEGA.sln --logger "trx;LogFileName=avance01.trx" --results-directory docs/evidencias/pruebas
```

El registro histórico del avance 01 y sus capturas están en `docs/evidencias`; la validación de la versión 0.2.0 está en `docs/evidencias/base_datos`. El script opcional `tools/browser-check.cjs` requiere Playwright y Edge, una base de demostración sin turno abierto y ejecuta operaciones ficticias; no debe usarse sobre datos operativos. Puede recibir la ubicación de Playwright mediante `MEGA_PLAYWRIGHT`.

## Límites de este avance

Este es un avance del cuarto corte. Siguen pendientes impresión, valoración de costos, reportes, correo, impresión automática y respaldo/restauración. Las ventas asignan lotes y conservan costos, promociones e impuestos históricos; las tasas se configuran expresamente y no acreditan fiscalidad real. La venta conserva costos históricos conocidos o desconocidos; la ausencia de costo no equivale a cero. No se inventan tasas fiscales. Tarjeta y transferencia no están habilitadas.

Las pruebas realizadas son automatizadas y usan datos ficticios; la aceptación con el negocio y los periféricos reales sigue pendiente. El documento final, manuales definitivos, presentación y demostración de todo el alcance se completarán con los siguientes avances.

La aplicación está restringida al equipo local, usa HTTPS, cookies protegidas, protección antifalsificación, hash de contraseñas y límite de intentos de acceso. No está preparada para implantación comercial. El modo de demostración solo arranca en Development o Testing. Las bases ficticia y futura real tienen archivos separados, y se rechaza cambiar de modo sobre una base existente. Antes de una futura implantación deberán definirse configuración, usuarios, copias, fiscalidad y políticas pendientes.

## Referencias técnicas

- [Autenticación con cookies en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0).
- [Protección antifalsificación en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).
- [Limitaciones del proveedor SQLite de EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations).
