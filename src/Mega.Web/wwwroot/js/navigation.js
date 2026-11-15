(() => {
    const sidebar = document.getElementById('sidebar');
    if (!sidebar) return;
    const body = document.body;
    const toggle = document.getElementById('sidebar-toggle');
    const menu = document.getElementById('mobile-menu');
    const close = document.getElementById('mobile-close');
    const backdrop = document.querySelector('.menu-backdrop');
    const shell = document.querySelector('.shell');
    const mobile = window.matchMedia('(max-width: 760px)');
    const groups = [...sidebar.querySelectorAll('.nav-group')];
    let returnFocus;
    body.classList.add('navigation-ready');

    function positionPanel(group) {
        if (!body.classList.contains('sidebar-compact') || mobile.matches || !group.open) return;
        const panel = group.querySelector('.nav-submenu');
        const row = group.querySelector('summary').getBoundingClientRect();
        panel.style.top = Math.max(8, Math.min(row.top, window.innerHeight - panel.offsetHeight - 8)) + 'px';
    }
    function compact(value) {
        body.classList.toggle('sidebar-compact', value);
        toggle.setAttribute('aria-expanded', String(!value));
        toggle.setAttribute('aria-label', value ? 'Expandir menú' : 'Reducir menú');
        groups.forEach(positionPanel);
    }
    try { compact(localStorage.getItem('mega.navigation.compact') === 'true'); } catch { compact(false); }
    toggle.addEventListener('click', () => {
        const value = !body.classList.contains('sidebar-compact');
        compact(value);
        try { localStorage.setItem('mega.navigation.compact', String(value)); } catch { /* Preference is optional. */ }
    });
    function mobileMenu(open) {
        if (open) returnFocus = document.activeElement;
        body.classList.toggle('menu-open', open);
        menu.setAttribute('aria-expanded', String(open));
        backdrop.hidden = !open;
        shell.inert = open;
        const quickNavigation = document.querySelector('.mobile-navigation');
        if (quickNavigation) quickNavigation.inert = open;
        if (open) {
            sidebar.setAttribute('role', 'dialog');
            sidebar.setAttribute('aria-modal', 'true');
            close.focus();
        } else {
            sidebar.removeAttribute('role');
            sidebar.removeAttribute('aria-modal');
            if (returnFocus?.isConnected) returnFocus.focus();
        }
    }
    menu.addEventListener('click', () => mobileMenu(true));
    close.addEventListener('click', () => mobileMenu(false));
    backdrop.addEventListener('click', () => mobileMenu(false));
    groups.forEach(group => {
        group.addEventListener('toggle', () => {
            if (!group.open) return;
            groups.filter(other => other !== group).forEach(other => { other.open = false; });
            positionPanel(group);
        });
        group.querySelector('.submenu-close').addEventListener('click', () => {
            group.open = false;
            group.querySelector('summary').focus();
        });
    });
    document.addEventListener('click', event => {
        if (!mobile.matches && body.classList.contains('sidebar-compact') && !sidebar.contains(event.target)) {
            groups.forEach(group => { group.open = false; });
        }
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            if (body.classList.contains('menu-open')) mobileMenu(false);
            else groups.filter(group => group.open).forEach(group => {
                group.open = false;
                group.querySelector('summary').focus();
            });
        }
        if (event.key === 'Tab' && body.classList.contains('menu-open')) {
            const focusable = [...sidebar.querySelectorAll('a, button, summary')].filter(element => element.getClientRects().length);
            const first = focusable[0], last = focusable.at(-1);
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
    });
    window.addEventListener('resize', () => {
        if (!mobile.matches && body.classList.contains('menu-open')) mobileMenu(false);
        groups.forEach(positionPanel);
    });
})();
