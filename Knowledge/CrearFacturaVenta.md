---
title: Crear Factura de Venta
category: ventas
tags: [factura, ventas, cliente]
last_updated: 2025-12-11
---

# Guía: Crear una Factura de Venta

## 1. Acceso al sistema
- Inicia sesión con tu usuario y contraseña.
- Verifica que tu rol tenga permisos para **Ventas > Facturas**.

## 2. Ir al módulo de Ventas
- En el menú principal, selecciona **Ventas**.
- Haz clic en **Facturas** o **Crear Factura**.

## 3. Datos del cliente
1) Presiona **Nueva Factura**.
2) Completa los datos del cliente:
   - **Cliente** (buscar por nombre o identificación)
   - **Dirección fiscal**
   - **RUT/NIF/Número fiscal**
   - **Condición de pago** (contado, crédito, 30 días, etc.)
   - **Vendedor** (opcional)

> Nota: Si el cliente no existe, usa **Crear cliente** y vuelve a la factura.

## 4. Agregar productos/servicios
1) Haz clic en **Agregar ítem**.
2) Selecciona el producto desde el catálogo o ingresa el código.
3) Define **cantidad**, **precio** y (opcional) **descuento**.
4) El sistema calcula **subtotal**, **impuestos** y **total**.

> Si el ítem no tiene stock suficiente, la factura podría quedar **On Hold**.

## 5. Impuestos y totales
- Revisa el **IVA/ITBIS** u otros impuestos aplicables.
- Verifica **subtotal**, **descuentos**, **impuestos** y **total**.
- Ajusta redondeos o percepciones si aplica.

## 6. Confirmar y guardar
- Presiona **Guardar** o **Emitir Factura**.
- Se generará un **número de factura** único y el estado inicial (Ej.: *Emitida* o *Borrador*).
- Puedes **Imprimir PDF** o **Enviar por correo** al cliente.

## 7. Estados especiales
- **On Hold (En espera):**
  - Falta de stock.
  - Cliente excede límite de crédito o requiere aprobación.
  - Datos fiscales incompletos.
- **Cancelada/Anulada:** por error o solicitud.
- **Pagada:** cuando se registra el cobro.

## 8. Registro del cobro (si corresponde)
- Ve a **Finanzas > Cobros** o desde la factura **Registrar pago**.
- Selecciona método: **Efectivo**, **Transferencia**, **Tarjeta**, **Cheque**.
- Confirma el monto y guarda.

## 9. Buenas prácticas
- Mantén actualizado el catálogo de **precios** y **impuestos**.
- Revisa el **límite de crédito** del cliente antes de emitir.
- Adjunta **Orden de Compra** si aplica.
- Documenta observaciones en el campo **Notas** de la factura.

---

### Preguntas frecuentes
**¿Cómo crear una factura?** → Sigue los pasos desde la sección [Acceso al sistema](#1-acceso-al-sistema) hasta [Confirmar y guardar](#6-confirmar-y-guardar).
**¿Qué hago si no hay stock?** → Marca la factura **On Hold** o genera una **orden de compra** para reabastecer.
**¿Cómo aplico un descuento?** → En el ítem, usa el campo **% descuento** o aplica un descuento general antes de guardar.
**¿Puedo editar una factura emitida?** → Según políticas, crea una **nota de crédito** o **anula** y vuelve a emitir.
**¿Cómo envío la factura por email?** → Usa el botón **Enviar** y confirma el correo del cliente.

---

### Trazabilidad
- Fuente: Manual interno de Ventas.
- Responsable: Área Comercial.
- Última actualización: 2025-12-11.
