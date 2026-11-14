# Matriz de pruebas — historial y estado actual

Estado actual **0.21.0: 223/223 pruebas aprobadas**, con matriz completa T01–T15, respaldo/restauración y recorridos de navegador aislados. [Matriz PDF, registro individual y revisión](Manuales_y_Validacion.md), [suite completa](evidencias/distribucion/suite.trx), [restauración](Restauracion.md) y [kanban](KANBAN.md). Las secciones siguientes conservan el alcance y los resultados históricos de cada avance; sus pendientes no describen por sí solos la versión actual.

## Avance 01

Ejecución finalizada en 2026-11-12T05:32:38.167068+00:00 (UTC). Resultado: **23/23 pruebas aprobadas**.

Pruebas automatizadas con datos ficticios y bases SQLite efímeras. No acreditan aceptación con usuarios, fiscalidad ni impresión física. Los criterios T05/T08/T09 se cubren dentro del alcance descrito; T07/T13 solo tienen cobertura parcial y el resto continúa pendiente según Estado_Cuarto_Corte.md.

Fuente verificable: [resultado TRX](evidencias/pruebas/avance01.trx). Código reproducible: tests/Mega.Tests/FlowTests.cs.

| Código | Requisito | Caso de prueba | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-01 | RF04 | T05 Pago insuficiente | C$80 rechaza venta de C$86; ninguna venta, pago, caja ni operación parcial | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-02 | RF04 RF03 RF05 RF08 | T05 Venta y reintento | Total C$86; recibido C$100; cambio C$14; una sola venta/pago/caja/impresión; existencias 48 lb y 29 unidades; caja C$566 | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-03 | RF05 | T08 Apertura de caja | Diferencia de fondo exige motivo; se rechaza una segunda apertura de la cuenta o estación | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-04 | RF05 | T09 Cierre de caja | Esperado C$566, contado C$560, diferencia -C$6 con motivo; turno cerrado impide preparar venta | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-05 | RF04 RF05 RF03 | Reintentos concurrentes | Dos confirmaciones simultáneas con misma clave devuelven la misma venta; una sola operación comercial | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-06 | RF06 | Revocación de sesión | Al cambiar la versión de credenciales, la cookie existente deja de autorizar acceso | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-07 | RF03 RF04 RF06 | Integridad de esquema | 39 tablas, foreign_keys=ON y foreign_key_check sin filas | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-08 | RF06 | Separación de base ficticia y base real | Rechazar iniciar una base de demostración con el modo ficticio desactivado | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-09 | RF03 RF04 | Costo conocido sin módulo de valoración | Rechazar el producto hasta integrar valoración, sin convertir su costo conocido en desconocido | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-10 | RF03 RF04 RF08 | Discrepancia de existencia física | Exigir comprobación explícita; con ella permitir saldo -1 y conservar evidencia de la confirmación | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-11 | RF01 RF04 | Cambio de precio después de revisar | Se rechaza la cotización anterior sin crear una venta | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-12 | RF04 | Validación de cantidades/modalidad (mode: "desconocida", value: "1", presentation: 1) | Rechazar fracción indivisible, signo, cero, precisión excesiva, desbordamiento o modalidad incompatible | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-13 | RF04 | Validación de cantidades/modalidad (mode: "importe", value: "2", presentation: 2) | Rechazar fracción indivisible, signo, cero, precisión excesiva, desbordamiento o modalidad incompatible | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-14 | RF04 | Validación de cantidades/modalidad (mode: "peso_manual", value: "-1", presentation: 1) | Rechazar fracción indivisible, signo, cero, precisión excesiva, desbordamiento o modalidad incompatible | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-15 | RF04 | Validación de cantidades/modalidad (mode: "peso_manual", value: "0", presentation: 1) | Rechazar fracción indivisible, signo, cero, precisión excesiva, desbordamiento o modalidad incompatible | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-16 | RF04 | Validación de cantidades/modalidad (mode: "peso_manual", value: "1.0001", presentation: 1) | Rechazar fracción indivisible, signo, cero, precisión excesiva, desbordamiento o modalidad incompatible | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-17 | RF04 | Validación de cantidades/modalidad (mode: "peso_manual", value: "9999999999999999999999999999", presentation: 1) | Rechazar fracción indivisible, signo, cero, precisión excesiva, desbordamiento o modalidad incompatible | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-18 | RF04 | Validación de cantidades/modalidad (mode: "unidad", value: "1.5", presentation: 2) | Rechazar fracción indivisible, signo, cero, precisión excesiva, desbordamiento o modalidad incompatible | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-19 | RF04 RF05 RF06 | Integración HTTP real | Inicio de sesión, antifalsificación, apertura, venta y comprobante; Inventario sin venta/comprobante, Cajero sin bitácora | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-20 | RF04 RF03 RF05 | Integración y reversión por fallo tardío | Un fallo al crear impresión revierte venta, detalles, pago, caja y existencias | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-21 | RF04 | Venta por importe | C$10 de arroz a C$24/lb produce 0.417 lb y total C$10.01 con redondeo documentado | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-22 | RF04 RF06 | Reutilización incompatible de clave | La misma clave con otro cobro o cuenta se rechaza | Aserciones verificadas sobre la ejecución registrada | Aprobado |
| CP-23 | RF05 RF04 | Cierre con versión anterior | Se rechaza cerrar desde una revisión anterior a una nueva venta | Aserciones verificadas sobre la ejecución registrada | Aprobado |

## Comprobación del navegador

El recorrido adicional verifica acceso, apertura con C$480, compra arroz/leche, bloqueo de C$80, cobro de C$100/cambio C$14, comprobante C$86, caja C$566, cierre con diferencia y consulta de existencias para Inventario. También se comprueba la vista móvil sin desbordamiento de la página y sin errores JavaScript.

El registro está en [resultado del navegador](evidencias/resultado_navegador.json); las capturas explicadas están en [evidencias del avance](Evidencias_Avance01.md). También se ejecutó la pérdida de respuesta después de guardar, recarga de pestaña y recuperación de una única venta de C$25 sin repetir caja ni cobro: [resultado de recuperación](evidencias/resultado_recuperacion.json).

## Incidencia corregida

La primera ejecución encontró que abrir caja pedía validar también el campo del formulario de cierre. Se separó la obligatoriedad por operación: cada manejador exige únicamente sus datos. La segunda ejecución aprobó el flujo HTTP completo. Las 20 pruebas restantes de la primera ejecución ya habían aprobado. El resultado final reemplaza al preliminar y conserva el registro de la corrección aquí.

## Ampliación comprobada al 12 de noviembre — v0.8.0

Los 23 casos anteriores conservan el registro del avance inicial. La suite acumulada actual tiene 120 pruebas aprobadas. Esta tabla resume las familias nuevas; sus documentos contienen el alcance y las capturas explicadas. Cada TRX conserva el nombre y resultado individual de las pruebas ejecutadas.

| Familia / requisito | Resultado comprobado | Evidencia | Estado |
|---|---|---|---|
| Base / RF03 RF06 RF09 | Contrato físico/EF, claves compuestas, integridad, migración sin pérdida y diagnóstico de solo lectura | `evidencias/base_datos`, `Base_Datos.md` | Aprobado en alcance implementado |
| Catálogo / RF01 T01 | Alta hasta venta; precios históricos, código no reutilizado y permisos | `evidencias/catalogo`, `Catalogo.md` | Aprobado |
| Configuración / RF05 RF06 | Nueva vigencia sin alterar turno abierto ni comprobantes anteriores | `evidencias/configuracion`, `Configuracion.md` | Aprobado |
| Cuentas / RF06 | Delegación explícita, último Propietario, turno abierto, credenciales, revocación y soporte vencido/revocado | `evidencias/cuentas`, `Cuentas.md` | Aprobado |
| Recepción / RF02 T02 T10 | Pendiente sin stock; revisión obsoleta rechazada; entrada única, costo declarado, lote y fallo tardío atómico | `evidencias/abastecimiento`, `Abastecimiento.md` | Aprobado |
| Conteos y lotes / RF03 T03 T04 parcial | Conteo no ajusta; movimientos posteriores exigen nueva comprobación; lotes/alertas sin márgenes | `evidencias/conteos`, `Conteos_y_Lotes.md` | Aprobado |
| Solicitudes / RF03 RF06 T04 T10 | Ajuste desde conteo, carga, salida y cancelación; misma versión/contenido/saldo, otra cuenta, concurrencia y reversión ante fallo | `evidencias/solicitudes/solicitudes.trx`, `Solicitudes_Inventario.md` | Aprobado para productos sin lotes y regularización a costo desconocido |

La autorización de anulaciones, valoración, promociones/impuestos, impresión física, reportes/exportaciones, correo y recuperación integral conservan sus pruebas pendientes en `ROADMAP.md`. La auditoría tiene eventos de los módulos nuevos; completar todos los rechazos críticos y T13 integral sigue pendiente. Ningún resultado ficticio se presenta como aceptación del negocio.

## Ampliación v0.9.0–0.14.0

162 pruebas acumuladas aprobadas. Valoración y lotes (`Valoracion.md`, `Inventario_Integrado.md`), promociones/impuestos (`Condiciones_Comerciales.md`), anulaciones/regularización (`Anulaciones.md`) y rechazos críticos T13 (`Auditoria.md`) cuentan con TRX y recorridos de navegador propios. El kanban muestra el alcance actual y distingue las validaciones reales pendientes.

## Estado acumulado v0.18.0

200 pruebas aprobadas y navegador por módulo. Las secciones anteriores conservan evidencia histórica de cada avance. Sus pendientes de valoración, anulaciones, rechazos y reportes se resolvieron en `Valoracion.md`, `Inventario_Integrado.md`, `Condiciones_Comerciales.md`, `Anulaciones.md`, `Auditoria.md`, `Impresion.md`, `Gastos.md`, `Reportes.md` y `Correo.md`. T07/T11/T12/T13 se verifican en su alcance implementado; equipo físico, SMTP real, respaldo/restauración y aceptación permanecen en el kanban.

## Ampliación de continuidad v0.19.0–0.20.0

| Criterio | Comprobación | Evidencia | Resultado |
|---|---|---|---|
| RF09 T14 | SQLite/WAL consistente con venta y archivos congelados; pauta condicionada; medio ausente sin perder última copia; SHA-256, rutas y punto validados | [Respaldos](Respaldos.md), `evidencias/respaldo` | Aprobado en carpeta de ensayo |
| RF09/RF06 T15 | Propietario, contraseña actual, revisión obsoleta rechazada y preservación comprobada/descargable | [Restauración](Restauracion.md), `RecoveryTests` | Aprobado en base aislada |
| RF09/RF08 T15 | Fallo de instalación revierte; intervención interrumpida se reconoce al iniciar; falta de preservación mantiene pausa | `evidencias/restauracion/dirigidas.trx` | Aprobado con fallos simulados |
| RF04/RF07/RF09 T15 | Colas recuperadas en cuarentena, intentos en curso inciertos, resolución sin reintento automático y reapertura explícita | `evidencias/restauracion/resultado.json` y capturas | Aprobado sin papel ni correo externo |
| RF04/RF06/RF09 T15 | Identificadores y folios no se reutilizan; sesiones previas inválidas; credenciales/permisos actuales no retroceden; soporte y revisiones pendientes revocados | `RecoveryTests`, suite acumulada 223/223 | Aprobado |

RTO/RPO con volumen y medios reales, rendimiento en equipo objetivo y aceptación por el negocio conservan sus criterios pendientes. La matriz T01–T15 y su exportación PDF revisada están consolidadas en [Manuales y validación](Manuales_y_Validacion.md).
