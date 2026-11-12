# Historial de versiones de MEGA

## 0.4.0 — 12 de noviembre de 2026

- Configuración versionada de nombre, dirección, contacto, fondo habitual, alerta de vencimiento y datos de emisor/impresora previstos.
- Los cambios conservan comprobantes y fondos de turnos abiertos; se aplican a nuevas operaciones.
- Ediciones obsoletas rechazadas, auditoría transaccional y permisos de gestión comprobados.
- 71 pruebas automatizadas aprobadas; recorrido de navegador sobre una base aislada con cinco comprobaciones y tres capturas.
- Fiscalidad, impresión física y medios adicionales siguen sujetos a integración y validación.

## 0.3.0 — 12 de noviembre de 2026

- Catálogo operativo para gestión: categorías, productos, desactivación, presentaciones/equivalencias, códigos y precios con vigencias.
- Códigos con ceros iniciales y rechazo de reasignación de códigos desactivados; unidades y equivalencias usadas se conservan.
- Control de ediciones desactualizadas y auditoría de cambios en la misma transacción.
- Precios inmediatos o programados, intervalos adyacentes y comprobantes anteriores conservados (T01).
- Límite de acceso aplicado solo a intentos de contraseña, sin consumir intentos al consultar el formulario. Mensaje y plazo de reintento ante 429.
- 65 pruebas automatizadas y recorrido de navegador aislado: alta completa, venta, cambio de precio, historia conservada y nuevo precio efectivo.
- Promociones, impuestos y venta con lotes/costos conocidos permanecen en los hitos posteriores.

## 0.2.0 — 12 de noviembre de 2026

### Base de datos

- Corrección del mapeo de claves compuestas de PromocionProducto y ProductoImpuesto, con persistencia y recuperación de múltiples asociaciones.
- Creación atómica y migración del esquema 1 al 2, copia consistente previa y conservación de datos. No se recrean bases existentes ni se sobrescriben versiones desconocidas.
- Contrato de estructura y mapeo de las 39 tablas, 499 columnas y 103 FK; comprobación física, relacional, de importes y de hashes de comprobantes.
- Dos vistas de lectura para existencias y resumen de caja; saldos en enteros escalados/centavos sin tablas mutables de saldo.
- 21 triggers para vigencias de precios, orígenes compatibles y protección de comprobantes, detalles, pagos, caja, cierres, auditoría y cantidades de inventario. Tres índices adicionales.
- Diagnóstico de solo lectura con `--db-check`, actualización explícita con `--db-upgrade` e iniciador Revisar_Base_Datos.cmd.
- Inicialización sin demostración con contraseña suministrada y cuenta inicial de Propietario; sin catálogo ni ventas ficticias.

### Validación

- 50 pruebas de integración y base de datos, incluidos migración con ventas existentes, venta posterior a la migración, reversión de migración fallida, claves compuestas y conservación de historia.
- Base local actualizada al esquema 2. Se verificaron por resumen criptográfico los registros anteriores de sus 39 tablas; solo se añadió el evento de actualización.
- Comprobación de una distribución publicada e informes en docs/evidencias/base_datos.

### Alcance

La copia previa de migración es un control técnico local. El módulo de respaldo externo, restauración autorizada y cuarentena de colas de RF09 continúa pendiente. Los permisos, autorizaciones/versiones, cálculos de negocio y reglas entre múltiples registros siguen requiriendo el servicio transaccional.

## 0.1.0 — 12 de noviembre de 2026

Primera versión registrada en GitHub, correspondiente al avance funcional iniciado el 11 de noviembre.

### Funcionalidades

- Acceso local con cuentas, roles, permiso operativo e invalidación de sesiones.
- Apertura y cierre de caja propia con fondo real, esperado, contado y motivos de diferencias.
- Venta en efectivo por unidad, peso manual e importe, cotización autoritativa y revisión ante cambios.
- Confirmación transaccional de venta, pago, caja, inventario, auditoría y solicitud de impresión.
- Recuperación de una confirmación incierta y protección ante reintentos concurrentes sin duplicar cobros.
- Ventas del turno, comprobantes conservados, consulta de existencias y bitácora por rol.
- Modelo de 39 tablas, documentación del avance, matriz y capturas reales con datos ficticios.

### Registro y comprobación

- Git y repositorio remoto carlosfaj/MEGA-Minisuper.
- Política persistente de commit/push por funcionalidad terminada y por versión estable.
- Compilación y pruebas en GitHub Actions; etiquetado de versiones aprobadas en main.
- 23 pruebas automatizadas de integración/validación y dos recorridos de navegador aprobados en el avance inicial. La comprobación de publicación se registra en GitHub Actions.

### Alcance

Versión estable para el recorrido implementado y su demostración académica. Recepciones, autorizaciones, anulaciones, impresión física, valoración, gestión completa y continuidad permanecen pendientes. No acredita aceptación por el negocio ni implantación comercial.
