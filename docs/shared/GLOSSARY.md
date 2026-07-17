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
| Veterinary clinic | العيادة البيطرية | The business VetFlow manages: a single clinic (multi-branch is post-MVP). |
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
| Batch | دفعة | A received lot of a product with its own expiry date. Exact definition TODO (Batch module docs). |
| Expiry date | تاريخ الصلاحية | The date after which a batch must not be sold. |
| Monitoring | المراقبة | Watching stock levels and expiry dates and raising alerts. |
| Low-stock threshold | الحد الأدنى للمخزون | Level below which an alert is raised. Exact behavior TODO (Monitoring module docs). |
| Alert | تنبيه | A warning raised by Monitoring before a loss occurs. |

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

## TODO — أسئلة للمالك

1. اعتماد الصيغ العربية أعلاه (خاصة: منتج/صنف، الكاشير/أمين الصندوق، دفعة/تشغيلة).
2. هل يُعتمد مصطلح واحد للعميل (عميل) أم يُستخدم «صاحب الحيوان» في سياقات لاحقة؟
