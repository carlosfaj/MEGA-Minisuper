# Inicios operativos por rol

Implementados primero en `main`, versión 0.23.0, con el prototipo de media fidelidad del 15 de noviembre de 2026. La revisión visual del usuario permanece pendiente. La rama brandeada recibe después el mismo comportamiento, conservando su identidad y sin fusionarse con main.

Esta copia documenta la adaptación posterior en `feature/frontend-branded`, versión 0.24.0. El servicio, el contenido de la vista compartida y los siete casos nuevos de pruebas coinciden con los de main. Se conservan los recursos, colores, fuentes y navegación de la rama brandeada.

## Criterio de diseño

- Inventario: identificar mínimos y vencimientos, continuar preparaciones y conteos propios y consultar los envíos que esperan otra cuenta. Acción principal: registrar recepción.
- Administrador: atender revisiones de recepciones, inventario y anulaciones; supervisar turnos abiertos, diferencias de cierres y ventas del día. Acción principal: ir a solicitudes.
- Propietario: conocer ingreso, gastos, margen bruto y resultado después de gastos del día; revisar excepciones y autorizaciones. Acción principal: abrir el reporte de resultados con el mismo periodo.
- Cajero: conserva su inicio y flujo actuales.

Se reutilizan tarjetas, controles, tablas y colores existentes de cada rama. Cuatro indicadores breves preceden al trabajo concreto; las listas enlazan al registro correspondiente. En móvil se apilan y se conserva el contenido. Sin datos se muestran estados explícitos; los valores no disponibles nunca se sustituyen por cero.

## Datos y alcances

`HomeDashboardService` consulta registros persistidos, sin crear operaciones ni cambiar existencias. Inventario recibe cantidades, mínimos, lotes y su propio trabajo; no recibe resultados comerciales, importes ni accesos de aprobación. Gestión recibe las revisiones pendientes no invalidadas, con consulta de solicitudes propias y revisión por otra cuenta. La pantalla de revisión vuelve a comprobar vigencia y permisos antes de una decisión.

Los indicadores económicos reutilizan `ReportService`, sus costos históricos, exclusión de ventas anuladas y gastos vigentes. El periodo es el día local UTC−6 y aparece en el encabezado. Los costos desconocidos producen «No calculable»; si el reporte excede sus límites, aparece «No disponible» con explicación. No se convierte un resumen parcial en un total completo.

Los turnos abiertos comprenden todas las estaciones. Las diferencias corresponden a cierres realizados durante el día, conservando importes históricos; no se suman fondos como ingresos. Las devoluciones pendientes cuentan anulaciones autorizadas cuyo efectivo aún no se ha entregado completamente.

Mínimos y lotes muestran el estado actual, incluyendo discrepancias. Los avisos de vencimiento usan únicamente el umbral configurado y lotes con saldo; si falta el umbral se indica. Los productos con control de vencimiento y saldo sin lote se señalan para conciliación. Las listas muestran hasta seis trabajos, cinco productos/lotes o cinco turnos y explicitan el total cuando hay más. Se actualizan al abrir o recargar, sin prometer actualización en tiempo real.

## Comprobación

- Compilación Release sin errores ni advertencias; 273 pruebas aprobadas (262 en main), con siete casos nuevos de inicios, permisos, trabajo propio, revisiones invalidadas, mínimos, vencimientos, resultados y cierres.
- `tools/home-check.cjs` recorre las cuatro cuentas en una base ficticia nueva, crea una recepción autorizada y dos pendientes, un conteo, una venta de C$38 y un cierre con diferencia de −C$1. Verifica enlaces y restricciones de revisión propia, estados vacíos y anchos de 320, 390, 768, 1024 y 1440 px.
- Capturas y resultado de esta rama en `docs/evidencias/inicios-roles-branded/`. Los errores del inicio se registran por separado de la consola de formularios usados en la preparación. La media fidelidad conserva avisos previos de CSP al emitir resúmenes vacíos de validación en esos formularios; la rama brandeada ya tenía corregida su emisión. No se debilita la política de seguridad.

Las comprobaciones usan datos ficticios. La aprobación visual y la aceptación del negocio continúan pendientes. No se cambian esquema, datos operativos ni políticas de autorización.
