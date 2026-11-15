# Frontend branded de MEGA · 0.23.0

Implementación en `feature/frontend-branded`, solicitada el 15 de noviembre de 2026. Se conserva `main` en 0.22.0; esta rama no se fusiona ni genera una etiqueta de main.

## Fuente y criterio visual

Referencia principal: [alta fidelidad de MEGA](https://www.figma.com/design/wzTrAOsYPbM6B6WxJdmRsW/MEGA-PROTOTIPO?node-id=46-2). Se revisaron el brief y los materiales de la carpeta de diseño MEGA: selección del principal ovalado v01, compacta minimalista v02, brandbook de 32 páginas, referencias y repertorio de 160 recursos. Para software prevalecen las correcciones de marca y navegación del 15 de noviembre y la composición despejada de las páginas 20 y 30 del brandbook.

Se mantienen verde #18392D, crema #F7F0DD y acento #EF8C45. Nunito Sans sirve a formularios, importes y navegación; DynaPuff Bold aparece en los titulares de bienvenida. Todas las fuentes y todos los recursos utilizados se sirven desde la aplicación, con sus licencias incluidas. No se necesitan solicitudes a Figma, Google Fonts ni otros servicios durante el uso.

Los principales SVG verde y negativo se copiaron sin alterar sus bytes desde el pack de identidad. Los iconos, la compacta de 56 px, la mascota negativa de 280 px y la ilustración del recibo se descargaron de Figma sin redibujarlos. El logo del comprobante se exportó de la instancia original 73:9532 para conservar el fondo transparente de esa pantalla. [Procedencia, medidas y SHA-256](evidencias/frontend-branded/recursos.json).

## Integración funcional

- Acceso de dos paneles, errores de credenciales y ayuda de demostración desplegable.
- Bienvenida con mascota y cuatro indicadores calculados desde el turno real. Administración e Inventario conservan inicios con tareas correspondientes a su rol.
- Barra lateral de 248 px y compacta de 80 px, grupos desplegables, panel relacionado y estado activo. La preferencia de tamaño se conserva localmente.
- Menú móvil con cierre, Escape, foco contenido y retorno al botón de apertura; accesos inferiores de caja en cuentas habilitadas. Los grupos usan controles nativos accesibles por teclado.
- Colores, tipografía, bordes y controles compartidos en los módulos existentes; venta de dos columnas en escritorio y apilada en móvil; comprobante con logo y recurso original de compra completada.
- Las opciones disponibles siguen los roles, el permiso operativo de caja y la delegación de cuentas existentes. No se habilitan acciones por aparecer en un estado prefijado del prototipo.
- La búsqueda, las cantidades, los importes, los cobros y las confirmaciones continúan usando sus servicios y datos reales. Se conserva la política de seguridad de contenido; los avisos de validación solo se generan cuando existen errores.

No se cambian esquema, datos operativos, transporte de impresión, correo ni política de autorización. La personalización del comprobante de esta entrega corresponde a su vista web; no modifica el generador del recibo térmico.

## Validación y evidencia

Compilación Release sin errores ni advertencias. [266/266 pruebas aprobadas](evidencias/frontend-branded/suite.trx), incluidas 11 nuevas comprobaciones de navegación, roles, delegación y selección de área. [Resultado de Edge](evidencias/frontend-branded/resultado.json) con base ficticia aislada, 21 rutas del menú en escritorio/móvil y 69 comprobaciones de geometría y recursos. Sin errores JavaScript ni consola, respuestas fallidas ni solicitudes externas en el recorrido final.

Se comprobaron anchos de 320, 390, 768, 1024 y 1440 px; logos e iconos cargados y sin deformación; menú compacto, panel relacionado, persistencia, teclado y menú móvil. Las capturas finales se revisaron frente a las pantallas y componentes de Figma.

Recorrido funcional: apertura con C$480, arroz y leche por C$86, rechazo del cobro insuficiente, C$100 recibidos, C$14 de cambio, C$566 esperados y cierre contado de C$560 con explicación de diferencia. Se verificaron los inicios de Cajero, Inventario, Administrador y Propietario.

Capturas principales:

- [Acceso](evidencias/frontend-branded/01_acceso.png).
- [Inicio y turno](evidencias/frontend-branded/05_inicio_turno.png).
- [Venta](evidencias/frontend-branded/06_venta.png) y [móvil](evidencias/frontend-branded/07_venta_movil.png).
- [Comprobante](evidencias/frontend-branded/10_comprobante.png).
- [Inventario](evidencias/frontend-branded/12_inicio_inventario.png).
- [Catálogo y panel compacto](evidencias/frontend-branded/17_catalogo_compacto.png).

Para repetir el recorrido, iniciar la aplicación en Testing o Development con `MEGA:DemoData=true`, una ruta de base nueva y separada y la URL HTTPS de prueba. Ejecutar `tools/frontend-check.cjs` con `MEGA_URL` y el Playwright disponible mediante `MEGA_PLAYWRIGHT`. El script registra una venta y un cierre ficticios; no usarlo contra una base operativa.

## Límites

Las 56 vistas de Figma incluyen estados prefijados y muestran datos de ejemplo. La implementación conserva estados y cálculos de la aplicación, por lo que el contenido y algunas alturas dependen de la cuenta y de sus registros. El recorrido de navegador cubre las rutas y estados indicados, sin atribuir una reproducción exhaustiva de todos los estados del prototipo.

Continúan pendientes la aceptación visual del diseñador/negocio, el ensayo en el equipo objetivo y las pruebas físicas de impresión. Los manuales PDF anteriores conservan su alcance histórico y sus capturas; esta entrega no los reedita.
