# Decisiones de implementación y validaciones pendientes

## Base vigente

La implementación parte del modelo corregido del 11 de noviembre de 2026: 39 tablas, 499 atributos y 103 FK. Las copias de Schema.sql y Modelo_Datos_MEGA.json conservan el contenido de sus fuentes. El desarrollo se ubica en 01_DESARROLLO; 00_OFICIALES conserva las entregas previas.

## Decisiones técnicas del avance

| Decisión | Aplicación y motivo |
|---|---|
| ASP.NET Core 10, Razor Pages, EF Core 10.0.14, SQLite | Continúa la arquitectura documentada y utiliza el SDK disponible 10.0.403. Las dependencias quedan fijadas en los proyectos |
| DDL original conservado y esquema actual 2 | Conserva STRICT, CHECK y FK. La migración 002 agrega índices, vistas y triggers en una transacción; una evolución posterior requiere otra migración |
| Dinero entero en centavos | Los cálculos autoritativos usan decimal; no se persisten valores monetarios REAL |
| Cantidad de peso a tres decimales | Implementa la propuesta del diseño; mitades hacia arriba para cantidad por importe y total por línea. Debe aprobarse comercialmente |
| Venta por importe | Muestra cantidad y total efectivos después del redondeo. C$10 de arroz a C$24/lb resulta en 0.417 lb y C$10.01; el usuario debe revisar ese total antes de cobrar |
| Transacción de confirmación | Venta, detalle, pago, caja, movimiento de inventario, auditoría y solicitud de impresión se confirman juntos; fallo tardío revierte todos |
| Clave de operación e intención | Recupera un resultado previo solo para su operador y contenido original. La misma clave con otro cobro o cuenta se rechaza |
| Concurrencia en SQLite | La transacción de escritura serializa confirmaciones; índices únicos refuerzan clave y turno. El cierre compara versión, incrementada por ventas |
| Revisión de condiciones | El hash de cotización incluye configuración, productos/presentaciones versionados, precios, cantidades y total; un cambio exige revisión antes de confirmar |
| Existencia negativa | No bloquea únicamente por signo. Exige comprobación física explícita y conserva incidencia en auditoría |
| Impresión | Solo se guarda la solicitud Pendiente dentro de la transacción. No se afirma integración con impresora ni salida física |
| Costos | Ventas básicas conservan costo desconocido NULL. Recepciones y cargas conservan costo conocido declarado por unidad base. El POS y regularizaciones de productos con costo conocido/lotes se bloquean hasta integrar su valoración; no se presenta margen inventado |
| Seguridad local | HTTPS, equipo local, cookies HttpOnly/Secure/SameSite Strict, antifalsificación, contraseñas con PasswordHasher, rol en servidor, sesión de 30 minutos y revocación por versión |
| Recuperación de confirmación incierta | La pestaña conserva clave, intención y cotización en sessionStorage por cuenta; permite reintentar sin otro cobro. No sustituye la continuidad ante pérdida completa del equipo/navegador |
| Zona horaria provisional | UTC en persistencia; UTC−6 en pantalla por ubicación del negocio en Managua. La zona operativa debe confirmarse con él |
| Inicialización ficticia | Cuentas, catálogo y carga demo con dos identidades diferentes; constituye una preparación técnica de pruebas y no una aprobación real del negocio |
| Claves compuestas | El mapeo identifica todas las columnas de PromocionProducto y ProductoImpuesto; permite asociaciones y periodos múltiples sin confundir identidades |
| Contrato y diagnóstico | Verifica fuentes originales, definiciones SQL, columnas, nulabilidad, PK, FK y mapeo; revisa integridad física, referencias, saldos y hashes de comprobantes |
| Copia previa de migración | Copia consistente local antes de modificar una base con esquema 1. No sustituye la política de respaldo externo y restauración de RF09 |
| Base sin demostración | Requiere contraseña suministrada; la cuenta inicial tiene rol Propietario y no se crean productos o ventas ficticios |

## Decisiones que requieren al negocio

- Fiscalidad, datos del emisor y condiciones de validez del comprobante.
- Redondeos, unidades, equivalencias, límites comerciales, valoración y tratamiento de costo desconocido.
- Disponibilidad física ante discrepancia, responsabilidad directa del Propietario y evidencia requerida.
- Moneda, ubicación/dirección, zona horaria y fondo habitual de operación real.
- Vencimiento: umbral de proximidad sin valor predeterminado inventado.
- Procedimiento para transferencia; permanece deshabilitada. Tarjeta excluida del MVP.
- Pauta y retención de respaldo externo, conservación de historia y evidencias, responsables de recuperación.
- Equipo/impresora reales y contingencias; objetivos de recuperación sujetos a ensayo.

Estas validaciones no impiden probar el recorrido con datos ficticios. Deben resolverse antes de implantar las funciones afectadas o acreditar aceptación real.

## Continuidad implementada en 0.20.0

La restauración de interfaz exige Propietario, copia registrada comprobada e identidad coincidente; conserva el estado anterior, drena agentes/solicitudes y mantiene pausa hasta conciliación. El núcleo no fusiona operaciones posteriores. Conserva numeración alcanzada y seguridad actual de cuentas coincidentes; revoca sesiones, soporte temporal y revisiones pendientes. La reapertura conserva cuarentena y requiere reautorizar periféricos/pauta. Detalles y límites: [Restauracion.md](Restauracion.md). Los ensayos aislados no sustituyen la aprobación de pauta, retención y recuperación con medios/volumen reales.
