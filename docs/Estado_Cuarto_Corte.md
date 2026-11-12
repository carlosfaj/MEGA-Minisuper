# Estado del cuarto corte de MEGA

Estado comprobado al 12 de noviembre de 2026: **0.18.0**, 200 pruebas acumuladas aprobadas y recorridos de navegador en bases aisladas. El [kanban](KANBAN.md) es la referencia del porcentaje y de todos los criterios, incluidos entregables y validaciones reales. La implementación es local; los recursos del núcleo no requieren Internet.

| Requisito | Implementado y comprobado | Pendiente real |
|---|---|---|
| RF01 Catálogo | Categorías, productos, presentaciones, equivalencias, códigos, precios históricos, promociones e impuestos configurables | Datos maestros y fiscalidad del negocio |
| RF02 Abastecimiento | Proveedores, recibido/aceptado, costos/lotes y revisión por otra cuenta | Aceptación y conciliación con documentos reales |
| RF03 Inventario | Existencias, conteos, lotes/alertas, cargas, ajustes/salidas autorizados y costos/lotes integrados | Comprobación física y políticas operativas reales |
| RF04 Ventas | Efectivo, recuperación sin doble cobro, promociones/impuestos, costos históricos, anulaciones, impresión/copias y contingencia | Impresora física, entrega real y validez del comprobante |
| RF05 Caja | Fondo real, cierre esperado/contado/diferencia y devolución posterior sin alterar cierres | Ensayo con responsables y efectivo reales |
| RF06 Acceso/autorización | Cuentas, roles/delegación, soporte temporal de diagnóstico y revisiones exactas | Restauración autorizada y aceptación del negocio |
| RF07 Gestión | Gastos, reportes, costos/márgenes, PDF/Excel, vista limitada de Inventario y correo congelado | Cuenta SMTP real y aceptación de resultados |
| RF08 Auditoría | Eventos transaccionales, rechazos críticos, fallos, intentos y resoluciones | Ampliar regresión con respaldo/restauración |
| RF09 Continuidad | Migración con copia previa, diagnóstico e integridad | Respaldo desde interfaz, destino externo, restauración, conciliación y cuarentena |

El modelo original conserva 39 tablas, 499 columnas y 103 claves externas. Sus bytes y procedencia se mantienen; el esquema 2 agrega restricciones y vistas sin sustituir el diseño. El usuario confirmó promedio ponderado móvil el 12 de noviembre. Efectivo es el medio habilitado.

## Trabajo siguiente

1. Respaldo consistente y verificado de base y archivos asociados; destino externo y errores visibles.
2. Restauración autorizada con preservación del estado actual, actividad posterior identificada, conciliación y cuarentena de impresiones/correos.
3. Regresión integrada y distribución reproducible sin bases operativas ni credenciales.
4. Consolidación de capítulos I–IV, manuales, matriz final, diagramas editables y presentación.
5. Ensayo con equipo, cuenta SMTP y personal del negocio; demostración y aceptación documentadas.

Los [documentos por módulo](../README.md) contienen alcance y evidencias. Los ensayos usan datos ficticios, impresora PDF y SMTP TLS local; no acreditan impresión física, mensajes a personas, entrevistas nuevas, fiscalidad, aceptación del negocio ni despliegue comercial. La guía exige un documento académico consolidado.
