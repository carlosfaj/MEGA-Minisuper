# Estado del cuarto corte de MEGA

Avance 01 del 11 de noviembre de 2026. Se implementó y comprobó el recorrido de acceso, apertura de caja, venta en efectivo, comprobante y cierre. Se utiliza la revisión del modelo del 11 de noviembre; los documentos académicos previos no se modificaron.

Actualización del 12 de noviembre, **v0.2.0**: base SQLite con esquema 2, migración conservadora desde esquema 1, corrección de claves compuestas, diagnóstico integral y dos vistas utilizadas por la aplicación. Se conservan las 39 tablas, 499 columnas y 103 FK del diseño. Los 21 triggers y los índices agregados refuerzan integridad e historia; los módulos funcionales pendientes de la tabla siguiente conservan su alcance. [Detalle de base de datos](Base_Datos.md).

| Requisito | Estado del avance | Trabajo restante |
|---|---|---|
| RF01 Catálogo | Alta, edición, desactivación, categorías, presentaciones, códigos y precios versionados (v0.3.0) | Promociones e impuestos según políticas validadas |
| RF02 Abastecimiento | Proveedores, recepción pendiente, recibido/aceptado, costos y lotes; revisión de versión/contenido y entrada única autorizada (v0.6.0) | Aceptación del negocio e integración de salidas con valoración |
| RF03 Inventario | Existencias, lotes/alertas, conteos y solicitudes autorizadas de carga/ajuste/salida sin lotes a costo desconocido (v0.7.0/0.8.0); costo declarado de carga conservado | Regularizaciones con lotes/costo conocido; asignación y valoración de salidas |
| RF04 Ventas | Preparación, cobro efectivo, comprobante, recuperación de reintento y consulta del turno | Anulación autorizada, agente de impresión, reimpresión y contingencia |
| RF05 Caja | Apertura y cierre propios con fondo real, esperado y diferencias | Integración con devoluciones autorizadas y comprobación en operación real |
| RF06 Acceso y autorización | Cuentas/delegación y soporte (v0.5.0); revisión por versión/contenido para recepciones e inventario (v0.6.0/0.8.0) | Autorización de anulaciones; restauración autorizada al integrar continuidad |
| RF07 Gestión | Sin módulo operativo | Valoración, gastos, reportes/exportaciones y correo con estados verificables |
| RF08 Auditoría | Eventos de inicialización, acceso, venta y caja; consulta para gestión | Completar todas las operaciones y rechazos críticos, detalle antes/después |
| RF09 Continuidad | Esquema disponible | Copia consistente, destino externo, comprobación, recuperación y cuarentena de colas |

Actualización funcional **0.8.0**: catálogo, configuración, cuentas y soporte temporal, proveedores/recepciones, revisión de contenido/versiones, conteos, lotes y solicitudes básicas de inventario. 120 pruebas automatizadas aprobadas y recorridos de navegador por módulo. El [roadmap](ROADMAP.md) y los documentos de operación detallan los límites de valoración y los pendientes reales.

## Siguiente secuencia de implementación

1. Validar con el negocio unidades, valoración/costo de salida, fiscalidad y política de excepciones; conservar las restricciones de los módulos afectados mientras no se resuelvan.
2. Integrar valoración/asignación de lotes en ventas y regularizaciones; promociones e impuestos con historia (T01/T03/T04/T05).
3. Anulación autorizada y regularización de fondos sin alterar cierres históricos (T06).
4. Agente local de impresión, estados inciertos y reimpresión sin repetir cobro (T07).
5. Gastos, reportes/exportaciones y correo (T11/T12); completar la auditoría y sus rechazos críticos (T13).
6. Respaldo externo y restauración con conciliación/cuarentena (T14/T15).
7. Aceptación del negocio, regresión integral, consolidación académica, manuales PDF, matriz final PDF/Excel, presentación y ensayo de demostración.

Se debe documentar cada cambio de alcance en los capítulos anteriores y conservar evidencia real. La prioridad de desarrollo y pruebas responde al 35 % y 20 % de la rúbrica, respectivamente. La guía exige un documento consolidado, no un cuarto informe independiente.

## Evidencia del avance

- `Matriz_Pruebas.md` y `evidencias/pruebas/avance01.trx`: resultados automatizados con bases separadas.
- `Evidencias_Avance01.md`: capturas de la aplicación ejecutada con datos ficticios.
- `evidencias/resultado_navegador.json`: comprobación de interfaz y ausencia de errores JavaScript durante el recorrido.
- No se atribuyen aceptación del negocio, entrevistas nuevas, impresión física ni despliegue comercial.
