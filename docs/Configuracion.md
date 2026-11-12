# Configuración del negocio

Disponible para Administrador y Propietario desde Configuración. Se pueden cambiar los datos del negocio, fondo habitual, umbral de vencimiento y datos previstos del emisor/impresora. Cada guardado cierra la vigencia anterior y crea una nueva versión, con responsable y auditoría en una transacción. Un formulario anterior se rechaza si otro usuario ya cambió la configuración.

El fondo habitual nuevo se propone en la siguiente apertura; no cambia el efectivo real ni esperado de un turno existente. Los comprobantes anteriores conservan los datos de su confirmación. Cambiar datos fiscales o la impresora invalida las respectivas marcas de validación: esta pantalla no acredita fiscalidad ni habilita impresión automática. Efectivo sigue siendo el único medio habilitado.

## Validación ejecutada

71 pruebas automatizadas aprobadas en `evidencias/configuracion/configuracion.trx`. Incluyen historia, permisos, edición obsoleta, invalidación de cotización, datos inválidos y formulario HTTP con protección antifalsificación. El script `tools/settings-check.cjs` usa exclusivamente una base ficticia aislada.

- `evidencias/configuracion/01_configuracion_versionada.png`: edición y dos versiones del negocio.
- `evidencias/configuracion/02_comprobante_anterior.png`: comprobante original conservado después del cambio.
- `evidencias/configuracion/03_comprobante_nueva_configuracion.png`: nueva venta con los nuevos datos.
- `evidencias/configuracion/resultado.json`: resultado del recorrido; también verificó cierre del fondo original y apertura con el nuevo fondo.

Los parámetros todavía necesitan aceptación del negocio. No se presentan las pruebas ficticias como validación comercial.
