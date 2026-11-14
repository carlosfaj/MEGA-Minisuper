# Estado del cuarto corte de MEGA

Estado comprobado al 14 de noviembre de 2026: **0.21.0**, 223 pruebas acumuladas aprobadas y recorridos de navegador en bases aisladas. El [kanban](KANBAN.md) es la referencia del porcentaje y de todos los criterios, incluidos entregables y validaciones reales. La implementación es local; los recursos del núcleo no requieren Internet.

| Requisito | Implementado y comprobado | Pendiente real |
|---|---|---|
| RF01 Catálogo | Categorías, productos, presentaciones, equivalencias, códigos, precios históricos, promociones e impuestos configurables | Datos maestros y fiscalidad del negocio |
| RF02 Abastecimiento | Proveedores, recibido/aceptado, costos/lotes y revisión por otra cuenta | Aceptación y conciliación con documentos reales |
| RF03 Inventario | Existencias, conteos, lotes/alertas, cargas, ajustes/salidas autorizados y costos/lotes integrados | Comprobación física y políticas operativas reales |
| RF04 Ventas | Efectivo, recuperación sin doble cobro, promociones/impuestos, costos históricos, anulaciones, impresión/copias y contingencia | Impresora física, entrega real y validez del comprobante |
| RF05 Caja | Fondo real, cierre esperado/contado/diferencia y devolución posterior sin alterar cierres | Ensayo con responsables y efectivo reales |
| RF06 Acceso/autorización | Cuentas, roles/delegación, soporte temporal de diagnóstico, revisiones exactas y restauración autorizada por Propietario | Aceptación del negocio |
| RF07 Gestión | Gastos, reportes, costos/márgenes, PDF/Excel, vista limitada de Inventario, correo congelado y cuatro aceptaciones SMTP Gmail en prueba personal | Recepción/lectura, envío real de Excel y aceptación de resultados por el negocio |
| RF08 Auditoría | Eventos transaccionales, rechazos críticos, fallos, intentos, recuperación y resoluciones; matriz T01–T15 revisada | Revisión final de seguridad |
| RF09 Continuidad | Copia consistente desde interfaz, pauta autorizada, archivos asociados, manifiesto/huellas, restauración autorizada, preservación, conciliación, cuarentena y numeración conservada | Medio externo real, volumen objetivo y aceptación de recuperación |

El modelo original conserva 39 tablas, 499 columnas y 103 claves externas. Sus bytes y procedencia se mantienen; el esquema 2 agrega restricciones y vistas sin sustituir el diseño. El usuario confirmó promedio ponderado móvil el 12 de noviembre. Efectivo es el medio habilitado.

## Trabajo siguiente

1. Respaldo verificable implementado; comprobar el destino y medios físicos con el negocio.
2. Restauración implementada y ensayada en bases aisladas; comprobar volumen, medios y conciliación con responsables reales.
3. Paquete Windows reproducible preparado y ensayado sin bases operativas ni credenciales; instalación y rendimiento en equipo objetivo pendientes.
4. Manuales y matriz T01–T15 consolidados y revisados en [Manuales y validación](Manuales_y_Validacion.md); faltan capítulos I–IV, diagramas editables actualizados y presentación.
5. Completar ensayo con equipo y personal del negocio; cuenta SMTP real ensayada a nivel de aceptación del servidor. Confirmar recepción/lectura y documentar demostración y aceptación.

Los [documentos por módulo](../README.md) contienen alcance y evidencias. Los ensayos automatizados usan datos ficticios, impresora PDF y SMTP TLS local. La [prueba manual adicional de Gmail](evidencias/correo/Prueba_Gmail_20261114.md) del 14 de noviembre incorpora cuatro aceptaciones SMTP externas y comprobación de los PDF conservados. No acredita recepción/lectura, impresión física, entrevistas nuevas, fiscalidad, aceptación del negocio ni despliegue comercial. La guía exige un documento académico consolidado.
