# Paquete de demostración para Windows

El paquete de ejecución incluye .NET y los componentes del programa para Windows de 64 bits. No requiere instalar el SDK ni descargar dependencias al iniciarlo. Se utiliza una [publicación que incluye el entorno de ejecución](https://learn.microsoft.com/en-us/dotnet/core/deploying/) y se conservan las versiones fijadas del proyecto.

## Abrir el paquete

1. Extraer el ZIP completo en una carpeta local. No ejecutar un archivo suelto desde dentro del ZIP.
2. Detener cualquier instancia anterior, abrir **Iniciar_MEGA.cmd** y conservar su ventana abierta. El iniciador avisa si el puerto está ocupado antes de crear datos.
3. Cuando aparezca el aviso de inicio, abrir **https://localhost:7248**.
4. El navegador puede pedir aceptar el certificado local para localhost. El iniciador no instala certificados de confianza ni cambia la confianza del equipo.
5. Entrar con `cajero` y `MegaDemo2026!`. También existen `admin`, `inventario`, `cajero2` y `propietario`, con la misma contraseña ficticia.

Los datos se crean por separado en `%LOCALAPPDATA%\MEGA\Demostracion`. Actualizar o cambiar la carpeta del programa conserva esos datos. La primera ejecución crea una demostración sin ventas; no importa automáticamente datos de otra instalación. Detener con **Ctrl+C** y esperar al cierre antes de iniciar otra versión. La aplicación rechaza una segunda instancia sobre la misma base.

El iniciador crea un certificado para localhost con la [API de certificados de .NET](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.certificaterequest.createselfsigned?view=net-10.0). Su material queda protegido por el perfil de Windows mediante DPAPI; el PFX de ejecución está cifrado con contraseña aleatoria y se retira al terminar normalmente. Un cierre forzado puede dejar un PFX cifrado residual. No se incluyen certificados, credenciales ni bases en el ZIP. Cambiar de perfil requiere revisar o regenerar el certificado local; no se borra automáticamente información para resolver un fallo.

El núcleo funciona sin Internet una vez extraído. El correo requiere configuración y conexión expresas. Impresora, medio externo y aceptación del negocio siguen pendientes de ensayo real. Este paquete corresponde a demostración local; no acredita implantación comercial.

## Verificar y reproducir

Junto al ZIP se entrega SHA-256. `paquete.json` registra versión, revisión de origen, SDK, componentes incluidos, tamaños y huellas de archivos. La huella detecta cambios respecto del paquete comprobado; no constituye firma digital del editor. Se debe conservar todo el contenido de `App`, incluido `Licencias` y los avisos de terceros.

Para construir una versión registrada: `pwsh -File tools/build-distribution.ps1`. Requiere SDK fijado en `global.json`, Git y acceso a dependencias si aún no están disponibles. Un ensayo con cambios sin registrar solo puede escribirse bajo `TestResults` usando `-AllowDirty`; el manifiesto lo identifica. La publicación estable exige árbol limpio.

Los ensayos técnicos pueden indicar otra carpeta de datos y puerto mediante `Iniciar_MEGA.ps1 -DataRoot RUTA_COMPLETA -Port 7366`. Estos parámetros no alteran la demostración habitual. La instalación limpia en otro equipo objetivo continúa pendiente hasta realizarla allí; ejecutar sin SDK disponible en el mismo equipo es una comprobación técnica diferente.

## Actualización y recuperación del programa

Antes de sustituir el programa, comprobar que no haya trabajos externos en curso, generar el respaldo autorizado, detener la ventana y conservar la carpeta anterior. Extraer el paquete nuevo en otra carpeta y abrirlo con el mismo perfil de Windows y carpeta de datos. Si el nuevo programa no arranca, detenerlo y volver a una versión anterior compatible conservando los datos; no restaurar una base solo para cambiar el programa. Después de usar restauración, no utilizar versiones anteriores a 0.20.0, que carecen del control de continuidad de numeración. Los cambios futuros de esquema requieren su procedimiento específico.

## Validación de 0.21.0

El ensayo privado verificó 575 archivos y sus huellas, componentes .NET 10.0.14 incluidos, licencias/avisos del entorno y ausencia de bases, App_Data, certificados o archivos de operación. Se inició mediante Windows PowerShell 5.1, con DOTNET_ROOT apuntando a una ubicación inexistente; una venta dejó C$538,00 y se conservó al iniciar desde otra carpeta del programa con la misma carpeta de datos. El navegador bloqueó destinos externos y no registró errores JavaScript. [Resultados y capturas](https://github.com/carlosfaj/MEGA-Minisuper/blob/v0.21.0/docs/evidencias/distribucion/initial.json), [reinicio](https://github.com/carlosfaj/MEGA-Minisuper/blob/v0.21.0/docs/evidencias/distribucion/restart.json), [comprobación del paquete de ensayo](https://github.com/carlosfaj/MEGA-Minisuper/blob/v0.21.0/docs/evidencias/distribucion/paquete_ensayo.json) y [suite 223/223](https://github.com/carlosfaj/MEGA-Minisuper/blob/v0.21.0/docs/evidencias/distribucion/suite.trx).

La [consulta de dependencias](https://github.com/carlosfaj/MEGA-Minisuper/blob/v0.21.0/docs/evidencias/distribucion/dependencias.json) no encontró vulnerabilidades declaradas por NuGet al ejecutarse el 14 de noviembre de 2026. No equivale a una garantía de ausencia de vulnerabilidades. GitHub ejecuta restauración fijada, revisión de sintaxis y dependencias, compilación/pruebas, preparación y comprobación del paquete. Conserva ZIP y SHA-256 como artefactos durante 30 días. El ensayo privado identifica que el árbol aún tenía cambios; los paquetes estables se generan desde una revisión registrada y árbol limpio, con ese origen en el manifiesto.

Se comprobó también el [rechazo de puerto ocupado sin crear datos](https://github.com/carlosfaj/MEGA-Minisuper/blob/v0.21.0/docs/evidencias/distribucion/puerto_ocupado.json). No se realizó instalación en otro equipo físico ni se alteró la confianza de certificados del sistema. La revisión de rendimiento, los periféricos y la aceptación del negocio conservan sus criterios pendientes.
