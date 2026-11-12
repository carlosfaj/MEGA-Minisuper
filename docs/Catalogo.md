# Catálogo operativo de MEGA

Disponible desde 0.3.0 para Propietario y Administrador. Ingresar con `admin` en la demostración y abrir **Catálogo**.

## Operación

1. En **Categorías**, crear o editar una categoría. La desactivación conserva sus productos y registros.
2. En **Nuevo producto**, indicar nombre, categoría, control por unidad/peso, unidad base, mínimo y control de vencimiento.
3. Guardar el producto y añadir una **Presentación** con su equivalencia en la unidad base.
4. En **Códigos**, añadir códigos de texto; sus ceros iniciales se conservan. Un código desactivado no se reasigna a otro producto.
5. En **Precios**, registrar un importe positivo por presentación. Puede aplicarse ahora o programarse a futuro usando la hora local provisional UTC−6.
6. El producto activo con presentación/precio vigentes aparece en la búsqueda de venta cuando cumple las condiciones admitidas por el módulo de venta actual.

No se crea stock al registrar un producto o su mínimo. Si el saldo digital es insuficiente, el recorrido de venta actual exige comprobar físicamente disponibilidad y registrar la discrepancia. Las entradas autorizadas se implementarán con recepciones/inventario.

## Conservación y permisos

Los cambios de precio generan una condición nueva y cierran la anterior sin cambiar su importe. Se admiten condiciones programadas y se conserva el intervalo del siguiente precio si ya existía. No se permiten condiciones retroactivas ni ventas gratuitas no definidas.

La venta guarda sus condiciones y comprobante originales. Desactivar o renombrar un producto no reescribe ventas anteriores. Un producto con historia conserva su unidad/escala, y una presentación usada conserva su equivalencia; una unidad diferente exige otro producto/presentación.

Las ediciones comparan la versión o contenido leído y rechazan pantallas desactualizadas. Los cambios y su responsable se registran en auditoría dentro de la misma transacción. Cajero e Inventario no tienen acceso a las pantallas ni a las operaciones de mantenimiento.

## Validación

65 pruebas aprobadas en la versión, incluidas alta completa, venta, T01, historia de precio, cambio concurrente, códigos desactivados, entradas inválidas y permisos HTTP. El recorrido automatizado de navegador creó un producto, vendió con C$10.50, cambió a C$20.50 y comprobó ambos importes en su contexto correcto.

- [Resultado de navegador](evidencias/catalogo/resultado.json).
- [Pruebas ejecutadas](evidencias/catalogo/v0.3.0_pruebas.trx).
- [Precio inicial](evidencias/catalogo/01_precio_inicial.png).
- [Venta del producto nuevo](evidencias/catalogo/02_venta_producto_nuevo.png).
- [Historial de precios](evidencias/catalogo/03_historial_precios.png).
- [Búsqueda por código](evidencias/catalogo/04_busqueda_codigo.png).
- [Comprobante anterior conservado](evidencias/catalogo/05_venta_historica_conservada.png).

Las capturas y pruebas usan datos ficticios en una base aislada. Promociones, impuestos, valoración y venta con lotes continúan pendientes; no se habilitan por dar de alta un producto ni se atribuye validación fiscal.
