# Trabajo y versiones de MEGA

## Autorización del usuario

El usuario autorizó el 12 de noviembre de 2026 usar Git y su conexión personal de GitHub **carlosfaj** para registrar versiones y hacer push de cada versión estable o funcionalidad terminada en **https://github.com/carlosfaj/MEGA-Minisuper.git**. Esta autorización persiste: no pedir de nuevo permiso para commits y pushes dentro de ese alcance.

## Flujo obligatorio

1. Trabajar en este repositorio de desarrollo. Antes de editar, revisar estado local, rama y cambios del usuario; conservar todo trabajo ajeno. Consultar el remoto antes de publicar y conciliar cambios si los hubiera.
2. Completar una funcionalidad o corrección coherente, actualizar documentación/evidencias pertinentes y ejecutar las pruebas adecuadas.
3. Mantener **main** funcional. Trabajos incompletos pueden usar `feature/<nombre>` o `fix/<nombre>` y deben indicarse como incompletos.
4. Para una funcionalidad terminada, incrementar el componente menor de `VERSION` (ej. 0.1.0 a 0.2.0); para una corrección, el componente de parche (ej. 0.1.0 a 0.1.1). Registrar cambios y validación en `CHANGELOG.md`. Los cambios solo de documentación no requieren otra versión de código.
5. Crear un commit con mensaje claro (`feat:`, `fix:`, `docs:`, `test:` o `chore:`) y publicar al completar cada funcionalidad o versión estable. No acumular varias funcionalidades terminadas sin push.
6. Preferir las herramientas de la conexión GitHub **Personal / carlosfaj** para publicar. Si la integración carece de permiso de escritura, puede utilizarse la sesión Git ya autenticada de **la misma cuenta carlosfaj**, verificando su identidad sin imprimir ni guardar credenciales. No pedir tokens o contraseñas al usuario. Si se publica mediante la API conectada, sincronizar después los mismos commits y árbol con la copia Git local; no dejar dos historias diferentes.
7. Verificar que el commit publicado corresponde al árbol local y comprobar el resultado de GitHub Actions. El flujo crea `v<VERSION>` en main después de compilar y aprobar pruebas. No mover ni reutilizar etiquetas existentes.
8. Informar al usuario la versión, enlace al commit o etiqueta, validación y limitaciones materiales.
9. Mantener el kanban de todo el proyecto solicitado por el usuario: `src/Mega.Web/Tracking/proyecto.json` es la fuente y `docs/KANBAN.md` la vista generada. Registrar criterios terminados solo con evidencia real; no confundir implementación con aceptación del negocio o ensayo físico. Al publicar, sincronizar VERSION, pruebas y criterios con `tools/update_kanban.py`, ejecutar `--check` y conservar el porcentaje calculado. El tablero `/Proyecto` se actualiza desde esa fuente.

## Conservación y exclusiones

- No hacer force push, reescribir historia compartida ni borrar ramas o cambios del usuario sin autorización específica.
- No versionar bases de operación/demostración, secretos, certificados privados, carpetas bin/obj, cachés del editor ni archivos generados de ejecución.
- Los datos/contraseña de demostración documentados son ficticios y forman parte de las pruebas; no son credenciales de GitHub ni de operación real.
- Conservar el DDL y JSON de procedencia con sus bytes originales; sus hashes están en `docs/Procedencia_Modelo.json`.
- La aplicación es local; esta autorización de Git/GitHub no implica implantarla comercialmente ni habilitarla en Internet.
