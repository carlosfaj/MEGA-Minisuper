const {chromium}=require(process.env.MEGA_PLAYWRIGHT || 'playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{
 const root=path.resolve(__dirname,'..'),out=path.join(root,'docs/evidencias/catalogo');fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({channel:'msedge',headless:true});
 const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:1000},locale:'es-NI'});
 const p=await context.newPage(),errors=[];p.on('pageerror',e=>errors.push(e.message));
 const base=process.env.MEGA_URL || 'https://localhost:7348';
 const login=async user=>{await p.goto(base+'/Acceso');await p.getByLabel('Usuario',{exact:true}).fill(user);await p.getByLabel('Contraseña',{exact:true}).fill('MegaDemo2026!');await p.getByRole('button',{name:'Entrar a MEGA'}).click();};
 const logout=async()=>p.getByRole('button',{name:'Salir',exact:true}).click();
 try{
 await login('admin');await p.goto(base+'/Catalogo/Categorias');await p.getByLabel('Nombre',{exact:true}).fill('Bebidas UI');await p.getByRole('button',{name:'Guardar categoría'}).click();await p.getByText('Categoría guardada.').waitFor();
 await p.goto(base+'/Catalogo/Producto');await p.getByLabel('Nombre',{exact:true}).fill('Agua UI');await p.getByLabel('Categoría',{exact:true}).selectOption({label:'Bebidas UI '});await p.getByLabel('Unidad base',{exact:true}).fill('botella');await p.getByRole('button',{name:'Guardar producto'}).click();await p.waitForURL(/Catalogo\/Producto\/\d+/);const productUrl=p.url();
 await p.getByRole('link',{name:'＋ Presentación',exact:true}).click();await p.getByLabel('Nombre de presentación',{exact:true}).fill('Botella');await p.getByLabel('Cantidad equivalente',{exact:false}).fill('1');await p.getByRole('button',{name:'Guardar presentación'}).click();await p.getByText('Presentación guardada.').waitFor();
 await p.getByRole('link',{name:'Códigos',exact:true}).click();await p.getByLabel('Presentación',{exact:true}).selectOption({label:'Botella'});await p.getByLabel('Código o código de barras').fill('00000999');await p.getByRole('button',{name:'Añadir código',exact:true}).click();await p.getByText('Código registrado.').waitFor();
 await p.goto(productUrl);await p.getByRole('link',{name:'Precios',exact:true}).click();const priceUrl=p.url();await p.getByLabel('Precio por presentación (C$)').fill('10.50');await p.getByRole('button',{name:'Registrar precio',exact:true}).click();await p.getByText('Precio registrado conservando las condiciones anteriores.').waitFor();await p.screenshot({path:path.join(out,'01_precio_inicial.png'),fullPage:true});
 await logout();await login('cajero');await p.goto(base+'/Caja');await p.getByLabel('Fondo real recibido (C$)').fill('500');await p.getByRole('button',{name:'Abrir mi turno'}).click();
 await p.goto(base+'/Venta');await p.getByRole('button',{name:/Agua UI.*00000999/}).click();await p.getByRole('button',{name:'Añadir a la compra'}).click();await p.locator('#total').filter({hasText:'10.50'}).waitFor();await p.locator('#physical-check').check();await p.getByRole('button',{name:'Continuar al cobro'}).click();await p.getByLabel('Efectivo recibido (C$)').fill('20');await p.getByRole('button',{name:'Confirmar venta',exact:true}).click();await p.waitForURL(/Comprobante\/\d+/);const receipt=p.url();await p.screenshot({path:path.join(out,'02_venta_producto_nuevo.png'),fullPage:true});
 await logout();await login('admin');await p.goto(priceUrl);await p.getByLabel('Precio por presentación (C$)').fill('20.50');await p.getByRole('button',{name:'Registrar precio',exact:true}).click();await p.getByText('Precio registrado conservando las condiciones anteriores.').waitFor();assert.equal(await p.locator('tbody tr').count(),2);await p.screenshot({path:path.join(out,'03_historial_precios.png'),fullPage:true});
 await p.goto(base+'/Catalogo');await p.getByLabel('Buscar producto o código').fill('00000999');await p.getByRole('button',{name:'Buscar',exact:true}).click();assert.equal(await p.locator('tbody tr').count(),1);await p.screenshot({path:path.join(out,'04_busqueda_codigo.png'),fullPage:true});
 await logout();await login('cajero');await p.goto(receipt);assert.match(await p.locator('#receipt-total').innerText(),/10.50/);await p.screenshot({path:path.join(out,'05_venta_historica_conservada.png'),fullPage:true});
 await p.goto(base+'/Venta');await p.getByRole('button',{name:/Agua UI.*00000999/}).click();await p.getByRole('button',{name:'Añadir a la compra'}).click();await p.locator('#total').filter({hasText:'20.50'}).waitFor();assert.deepEqual(errors,[]);
 const result={estado:'Aprobado',fecha:new Date().toISOString(),casos:['categoría y producto reales','presentación y código con ceros','precio inicial y venta','cambio de precio con historia','búsqueda por código','comprobante anterior inmutable','venta nueva con precio vigente'],erroresJavaScript:errors,datos:'Ficticios; base aislada de prueba'};
 fs.writeFileSync(path.join(out,'resultado.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
