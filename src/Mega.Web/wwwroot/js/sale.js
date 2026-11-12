"use strict";
(() => {
  const app = document.getElementById("sale-app");
  if (!app) return;
  const $ = id => document.getElementById(id);
  const money = cents => "C$ " + (cents / 100).toLocaleString("es-NI", {minimumFractionDigits: 2, maximumFractionDigits: 2});
  let products = [], selected = null, lines = [], quote = null, key = crypto.randomUUID(), checkingOut = false, busy = false, pendingConfirmation = null;
  const csrf = app.querySelector('[name="__RequestVerificationToken"]').value;
  const recoveryKey = "mega.pending." + app.dataset.account;
  const showError = message => { $("sale-error").textContent = message; $("sale-error").hidden = !message; };
  async function request(handler, data) {
    const response = await fetch("/Venta?handler=" + handler, {
      method: data === undefined ? "GET" : "POST", headers: {"Content-Type": "application/json", "X-CSRF-TOKEN": csrf},
      credentials: "same-origin", body: data === undefined ? undefined : JSON.stringify(data)
    });
    if (response.redirected || !response.headers.get("content-type")?.includes("application/json")) throw new Error("Tu sesión cambió o la solicitud no pudo verificarse. Recarga la página e inicia sesión si corresponde.");
    const result = await response.json();
    if (!response.ok) throw new Error(result.error || "No se pudo completar la operación.");
    return result;
  }
  function renderProducts() {
    const filter = $("search").value.trim().toLocaleLowerCase();
    const list = $("product-list"); list.replaceChildren();
    for (const p of products.filter(p => (p.name + " " + p.code).toLocaleLowerCase().includes(filter))) {
      const button = document.createElement("button"); button.type = "button";
      button.className = "product-button" + (selected?.id === p.id ? " selected" : "");
      const name = document.createElement("strong"); name.textContent = p.name;
      const description = document.createElement("small"); description.textContent = p.presentation + " · " + p.code;
      const price = document.createElement("b"); price.textContent = money(p.price);
      button.append(name, description, price); button.addEventListener("click", () => selectProduct(p)); list.append(button);
    }
    if (!list.children.length) { const p = document.createElement("p"); p.className = "muted"; p.textContent = "No se encontraron productos."; list.append(p); }
  }
  function selectProduct(p) {
    if (busy || checkingOut || pendingConfirmation) return;
    selected = p; $("product-editor").hidden = false; $("selected-name").textContent = p.name + " · " + p.presentation;
    for (const option of $("mode").options) option.disabled = p.control === "unidad" ? option.value !== "unidad" : option.value === "unidad";
    $("mode").value = p.control === "unidad" ? "unidad" : "peso_manual"; $("quantity").value = "1";
    updateMode(); renderProducts(); $("quantity").focus(); $("quantity").select();
  }
  function updateMode() { $("quantity-label").textContent = $("mode").value === "importe" ? "Importe solicitado (C$)" : "Cantidad (" + (selected?.unit || "unidad") + ")"; }
  function renderCart() {
    const list = $("cart-lines"); list.replaceChildren();
    if (!quote) {
      const empty = document.createElement("div"); empty.className = "empty-cart"; empty.textContent = "Tu compra está vacía. Añade el primer producto."; list.append(empty);
    } else quote.lines.forEach((line, index) => {
      const row = document.createElement("div"); row.className = "cart-line";
      const text = document.createElement("div"), title = document.createElement("strong"), detail = document.createElement("small");
      title.textContent = line.name; const quantity = line.quantity / 10 ** line.quantityScale;
      detail.textContent = quantity.toLocaleString("es-NI", {maximumFractionDigits: 3}) + " × " + money(line.price) + " · " + line.presentation;
      if(line.promotion) detail.textContent += " · " + line.promotion.name + " −" + money(line.discount); if(line.tax) detail.textContent += " · impuestos " + money(line.tax); text.append(title, detail); const total = document.createElement("span"); total.textContent = money(line.total ?? line.subtotal);
      const remove = document.createElement("button"); remove.type = "button"; remove.className = "remove-line"; remove.textContent = "×"; remove.setAttribute("aria-label", "Retirar " + line.name);
      remove.disabled = checkingOut || busy || !!pendingConfirmation; remove.addEventListener("click", () => changeLines(lines.filter((_, i) => i !== index)));
      row.append(text, total, remove); list.append(row);
    });
    $("item-count").textContent = lines.length + (lines.length === 1 ? " línea" : " líneas");
    $("subtotal").textContent = money(quote?.subtotal || 0); $("discount-total").textContent = "−" + money(quote?.discount || 0); $("tax-total").textContent = money(quote?.tax || 0); $("total").textContent = money(quote?.total || 0);
    $("stock-warning").hidden = !quote?.needsPhysicalCheck;
    $("continue").disabled = !quote || busy; $("continue").hidden = checkingOut;
    $("checkout").hidden = $("confirm").hidden = $("back").hidden = !checkingOut;
    $("add-product").disabled = checkingOut || busy || !!pendingConfirmation;
    $("cancel").disabled = busy || !!pendingConfirmation; $("back").disabled = busy || !!pendingConfirmation;
    $("received").disabled = busy || !!pendingConfirmation;
    $("physical-check").disabled = busy || !!pendingConfirmation;
    updatePayment();
  }
  async function changeLines(next) {
    if (busy || pendingConfirmation) return;
    busy = true; showError(""); renderCart();
    try { const nextQuote = next.length ? await request("Cotizar", next) : null; lines = next; quote = nextQuote; $("physical-check").checked = false; key = crypto.randomUUID(); }
    catch (e) { showError(e.message); }
    finally { busy = false; renderCart(); }
  }
  function receivedCents() {
    const text = $("received").value.trim();
    if (!/^\d+(\.\d{1,2})?$/.test(text)) return null;
    const [whole, fraction = ""] = text.split("."); const value = Number(whole) * 100 + Number(fraction.padEnd(2, "0"));
    return Number.isSafeInteger(value) ? value : null;
  }
  function updatePayment() {
    const received = receivedCents(); const difference = (received ?? 0) - (quote?.total || 0);
    $("change").textContent = difference < 0 ? "Faltan " + money(-difference) : money(difference);
    $("confirm").disabled = busy || !quote || (!pendingConfirmation && (received === null || difference < 0 || (quote.needsPhysicalCheck && !$("physical-check").checked)));
    $("confirm").textContent = busy ? "Guardando venta…" : pendingConfirmation ? "Recuperar resultado de la venta" : "Confirmar venta";
  }
  $("search").addEventListener("input", renderProducts); $("mode").addEventListener("change", updateMode);
  $("add-product").addEventListener("click", async () => { if (!selected) return; await changeLines([...lines, {presentationId: selected.id, mode: $("mode").value, value: $("quantity").value.trim()}]); });
  $("continue").addEventListener("click", async () => {
    busy = true; showError(""); renderCart();
    try { quote = await request("Cotizar", lines); checkingOut = true; }
    catch (e) { showError(e.message); }
    finally { busy = false; renderCart(); if (checkingOut) $("received").focus(); }
  });
  $("received").addEventListener("input", updatePayment); $("physical-check").addEventListener("change", updatePayment);
  $("back").addEventListener("click", () => { if (pendingConfirmation) return; checkingOut = false; showError(""); renderCart(); });
  $("cancel").addEventListener("click", () => { if (pendingConfirmation) return; checkingOut = false; lines = []; quote = null; key = crypto.randomUUID(); $("received").value = ""; showError(""); renderCart(); });
  $("confirm").addEventListener("click", async () => {
    if (busy || !quote) return; busy = true; showError("");
    const payload = pendingConfirmation || {key, quoteHash: quote.hash, received: $("received").value.trim(), physicalStockChecked: $("physical-check").checked, lines};
    pendingConfirmation = payload;
    sessionStorage.setItem(recoveryKey, JSON.stringify({payload, quote}));
    renderCart();
    try {
      const response = await fetch("/Venta?handler=Confirmar", {method: "POST", headers: {"Content-Type": "application/json", "X-CSRF-TOKEN": csrf}, credentials: "same-origin", body: JSON.stringify(payload)});
      if (response.redirected || !response.headers.get("content-type")?.includes("application/json")) throw new Error("No se pudo comprobar el resultado. Usa Recuperar resultado; no repitas el cobro.");
      const result = await response.json();
      if (!response.ok) { if (response.status === 400) {pendingConfirmation = null; sessionStorage.removeItem(recoveryKey);} throw new Error(result.error || "No se pudo comprobar el resultado. Recupera la misma venta antes de continuar."); }
      pendingConfirmation = null; sessionStorage.removeItem(recoveryKey);
      window.location.assign(result.url);
    } catch (e) { showError(e.message || "No se pudo comprobar el resultado. Recupera la misma venta antes de continuar."); busy = false; renderCart(); }
  });
  window.addEventListener("beforeunload", e => { if (pendingConfirmation) {e.preventDefault(); e.returnValue = "";} });
  try {
    const saved = JSON.parse(sessionStorage.getItem(recoveryKey) || "null");
    if (saved?.payload && saved?.quote) {
      pendingConfirmation = saved.payload; quote = saved.quote; lines = saved.payload.lines; key = saved.payload.key; checkingOut = true;
      $("received").value = saved.payload.received; $("physical-check").checked = saved.payload.physicalStockChecked;
      showError("Hay una confirmación pendiente de comprobar. Recupera su resultado antes de preparar otra venta; no repitas el cobro."); renderCart();
    }
  } catch { showError("No se pudo leer la recuperación local. Revisa Ventas del turno antes de repetir cualquier cobro."); }
  request("Productos").then(result => { products = result; renderProducts(); }).catch(e => showError(e.message));
})();
