# Revisión de la base con MEGA abierto

Corrección oficial 0.23.1 · 15 de noviembre de 2026 · rama `main`.

## Causa y solución

`Revisar_Base_Datos.cmd` invoca `tools/database.ps1`. Antes, `dotnet run` recompilaba sobre `src/Mega.Web/bin/Release/net10.0/Mega.Web.exe`, bloqueado por el servidor abierto. Fallaba antes de ejecutar el diagnóstico, con MSB3026, MSB3027 y MSB3021.

La corrección utiliza una carpeta temporal única `MEGA-database-<GUID>`, compila sin apphost y ejecuta la DLL con el directorio de contenido original y el entorno del perfil MEGA. Conserva la configuración y selección de datos, incluidos los ajustes mediante variables de entorno. El bloque `finally` elimina únicamente la carpeta creada, después de verificar su ubicación y nombre, y conserva el código de salida del proceso.

El diagnóstico mantiene `--db-check` y la conexión SQLite de solo lectura. La opción `upgrade` comparte la compilación independiente, conservando la exclusión de instancia y las copias previas existentes. Esta corrección no autoriza actualizar el esquema mientras el servidor usa la base.

## Separación de las ramas

La misma corrección está en `feature/frontend-branded` 0.24.1, commit `4f95959`. Se trasladó únicamente el script y se adaptaron versión, documentación y kanban oficiales. No se importaron vistas, estilos, recursos de marca ni navegación del nuevo frontend. El servicio de inicios por rol, su registro y sus pruebas ya coincidían entre ambas ramas.

`main` conserva la interfaz oficial. El nuevo frontend permanece pendiente de decisión en `feature/frontend-branded`. Las funcionalidades y correcciones generales deben publicarse en `main` y compartirse con la rama experimental cuando corresponda.

## Validación

- Script idéntico al corregido de la rama experimental y sintaxis comprobada con el analizador de PowerShell.
- Compilación Release del revisor y ejecución de `Revisar_Base_Datos.cmd`: código 0, `Healthy: true`, `UpToDate: true`, `Errors: []`, hash de la base de ensayo sin cambios y ninguna carpeta temporal sobrante. El mismo servidor oficial siguió abierto.
- Ningún archivo de vistas, estilos, navegación ni recursos del programa cambió respecto a `main` 0.23.0. Solo se actualiza el seguimiento de versión dentro de `src`.
- Suite local Debug: 248 aprobadas y 14 bloqueadas al cargar `MimeKit.Core.dll` por Smart App Control de Windows, código `0x800711C7`. Los eventos de Code Integrity confirman el bloqueo; la DLL tiene el mismo SHA-256 que las copias Release y de la rama experimental. No se modificaron bibliotecas ni políticas de seguridad. Este ensayo local no se presenta como una suite completamente aprobada.
- La validación completa de esta publicación corresponde a GitHub Actions de `main`, que compila, ejecuta las 262 pruebas y genera la etiqueta oficial solo tras aprobar. El kanban conserva el último total aprobado de 262 pruebas y 111/133 criterios (83.5%); no se completan criterios de negocio adicionales por esta corrección.

Los registros locales se conservan en `TestResults/DatabaseLauncher/`, fuera del control de versiones. El ensayo del iniciador utiliza una copia SQLite consistente aislada, seleccionada mediante `MEGA__DatabasePath`, mientras permanece abierto el servidor Release de esta copia oficial. No crea una base predeterminada ni altera datos operativos. Las pruebas de código no equivalen a aceptación del negocio o ensayos de periféricos físicos.
