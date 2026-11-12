# Matriz de pruebas del avance 01

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
