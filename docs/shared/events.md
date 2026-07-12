# Business Events — أحداث العمل

> Status: Draft — initial list, names only (per owner instruction, 2026-07-12).
> Descriptions, source/consuming modules, and payloads are added during each
> module's documentation phase. Do not implement from this file.

القائمة الأولية لأحداث العمل — أسماء فقط، مشتقة من نطاق النسخة الأولى (MVP). تُراجَع وتُستكمل مع المالك أثناء توثيق كل وحدة.

## القائمة الأولية — Initial list

| ID | الاسم بالعربية | Module |
|---|---|---|
| `ProductAdded` | تمت إضافة منتج | Catalog |
| `ProductUpdated` | تم تعديل منتج | Catalog |
| `ProductPriceChanged` | تم تغيير سعر منتج | Catalog (Pricing) |
| `PurchaseRecorded` | تم تسجيل عملية شراء | Purchasing |
| `PurchaseReturned` | تم إرجاع مشتريات | Purchasing |
| `SaleRecorded` | تم تسجيل عملية بيع | Sales |
| `SaleRefunded` | تم استرجاع عملية بيع | Sales |
| `StockAdjusted` | تم تعديل المخزون | Inventory |
| `BatchReceived` | تم استلام دفعة | Batch |
| `BatchExpired` | انتهت صلاحية دفعة | Batch |
| `LowStockDetected` | انخفض المخزون تحت الحد الأدنى | Monitoring |
| `ExpiryApproaching` | اقترب انتهاء الصلاحية | Monitoring |
| `CashSessionOpened` | تم فتح جلسة نقدية | Cash Management |
| `CashSessionClosed` | تم إغلاق جلسة نقدية | Cash Management |
| `ExpenseRecorded` | تم تسجيل مصروف | Expenses |
| `CustomerRegistered` | تم تسجيل عميل | Customers |
| `SupplierRegistered` | تم تسجيل مورد | Suppliers |
| `BackupSucceeded` | نجح النسخ الاحتياطي | Local Backup |
| `BackupFailed` | فشل النسخ الاحتياطي | Local Backup |
