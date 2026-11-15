# MP58-16 por Bluetooth

Disponible desde **0.22.0** como alternativa a las impresoras instaladas de Windows. El soporte de software está implementado y ensayado con conexiones simuladas; **la unidad MP58-16 del negocio todavía no se ha conectado ni probado**. No se encontró un manual verificable de su fabricante para la variante exacta. El perfil requiere Bluetooth clásico SPP con puerto COM saliente y comandos ESC/POS; el nombre comercial por sí solo no acredita esos requisitos. Bluetooth LE sin puerto COM no está incluido.

## Configuración en el equipo de caja

1. Encender la impresora con batería y papel, y emparejarla en Bluetooth de **la computadora donde se ejecuta MEGA**. No basta con emparejarla en un teléfono o en otra computadora que abra el navegador.
2. En las opciones adicionales de Bluetooth de Windows, consultar **Puertos COM** e identificar el puerto **saliente** asociado a la impresora. Si no existe, comprobar el soporte SPP con el proveedor; no elegir un puerto de otro dispositivo.
3. Entrar como Administrador o Propietario y abrir **Configuración del negocio**. En **Nombre de impresora instalada o conexión Bluetooth**, elegir el destino correspondiente, por ejemplo `MP58-16 Bluetooth (COM7)`.
4. Marcar **Habilitar impresión automática de nuevos comprobantes** y guardar. La lista enumera puertos serie disponibles para configurar: no descubre ni identifica automáticamente impresoras. Consultarla nunca abre ni escribe en los puertos.
5. Hacer el ensayo de abajo con datos ficticios antes de operar con clientes. Si Windows ya tiene un controlador compatible, puede mantenerse el nombre de esa impresora para usar la ruta de impresión habitual.

El ejemplo COM7 no es una asignación fija. No se instalan controladores, no se cambian dispositivos ni se habilita impresión sobre la base existente como parte de esta actualización. MEGA rechaza habilitar un destino que no esté disponible al guardar; un puerto ocupado, apagado o desconectado también puede fallar posteriormente.

## Formato y conservación de operaciones

- Perfil para papel de 58 mm, Font A y **32 columnas**; los nombres largos se distribuyen en líneas, sin truncarlos. El área imprimible real requiere prueba.
- Texto ESC/POS con PC850 (`ESC t 2`) para español, incluidos acentos y ñ. La correspondencia de tablas en el firmware concreto necesita ensayo. Un carácter que PC850 no puede representar detiene ese envío antes de abrir el puerto; no se sustituye silenciosamente.
- Conserva el contenido histórico, folio, cantidades, importes, original/copia y número de copia. No recalcula ventas, pagos, existencias ni caja.
- Envío en fragmentos de hasta 256 bytes con pausa y tiempo de escritura limitado. La apertura tiene un límite de 10 segundos: si el controlador termina tarde, se cierra esa conexión sin escribir. El envío/vaciado tiene un presupuesto de 30 segundos y cada escritura un máximo de 5 segundos. Configuración del puerto virtual: 9600, 8N1, sin control de flujo; no se afirma que sea la velocidad interna del enlace Bluetooth.
- No envía órdenes de corte ni apertura de gaveta. Se usa avance de papel y separación manual.

Las impresoras Windows conservan su transporte y el formato predeterminado de **40 columnas**. Microsoft Print to PDF conserva sus archivos administrados. No hay migración del esquema ni cambios al modelo SQL/JSON de procedencia. El destino completo se conserva en los campos históricos existentes; cambiarlo no redirige trabajos anteriores.

## Fallos y estados

La cola, los permisos, la auditoría, la recuperación y la cuarentena existentes también se aplican a Bluetooth:

| Resultado | Estado y acción |
|---|---|
| Puerto no disponible, ocupado, apertura vencida o texto no representable antes del primer envío | **Fallida**; revisar y autorizar expresamente el reintento del mismo trabajo cuando corresponda. |
| Error después de comenzar a escribir, durante el vaciado/cierre o reserva vencida | **Incierta**; comprobar papel y conexión. No se reenvía automáticamente ni se permite un reintento ciego. |
| Escritura terminada y búfer de Windows vacío | **Aceptada** con referencia `BT-…` y evidencia de puerto/bytes; no prueba que haya salido papel. |
| Papel comprobado y entregado al cliente | Declarar **entrega física** por separado, como en la impresión habitual. |

El envío directo al puerto **no aparece en la cola de impresión de Windows**. Ante incertidumbre, revisar el papel y el estado de la conexión; la cola solo corresponde a los destinos con controlador. Nunca se cambia automáticamente a otra impresora ni se repite el cobro. El código histórico `aceptada_SO` indica aceptación del envío por el sistema, también para el búfer del puerto; la evidencia distingue ambos transportes.

## Ensayo físico pendiente

En una base de demostración separada, comprobar primero una venta con un nombre largo, café/piñón/María/ñ y los importes recibidos/cambio. Confirmar que sale un solo original, que el papel no recorta columnas, que avanza suficientemente y que una copia conserva folio/importes con su marca. Probar impresora apagada antes del envío y desconexión durante el envío, revisando los estados antes de resolver. Verificar que ninguna contingencia crea otra venta o pago. Registrar equipo, puerto, firmware/autoprueba, resultado y evidencia antes de cerrar el criterio de impresora física del kanban.

Si no existe puerto SPP, no se reconoce ESC/POS o los acentos no corresponden a PC850, conservar la impresión deshabilitada para ese destino y revisar el controlador/manual del proveedor. No se acredita compatibilidad universal con equipos vendidos con el mismo nombre.

## Evidencia de software

- Suite completa en [bluetooth.trx](evidencias/bluetooth/bluetooth.trx), más pruebas específicas de formato, protocolo, selección de destinos, apertura lenta, envío parcial, original/copia y conservación de venta/cobro/caja/inventario.
- Recorrido real de Edge con base ficticia aislada y controlador **Microsoft Print to PDF**: [resultado](evidencias/bluetooth/navegador/resultado.json), original y copia aceptados sin entrega física inventada, ayuda Bluetooth visible y pantallas de escritorio/móvil sin desbordamiento. No había una impresora Bluetooth conectada.
- Comprobación deliberada de la protección contra duplicados: cambiar temporalmente la clasificación de una escritura iniciada hizo fallar las tres pruebas de error parcial/vaciado/cierre; la implementación fue restaurada antes de la validación final.

Este documento complementa los manuales PDF de 0.21.0 para el nuevo destino de 0.22.0.

## Referencias primarias

- [Microsoft: impresión Bluetooth y soporte SPP](https://learn.microsoft.com/en-us/windows-hardware/drivers/print/bluetooth-printing).
- [Microsoft: SerialPort, escritura, búfer y tiempos](https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport).
- [Epson: inicialización ESC @](https://download4.epson.biz/sec_pubs/pos/reference_en/escpos/esc_atsign.html) y [selección de tabla ESC t](https://download4.epson.biz/sec_pubs/pos/reference_en/escpos/esc_lt.html). Son referencias del protocolo; no certifican el firmware de la MP58-16.
