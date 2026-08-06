# Glossary — English ↔ العربية

> Status: Draft — seeded from terms already used in approved docs
> (`events.md`, `VISION.md`, module names); Arabic forms pending owner
> approval.
> Single source of truth for canonical domain vocabulary. Every domain term
> used in documentation must have a row here, with its approved Arabic form.
> The English form is what appears in code and engineering docs (per ADR-0002).

## Actors — الأطراف

| English | العربية | Definition |
|---|---|---|
| Veterinary clinic | العيادة البيطرية | The business VetFlow manages. **Amended 2026-08-02 (ADR-0022):** the platform hosts many clinics, each a **Tenant** with one or more **Branches**; the Pilot runs one tenant with one branch, and branch *management* remains post-MVP. *(Superseded wording, kept for history: "a single clinic (multi-branch is post-MVP)".)* |
| Owner (Veterinary Doctor) | الطبيب البيطري (المالك) | Owner of the clinic and of all business decisions. |
| Cashier / Assistant | الكاشير / المساعد | Staff member handling day-to-day sales and recording. |
| Customer | عميل | A person the clinic sells to. MVP keeps customer data minimal. |
| Supplier | مورد | A party the clinic purchases from. |

## Catalog & stock — الكتالوج والمخزون

| English | العربية | Definition |
|---|---|---|
| Product | منتج | An item the clinic buys and sells. MVP sells products only — no medical services (DEC-CAT-002). |
| Internal code | الكود الداخلي | System-generated `PRD-` + ascending zero-padded sequence (`PRD-000001`); a reporting/audit/support reference, never a search key (BR-CAT-006, DEC-CAT-016, DEC-CAT-026). |
| Possible duplicate | تكرار محتمل | A warning at create time when the Arabic name is similar and the manufacturer matches; never blocks — the user decides (BR-CAT-042, DEC-CAT-018, DEC-CAT-027). |
| Catalog | الكتالوج | The clinic's product catalog; includes pricing as a capability. |
| Category | تصنيف | A grouping of products; flat single-level list in the first version (BR-CAT-013). |
| Manufacturer | شركة مصنعة | Managed lookup, name only in the first version; mandatory for every product (BR-CAT-007). |
| Product nature | طبيعة المنتج | Mandatory extensible property that drives system behavior, independent of category (BR-CAT-014). |
| Unit | وحدة | A user-managed measurement/packaging unit; a default common set ships with the system (BR-CAT-017). |
| Unit profile | ملف الوحدات | A product's single source of truth for purchase/sale/stock calculations (BR-CAT-016). |
| Stock-keeping unit | وحدة المخزون | The one user-chosen unit all quantity calculations are based on (BR-CAT-020). |
| Conversion factor | معامل التحويل | User-entered fixed factor between units; never derived by the system (BR-CAT-018). |
| Barcode | باركود | One optional barcode per unit — a unit-level property, not a product-level one (BR-CAT-024). |
| Selling price | سعر البيع | Independent manual price per sale unit (BR-CAT-025). |
| Splittable | قابل للتجزئة | Product-level capability: sellable in units smaller than the purchase unit (BR-CAT-031). |
| Refrigerated product | منتج ثلاجة | Display/filter/report flag only (BR-CAT-045). |
| Pricing | التسعير | Setting and changing product prices (Catalog capability, not a module). |
| Inventory / Stock | المخزون | Quantities of products held by the clinic. |
| Stock adjustment | تعديل المخزون | A recorded correction/change of stock. Exact rules TODO (Inventory module docs). |
| Batch | دفعة | A received lot of a product (`InventoryBatch`), with its own quantity, unit cost snapshot, and optional expiry; an Inventory-owned domain entity created on receiving (BR-INV-001, DEC-INV-008). |
| Expiry date | تاريخ الصلاحية | The date after which a batch must not be sold. |
| Monitoring | المراقبة | Watching stock levels and expiry dates and raising alerts. |
| Low-stock threshold | الحد الأدنى للمخزون | Level below which an alert is raised. Exact behavior TODO (Monitoring module docs). |
| Alert | تنبيه | A warning raised by Monitoring before a loss occurs. |
| On-hand quantity | الرصيد المتاح | The committed current quantity of a product held in inventory, in the product's stock-keeping unit (`ProductOnHand`; BR-INV-002, BR-INV-008). |
| Inventory projection | إسقاط المخزون | Read-only, on-the-fly view of committed inventory; owns no state and is rebuildable from `InventoryBatch`/`ProductOnHand` (BR-INV-006). |
| Batch count | عدد الدفعات | Number of a product's active inventory batches (`RemainingQuantity > 0`) (BR-INV-009). |
| Nearest expiry | أقرب صلاحية | The earliest expiry date among a product's active batches (BR-INV-010). |
| Expiring soon | قرب انتهاء الصلاحية | A product whose nearest expiry falls within 30 calendar days (BR-INV-013, DEC-INV-005). |
| Out of stock | نفاد المخزون | On-hand quantity equals zero (BR-INV-011). |
| Batch viewer | عارض الدفعات | Read-only per-product screen listing all of a product's inventory batches; an Inventory read screen, not a separate module (REQ-INV-003, DEC-INV-008). |
| Batch identifier | معرّف الدفعة | A batch's existing stable identity, shown read-only; no human Batch Code, generated number, or new field (BR-INV-025, DEC-INV-009). |
| Batch status | حالة الدفعة | Derived Active (`RemainingQuantity > 0`) / Depleted (`== 0`) only, never stored; Expired is never a status (BR-INV-021, DEC-INV-011). |
| Depleted batch | دفعة مستنفدة | An inventory batch whose `RemainingQuantity == 0` (BR-INV-021). |
| Expired batch | دفعة منتهية الصلاحية | A batch whose `ExpiryDate` is before today — a filter, never a status; a batch can be Active and Expired at once (BR-INV-026, DEC-INV-012). |
| Purchase reference | المرجع الشرائي | The owning purchase-invoice number of a batch's source line; a navigation link to Purchase Invoice Details (BR-INV-024, DEC-INV-010). |
| Reorder level | حد إعادة الطلب | Future per-product Catalog capability that will define Low Stock; not yet in the model (DEC-INV-004, BR-INV-012). |

## Trading — الحركة التجارية

| English | العربية | Definition |
|---|---|---|
| Sale | عملية بيع | Selling to a customer. |
| Sale refund | استرجاع بيع | Reversing a sale. Exact rules TODO (Sales module docs). |
| Purchase | عملية شراء | Buying from a supplier. |
| Purchase invoice | فاتورة شراء | The document recording a purchase from a supplier — the header identity that goods enter inventory by (BR-PUR-001). Its system number is its stable identity (BR-PUR-002). |
| Purchase invoice number | رقم فاتورة الشراء | System-generated internal number, `PUR-000001` format, immutable and never reused (BR-PUR-002). |
| Purchase return | إرجاع مشتريات | Returning purchased goods to a supplier. Exact rules TODO (Purchasing module docs). |

## Money & records — النقدية والسجلات

| English | العربية | Definition |
|---|---|---|
| Cash management | إدارة النقدية | Managing the clinic's daily cash. |
| Cash session | جلسة نقدية | A daily open→close cash cycle. Exact definition TODO (Cash module docs). |
| Expense | مصروف | Money spent by the clinic outside purchasing. Exact boundary TODO (Expenses module docs). |
| Report | تقرير | A read-only view of recorded activity. |
| Audit log | سجل التدقيق | The record answering: who did what, and when. |
| Local backup | النسخ الاحتياطي المحلي | A safety copy of the clinic's data, kept locally. |
| Settings | الإعدادات | Clinic-level configuration. Scope TODO (Settings module docs). |
| Business event | حدث عمل | A named business occurrence crossing module boundaries (see `events.md`). |

## Organization & identity — التنظيم والهويّة

> Added 2026-08-02 with ADR-0022. Arabic forms pending owner approval.

| English | العربية | Definition |
|---|---|---|
| Tenant | المنشأة | The commercial customer: one veterinary business. The boundary of data ownership, security and subscription. Every business row belongs to exactly one (BR-ORG-001). |
| Branch | الفرع | A physical site belonging to one tenant. The scope of business documents and their numbering (BR-ORG-002). A future warehouse is modelled as a branch, not as a level below it (ADR-0022 §11.1). |
| Membership | العضوية | The link between a user and a tenant, carrying the user's role. Access is derived from it, never from a field on the user (BR-ORG-005). |
| User | مستخدم | A person who signs in and to whom every operation is attributed. Identified by phone number, unique platform-wide (BR-IDN-001). |
| Role | الدور | What a membership permits: **Owner (مالك)** or **Cashier (كاشير)** — closed set, per BD-PRD-003 (BR-ORG-006). |
| Sign in | تسجيل الدخول | Phone number + password only. No email, username, OTP or MFA (DEC-IDN-001). |
| Access token | رمز الوصول | The JWT issued on successful sign-in, carrying user, tenant, branch and role. No refresh token exists (DEC-IDN-003). |
| Tenant context | سياق المنشأة | The current tenant, branch and user, resolved **only** from authenticated claims — never from configuration, header, route or body (BR-IDN-004). |
| Document number counter | عدّاد أرقام المستندات | The per-`(tenant, branch, series)` counter that replaces the global database sequences; allocated inside the document's transaction, so numbering is gapless (ADR-0022 §6). |

## Dashboard — لوحة التشغيل

> Added 2026-08-02 with the Dashboard commission. **Arabic forms APPROVED by
> the owner 2026-08-03 (OQ-DSH-3) — «لوحة التشغيل» is the ruled name.**

| English | العربية | Definition |
|---|---|---|
| Operational Dashboard | لوحة التشغيل | The first screen after sign-in. Answers one question — «what needs my attention right now?» — and **every element on it navigates to work** (BR-DSH-002). **Deliberately not** a reporting, analytics or executive dashboard. **Owner-ruled term (2026-08-03): «لوحة التشغيل», not «لوحة المعلومات»** — the latter names the very thing the commission excludes. `design-language.md` §17 uses «لوحة المعلومات» to describe a **kind of screen**, not this module. |
| Clinic local date | تاريخ العيادة المحلّي | The one reference date for every business date decision, resolved from the **tenant's** time zone. UTC, server time and device time are prohibited sources. Home: `docs/architecture/cross-cutting/clinic-date.md` (owner ruling OQ-DSH-2, 2026-08-03); originally BR-INV-059/060, whose identifiers are preserved. |
| Attention item | بند انتباه | One counted condition on the dashboard that is worth acting on this morning, shown with its count and a link to the destination **filtered on that same condition** (BR-DSH-018). |
| All clear | حالة الاطمئنان | What the dashboard shows when every attention item is zero: **one explicit reassurance, not five zero tiles and not an empty state** — because zero here is good news, not absent data (BR-DSH-013). |

## TODO — أسئلة للمالك

1. اعتماد الصيغ العربية أعلاه (خاصة: منتج/صنف، الكاشير/أمين الصندوق، دفعة/تشغيلة).
2. هل يُعتمد مصطلح واحد للعميل (عميل) أم يُستخدم «صاحب الحيوان» في سياقات لاحقة؟
