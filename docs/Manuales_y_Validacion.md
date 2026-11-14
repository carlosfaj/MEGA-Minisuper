# Manuales y validación de MEGA 0.21.0

Edición documental del **14 de noviembre de 2026**, correspondiente al código 0.21.0. Consolida instalación, operación, contingencias y resultados comprobados; no modifica el programa ni sustituye el documento académico de capítulos I–IV.

## Documentos para entregar

| Documento | Contenido | Extensión |
|---|---|---|
| [Manual de usuario](entregables/Manual_Usuario_MEGA.pdf) | Roles, caja, ventas, inventario, revisiones, anulaciones, reportes, impresión/correo, respaldo, recuperación e incidencias | 14 páginas, carta vertical |
| [Manual técnico](entregables/Manual_Tecnico_MEGA.pdf) | Arquitectura, versiones, instalación, conservación de datos, configuración, integridad, agentes, mantenimiento, actualización y recuperación | 14 páginas, carta vertical |
| [Matriz de validación](entregables/Matriz_Validacion_MEGA.pdf) | T01–T15: caso, esperado, obtenido, estado, evidencias E01–E15, seis incidencias corregidas y validaciones pendientes | 12 páginas, carta horizontal |

Los manuales incluyen contenido automático y marcadores de navegación. Las dos figuras del manual de usuario tienen número, título y explicación; proceden de capturas conservadas, con un encuadre del área pertinente sin alterar los PNG originales. La matriz referencia los ensayos de sus módulos y explica sus diferencias de fecha y alcance.

## Fuentes y trazabilidad

- [Fuente del manual de usuario](manuales/Manual_Usuario.md) y [fuente del manual técnico](manuales/Manual_Tecnico.md).
- [Criterios originales copiados](validacion/Criterios_Aceptacion_20261111.csv) desde la revisión oficial del tercer corte del 11 de noviembre. `origenCriterios` en el JSON expresa una ruta relativa al espacio académico, no al repositorio.
- [Resultados y límites por criterio](validacion/criterios_resultados.json), con relación explícita a clases/casos y evidencia conservada.
- [Registro de las 223 pruebas](validacion/Registro_223_Pruebas.csv), derivado de la [suite publicada](evidencias/distribucion/suite.trx): 223 ejecuciones únicas aprobadas, ninguna fallida ni omitida. No es una nueva ejecución de pruebas.
- [Cobertura calculada](validacion/cobertura.json), con huella del TRX normalizada a LF. Cada uno de T01–T15 tiene casos relacionados. Una prueba puede respaldar varios criterios; los recuentos no se suman como ejecuciones independientes. Las 47 pruebas transversales sin asignación directa a esos criterios también permanecen en el registro de 223.
- [Revisión de documentos](validacion/Revision_Documentos.json), con páginas, huellas de PDF/fuentes, navegación y verificaciones realizadas.

Las secciones históricas de [Matriz de pruebas](Matriz_Pruebas.md) y los TRX anteriores conservan los resultados de cada versión. La regresión acumulada comprueba el software integrado de T01–T15; no equivale a aceptación del personal del negocio ni a certificación de producción.

## Reconstruir los PDF

Se requiere Python con las [dependencias documentales fijadas](../tools/requirements-manuals.txt), fuentes Arial instaladas y las evidencias versionadas. Desde la raíz del repositorio:

```powershell
python -m pip install -r tools/requirements-manuals.txt
python tools/build-manuals.py
```

También se admite `--only usuario`, `--only tecnico` o `--only matriz`. El generador comprueba la versión, la presencia de los 15 criterios, las fuentes de evidencia y las 223 ejecuciones únicas de la suite referenciada. Al cambiar versión o resultados deben actualizarse los contenidos y su fuente; el generador no inventa resultados nuevos. Los PDF pueden incorporar metadatos variables entre construcciones; sus huellas identifican los archivos revisados de esta edición.

## Revisión de esta edición

Se renderizaron y revisaron visualmente las **40 páginas** con Poppler. Se comprobaron extracción de texto, fuentes Arial, márgenes, ausencia de páginas vacías/marcadores y correspondencia entre los destinos del índice y los títulos. Los manuales usan cuerpo de 12 puntos, interlineado de 18 y márgenes de 2.54 cm; las tablas y pies emplean tamaños menores para su función. Se corrigieron cortes de encabezados, espaciado de celdas, rutas largas y legibilidad de las figuras. Esta es una revisión propia, no una evaluación independiente ni del docente.

Los criterios cerrados en el kanban son MANUAL 2–4, MATRIZ 3–4 y PRUEBAS 4. Los permisos/seguridad final, rendimiento e instalación en equipo objetivo, medio externo, impresora física, SMTP real y aceptación del negocio mantienen sus criterios abiertos. También faltan el documento académico consolidado, la actualización final de diagramas/prototipos y la presentación/demostración integrada.
