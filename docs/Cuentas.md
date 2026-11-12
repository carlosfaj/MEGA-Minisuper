# Cuentas y acceso temporal

El Propietario accede a **Cuentas** para crear y administrar cuentas comerciales con los cuatro roles previstos. La cuenta de demostración `propietario` usa `MegaDemo2026!`; no es una credencial operativa. Al abrir una base ficticia anterior sin Propietario, se agrega esa cuenta una vez con auditoría y se conservan las cuentas existentes. Fuera del modo de demostración no se agrega ninguna contraseña conocida.

El Propietario puede delegar la gestión de cuentas a un Administrador. Este puede administrar cuentas comerciales ordinarias, pero no asignar el rol de Propietario, modificar Propietarios, gestionar Administradores delegados ni delegar su facultad. Inventario no recibe permiso de caja. No se borra ninguna cuenta ni se reutilizan usuarios desactivados.

La aplicación impide retirar al último Propietario activo. Tampoco permite desactivar, cambiar de rol o retirar el permiso de caja a un operador con turno abierto: debe cerrar primero su turno. Se puede restablecer su contraseña para recuperar el acceso. Un formulario obsoleto se rechaza. Cada edición incrementa la versión de credenciales y obliga a iniciar sesión nuevamente.

Todos los usuarios comerciales pueden cambiar su propia contraseña desde **Mi contraseña**, comprobando la actual y confirmando la nueva. Se admiten de 12 a 200 caracteres; se guarda un hash. La bitácora no incluye contraseñas ni hashes. El cambio cierra todas las sesiones anteriores.

## Soporte temporal

Solo el Propietario puede autorizar un usuario técnico nuevo con motivo, contraseña temporal y duración de 1 a 24 horas (límite técnico conservador). La cuenta tiene `rolId` nulo, sin caja ni delegación; no constituye un quinto rol comercial. Su alcance actual es `diagnostico_integridad_lectura`: revisar integridad física, relaciones y versión del esquema. La consulta se audita y no muestra datos comerciales o secretos.

El vencimiento y la autorización no revocada se comprueban al iniciar sesión y en cada petición posterior. La revocación desactiva la cuenta y sus sesiones inmediatamente. Este acceso no permite restaurar una base; esa operación requiere el módulo y procedimiento de recuperación pendientes.

## Evidencias

83 pruebas aprobadas en `evidencias/cuentas/cuentas.trx`. Comprueban permisos directos y HTTP, delegación, sesiones revocadas, último Propietario, turno abierto, contraseñas, formularios y soporte vencido/revocado. `tools/accounts-check.cjs` ejecutó seis comprobaciones en una base aislada:

- `evidencias/cuentas/01_cuentas.png`: cuenta nueva y lista de responsables.
- `evidencias/cuentas/02_soporte_autorizado.png`: autorización temporal explícita.
- `evidencias/cuentas/03_diagnostico.png`: diagnóstico permitido al usuario técnico.
- `evidencias/cuentas/resultado.json`: acceso del nuevo Inventario, delegación, cambio de contraseña y revocación sin errores JavaScript.

Las pruebas usan datos ficticios. La política de entrega de credenciales y la aceptación del negocio siguen pendientes.
