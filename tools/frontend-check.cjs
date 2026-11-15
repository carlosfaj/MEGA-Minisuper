// Run only against a fresh, isolated demonstration database.
const {chromium} = require(process.env.MEGA_PLAYWRIGHT || 'playwright');
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
(async () => {
    const base = process.env.MEGA_URL || 'https://localhost:7375';
    const out = path.resolve(process.env.MEGA_FRONTEND_OUTPUT || 'docs/evidencias/frontend-branded');
    fs.mkdirSync(out, {recursive: true});
    const browser = await chromium.launch({channel: 'msedge', headless: true});
    const errors = [], consoleErrors = [], failedResponses = [], externalRequests = [], checks = [], images = [];
    const context = await browser.newContext({ignoreHTTPSErrors: true, viewport: {width:1440,height:1120}, locale:'es-NI'});
    const page = await context.newPage();
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
    page.on('response', response => { if (response.status() >= 400) failedResponses.push({url:response.url(),status:response.status()}); });
    page.on('request', request => { if (new URL(request.url()).origin !== base) externalRequests.push(request.url()); });
    const settle = async () => { await page.evaluate(() => document.fonts.ready); await page.waitForTimeout(100); };
    const geometry = async () => {
        await settle();
        const state = await page.evaluate(() => ({
            width:innerWidth, scroll:document.documentElement.scrollWidth,
            assets:[...document.querySelectorAll('img')].filter(img => img.getBoundingClientRect().width > 0).map(img => {
                const rect=img.getBoundingClientRect(); return {src:new URL(img.src).pathname,loaded:img.complete&&img.naturalWidth>0,width:rect.width,height:rect.height,naturalWidth:img.naturalWidth,naturalHeight:img.naturalHeight};
            })
        }));
        assert.ok(state.scroll <= state.width + 1, 'Overflow '+page.url()+': '+JSON.stringify(state));
        for (const img of state.assets) {
            assert.ok(img.loaded, 'Missing asset '+img.src);
            const root=img.src.endsWith('.svg') ? fs.readFileSync(path.join('src/Mega.Web/wwwroot',img.src),'utf8').match(/<svg[^>]*>/)[0] : null;
            const viewBox=root ? root.match(/viewBox="([^"]+)"/)[1].split(/\s+/).map(Number) : [0,0,img.naturalWidth,img.naturalHeight];
            assert.ok(Math.abs((img.width/img.height)/(viewBox[2]/viewBox[3])-1)<0.015, 'Distorted asset '+JSON.stringify(img));
        }
        images.push({page:new URL(page.url()).pathname,viewport:state.width,assets:state.assets});
    };
    const shot = async name => { await geometry(); await page.screenshot({path:path.join(out,name+'.png'),fullPage:true}); };
    const login = async name => {
        await page.goto(base+'/Acceso');
        await page.getByLabel('Usuario',{exact:true}).fill(name);
        await page.getByLabel('Contraseña',{exact:true}).fill('MegaDemo2026!');
        await page.getByRole('button',{name:'Entrar a MEGA'}).click();
        await page.waitForURL(base+'/');
    };
    const group = label => page.locator('.nav-group').filter({has:page.locator('summary').filter({hasText:label})});
    try {
        await page.goto(base+'/Acceso'); await shot('01_acceso');
        for (const width of [320,390,768,1024,1440]) { await page.setViewportSize({width,height:1120}); await geometry(); }
        assert.equal(await page.evaluate(() => getComputedStyle(document.body).backgroundColor),'rgb(247, 240, 221)');
        assert.ok(await page.evaluate(() => document.fonts.check('15px "Nunito Sans"') && document.fonts.check('700 40px DynaPuff')));
        await page.getByLabel('Usuario',{exact:true}).fill('cajero');
        await page.getByLabel('Contraseña',{exact:true}).fill('incorrecta');
        await page.getByRole('button',{name:'Entrar a MEGA'}).click();
        await page.getByText('Usuario o contraseña no válidos.').waitFor(); await shot('02_acceso_error');
        await page.getByLabel('Contraseña',{exact:true}).fill('MegaDemo2026!');
        await page.getByRole('button',{name:'Entrar a MEGA'}).click(); await page.waitForURL(base+'/');
        assert.equal(await page.locator('.work-navigation a[href="/Configuracion"]').count(),0);
        await shot('03_inicio_cajero'); checks.push('Acceso, error de credenciales, identidad y permisos de cajero');
        await page.getByRole('button',{name:'Reducir menú',exact:true}).click();
        assert.equal(await page.locator('.sidebar').evaluate(e => Math.round(e.getBoundingClientRect().width)),80);
        await page.locator('.nav-group summary').filter({hasText:'Ventas y caja'}).click();
        await page.locator('.sidebar-compact .nav-submenu:visible').waitFor();
        await shot('04_menu_compacto');
        const panel=await page.locator('.nav-submenu:visible').boundingBox(); assert.ok(panel.x+panel.width<=1440 && panel.y+panel.height<=1120);
        await page.keyboard.press('Escape'); assert.equal(await page.locator('.nav-submenu:visible').count(),0);
        await page.goto(base+'/Caja');
        assert.equal(await page.locator('.sidebar').evaluate(e => Math.round(e.getBoundingClientRect().width)),80);
        await page.getByRole('button',{name:'Expandir menú',exact:true}).click();
        assert.equal(await page.locator('.sidebar').evaluate(e => Math.round(e.getBoundingClientRect().width)),248);
        checks.push('Menú 248/80 px, grupos, panel relacionado, Escape y preferencia tras navegar');
        await page.getByLabel('Fondo real recibido (C$)').fill('480');
        await page.getByLabel('Motivo de diferencia con el fondo habitual').fill('Prueba frontend: fondo recibido C$20 menor');
        await page.getByRole('button',{name:'Abrir mi turno'}).click();
        await page.getByText('Tu turno está abierto. Ya puedes registrar ventas.').waitFor();
        await page.goto(base+'/'); await shot('05_inicio_turno');
        await page.goto(base+'/Venta');
        await page.getByRole('button',{name:/Arroz blanco.*DEMO001/}).click();
        await page.getByLabel('Cantidad (lb)',{exact:true}).fill('2.000');
        await page.getByRole('button',{name:'Añadir a la compra'}).click();
        await page.locator('#total').filter({hasText:'48.00'}).waitFor();
        await page.getByRole('button',{name:/Leche entera.*DEMO002/}).click();
        await page.getByRole('button',{name:'Añadir a la compra'}).click();
        await page.locator('#total').filter({hasText:'86.00'}).waitFor(); await shot('06_venta');
        for (const width of [320,390,768,1024,1440]) {
            await page.setViewportSize({width,height:width<761?844:1120}); await geometry();
            if (width===390) {
                await shot('07_venta_movil');
                await page.getByRole('button',{name:'Menú',exact:true}).click();
                await page.getByRole('dialog',{name:'Menú de MEGA'}).waitFor();
                await page.keyboard.press('Shift+Tab');
                assert.ok(await page.locator('.sidebar').evaluate(el => el.contains(document.activeElement)));
                await shot('08_menu_movil');
                await page.keyboard.press('Escape');
                assert.equal(await page.getByRole('button',{name:'Menú',exact:true}).getAttribute('aria-expanded'),'false');
                assert.ok(await page.getByRole('button',{name:'Menú',exact:true}).evaluate(el=>el===document.activeElement));
            }
        }
        checks.push('Anchos 320/390/768/1024/1440, menú móvil, foco, Escape y acceso rápido fijo');
        await page.getByRole('button',{name:'Continuar al cobro'}).click();
        await page.getByLabel('Efectivo recibido (C$)').fill('80');
        assert.ok(await page.getByRole('button',{name:'Confirmar venta',exact:true}).isDisabled()); await shot('09_pago_insuficiente');
        await page.getByLabel('Efectivo recibido (C$)').fill('100');
        await page.locator('#change').filter({hasText:'14.00'}).waitFor();
        await page.getByRole('button',{name:'Confirmar venta',exact:true}).click();
        await page.waitForURL(/Comprobante\/\d+/);
        assert.match(await page.locator('#receipt-total').innerText(),/86.00/);
        assert.match(await page.locator('#receipt-change').innerText(),/14.00/);
        await shot('10_comprobante');
        await page.setViewportSize({width:390,height:844}); await shot('11_comprobante_movil');
        await page.setViewportSize({width:1440,height:1120}); await page.goto(base+'/Caja');
        assert.match(await page.locator('#cash-expected').innerText(),/566.00/);
        await page.getByRole('link',{name:'Revisar cierre'}).click();
        await page.getByLabel('Efectivo contado (C$)').fill('560');
        await page.getByLabel('Motivo de diferencia de cierre').fill('Prueba frontend: diferencia C$6');
        await page.getByRole('button',{name:'Confirmar cierre',exact:true}).click();
        await page.getByText('Turno cerrado. El conteo y la diferencia quedaron registrados.').waitFor();
        checks.push('Apertura 480, venta arroz/leche 86, pago insuficiente, recibido 100/cambio 14, caja 566 y cierre 560');
        await page.getByRole('button',{name:'Salir',exact:true}).click();
        await login('inventario'); await shot('12_inicio_inventario');
        assert.equal(await page.locator('.work-navigation a[href="/Venta"]').count(),0);
        assert.equal(await page.locator('.work-navigation a[href="/Configuracion"]').count(),0);
        await page.goto(base+'/Existencias'); await shot('13_existencias');
        await page.setViewportSize({width:390,height:844}); await shot('14_existencias_movil');
        await page.setViewportSize({width:1440,height:1120});
        await page.getByRole('button',{name:'Salir',exact:true}).click();
        await login('admin'); await shot('15_inicio_administracion');
        await page.getByRole('button',{name:'Salir',exact:true}).click();
        await login('propietario');
        const routes=[...new Set(await page.locator('.work-navigation a').evaluateAll(links=>links.map(a=>new URL(a.href).pathname)))];
        for (const route of routes) {
            const response=await page.goto(base+route);
            assert.equal(response.status(),200,route); await geometry();
            await page.setViewportSize({width:390,height:844}); await geometry();
            await page.setViewportSize({width:1440,height:1120});
        }
        await page.goto(base+'/Catalogo'); await shot('16_productos');
        await page.getByRole('button',{name:'Reducir menú',exact:true}).click();
        if (!await group('Catálogo').evaluate(el=>el.open)) await page.locator('.nav-group summary').filter({hasText:'Catálogo'}).click();
        await page.locator('.nav-submenu:visible').waitFor(); await shot('17_catalogo_compacto');
        checks.push('Inicio por rol y todas las rutas del menú de propietario en escritorio y móvil: '+routes.length);
        assert.deepEqual(errors,[]); assert.deepEqual(consoleErrors,[]); assert.deepEqual(failedResponses,[]); assert.deepEqual(externalRequests,[]);
        const result={estado:'Aprobado',fecha:new Date().toISOString(),version:fs.readFileSync('VERSION','utf8').trim(),datos:'Ficticios; base aislada; sin impresión física ni envío de correos',checks,routes,erroresJavaScript:errors,erroresConsola:consoleErrors,respuestasFallidas:failedResponses,solicitudesExternas:externalRequests,geometriaRecursos:images};
        fs.writeFileSync(path.join(out,'resultado.json'),JSON.stringify(result,null,2));
        console.log(JSON.stringify({estado:result.estado,checks,rutas:routes.length,comprobacionesGeometria:images.length}));
    } catch(error) {
        await page.screenshot({path:path.join(out,'fallo.png'),fullPage:true});
        console.error({errors,consoleErrors,failedResponses}); throw error;
    } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
