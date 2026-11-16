# Revisión de la base con MEGA abierto

Corrección 0.24.1 · 15 de noviembre de 2026 · rama `feature/frontend-branded`.

## Causa y solución

`Revisar_Base_Datos.cmd` invoca `tools/database.ps1`. Antes, `dotnet run` recompilaba en `src/Mega.Web/bin/Release/net10.0`, donde el servidor abierto mantenía bloqueado `Mega.Web.exe`. La copia fallaba tras diez intentos con MSB3026, MSB3027 y MSB3021, antes de ejecutar el diagnóstico.

Ahora el script compila en una carpeta temporal única `MEGA-database-<GUID>`, sin apphost, y ejecuta la DLL con el directorio de contenido original y el entorno del perfil MEGA. Así conserva `appsettings`, el contrato de datos y la selección de la base original. El bloque `finally` elimina únicamente esa carpeta temporal, después de verificar su ubicación y nombre. El resultado del proceso se conserva como código de salida.

La comprobación sigue usando `--db-check` y la conexión SQLite de solo lectura. La opción `upgrade` comparte la compilación independiente, pero conserva la exclusión de instancia y las copias previas existentes: esta corrección no autoriza actualizar el esquema mientras el servidor usa la base.

## Validación local

- Windows PowerShell 5.1: script analizado y ejecutado sin errores de sintaxis.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/database.ps1 -Operation check`: compilación Release sin advertencias ni errores y diagnóstico correcto con el servidor abierto.
- `Revisar_Base_Datos.cmd`, desde su carpeta y con entrada redirigida para pasar la pausa: diagnóstico completo, sin los errores de bloqueo originales.
- Resultado: `Integrity: "ok"`, `Healthy: true`, `UpToDate: true`, `Errors: []`; 39 tablas y 103 relaciones externas.
- SHA-256 de la base idéntico antes y después del ensayo; el mismo servidor siguió abierto.
- Ninguna carpeta temporal nueva quedó después del ensayo.
- Base inexistente indicada mediante `MEGA__DatabasePath`: salida distinta de cero, sin crear la base ausente y sin carpetas temporales sobrantes.
- Suite completa: 273/273 pruebas aprobadas, sin omisiones, mediante `dotnet test MEGA.sln --configuration Debug --nologo --logger "trx;LogFileName=mega.trx" --results-directory TestResults/DatabaseLauncher`. Se usa Debug para conservar los archivos Release del servidor abierto; la herramienta se compiló y ensayó en Release.

Los registros completos permanecen en `TestResults/DatabaseLauncher/`, fuera del control de versiones, para no publicar datos locales. La prueba sobre la base de demostración no acredita aceptación del negocio ni periféricos físicos.
