const {chromium}=require(process.env.MEGA_PLAYWRIGHT||'playwright');
const path=require('node:path'),fs=require('node:fs'),assert=require('node:assert/strict');
(async()=>{
 const base=process.env.MEGA_URL||'https://localhost:7366',phase=process.env.MEGA_DISTRIBUTION_PHASE||'initial';assert.equal(new URL(base).hostname,'localhost');assert.equal(new URL(base).port,'7366');assert.ok(['initial','restart'].includes(phase));assert.equal(process.env.MEGA_DISTRIBUTION_ISOLATED,'1');
 const out=path.resolve('docs/evidencias/distribucion');fs.mkdirSync(out,{recursive:true});const external=[],errors=[];
 const browser=await chromium.launch({channel:'msedge',headless:true}),context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:1100}});
 await context.route('**/*',route=>{if(new URL(route.request().url()).hostname!=='localhost'){external.push(route.request().url());return route.abort();}return route.continue();});const page=await context.newPage();page.on('pageerror',error=>errors.push(error.message));
 try{
  await page.goto(base+'/Acceso');await page.getByLabel('Usuario',{exact:true}).fill('cajero');await page.getByLabel('Contraseña',{exact:true}).fill('MegaDemo2026!');await page.getByRole('button',{name:'Entrar a MEGA'}).click();await page.waitForURL('**/');
  if(phase==='initial'){
   await page.goto(base+'/Caja');await page.getByLabel('Fondo real recibido (C$)').fill('500');await page.getByRole('button',{name:'Abrir mi turno'}).click();await page.goto(base+'/Venta');await page.getByRole('button',{name:/Leche entera.*DEMO002/}).click();await page.getByRole('button',{name:'Añadir a la compra'}).click();await page.getByRole('button',{name:'Continuar al cobro'}).click();await page.getByLabel('Efectivo recibido (C$)').fill('50');await page.getByRole('button',{name:'Confirmar venta',exact:true}).click();await page.waitForURL(/Comprobante\/1$/);await page.screenshot({path:path.join(out,'01_venta_paquete.png'),fullPage:true});
  }
  await page.goto(base+'/Ventas');assert.equal(await page.getByRole('link',{name:'Ver comprobante'}).count(),1);await page.goto(base+'/Caja');assert.match(await page.locator('#cash-expected').innerText(),/538.00/);await page.screenshot({path:path.join(out,phase==='initial'?'02_caja_paquete.png':'03_datos_tras_actualizar.png'),fullPage:true});assert.deepEqual(external,[]);assert.deepEqual(errors,[]);
  const result={estado:'Aprobado',version:'0.21.0',fase:phase,fecha:new Date().toISOString(),alcance:'Carpeta y base aisladas. Navegador bloquea destinos externos. No acredita otro equipo fisico.',ventas:1,cajaEsperadaCentavos:53800,recursosExternos:external,erroresJavaScript:errors};fs.writeFileSync(path.join(out,phase+'.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
