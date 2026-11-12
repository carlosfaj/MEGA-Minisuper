(() => {
 const search=document.querySelector('#project-search'),area=document.querySelector('#project-area');
 const filter=()=>{const term=search.value.trim().toLocaleLowerCase('es');for(const card of document.querySelectorAll('.kanban-card'))card.hidden=(area.value&&card.dataset.area!==area.value)||!card.dataset.search.toLocaleLowerCase('es').includes(term);for(const lane of document.querySelectorAll('.kanban-lane'))lane.querySelector('[data-lane-count]').textContent=lane.querySelectorAll('.kanban-card:not([hidden])').length;};
 search.addEventListener('input',filter);area.addEventListener('change',filter);
 setInterval(async()=>{try{const r=await fetch('/Proyecto?handler=Estado',{cache:'no-store'});if(r.ok&&(await r.json()).hash!==document.querySelector('#project-board').dataset.hash)location.reload();}catch{}},15000);
})();
