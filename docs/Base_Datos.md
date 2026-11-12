# Implementación de la base de datos de MEGA

Versión de aplicación 0.2.0, del 12 de noviembre de 2026. El esquema de SQLite es **2**. Se conserva el diseño de 39 tablas, 499 columnas y 103 claves foráneas. La base de demostración existente se actualizó y sus registros previos se conservaron.

## Archivos y acceso

- `src/Mega.Web/App_Data/mega-demo.db`: base de demostración de esta computadora; archivo local excluido de Git.
- `src/Mega.Web/App_Data/mega.db`: ubicación predeterminada cuando la demostración está desactivada.
- `src/Mega.Web/Data/Schema.sql`: definición original del esquema 1, con hash conservado.
- `src/Mega.Web/Data/Modelo_Datos_MEGA.json`: fuente académica original de entidades/campos/relaciones, con hash conservado.
- `src/Mega.Web/Data/Migrations/002_Integridad.sql`: evolución al esquema 2.
- `src/Mega.Web/Data/DatabaseContract.cs`: contrato entre fuentes, SQL y versión.
- `DatabaseSchema.cs`: creación, actualización atómica y copia previa.
- `DatabaseInspector.cs`: diagnóstico de solo lectura y contraste del mapeo.
- `DatabaseViews.cs`: mapeo de vistas de consulta.
- `tests/Mega.Tests/DatabaseTests.cs`: pruebas específicas de persistencia e integridad.

`MEGA:DatabasePath` permite seleccionar otro archivo. Una ruta relativa se resuelve desde la carpeta de la aplicación, no desde la carpeta donde se abrió una terminal. Las bases contienen datos y hashes de contraseñas; no se publican en GitHub. El repositorio incluye las fuentes y el inicializador para reproducir una base ficticia nueva.

## Estructura por áreas

| Área | Contenido |
|---|---|
| Catálogo/configuración | Categoría, producto, presentación, código, precio, promoción, impuesto y condiciones versionadas |
| Abastecimiento/inventario | Proveedor, recepción/detalle, lote, conteo/detalle, solicitudes/detalles, operaciones y movimientos |
| Venta y caja | Venta/detalle, pago, turno, movimiento de caja, gasto y solicitud de anulación |
| Control | Usuario, rol, autorización, acceso temporal y auditoría |
| Impresión | Solicitudes, intentos y entregas de comprobantes |
| Correo/continuidad | Solicitudes e intentos de correo, ejecución de respaldo y restauración |

Las tablas de los módulos pendientes están creadas. Su presencia no acredita que esos módulos ya tengan interfaces, reglas transaccionales o pruebas de aceptación.

## Claves, cantidades y consultas

El mapeo de PromocionProducto usa `(promocionId, presentacionId)`. ProductoImpuesto usa `(productoId, impuestoId, vigenteDesde)`. Las pruebas guardan y recuperan varias asociaciones dentro de una misma promoción y varios periodos de una condición de impuesto. No se inventan políticas tributarias reales.

El dinero permanece en enteros de centavos. Cantidades, factores, tasas y costos conservan su escala explícita y los campos de costo desconocido siguen admitiendo NULL con su estado. Las vistas no añaden tablas mutables de saldo:

| Vista | Resultado |
|---|---|
| vw_Existencias | Suma firmada de movimientos en la unidad/escala base de cada producto; incluye productos sin movimientos con saldo cero |
| vw_ResumenCaja | Fondo real y movimientos netos en centavos. Para un turno cerrado conserva el esperado persistido |

La aplicación consulta estas vistas para existencias y resumen de caja. La confirmación sigue usando una transacción única para venta, detalle, inventario, pago, caja, auditoría e impresión pendiente. El pago se guarda antes del movimiento de caja dentro de esa misma transacción para comprobar su correspondencia.

## Actualización sin recrear datos

1. Verificar que el esquema y los datos existentes sean compatibles; rechazar versiones desconocidas o archivos no vacíos sin versión reconocida.
2. Verificar que el modo de la base corresponda a demostración o inicialización sin datos ficticios.
3. Para esquema 1, crear una copia consistente en `App_Data/PreMigration` mediante la API de copia de SQLite y comprobarla.
4. Aplicar índices, vistas, triggers, versión 2 y evento de auditoría dentro de una transacción.
5. Comprobar el resultado antes de confirmar. Un fallo revierte los cambios del esquema, la versión y el evento; conserva la copia previa.

Una base nueva se crea con el DDL original y la migración 002 en una sola transacción. La inicialización posterior de cuentas/configuración/datos ficticios también es transaccional e idempotente. Una base sin demostración exige `MEGA_BOOTSTRAP_PASSWORD` de al menos 12 caracteres y crea una cuenta inicial de Propietario con contraseña hasheada; no carga productos ficticios.

La aplicación anterior 0.1.0 espera esquema 1 y no debe abrir el archivo ya actualizado a 2. Una reversión de código exige revisar la copia previa y conciliar cualquier actividad posterior; no se proporciona restauración automática. Las futuras versiones deben agregar una migración explícita, manteniendo el DDL académico y las migraciones publicadas como referencias conservadas.

## Controles del esquema 2

Los 21 triggers refuerzan las siguientes condiciones:

- Intervalos de precio sin superposición; se admiten periodos adyacentes.
- Venta compatible con turno propio abierto, operación de venta y moneda configurada.
- Producto/presentación/unidad coherentes en detalle de venta.
- Origen, signo, producto, escala y lote compatibles en movimientos de inventario; origen/cantidad no se reescriben ni borran.
- Pago compatible con la venta y operador, sin exceder su cobertura.
- Movimiento de caja compatible con turno abierto, operador, origen y pago efectivo.
- Conservación de comprobante, importes, detalle, pagos, movimientos de caja y cierres históricos.
- Auditoría que solo admite agregar eventos.

Los índices adicionales sirven para ventas del turno, consulta de costo por producto y suma de movimientos de caja. El esquema tiene 159 índices contando los índices automáticos de claves/unicidad, frente a 156 del original.

Los triggers no sustituyen permisos, autorizaciones de otra cuenta, versión/hash del contenido aprobado, valoración de costos ni sumas definitivas de toda una transacción. El inspector detecta también ventas incompletas, pagos/detalles que no cubren el total, salidas que no coinciden con cantidades base, cierres incoherentes y comprobantes con hash diferente. El control de costo histórico sigue requiriendo el futuro servicio de valoración; esta versión conserva explícitamente el alcance de sus protecciones de origen/cantidad.

## Revisar o actualizar

Abrir **Revisar_Base_Datos.cmd** para una comprobación de solo lectura.

```powershell
dotnet run --project src/Mega.Web --configuration Release -- --db-check
```

El reporte muestra integridad, versión, tablas/campos/FK, vistas, triggers, índices y errores. `Healthy=true` indica que el esquema reconocido y sus datos superaron el diagnóstico; `UpToDate=true` indica que ya es esquema 2. Una base válida de esquema 1 puede estar íntegra y pendiente de actualizar.

La actualización ocurre al arrancar la nueva aplicación. También puede ejecutarse sin abrir el servidor web:

```powershell
dotnet run --project src/Mega.Web --configuration Release -- --db-upgrade
```

No se hace reparación silenciosa: un objeto faltante, una definición alterada o datos incompatibles detienen la actualización para revisión. La comprobación de solo lectura no crea una base inexistente ni corrige datos.

## Evidencias y límites

- [Diagnóstico anterior](evidencias/base_datos/v0.2.0_antes.json): esquema 1 íntegro.
- [Diagnóstico posterior](evidencias/base_datos/v0.2.0_despues.json): esquema 2 íntegro y actualizado.
- [Conservación](evidencias/base_datos/v0.2.0_conservacion.json): resumen criptográfico de registros previos en las 39 tablas y un nuevo evento de migración.
- [Distribución publicada](evidencias/base_datos/v0.2.0_publicada.json): una base nueva creada desde la aplicación publicada, con todos los recursos SQL/modelo presentes.
- [Pruebas ejecutadas](evidencias/base_datos/v0.2.0_pruebas.trx): ejecución con bases separadas de la demostración local.

La comprobación anterior/posterior se realizó sobre la base ficticia existente. Las pruebas destructivas usan archivos efímeros independientes. La copia local previa de migración no resuelve RF09: siguen pendientes la política de respaldo externo, autorización de restauración, conciliación y cuarentena de colas.

Referencias técnicas: [copia consistente de Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup), [transacciones](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions), [PRAGMA de SQLite](https://www.sqlite.org/pragma.html) y [triggers](https://www.sqlite.org/lang_createtrigger.html).
