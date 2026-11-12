# Kanban de MEGA

**Avance global: 61.7% — 82/133 criterios.**
Versión 0.15.0 · actualizado 2026-11-12T20:35:46Z · 175 pruebas aprobadas.

[Abrir tablero interactivo local](https://localhost:7248/Proyecto)

Porcentaje = criterios completados / criterios totales. Cada criterio tiene el mismo peso. No representa tiempo restante ni aceptación del negocio.

## Avance por área

| Área | Criterios | Avance |
|---|---|---|
| Software | 62/83 | 74.7% |
| Calidad | 9/13 | 69.2% |
| Entrega | 5/11 | 45.5% |
| Documentación | 5/19 | 26.3% |
| Validación | 1/7 | 14.3% |

## Pendiente

### GASTOS · Gastos operativos — 0%

Área: Software.
Depende de: ACCESO.

- [ ] Registro y corrección trazable
- [ ] Anulación con motivo
- [ ] Permisos y pruebas
### REPORTES · Reportes y exportaciones — 0%

Área: Software.
Depende de: COSTOS, GASTOS, ANULA.

- [ ] Ventas y caja reconciliados
- [ ] Existencias y valoración
- [ ] Costos y márgenes históricos
- [ ] Exportación PDF y Excel
- [ ] Vista de Inventario sin márgenes
### CORREO · Envío de reportes — 0%

Área: Software.
Depende de: REPORTES.

- [ ] Configuración y secreto protegidos
- [ ] Cola con reportes congelados
- [ ] Reintentos seguros y estado incierto
- [ ] Pruebas sin conexión y errores
### BACKUP · Respaldo verificable — 0%

Área: Software.
Depende de: BD.

- [ ] Copia consistente desde la aplicación
- [ ] Destino y pauta configurables
- [ ] Checksum, manifiesto y diagnóstico
- [ ] Fallos visibles y ensayo
### RESTORE · Restauración y conciliación — 0%

Área: Software.
Depende de: BACKUP, PRINT, CORREO.

- [ ] Autorización y preservación de base actual
- [ ] Restauración comprobada
- [ ] Cuarentena de impresión y correo
- [ ] Conciliación y reapertura explícita
- [ ] Ensayo sin duplicar trabajos
### SLIDES · Presentación de defensa — 0%

Área: Documentación.

- [ ] Guion y resultados reales
- [ ] Presentación PowerPoint o PDF
- [ ] Ensayo y revisión visual

## En curso

### PRUEBAS · Pruebas integrales — 60%

Área: Calidad.

- [x] Pruebas de integración con bases aisladas ([evidencia](../docs/Matriz_Pruebas.md))
- [x] Recorridos de navegador con evidencias ([evidencia](../docs/Matriz_Pruebas.md))
- [x] Datos válidos, inválidos y concurrencia ([evidencia](../docs/Matriz_Pruebas.md))
- [ ] Regresión T01–T15 de todo el alcance
- [ ] Rendimiento y fallos en equipo objetivo
### SEGURIDAD · Revisión de seguridad final — 50%

Área: Calidad.

- [x] Roles, sesiones y antifalsificación comprobados ([evidencia](../AGENTS.md))
- [x] Secretos y bases excluidos del repositorio ([evidencia](../AGENTS.md))
- [ ] Permisos de todos los módulos finales
- [ ] Dependencias y configuración final revisadas
### DIST · Instalación y distribución — 50%

Área: Entrega.

- [x] Arranque local documentado ([evidencia](../README.md))
- [x] Fuentes y dependencias fijadas ([evidencia](../README.md))
- [ ] Distribución reproducible preparada
- [ ] Instalación limpia en equipo objetivo
### DOC · Documento académico consolidado — 20%

Área: Documentación.

- [x] Estructura y fuentes anteriores localizadas ([evidencia](../docs/Documento_Final_Estructura.md))
- [ ] Capítulos I–IV alineados al sistema final
- [ ] Resultados, conclusiones y recomendaciones
- [ ] APA 7 e índices automáticos
- [ ] PDF final revisado
### MANUAL · Manuales técnico y de usuario — 25%

Área: Documentación.

- [x] Guías de módulos implementados ([evidencia](../README.md))
- [ ] Instalación y operación final consolidadas
- [ ] Procedimientos de contingencia y recuperación
- [ ] PDF de ambos manuales revisados
### DIAGRAMAS · Diagramas y prototipos — 33.3%

Área: Documentación.

- [x] Modelo de datos original conservado ([evidencia](../docs/Procedencia_Modelo.json))
- [ ] Fuentes editables actualizadas al sistema final
- [ ] Correspondencia RF/CU/pantalla revisada
### MATRIZ · Matriz y evidencias finales — 50%

Área: Documentación.

- [x] Matriz parcial con resultados reales ([evidencia](../docs/Matriz_Pruebas.md))
- [x] TRX y capturas por módulo ([evidencia](../docs/Matriz_Pruebas.md))
- [ ] Cobertura completa T01–T15
- [ ] PDF o Excel final revisado

## Por validar

### DEMO · Demostración y aceptación — 0%

Área: Entrega.
La aceptación requiere participación real del negocio.

- [ ] Recorrido integrado de todo el alcance
- [ ] Datos de demostración y contingencias
- [ ] Ensayo reproducible
- [ ] Aceptación del personal del negocio
### NEGOCIO · Validaciones del negocio — 25%

Área: Validación.

- [x] Política de costo confirmada ([evidencia](../docs/Valoracion.md))
- [ ] Unidades, redondeos y discrepancias validados
- [ ] Fiscalidad y transferencia definidas
- [ ] Pauta de respaldo y recuperación aceptada
### EQUIPO · Periféricos y servicios reales — 0%

Área: Validación.
Actualmente solo se detectaron impresoras virtuales; faltan equipo y configuración reales.

- [ ] Impresora física instalada y ensayada
- [ ] Correo real configurado y probado
- [ ] Medio externo y recuperación ensayados

## Completado

### BD · Base de datos e integridad — 100%

Área: Software.

- [x] Modelo original y procedencia conservados ([evidencia](../docs/Base_Datos.md))
- [x] Migración consistente y diagnóstico ([evidencia](../docs/Base_Datos.md))
- [x] Claves, restricciones y mapeo verificados ([evidencia](../docs/Base_Datos.md))
### ACCESO · Acceso y seguridad de cuentas — 100%

Área: Software.

- [x] Acceso por rol y antifalsificación ([evidencia](../docs/Cuentas.md))
- [x] Revocación y límite de intentos ([evidencia](../docs/Cuentas.md))
- [x] Delegación y protección del último Propietario ([evidencia](../docs/Cuentas.md))
- [x] Soporte temporal con vencimiento ([evidencia](../docs/Cuentas.md))
### CAT · Catálogo y precios — 100%

Área: Software.

- [x] Categorías y productos ([evidencia](../docs/Catalogo.md))
- [x] Presentaciones, equivalencias y códigos ([evidencia](../docs/Catalogo.md))
- [x] Precios con vigencias e historia ([evidencia](../docs/Catalogo.md))
- [x] Permisos y recorrido de alta hasta venta ([evidencia](../docs/Catalogo.md))
### CONFIG · Configuración del negocio — 100%

Área: Software.

- [x] Configuración versionada ([evidencia](../docs/Configuracion.md))
- [x] Fondos y comprobantes anteriores conservados ([evidencia](../docs/Configuracion.md))
- [x] Conflictos de edición y permisos comprobados ([evidencia](../docs/Configuracion.md))
### PROV · Proveedores — 100%

Área: Software.

- [x] Alta, edición y desactivación ([evidencia](../docs/Abastecimiento.md))
- [x] Procedencia y permisos conservados ([evidencia](../docs/Abastecimiento.md))
### REC · Recepciones autorizadas — 100%

Área: Software.

- [x] Preparación sin alterar existencias ([evidencia](../docs/Abastecimiento.md))
- [x] Revisión exacta por otra cuenta ([evidencia](../docs/Abastecimiento.md))
- [x] Aceptación, costos, lotes y entrada única ([evidencia](../docs/Abastecimiento.md))
- [x] Reintentos y reversión ante fallos ([evidencia](../docs/Abastecimiento.md))
### CONTEO · Conteos físicos — 100%

Área: Software.

- [x] Observación física y saldo de referencia ([evidencia](../docs/Conteos_y_Lotes.md))
- [x] Diferencias sin ajuste automático ([evidencia](../docs/Conteos_y_Lotes.md))
- [x] Nuevos movimientos exigen nueva comprobación ([evidencia](../docs/Conteos_y_Lotes.md))
### LOTES · Lotes y vencimientos — 100%

Área: Software.

- [x] Existencias y procedencia por lote ([evidencia](../docs/Valoracion.md))
- [x] Alertas configurables y vencidos ([evidencia](../docs/Valoracion.md))
- [x] Asignación en ventas sin sobreconsumo ([evidencia](../docs/Valoracion.md))
### COSTOS · Promedio ponderado móvil — 100%

Área: Software.

- [x] Política confirmada por el usuario ([evidencia](../docs/Valoracion.md))
- [x] Conocido, desconocido y cero diferenciados ([evidencia](../docs/Valoracion.md))
- [x] Costo histórico preservado en salidas ([evidencia](../docs/Valoracion.md))
- [x] Orden y precisión comprobados ([evidencia](../docs/Valoracion.md))
### INV · Cargas, ajustes y salidas — 100%

Área: Software.

- [x] Motivo, evidencia y revisión por otra cuenta ([evidencia](../docs/Inventario_Integrado.md))
- [x] Ajustes desde conteo finalizado ([evidencia](../docs/Inventario_Integrado.md))
- [x] Lotes y costo vigente en regularizaciones ([evidencia](../docs/Inventario_Integrado.md))
- [x] Cancelación, concurrencia y atomicidad ([evidencia](../docs/Inventario_Integrado.md))
### CAJA · Caja propia — 100%

Área: Software.

- [x] Apertura y exclusión de turnos simultáneos ([evidencia](../docs/Matriz_Pruebas.md))
- [x] Fondo real y esperado ([evidencia](../docs/Matriz_Pruebas.md))
- [x] Cierre, diferencias y versión ([evidencia](../docs/Matriz_Pruebas.md))
### VENTA · Venta en efectivo — 100%

Área: Software.

- [x] Preparación y cobro en servidor ([evidencia](../docs/Valoracion.md))
- [x] Comprobante histórico ([evidencia](../docs/Valoracion.md))
- [x] Reintento sin duplicar venta o cobro ([evidencia](../docs/Valoracion.md))
- [x] Integración con costos y lotes ([evidencia](../docs/Valoracion.md))
### PROMO · Promociones — 100%

Área: Software.
Depende de: CAT, VENTA.

- [x] Mantenimiento y vigencias ([evidencia](../docs/Condiciones_Comerciales.md))
- [x] Promoción exclusiva y prioridad ([evidencia](../docs/Condiciones_Comerciales.md))
- [x] Cálculo visible en venta ([evidencia](../docs/Condiciones_Comerciales.md))
- [x] Historia y pruebas de cambios ([evidencia](../docs/Condiciones_Comerciales.md))
### IMPUESTO · Impuestos y exenciones — 100%

Área: Software.
Depende de: CONFIG, VENTA.

- [x] Tasas configurables sin tasa inventada ([evidencia](../docs/Condiciones_Comerciales.md))
- [x] Vigencias y exenciones con motivo ([evidencia](../docs/Condiciones_Comerciales.md))
- [x] Base, redondeo y cálculo en venta ([evidencia](../docs/Condiciones_Comerciales.md))
- [x] Condiciones históricas y pruebas ([evidencia](../docs/Condiciones_Comerciales.md))
### ANULA · Anulaciones y fondos — 100%

Área: Software.
Depende de: COSTOS, CAJA, VENTA.

- [x] Solicitud y evidencia ([evidencia](../docs/Anulaciones.md))
- [x] Otra cuenta decide versión exacta ([evidencia](../docs/Anulaciones.md))
- [x] Reincorporación condicionada a verificación física declarada ([evidencia](../docs/Anulaciones.md))
- [x] Devolución o regularización sin alterar cierres ([evidencia](../docs/Anulaciones.md))
- [x] Reintentos, concurrencia y pruebas ([evidencia](../docs/Anulaciones.md))
### PRINT · Impresión y contingencia — 100%

Área: Software.
Depende de: VENTA.

- [x] Agente e impresora configurables ([evidencia](../docs/Impresion.md))
- [x] Cola e intentos verificables ([evidencia](../docs/Impresion.md))
- [x] Resultado incierto sin reenvío ciego ([evidencia](../docs/Impresion.md))
- [x] Reimpresión del mismo folio ([evidencia](../docs/Impresion.md))
- [x] Contingencia y pruebas ([evidencia](../docs/Impresion.md))
### AUDIT · Auditoría integral — 100%

Área: Calidad.

- [x] Bitácora y eventos de operaciones implementadas ([evidencia](../docs/Auditoria.md))
- [x] Responsable, cambios y autorización ([evidencia](../docs/Auditoria.md))
- [x] Todos los rechazos críticos ([evidencia](../docs/Auditoria.md))
- [x] Regresión integral T13 ([evidencia](../docs/Auditoria.md))
### GIT · Versiones y GitHub — 100%

Área: Entrega.

- [x] Repositorio e historia conservados ([evidencia](../AGENTS.md))
- [x] Push por funcionalidad estable ([evidencia](../AGENTS.md))
- [x] Compilación, pruebas y etiquetas automáticas ([evidencia](../AGENTS.md))
### KANBAN · Kanban y avance del proyecto — 100%

Área: Software.

- [x] Tablero de todo el alcance ([evidencia](../docs/evidencias/kanban/resultado.json))
- [x] Porcentajes calculados y filtros comprobados ([evidencia](../docs/evidencias/kanban/resultado.json))
- [x] Actualización y registro en GitHub ([evidencia](../docs/evidencias/kanban/resultado.json))
