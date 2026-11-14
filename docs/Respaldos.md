# Respaldo verificable

Desde 0.19.0 **Respaldos** permite generar, descargar y volver a comprobar una copia. El Propietario autoriza una carpeta existente, identifica el medio y decide si activa copias periódicas, al cierre o antes de otra versión de la aplicación. Gestión puede ejecutar una copia manual con esa pauta. Las opciones automáticas permanecen deshabilitadas hasta su autorización. No se borran copias anteriores automáticamente.

Una carpeta del mismo equipo permite ensayar el recorrido; no equivale a protección externa. Conectar y alternar medios reales y comprobar capacidad siguen pendientes del negocio. La propuesta de 30 minutos y el objetivo de recuperación no se presentan como garantías alcanzadas.

## Contenido y comprobación

Se utiliza la [API de respaldo de SQLite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup), que obtiene una instantánea consistente, incluidos cambios confirmados en WAL. No se copia a ciegas un archivo de base abierto. La API puede bloquear brevemente otras escrituras mientras toma la instantánea.

El ZIP contiene la base, los reportes congelados referenciados, las credenciales cifradas referenciadas y las salidas PDF administradas. Los archivos operativos están junto a la base en `<nombre-base>.archivos`, de modo que actualizar el programa conserva su ubicación. Las salidas PDF antiguas que estén fuera de ese almacén se identifican como advertencia y conservan su referencia histórica.

Desde 0.20.0 el manifiesto 2 agrega límites de numeración y preservaciones anteriores; se sigue leyendo el formato 1. El manifiesto registra versión del formato, aplicación y esquema, intervalo de toma de la instantánea, últimos identificadores de operaciones/movimientos/cierres/auditoría y cantidades de filas. Cada archivo tiene tamaño y SHA-256. Después de escribir el ZIP se vuelve a abrir, se comprueban archivo completo, referencias, tamaños y huellas, se extrae en un espacio temporal controlado y se verifica integridad física, relaciones, esquema y coincidencia del punto de recuperación. Solo entonces la ejecución queda Comprobada.

Se rechazan archivos incompatibles, nombres duplicados, rutas fuera del formato, contenido incompleto y puntos de recuperación incoherentes. La revisión tiene límites de archivos y tamaño, y no extrae fuera del directorio temporal. Los archivos de copia se crean con nombre único sin sobrescribir anteriores. Las descargas vuelven a comparar la huella con el registro de la base.

La copia de la base refleja el momento de la instantánea: su propia ejecución puede figurar todavía En_proceso dentro de ella. El manifiesto y el registro final externo a esa instantánea reflejan su comprobación posterior. Esa diferencia debe considerarse al registrar una restauración; no se altera el ZIP ya comprobado.

## Pautas y fallos

El agente revisa la pauta cada treinta segundos. Una copia periódica requiere intervalo autorizado; una copia al cierre se deriva de cierres todavía no incluidos en una copia comprobada. Se evita repetir inmediatamente la misma copia y los fallos automáticos esperan el intervalo. La copia previa a una versión se ejecuta antes de inicializar el núcleo cuando cambia la versión registrada y el esquema es compatible. Un fallo del medio queda visible y no borra la copia anterior.

Un medio ausente, falta de permisos/espacio o fallo de diagnóstico no producen un éxito ficticio. La nueva comprobación distingue un archivo alterado de un medio que no se puede consultar ahora. Los intentos, autorizaciones y resultados quedan en auditoría. Las credenciales cifradas requieren el perfil original de Windows o configuración nueva en el equipo recuperado.

## Evidencias y alcance

208 pruebas acumuladas aprobadas en `evidencias/respaldo/respaldo.trx`. Las pruebas dirigidas verifican WAL, ventas y archivos asociados, credenciales protegidas, claves recuperables, solicitudes concurrentes, permisos, destino, ausencia del medio conservando la copia previa, copia alterada, pautas y manifiestos manipulados.

El navegador autorizó la pauta con Propietario, generó con Admin sobre una venta y un reporte conservado, comparó el ZIP descargado con su SHA-256, simuló medio ausente moviendo únicamente una carpeta privada de ensayo, repuso el medio y comprobó la copia anterior. Se corrigió un desbordamiento de rutas largas en móvil. Capturas, manifiesto y JSON documentan el resultado; el ZIP con base de prueba permanece excluido de Git. No se acredita USB real ni restauración comercial. La [restauración, preservación y conciliación](Restauracion.md) están implementadas y ensayadas en 0.20.0.
