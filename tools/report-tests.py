"""Genera una matriz de trabajo desde los resultados TRX realmente ejecutados."""
from pathlib import Path
from xml.etree import ElementTree as ET
from datetime import datetime, timezone

root=Path(__file__).resolve().parents[1]
trx=root/"docs/evidencias/pruebas/avance01.trx"
ns={"t":"http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
tree=ET.parse(trx)
cases={
 "T05_sale_is_atomic_and_repeat_recovers_one_sale":("RF04 RF03 RF05 RF08","T05 Venta y reintento","Total C$86; recibido C$100; cambio C$14; una sola venta/pago/caja/impresión; existencias 48 lb y 29 unidades; caja C$566"),
 "T05_insufficient_payment_leaves_no_partial_effects":("RF04","T05 Pago insuficiente","C$80 rechaza venta de C$86; ninguna venta, pago, caja ni operación parcial"),
 "rollback_after_late_database_failure_removes_sale_payment_and_stock_effects":("RF04 RF03 RF05","Integración y reversión por fallo tardío","Un fallo al crear impresión revierte venta, detalles, pago, caja y existencias"),
 "T08_one_station_rejects_two_open_shifts_and_requires_difference_reason":("RF05","T08 Apertura de caja","Diferencia de fondo exige motivo; se rechaza una segunda apertura de la cuenta o estación"),
 "T09_close_saves_expected_count_difference_and_keeps_snapshot":("RF05","T09 Cierre de caja","Esperado C$566, contado C$560, diferencia -C$6 con motivo; turno cerrado impide preparar venta"),
 "stale_close_cannot_hide_a_new_sale":("RF05 RF04","Cierre con versión anterior","Se rechaza cerrar desde una revisión anterior a una nueva venta"),
 "price_change_requires_review_before_payment":("RF01 RF04","Cambio de precio después de revisar","Se rechaza la cotización anterior sin crear una venta"),
 "same_key_with_other_content_or_account_is_rejected":("RF04 RF06","Reutilización incompatible de clave","La misma clave con otro cobro o cuenta se rechaza"),
 "quantity_mode_and_overflow_are_validated":("RF04","Validación de cantidades/modalidad","Rechazar fracción indivisible, signo, cero, precisión excesiva, desbordamiento o modalidad incompatible"),
 "sales_by_amount_show_actual_rounded_quantity_and_total":("RF04","Venta por importe","C$10 de arroz a C$24/lb produce 0.417 lb y total C$10.01 con redondeo documentado"),
 "negative_stock_requires_physical_check_without_blocking_by_sign":("RF03 RF04 RF08","Discrepancia de existencia física","Exigir comprobación explícita; con ella permitir saldo -1 y conservar evidencia de la confirmación"),
 "database_foreign_keys_and_39_tables_match_reference":("RF03 RF04 RF06","Integridad de esquema","39 tablas, foreign_keys=ON y foreign_key_check sin filas"),
 "real_http_flow_checks_login_csrf_sale_and_role_boundaries":("RF04 RF05 RF06","Integración HTTP real","Inicio de sesión, antifalsificación, apertura, venta y comprobante; Inventario sin venta/comprobante, Cajero sin bitácora"),
 "credential_version_change_invalidates_existing_session":("RF06","Revocación de sesión","Al cambiar la versión de credenciales, la cookie existente deja de autorizar acceso"),
 "concurrent_retries_create_a_single_business_operation":("RF04 RF05 RF03","Reintentos concurrentes","Dos confirmaciones simultáneas con misma clave devuelven la misma venta; una sola operación comercial"),
 "known_cost_product_waits_for_valuation_instead_of_discarding_cost":("RF03 RF04","Costo conocido sin módulo de valoración","Rechazar el producto hasta integrar valoración, sin convertir su costo conocido en desconocido"),
 "existing_demo_database_cannot_be_switched_to_real_mode":("RF06","Separación de base ficticia y base real","Rechazar iniciar una base de demostración con el modo ficticio desactivado"),
}
results=tree.findall(".//t:UnitTestResult",ns)
passed=sum(x.get("outcome")=="Passed" for x in results)
end=datetime.fromisoformat(tree.find("t:Times",ns).get("finish")).astimezone(timezone.utc).isoformat()
text=["# Matriz de pruebas del avance 01", "",f"Ejecución finalizada en {end} (UTC). Resultado: **{passed}/{len(results)} pruebas aprobadas**.", "",
      "Pruebas automatizadas con datos ficticios y bases SQLite efímeras. No acreditan aceptación con usuarios, fiscalidad ni impresión física. Los criterios T05/T08/T09 se cubren dentro del alcance descrito; T07/T13 solo tienen cobertura parcial y el resto continúa pendiente según Estado_Cuarto_Corte.md.","",
      "Fuente verificable: [resultado TRX](evidencias/pruebas/avance01.trx). Código reproducible: tests/Mega.Tests/FlowTests.cs.","",
      "| Código | Requisito | Caso de prueba | Resultado esperado | Resultado obtenido | Estado |",
      "|---|---|---|---|---|---|"]
for index,result in enumerate(sorted(results,key=lambda x:x.get("testName")),1):
    full=result.get("testName")
    name=full.partition("FlowTests.")[2] or full
    base=name.split("(")[0].strip()
    rf,title,expected=cases.get(base,("Por revisar",name,"Revisar mapeo de la prueba"))
    if "(" in name: title+=" "+name[name.index("("):]
    status="Aprobado" if result.get("outcome")=="Passed" else "No aprobado"
    actual="Aserciones verificadas sobre la ejecución registrada" if status=="Aprobado" else "Consultar incidencia en TRX"
    text.append(f"| CP-{index:02} | {rf} | {title} | {expected} | {actual} | {status} |")
text += ["","## Comprobación del navegador", "", "El recorrido adicional verifica acceso, apertura con C$480, compra arroz/leche, bloqueo de C$80, cobro de C$100/cambio C$14, comprobante C$86, caja C$566, cierre con diferencia y consulta de existencias para Inventario. También se comprueba la vista móvil sin desbordamiento de la página y sin errores JavaScript.","",
         "El registro está en [resultado del navegador](evidencias/resultado_navegador.json); las capturas explicadas están en [evidencias del avance](Evidencias_Avance01.md). También se ejecutó la pérdida de respuesta después de guardar, recarga de pestaña y recuperación de una única venta de C$25 sin repetir caja ni cobro: [resultado de recuperación](evidencias/resultado_recuperacion.json).", "", "## Incidencia corregida", "",
         "La primera ejecución encontró que abrir caja pedía validar también el campo del formulario de cierre. Se separó la obligatoriedad por operación: cada manejador exige únicamente sus datos. La segunda ejecución aprobó el flujo HTTP completo. Las 20 pruebas restantes de la primera ejecución ya habían aprobado. El resultado final reemplaza al preliminar y conserva el registro de la corrección aquí.", ""]
(root/"docs/Matriz_Pruebas.md").write_text("\n".join(text),encoding="utf-8")
print(f"Matriz generada desde TRX: {passed}/{len(results)}.")
