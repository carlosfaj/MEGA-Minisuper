# Acceso de demostración desde celular

Ejecutar `Iniciar_MEGA.cmd` y conservar el proceso abierto. El iniciador detecta la dirección IPv4 con puerta de enlace y muestra la dirección HTTPS para un celular conectado al mismo Wi-Fi. Se usa el mismo iniciador para la computadora y el celular; ejecutar una sola instancia.

El certificado de desarrollo es local: el navegador del celular pedirá aceptar el aviso para esta demostración. Para operación real se necesita un certificado válido para el nombre del servidor y de confianza en los dispositivos.

La aplicación mantiene cookies Secure, autenticación y antifalsificación. `MEGA:AllowedNetwork` acepta solo una subred IPv4 privada explícita; sin esa opción se permiten únicamente conexiones de loopback. `AllowedHosts` incluye únicamente los hosts locales y la dirección detectada. El iniciador no cambia certificados ni reglas de Windows. Windows debe permitir Mega.Web.exe en la red utilizada. Si cambia la carpeta del ejecutable puede requerirse otra regla.

## Comprobación del 16 de noviembre

- Compilación sin errores ni advertencias.
- 283 pruebas aprobadas en la rama experimental; 272 en main.
- Diez casos automatizados para acceso local, subred habilitada, IP mapeada, direcciones externas y configuraciones inválidas.
- Proceso iniciado en la copia MEGA con la base de demostración existente.
- GET de https://192.168.0.13:7248/Acceso devuelve 200; POST de acceso con antifalsificación llega a Inicio y entrega cookie Secure.
- El usuario confirmó desde el celular que, tras aceptar el aviso del certificado, puede entrar y usar el sistema. Esta confirmación verifica el trayecto de red en el Wi-Fi actual.

La impresión sigue a cargo de Windows en la computadora; abrir la web en un celular no empareja su impresora Bluetooth con el servidor.
