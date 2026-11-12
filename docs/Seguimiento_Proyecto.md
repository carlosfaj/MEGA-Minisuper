# Seguimiento vivo del proyecto

El usuario solicitó un kanban con porcentaje y todo el alcance. Está disponible en **Avance del proyecto** (`/Proyecto`) para cuentas comerciales y en `docs/KANBAN.md` en GitHub. Incluye software, calidad, documentación, entrega, decisiones y pruebas reales con periféricos/negocio; no se limita al código.

La fuente es `src/Mega.Web/Tracking/proyecto.json`. Cada tarjeta contiene criterios verificables y referencias de evidencia. El porcentaje es criterios completados dividido entre criterios totales, con igual peso por criterio, redondeado a un decimal. La misma regla se utiliza por área y tarjeta. No representa horas restantes ni sustituye aceptación real. Una tarjeta parcial nunca aparece completada; las validaciones pendientes conservan su peso en el total.

Hay cuatro columnas: Pendiente, En curso, Por validar y Completado. Se puede buscar o filtrar por área sin modificar el porcentaje global. Los detalles muestran criterios, dependencias, bloqueos y enlaces de evidencia. La pantalla comprueba cambios cada 15 segundos y se actualiza cuando cambia la fuente. Al publicar una copia, `MEGA:ProjectPlanPath` puede apuntar a la fuente mantenida del repositorio; sin ese parámetro se utiliza la copia de la distribución.

## Mantenimiento por cada avance

Actualizar la tarjeta o criterios que realmente se completaron y registrar una evidencia existente. Sincronizar versión y generar Markdown:

```powershell
python tools/update_kanban.py --version --complete ID_TARJETA --evidence docs/Documento_Con_Evidencia.md --results TestResults/pruebas.trx
python tools/update_kanban.py --check
```

Para avances parciales se utiliza `--done TARJETA:CRITERIO`. La herramienta no completa otras tarjetas por inferencia. El flujo de GitHub rechaza una versión/tablero desincronizados, criterios sin evidencia o Markdown generado obsoleto. No se marca una prueba física, decisión fiscal ni aceptación del negocio por haber programado una función.

136 pruebas aprobadas en `evidencias/kanban/kanban.trx`; prueba de actualización sin reiniciar, permisos, cálculo y alcance. Capturas de escritorio, filtro de documentación y móvil en la misma carpeta; `resultado.json` registra el recorrido real con datos ficticios y sin errores JavaScript.
