# Release: MVP — Scope Contract

> Status: **Amended by owner ruling (2026-07-31)** — restructured into
> **Pilot MVP Scope** (approved, implemented, validated) and **Post-Pilot
> Scope** (intentionally postponed by owner decision). The original scope list
> is preserved in the appendix; nothing was removed from the product vision,
> and no history is rewritten.

## 1 · Pilot MVP Scope — approved, implemented, and validated for the Pilot

Delivered end-to-end, gate-verified, and operationally validated by the Pilot
Readiness phase (WS1–WS6, owner Go decision 2026-07-31 —
[`docs/operations/go-no-go.md`](../../../operations/go-no-go.md)); code state
tagged **`pilot-2026-07-31`**.

| Capability | What the Pilot runs |
|---|---|
| **Catalog (incl. Pricing)** | قائمة المنتجات · إنشاء/تفاصيل/تحرير المنتج · سلسلة الوحدات ووحدة المخزون الصغرى (BR-CAT-020) · سعر البيع على وحدة البيع (لقطة سعر — DEC-SAL-003) · الشركات المصنّعة |
| **Categories** | إدارة التصنيفات (بيانات مُدارة) |
| **Purchasing** | قائمة فواتير الشراء · التفاصيل · الإنشاء · البنود · **الاستلام بدُفعات وصلاحيات** · **مرتجع الشراء** (`PRT-`) |
| **Sales** | فاتورة البيع (مسودة → مثبَّتة) · التفاصيل · **قائمة فواتير البيع** (DEC-SAL-005، حسم 2026-07-31) · **مرتجع البيع** (`SRT-`) بلا أثر ماليّ (DEC-INV-035) |
| **Inventory** | نواة الكتابة · إسقاط المخزون · **عارض الدفعات** · **مراقبة الصلاحية** · **سجلّ الحركة وتاريخه** · استهلاك FEFO بتتبّع على مستوى بند البيع · **التسويات** · **الإهلاك** |
| **Operational backbone** (ليست وحدة، لكنها شرط التشغيل) | النشر بأمر واحد · نسخ احتياطي واستعادة مُتحقَّق منهما بسكربت (حسم PRS-Q-03) · بوّابة `STD-BE-051` |

**Two scope resolutions recorded where they were ruled, echoed here so this
list reads correctly:** «Batch» is **not a separate module** — the batch is
Inventory-owned and the Batch Viewer is an Inventory read screen
(DEC-INV-008); and the delivered expiry monitoring is an **Inventory read
screen**, while the Monitoring *module* (alerts/notifications/jobs) remains
post-Pilot (DEC-INV-017).

**Definition of done for this section — met:** every capability above passed
its approved acceptance criteria, the full gate sweep, and the Pilot Readiness
verification (deployment · backup/restore · clean database · smoke ·
UAT-ready), closed by the owner's **GO**.

## 2 · Post-Pilot Scope — planned, intentionally postponed by owner decision

**These were postponed deliberately — by recorded owner rulings — not
omitted.** They remain part of the MVP product vision and return to the table
after the Pilot, on the owner's direction (`BD-PRD-008`: no new capability
during the Pilot unless required to keep the system operational).

| Deferred capability | Deferral basis |
|---|---|
| **Cash Management** | Not started — postponed by the pre-Pilot scope freeze (`BD-PRD-008`, owner 2026-07-31) |
| **Expenses** | Same |
| **Reports** | Same |
| **Audit Log** | Same |
| **Settings** | Same |
| **Local Backup (in-app)** | **Explicit owner ruling (PRS-Q-03):** the verified script-based procedure suffices for the Pilot; the in-app module is not required for it |
| **Suppliers (module)** | Postponed; the Pilot records the supplier as free text on the purchase invoice (BR-PUR-001) |
| **Customers (minimal)** | Postponed; the Pilot records the customer as optional free text (DEC-SAL-002) |
| **Monitoring (alerts/notifications/jobs)** | DEC-INV-017 — deferred as a module; the read screen shipped in Inventory |

Finer-grained in-module deferrals (price override & discounts — DEC-SAL-003 ·
open package — DEC-SAL-008 · reservations — DEC-SAL-001 · «ملغاة» sales
status — DEC-SAL-009 open · per-product reorder level — DEC-INV-004 · …) live
in each module's `decisions.md`, which stays their single home.

Definition of done for post-Pilot capabilities: defined per capability with
the owner when scheduled — nothing is pre-committed here.

## 3 · Explicitly out of scope — the long-term product vision (unchanged)

All future modules listed in `docs/shared/roadmap/ROADMAP.md` (medical
services, animal records, appointments, vaccinations, prescriptions,
laboratory, imaging, multi-branch, cloud synchronization). Future modules must
remain additive.

---

## Appendix — the original scope list (superseded in structure, preserved verbatim)

> The pre-amendment document read — Status: *"Placeholder — details pending
> owner documentation"* — In scope: *"Catalog (incl. Pricing) · Categories ·
> Suppliers · Customers (minimal) · Purchasing · Sales · Inventory · Batch ·
> Monitoring · Cash Management · Expenses · Reports · Audit Log · Local
> Backup · Settings"* — Definition of done: *"To be defined with the owner
> before implementation begins."*
>
> Every item is accounted for above: delivered in §1, deliberately postponed
> in §2 with its ruling, or resolved into another module's ownership (Batch ·
> the Monitoring read screen). **Nothing was dropped.**
