# Gastos operativos

Gestión registra fecha, concepto, importe positivo y referencia opcional de comprobante. Es información para resultados del negocio: registrar un gasto no representa una retirada de efectivo de una caja. No se admiten fecha futura, importe cero/negativo ni más de dos decimales.

Corregir crea un nuevo registro enlazado y marca el anterior como Corregido, conservando todos sus datos. Solo se corrige el registro vigente y se exige motivo y revisión actual. Anular requiere motivo y conserva responsable y fecha. Anular una corrección no reactiva sus antecedentes. Los reportes utilizan exclusivamente registros con estado Registrado.

La clave de cada formulario recupera reintentos idénticos y rechaza su reutilización con otro contenido. Dos correcciones simultáneas no sustituyen dos veces el mismo registro. Registro, estados e información de auditoría son transaccionales; un fallo tardío no deja una corrección parcial.

180 pruebas acumuladas aprobadas en `evidencias/gastos/gastos.trx`, incluidas recuperación, valores inválidos, permisos, concurrencia y fallo tardío. El navegador verificó gasto ficticio 100, corrección a 120 conservando el anterior y anulación con motivo; dos capturas y `resultado.json` documentan el recorrido sobre base aislada. Estos datos no son gastos reales ni aceptación del negocio.
