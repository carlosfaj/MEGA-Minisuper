# Estado del cuarto corte de MEGA

Avance 01 del 11 de noviembre de 2026. Se implementó y comprobó el recorrido de acceso, apertura de caja, venta en efectivo, comprobante y cierre. Se utiliza la revisión del modelo del 11 de noviembre; los documentos académicos previos no se modificaron.

Actualización del 12 de noviembre, **v0.2.0**: base SQLite con esquema 2, migración conservadora desde esquema 1, corrección de claves compuestas, diagnóstico integral y dos vistas utilizadas por la aplicación. Se conservan las 39 tablas, 499 columnas y 103 FK del diseño. Los 21 triggers y los índices agregados refuerzan integridad e historia; los módulos funcionales pendientes de la tabla siguiente conservan su alcance. [Detalle de base de datos](Base_Datos.md).

| Requisito | Estado del avance | Trabajo restante |
|---|---|---|
| RF01 Catálogo | Alta, edición, desactivación, categorías, presentaciones, códigos y precios versionados (v0.3.0) | Promociones e impuestos según políticas validadas |
| RF02 Abastecimiento | Esquema y carga ficticia inicial trazada | Proveedores, recepción, revisión y autorización de entrada |
| RF03 Inventario | Consulta de existencias y alertas de mínimo/discrepancia; descuento transaccional por venta | Lotes/vencimiento, conteos, cargas, ajustes y salidas autorizadas |
| RF04 Ventas | Preparación, cobro efectivo, comprobante, recuperación de reintento y consulta del turno | Anulación autorizada, agente de impresión, reimpresión y contingencia |
| RF05 Caja | Apertura y cierre propios con fondo real, esperado y diferencias | Integración con devoluciones autorizadas y comprobación en operación real |
| RF06 Acceso y autorización | Inicio de sesión, roles, permisos operativos, revocación por versión y límite de intentos | Gestión de cuentas/delegación, soporte temporal y autorizaciones de objetos/versiones |
| RF07 Gestión | Sin módulo operativo | Valoración, gastos, reportes/exportaciones y correo con estados verificables |
| RF08 Auditoría | Eventos de inicialización, acceso, venta y caja; consulta para gestión | Completar todas las operaciones y rechazos críticos, detalle antes/después |
| RF09 Continuidad | Esquema disponible | Copia consistente, destino externo, comprobación, recuperación y cuarentena de colas |

## Siguiente secuencia de implementación

1. Catálogo/configuración/cuentas y mantenimiento de precios conservando historia, con pruebas T01 y de permisos.
2. Proveedores, recepciones y aprobación por otra cuenta; versión/hash revisados en la misma transacción (T02/T10).
3. Conteos, carga/ajuste/salida y lotes; valoración y costos desconocidos explícitos (T03/T04).
4. Anulación y regularización de fondos sin alterar cierres históricos (T06).
5. Agente local de impresión, estados inciertos y reimpresión sin repetir cobro (T07).
6. Gastos, reportes y correo (T11/T12), ampliación de auditoría (T13), respaldo/recuperación (T14/T15).
7. Aceptación con el negocio, consolidación académica, manuales PDF, matriz final PDF/Excel, presentación y ensayo de demostración.

Se debe documentar cada cambio de alcance en los capítulos anteriores y conservar evidencia real. La prioridad de desarrollo y pruebas responde al 35 % y 20 % de la rúbrica, respectivamente. La guía exige un documento consolidado, no un cuarto informe independiente.

## Evidencia del avance

- `Matriz_Pruebas.md` y `evidencias/pruebas/avance01.trx`: resultados automatizados con bases separadas.
- `Evidencias_Avance01.md`: capturas de la aplicación ejecutada con datos ficticios.
- `evidencias/resultado_navegador.json`: comprobación de interfaz y ausencia de errores JavaScript durante el recorrido.
- No se atribuyen aceptación del negocio, entrevistas nuevas, impresión física ni despliegue comercial.
