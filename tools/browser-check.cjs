// Comprobación de la aplicación local. Usa Playwright suministrado por el entorno.
const {chromium}=require(process.env.MEGA_PLAYWRIGHT || 'playwright');
const fs=require('node:fs');const path=require('node:path');const assert=require('node:assert/strict');
(async()=>{
 const root=path.resolve(__dirname,'..');const output=path.join(root,'docs/evidencias/pantallas');fs.mkdirSync(output,{recursive:true});
 const browser=await chromium.launch({channel:'msedge',headless:true});
 const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:1000},locale:'es-NI'});
 const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const steps=[];const shot=async(name)=>{await page.screenshot({path:path.join(output,name+'.png'),fullPage:true});steps.push(name);};
 try{
 await page.goto('https://localhost:7248/Acceso');await shot('01_acceso');
 await page.getByLabel('Usuario',{exact:true}).fill('cajero');await page.getByLabel('Contraseña',{exact:true}).fill('incorrecta');await page.getByRole('button',{name:'Entrar a MEGA'}).click();
 await page.getByText('Usuario o contraseña no válidos.').waitFor();
 await page.getByLabel('Contraseña',{exact:true}).fill('MegaDemo2026!');await page.getByRole('button',{name:'Entrar a MEGA'}).click();await page.waitForURL('https://localhost:7248/');
 await page.goto('https://localhost:7248/Caja');
 await page.getByLabel('Fondo real recibido (C$)').fill('480');await page.getByLabel('Motivo de diferencia con el fondo habitual').fill('Demostración: recibido C$20 menos del habitual');
 await page.getByRole('button',{name:'Abrir mi turno'}).click();await page.getByText('Tu turno está abierto. Ya puedes registrar ventas.').waitFor();await shot('02_caja_abierta');
 await page.goto('https://localhost:7248/Venta');
 await page.getByRole('button',{name:/Arroz blanco.*DEMO001/}).click();await page.getByLabel('Cantidad (lb)',{exact:true}).fill('2.000');await page.getByRole('button',{name:'Añadir a la compra'}).click();
 await page.locator('#total').filter({hasText:'48.00'}).waitFor();
 await page.getByRole('button',{name:/Leche entera.*DEMO002/}).click();await page.getByLabel('Cantidad (unidad)',{exact:true}).fill('1');await page.getByRole('button',{name:'Añadir a la compra'}).click();
 await page.locator('#total').filter({hasText:'86.00'}).waitFor();await shot('03_compra_preparacion');
 await page.getByRole('button',{name:'Continuar al cobro'}).click();await page.getByLabel('Efectivo recibido (C$)').fill('80');
 assert.equal(await page.getByRole('button',{name:'Confirmar venta',exact:true}).isDisabled(),true);await shot('04_pago_insuficiente');
 await page.getByLabel('Efectivo recibido (C$)').fill('100');await page.locator('#change').filter({hasText:'14.00'}).waitFor();await shot('05_cobro_efectivo');
 await page.getByRole('button',{name:'Confirmar venta',exact:true}).click();await page.waitForURL(/Comprobante\/\d+/);await page.locator('#receipt-total').filter({hasText:'86.00'}).waitFor();
 assert.match(await page.locator('#receipt-change').innerText(),/14.00/);await shot('06_comprobante');
 await page.goto('https://localhost:7248/Caja');assert.match(await page.locator('#cash-expected').innerText(),/566.00/);await shot('07_caja_despues_venta');
 await page.getByRole('link',{name:'Revisar cierre'}).click();await page.getByLabel('Efectivo contado (C$)').fill('560');await page.getByLabel('Motivo de diferencia de cierre').fill('Demostración: diferencia de C$6 en conteo físico');await shot('08_revision_cierre');
 await page.getByRole('button',{name:'Confirmar cierre',exact:true}).click();await page.getByText('Turno cerrado. El conteo y la diferencia quedaron registrados.').waitFor();
 await page.getByRole('button',{name:'Salir',exact:true}).click();await page.getByLabel('Usuario',{exact:true}).fill('inventario');await page.getByLabel('Contraseña',{exact:true}).fill('MegaDemo2026!');await page.getByRole('button',{name:'Entrar a MEGA'}).click();
 await page.goto('https://localhost:7248/Existencias');await page.getByRole('heading',{name:'Existencias',exact:true}).waitFor();await shot('09_existencias');
 assert.equal(await page.getByRole('link',{name:'Nueva venta',exact:false}).count(),0);
 await page.setViewportSize({width:390,height:844});await shot('10_existencias_movil');
 assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth),false);
 assert.deepEqual(errors,[]);
 const result={fecha:new Date().toISOString(),estado:'Aprobado',tipo:'Prueba automatizada de interfaz; datos ficticios, sin usuarios del negocio',pasos:steps,erroresJavaScript:errors};
 fs.writeFileSync(path.join(root,'docs/evidencias/resultado_navegador.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
