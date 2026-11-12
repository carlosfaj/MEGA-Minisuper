// Simula pérdida de respuesta después de que el servidor guardó una venta.
const {chromium}=require(process.env.MEGA_PLAYWRIGHT || 'playwright');
const fs=require('node:fs');const path=require('node:path');const assert=require('node:assert/strict');
(async()=>{
 const root=path.resolve(__dirname,'..');const browser=await chromium.launch({channel:'msedge',headless:true});
 const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:1000}});const page=await context.newPage();
 try{
 await page.goto('https://localhost:7248/Acceso');await page.getByLabel('Usuario',{exact:true}).fill('cajero');await page.getByLabel('Contraseña',{exact:true}).fill('MegaDemo2026!');await page.getByRole('button',{name:'Entrar a MEGA'}).click();
 await page.goto('https://localhost:7248/Caja');await page.getByLabel('Fondo real recibido (C$)').fill('500');await page.getByRole('button',{name:'Abrir mi turno'}).click();
 await page.goto('https://localhost:7248/Venta');await page.getByRole('button',{name:/Jabón.*DEMO006/}).click();await page.getByRole('button',{name:'Añadir a la compra'}).click();
 await page.locator('#total').filter({hasText:'25.00'}).waitFor();await page.getByRole('button',{name:'Continuar al cobro'}).click();await page.getByLabel('Efectivo recibido (C$)').fill('50');
 let intercepted=false;
 await page.route('**/Venta?handler=Confirmar',async route=>{
  if(intercepted){await route.continue();return;}intercepted=true;
  const response=await route.fetch();assert.equal(response.status(),200);await route.abort('failed');
 });
 await page.getByRole('button',{name:'Confirmar venta',exact:true}).click();
 await page.getByRole('button',{name:'Recuperar resultado de la venta',exact:true}).waitFor();
 assert.equal(await page.getByRole('button',{name:'Cancelar preparación',exact:true}).isDisabled(),true);
 page.once('dialog',d=>d.accept());await page.reload();
 await page.getByRole('button',{name:'Recuperar resultado de la venta',exact:true}).waitFor();
 await page.screenshot({path:path.join(root,'docs/evidencias/pantallas/11_recuperacion_confirmacion.png'),fullPage:true});
 await page.getByRole('button',{name:'Recuperar resultado de la venta',exact:true}).click();await page.waitForURL(/Comprobante\/\d+/);
 assert.match(await page.locator('#receipt-total').innerText(),/25.00/);
 await page.goto('https://localhost:7248/Ventas');assert.equal(await page.getByRole('link',{name:'Ver comprobante'}).count(),1);
 await page.goto('https://localhost:7248/Caja');assert.match(await page.locator('#cash-expected').innerText(),/525.00/);
 await page.getByRole('link',{name:'Revisar cierre'}).click();await page.getByLabel('Efectivo contado (C$)').fill('525');await page.getByRole('button',{name:'Confirmar cierre',exact:true}).click();
 await page.getByText('Turno cerrado. El conteo y la diferencia quedaron registrados.').waitFor();
 const result={fecha:new Date().toISOString(),estado:'Aprobado',escenario:'Respuesta perdida después de confirmar; recarga de pestaña; recuperación de la misma venta sin otro cobro',ventasEnTurno:1,totalCentavos:2500,cajaEsperadaCentavos:52500,tipo:'Prueba automatizada con datos ficticios'};
 fs.writeFileSync(path.join(root,'docs/evidencias/resultado_recuperacion.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
