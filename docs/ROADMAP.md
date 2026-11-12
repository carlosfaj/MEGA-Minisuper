# Roadmap pendiente de MEGA

Estado al 12 de noviembre de 2026, con la versión de código 0.12.0. El orden sigue las dependencias del sistema y los entregables del cuarto corte. Cada hito puede requerir varias funcionalidades y versiones; las próximas versiones se asignan al publicar cada funcionalidad comprobada.

## Punto de partida

Están implementados el acceso por rol, apertura/cierre de caja, preparación y confirmación de ventas básicas en efectivo, recuperación sin duplicar cobros, comprobante en pantalla, consulta de ventas del turno, existencias y bitácora inicial. La base tiene 39 tablas con diagnóstico y migración al esquema 2. La versión 0.2.0 supera 50 pruebas automatizadas.

La consulta de existencias y la estructura de una tabla no equivalen a un módulo operativo completo. Siguen pendientes los recorridos de negocio que se detallan a continuación.

Desde ese punto inicial se completaron catálogo (0.3.0), configuración (0.4.0), cuentas/soporte (0.5.0), recepciones/revisiones (0.6.0), conteos/lotes (0.7.0) y solicitudes básicas de inventario (0.8.0). La suite actual tiene 132 pruebas aprobadas, además de recorridos de navegador por módulo. Las restricciones de valoración y validación del negocio continúan explícitas.

## Secuencia de implementación

| Orden | Hito | Funciones pendientes | Resultado para darlo por terminado |
|---|---|---|---|
| 1 | Catálogo — completado en 0.3.0 | Categorías; productos; presentaciones, equivalencias y códigos; precios con vigencias | Alta y venta real en base de prueba, cambios conservando historia y permisos comprobados; 65 pruebas aprobadas y recorrido de navegador |
| 2 | Configuración y cuentas — completado en 0.4.0/0.5.0 | Configuración versionada, cuentas/credenciales, roles/delegación y soporte temporal de diagnóstico | 83 pruebas y recorridos de navegador aprobados. Restauración por soporte continúa en el hito 11; aceptación del negocio pendiente |
| 3 | Autorizaciones — recepciones e inventario en 0.6.0/0.8.0 | Extender revisión por contenido/versión a anulaciones | T10 comprobado en recepciones e inventario: versiones y saldos anteriores se rechazan sin efectos; anulaciones se prueban en su módulo |
| 4 | Proveedores y recepciones — completado en 0.6.0 | Mantenimiento, borradores, aceptación, costo de entrada, lotes, revisión y entrada única | T02 comprobado con 99 pruebas totales y navegador; integración de costo/lotes con salidas se conserva en los hitos 5/6 |
| 5 | Inventario operativo y valoración — integrado en 0.9.0/0.10.0 | Promedio ponderado móvil confirmado por el usuario; ventas, cargas, ajustes y salidas con lotes y costos históricos | 132 pruebas y navegador aprobados. Aceptación y conciliación física real pendientes |
| 6 | Promociones, impuestos e integración de ventas — implementado en 0.12.0 | Promociones exclusivas, vigencias, tasas/exenciones configurables y condiciones históricas; costo/lotes integrados | 145 pruebas y navegador aprobados; fiscalidad real y aceptación operativa permanecen pendientes |
| 7 | Anulaciones y regularización de caja | Solicitud con motivo/evidencia, aprobación, mercadería físicamente reincorporada, devolución de fondos y estados pendientes | Conservar venta original y cierre antiguo; no devolver stock o dinero sin evidencia ni repetir efectos. T06 y regresiones T08/T09 comprobados |
| 8 | Impresión y reimpresión | Agente local, impresión automática tras confirmar, intentos y estados, copia del mismo folio y contingencia | La falla de impresora no repite venta/cobro. La aceptación de la cola no se confunde con papel físico. T07 y pruebas con equipo real comprobados |
| 9 | Gastos y reportes | Registro/corrección trazable de gastos; ventas, inventario, costos/márgenes y caja; filtros; exportación PDF/Excel; vista limitada para Inventario | Reportes reconciliados con operaciones; costo desconocido muestra margen no calculable; permisos y T11 comprobados |
| 10 | Correo | Configuración protegida, envío de reportes, pendientes sin conexión, intentos y resultado incierto | El núcleo funciona sin Internet; no hay reenvío automático de un correo posiblemente aceptado. T12 comprobado |
| 11 | Respaldo y recuperación | Copia consistente a medio externo, verificación y errores visibles; restauración autorizada; preservación, conciliación y cuarentena de colas | Restaurar una copia de prueba sin duplicar impresiones/correos y con revisión de actividad posterior. T14/T15 y objetivos acordados ensayados |
| 12 | Validación integral y ejecución final | Regresión de todos los módulos; datos válidos/inválidos; permisos, rendimiento y operación sin Internet; periféricos; instalación/arranque local y aceptación con el negocio | Sistema reproducible en el equipo objetivo, matriz completa con resultados/evidencias, incidencias resueltas o limitaciones explícitas y demostración ensayada |

Los hitos 2 y 3 habilitan las autorizaciones de recepciones, inventario y anulaciones. El hito 5 aporta la valoración necesaria para márgenes de gestión. La recuperación de los trabajos de impresión/correo se cierra cuando esos componentes ya pueden probarse conjuntamente.

## Trabajo continuo en cada hito

- Ampliar la auditoría a la operación y a sus rechazos críticos, con responsable, referencia, cambios y autorización cuando corresponda (T13).
- Comprobar permisos del servidor y coherencia de datos; usar migraciones explícitas si cambia la estructura.
- Ejecutar pruebas funcionales, de integración y de validación. Conservar capturas explicadas y trazabilidad RF/CU/pantalla.
- Actualizar modelos, documentación técnica, manuales y matriz mientras se desarrolla.
- Publicar commit y push por cada funcionalidad terminada, actualizar VERSION/CHANGELOG, comprobar GitHub Actions y conservar su etiqueta. Las correcciones reciben versión de parche; la documentación sola no cambia la versión de código.

Las pruebas de aceptación con el personal del negocio son una actividad distinta de las pruebas automatizadas con datos ficticios.

## Validaciones del negocio que deben resolverse antes de cerrar funciones afectadas

| Decisión | Funciones afectadas |
|---|---|
| Unidades, equivalencias, redondeos y valoración de costos | Catálogo, recepción, inventario, ventas y reportes |
| Fiscalidad, datos del emisor y validez del comprobante | Configuración, condiciones comerciales y comprobantes |
| Discrepancia física, evidencia y responsabilidad directa del Propietario | Inventario, autorizaciones y anulaciones |
| Fondo habitual, zona horaria y umbral de próximo vencimiento | Caja, configuración y alertas |
| Procedimiento de transferencia, si se decide habilitarlo | Cobro, verificación, anulación y conciliación |
| Impresora, contingencias y configuración de correo | Impresión y envío de reportes |
| Pauta de copia externa, retención, responsables y recuperación | Continuidad y conservación de evidencias |

Efectivo continúa siendo el medio habilitado del MVP. Tarjeta permanece excluida y transferencia condicionada a un procedimiento validado. No se amplía el alcance a múltiples sucursales, contabilidad general, nómina u otras funciones no previstas.

## Documentación y entrega del cuarto corte

La documentación avanza en paralelo y se cierra con resultados reales del sistema. La guía exige los siguientes ocho entregables:

| Entregable | Trabajo pendiente |
|---|---|
| Documento técnico-académico consolidado PDF | Integrar capítulos I–IV; actualizar requisitos/diseño cuando cambie la implementación; completar introducción, resumen/abstract, marco teórico, resultados, conclusiones, recomendaciones, APA 7 e índices automáticos |
| Código fuente | Mantener el repositorio y etiquetar la versión final comprobada |
| Base y archivos de ejecución | Preparar distribución reproducible y datos apropiados para entrega; verificar dependencias y arranque en el equipo objetivo. Las bases operativas y secretos no se publican en GitHub |
| Diagramas y prototipos editables | Actualizar las fuentes para que correspondan al sistema implementado y conservar su editabilidad |
| Manual técnico y manual de usuario PDF | Instalar/configurar/operar los módulos reales; incluir procedimientos y limitaciones verificadas |
| Matriz de pruebas y evidencias PDF o Excel | Completar casos por requisito con esperado/obtenido/estado, incidencias, correcciones y capturas numeradas/tituladas/explicadas |
| Presentación PowerPoint o PDF | Actualizar el contenido con desarrollo, pruebas, resultados y límites reales |
| Demostración funcional | Preparar y ensayar un recorrido de negocio integrado, con instalación y datos controlados |

El documento final integra los cortes anteriores; no se entregará un cuarto informe aislado. Los índices de contenido/tablas/figuras se generarán automáticamente. Los ensayos y capturas no se presentarán como aceptación del negocio si no hubo participación real.

## Referencias de trabajo

- [Estado implementado](Estado_Cuarto_Corte.md).
- [Decisiones pendientes](Decisiones_y_Pendientes.md).
- [Base de datos](Base_Datos.md).
- [Estructura del documento final](Documento_Final_Estructura.md).
- Guía académica local: Unidad IV/Cuarto Corte.pdf.
- Matrices de requisitos y criterios T01–T15: revisión del tercer corte del 11 de noviembre de 2026.
