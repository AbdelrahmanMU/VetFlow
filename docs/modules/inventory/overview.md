# Inventory — Overview

> Arabic name: المخزون
> Status: **Approved (2026-07-22)** — **Slice 1: Inventory Projection (read model)** DoR complete, owner-ruled (REQ-INV-002 / BR-INV-006..017 / AC-INV-004..013 / TS-INV-004..013 / DEC-INV-003..007). The **Write Kernel** (Receiving support) is **Approved & implemented** ([`write-kernel.md`](write-kernel.md)). The rest of the module proper (movements, batch management, monitoring) **remains pending and undesigned**.

> **Write Kernel carve-out (2026-07-22):** a minimal **Inventory Write Kernel** supporting **Purchase
> Receiving only** is specified in [`write-kernel.md`](write-kernel.md) (**Approved**). It is **not** the
> Inventory module — the module proper (stock levels, movements, projection, batch management) **remains
> pending and undesigned** beyond the Projection slice below.

Scope note (owner decision, 2026-07-12): the original Inventory domain was split into three modules — **Inventory** (this module: stock levels and movements), **Batch** (`../batch/`), and **Monitoring** (`../monitoring/`). Exact boundaries between the three will be defined during the documentation phase.

## Slice 1 — إسقاط المخزون (Inventory Projection) — read model

الشريحة الأولى من وحدة المخزون هي **إسقاط للقراءة فقط** يتيح للمستخدم **مطالعة الحالة الحالية للمخزون** — قائمة
صفّها منتج يعرض **الرصيد المتاح · وحدة المخزون · عدد الدفعات · أقرب صلاحية**. **لا تُدخِل أيّ منطق عمل جديد**؛
تُسقِط فقط بيانات أنتجها **استلام المشتريات** (REQ-PUR-005 / BR-PUR-010) والمخزّنة في `InventoryBatch` و
`ProductOnHand`. الإسقاط **قابل للتخلّص**: يُحتسب لحظة الاستعلام (نمط CQRS-lite المعتمد) ويمكن دائمًا إعادة
بنائه من البيانات القانونية — **لا يملك حالة مخزون** (BR-INV-006). وهو **متّسق نهائيًّا** يعرض **الحالة
المُثبَّتة فقط** (BR-INV-016)، ويُنفَّذ بـ **استعلام قراءة واحد بلا N+1** (BR-INV-017، DEC-INV-006).

### In scope (Slice 1)

- **قائمة مخزون للقراءة فقط** (REQ-INV-002): الأعمدة الخمسة، فلاتر (بحث · تصنيف · قرب انتهاء الصلاحية 30 يومًا ·
  نفاد المخزون)، فرز (منتج · رصيد متاح · أقرب صلاحية)، والحالات الأربع للعرض — بنمط قائمة المنتجات/المشتريات
  المعتمد. **التنقّل** من الصفّ إلى عارض الدفعات المستقبلي (توثيق التنقّل فقط). *(فلتر «مخزون منخفض» مؤجَّل —
  DEC-INV-004.)*

### Out of scope (Slice 1) — قفل النطاق

تسويات المخزون · تحرير الدفعة · دمج/تقسيم الدفعات · **عارض الدفعات نفسه** · استلام المشتريات · مرتجعات الشراء ·
المبيعات · FEFO · FIFO · الحجوزات · التحويلات · التقارير · لوحات المعلومات · وحدة المراقبة (Monitoring) ·
**فلتر «مخزون منخفض» (Low Stock)** — مؤجَّل (DEC-INV-004). أيّ إجراء يغيّر المخزون ممنوع (للقراءة فقط).

## Users & primary flows

- **الطبيب / الكاشير:** يفتح «المخزون» ليطالع الحالة الحالية، ويبحث ويفلتر ويفرز، وينتقل من صفّ إلى دفعات المنتج
  (عارض الدفعات — لاحقًا).

## Related modules

- **Catalog** — بيانات المنتج المرجعية (الاسم/التصنيف/وحدة المخزون) تُقرأ عبر مسار القراءة المعتمد باستعلام واحد بلا N+1 (DEC-INV-006). قدرة **حدّ إعادة الطلب لكل منتج** (لفلتر «مخزون منخفض» المؤجَّل) تخصّ الكتالوج (DEC-INV-004).
- **Purchasing** — منتج الإسقاط (`InventoryBatch`/`ProductOnHand`) من **استلام المشتريات** (نواة الكتابة).
- **Batch** — **عارض الدفعات** المستقبلي وجهة تنقّل الصفّ (غير مصمَّم هنا — DEC-INV-007).
