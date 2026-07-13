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
| Product | منتج | An item the clinic buys and sells. TODO: does MVP also sell services? (see domain-overview TODO). |
| Catalog | الكتالوج | The clinic's product catalog; includes pricing as a capability. |
| Category | تصنيف | A grouping of products. |
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
