// Execute only against a fresh isolated demonstration database. Creates fictional operations.
const {chromium} = require(process.env.MEGA_PLAYWRIGHT || 'playwright');
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
(async () => {
    const base = process.env.MEGA_URL;
    assert.ok(base && process.env.MEGA_HOME_ISOLATED === '1', 'Set MEGA_URL and MEGA_HOME_ISOLATED=1 for a fresh test database.');
    const out = path.resolve(process.env.MEGA_HOME_OUTPUT || 'docs/evidencias/inicios-roles'); fs.mkdirSync(out, {recursive:true});
    const browser = await chromium.launch({channel:'msedge',headless:true});
    const errors = [], setupConsole = [], failures = [], external = [], checks = [];
    async function login(account) {
        const context = await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:1080},locale:'es-NI'});
        const page = await context.newPage();
        page.on('pageerror', e => errors.push(e.message));
        page.on('console', m => { if (m.type()==='error') {
            if (page.url()===base+'/') errors.push(m.text());
            else setupConsole.push({ruta:new URL(page.url()).pathname,mensaje:m.text()});
        }});
        page.on('response', r => { if(r.status()>=400) failures.push({url:r.url(),status:r.status()}); });
        page.on('request', r => { if(new URL(r.url()).origin!==base) external.push(r.url()); });
        await page.goto(base+'/Acceso'); await page.getByLabel('Usuario',{exact:true}).fill(account);
        await page.getByLabel('Contraseña',{exact:true}).fill('MegaDemo2026!');
        await page.getByRole('button',{name:'Entrar a MEGA'}).click(); await page.waitForURL(base+'/'); return page;
    }
    async function shot(page, name) { await page.evaluate(()=>document.fonts.ready); await page.screenshot({path:path.join(out,name+'.png'),fullPage:true}); }
    async function receipt(page, reference, lot) {
        await page.goto(base+'/Recepciones/Editar'); await page.getByLabel('Referencia interna única').fill(reference);
        await page.getByLabel('Proveedor',{exact:true}).selectOption({label:'Proveedor de ensayo de inicios'});
        await page.getByLabel('Número de documento del proveedor').fill('DOC-'+reference);
        const now = new Date(Date.now()-6*3600000); const day=now.toISOString().slice(0,10);
        await page.getByLabel('Fecha del documento').fill(day); await page.getByLabel('Producto y presentación').selectOption('2');
        await page.getByLabel('Cantidad recibida').fill('2'); await page.getByLabel('Cantidad aceptada').fill('2');
        await page.getByLabel('Costo C$ por unidad base').fill('24');
        if(lot) { await page.getByLabel('Referencia de lote').fill('LOTE-REVISION'); await page.getByLabel('Vencimiento',{exact:true}).fill(new Date(now.getTime()+5*86400000).toISOString().slice(0,10)); }
        await page.getByRole('button',{name:'Guardar preparación',exact:true}).click();
        await page.getByText('Preparación guardada. No se han modificado existencias.').waitFor();
        await page.getByRole('button',{name:'Enviar versión guardada a revisión'}).click();
        await page.getByText('Recepción enviada. Otra cuenta de gestión debe revisar esta versión.').waitFor();
        return page.getByRole('link',{name:'Ver contenido revisado'}).first().getAttribute('href');
    }
    try {
        const admin = await login('admin'), inv = await login('inventario'), owner = await login('propietario'), cashier = await login('cajero');
        assert.match(await owner.locator('main').innerText(),/Todavía no hay ventas/);
        assert.match(await inv.locator('main').innerText(),/No tienes trabajo pendiente/);
        await shot(owner,'01_propietario_sin_ventas'); await shot(inv,'02_inventario_sin_pendientes'); checks.push('Estados sin ventas, sin pendientes y umbral sin configurar');
        await admin.goto(base+'/Configuracion'); await admin.getByLabel('Próximo vencimiento, en días').fill('7');
        await admin.getByRole('button',{name:'Guardar configuración'}).click(); await admin.getByText('Configuración actualizada.').waitFor();
        await admin.goto(base+'/Catalogo/Producto/2'); await admin.getByLabel('Existencia mínima, en unidad base').fill('35');
        await admin.getByRole('button',{name:'Guardar producto',exact:true}).click(); await admin.getByText('Producto guardado.').waitFor();
        await admin.goto(base+'/Proveedores'); await admin.getByLabel('Nombre del proveedor').fill('Proveedor de ensayo de inicios');
        await admin.getByRole('button',{name:'Guardar proveedor'}).click(); await admin.getByText('Proveedor guardado.').waitFor();
        const approved = await receipt(inv,'REC-LOTE-CONFIRMADO',true);
        await admin.goto(base+approved); await admin.getByLabel('Motivo de la decisión y comprobación realizada').fill('Ensayo ficticio de recepción para revisar el inicio');
        await admin.getByRole('button',{name:'Aprobar y registrar entrada'}).click(); await admin.getByText('Recepción confirmada. Se registró una única entrada de existencias.').waitFor();
        await receipt(inv,'REC-POR-REVISAR',false); await receipt(admin,'REC-ESPERA-OTRA-CUENTA',false);
        await inv.goto(base+'/Conteos/Editar'); await inv.getByLabel('Referencia única del conteo').fill('CON-EN-CURSO');
        await inv.getByLabel('Producto y unidad base').selectOption('2'); await inv.getByLabel('Cantidad física en unidad base').fill('31');
        await inv.getByLabel('He comprobado físicamente').check(); await inv.getByRole('button',{name:'Guardar conteo',exact:true}).click();
        await inv.getByText('Conteo guardado con saldo de referencia. Las existencias no se modificaron.').waitFor();
        await cashier.goto(base+'/Caja'); await cashier.getByLabel('Fondo real recibido (C$)').fill('500');
        await cashier.getByRole('button',{name:'Abrir mi turno'}).click(); await cashier.goto(base+'/Venta');
        await cashier.getByRole('button',{name:/Leche entera.*DEMO002/}).click(); await cashier.getByRole('button',{name:'Añadir a la compra'}).click();
        await cashier.getByRole('button',{name:'Continuar al cobro'}).click(); await cashier.getByLabel('Efectivo recibido (C$)').fill('100');
        await cashier.getByRole('button',{name:'Confirmar venta',exact:true}).click(); await cashier.waitForURL('**/Comprobante/*');
        await cashier.goto(base+'/Caja?Review=true'); await cashier.getByLabel('Efectivo contado (C$)').fill('537');
        await cashier.getByLabel('Motivo de diferencia de cierre').fill('Diferencia ficticia de un córdoba para revisión');
        await cashier.getByRole('button',{name:'Confirmar cierre'}).click();
        await cashier.goto(base+'/'); await shot(cashier,'03_cajero_conservado');
        for(const [role,page,title] of [['inventario',inv,'Trabajo de inventario'],['administrador',admin,'Operación del día'],['propietario',owner,'Resumen del negocio']]) {
            await page.goto(base+'/'); assert.equal(await page.getByRole('heading',{level:1}).innerText(),title);
            if(role==='inventario') { const text=await page.locator('main').innerText(); assert.ok(!text.includes('C$')); assert.ok(!text.includes('REC-ESPERA-OTRA-CUENTA')); assert.match(text,/CON-EN-CURSO/); }
            else { assert.match(await page.locator('main').innerText(),/C\$ 38.00/); assert.match(await page.locator('main').innerText(),/C\$ -1.00/); }
            if(role==='propietario') assert.match(await page.locator('main').innerText(),/No calculable/);
            if(role==='administrador') {
                const own=page.locator('.dashboard-list li').filter({hasText:'REC-ESPERA-OTRA-CUENTA'});
                assert.match(await own.innerText(),/Espera otra cuenta/);
                assert.equal(await own.getByRole('link',{name:'Revisar',exact:true}).count(),0);
            }
            await shot(page,'04_'+role+'_jornada');
            const task = page.locator('.dashboard-list li').filter({hasText:'REC-POR-REVISAR'});
            await task.getByRole('link',{name:role==='inventario'?'Consultar':'Revisar',exact:true}).click();
            assert.ok(page.url().includes(role==='inventario'?'/Recepciones/Editar':'/Recepciones/Revision')); await page.goto(base+'/');
            for(const width of [320,390,768,1024,1440]) {
                await page.setViewportSize({width,height:1000}); await page.evaluate(()=>document.fonts.ready);
                const geometry=await page.evaluate(()=>({viewport:innerWidth,width:document.documentElement.scrollWidth}));
                assert.ok(geometry.width<=width+1,JSON.stringify({role,geometry}));
                checks.push(role+' sin desbordamiento a '+width+' px');
                if(width===390) await shot(page,'05_'+role+'_movil');
            }
        }
        assert.deepEqual(errors,[]); assert.deepEqual(failures,[]); assert.deepEqual(external,[]);
        const result={estado:'Aprobado',fecha:new Date().toISOString(),base:'Ficticia aislada',checks,erroresInicio:errors,consolaPreparacion:setupConsole,respuestasFallidas:failures,solicitudesExternas:external};
        fs.writeFileSync(path.join(out,'resultado.json'),JSON.stringify(result,null,2)+'\n'); console.log(JSON.stringify({estado:result.estado,comprobaciones:checks.length,erroresInicio:errors,avisosPreparacion:setupConsole.length}));
    } finally { await browser.close(); }
})().catch(e=>{console.error(e);process.exitCode=1;});
