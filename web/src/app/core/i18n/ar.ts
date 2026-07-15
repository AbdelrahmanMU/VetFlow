/**
 * Arabic copy — the MVP's single localization resource (ADR-0007,
 * STD-FE-040). No user-facing string lives outside a resource file.
 * The canonical vocabulary follows docs/shared/GLOSSARY.md and the module's
 * approved microcopy (catalog/ui.md §13).
 */
export const AR = {
  'app.name': 'VetFlow',
  'nav.catalog': 'الكتالوج',
  'nav.products': 'المنتجات',

  'products.title': 'المنتجات',
  'products.search.placeholder': 'ابحث بالاسم أو الباركود…',
  'products.search.clear': 'مسح البحث',
  'products.filters.open': 'الفلاتر',
  'products.filters.title': 'فلاتر القائمة',
  'products.filters.clearAll': 'مسح الكل',
  'products.filters.apply': 'تطبيق',
  'products.columns.open': 'الأعمدة',
  'products.columns.title': 'أعمدة الجدول',

  'products.filter.category': 'التصنيف',
  'products.filter.manufacturer': 'الشركة المصنعة',
  'products.filter.nature': 'طبيعة المنتج',
  'products.filter.status': 'الحالة',
  'products.filter.refrigerated': 'منتج ثلاجة',
  'products.filter.hasExpiration': 'له صلاحية',
  'products.filter.splittable': 'قابل للتجزئة',
  'products.filter.hasSellingPrice': 'له سعر بيع',
  'products.filter.any': 'الكل',
  'products.filter.yes': 'نعم',
  'products.filter.no': 'لا',
  'products.filter.active': 'نشط',
  'products.filter.disabled': 'معطَّل',

  'products.column.name': 'المنتج',
  'products.column.category': 'التصنيف',
  'products.column.manufacturer': 'الشركة المصنعة',
  'products.column.nature': 'الطبيعة',
  'products.column.storageUnit': 'وحدة المخزون',
  'products.column.price': 'السعر',
  'products.column.capabilities': 'الخصائص',
  'products.column.status': 'الحالة',

  'products.status.active': 'نشط',
  'products.status.disabled': 'معطَّل',
  'products.status.noPrice': 'بلا سعر',

  'products.capability.splittable': 'قابل للتجزئة',
  'products.capability.refrigerated': 'منتج ثلاجة',
  'products.capability.hasExpiration': 'له صلاحية',

  'products.price.perUnit': 'لكل {unit}',

  'pagination.range': '{from}–{to} من {total}',
  'pagination.zero': 'لا نتائج',
  'pagination.previous': 'السابق',
  'pagination.next': 'التالي',
  'pagination.label': 'التنقل بين الصفحات',

  'products.empty.new.title': 'لم تُضف أي منتجات بعد.',
  'products.empty.new.body': 'ستظهر منتجات العيادة هنا فور إضافتها إلى الكتالوج.',
  'products.empty.search.title': 'لا يوجد منتج مطابق لـ «{query}».',
  'products.empty.search.body': 'جرّب اسمًا آخر أو جزءًا من الاسم، أو امسح البحث.',
  'products.empty.filters.title': 'لا نتائج مطابقة للفلاتر المطبَّقة.',
  'products.empty.filters.body': 'وسّع الفلاتر أو امسحها لعرض المنتجات.',
  'products.error.title': 'تعذّر تحميل المنتجات.',
  'products.error.body': 'تحقق من الاتصال ثم أعد المحاولة.',
  'products.error.retry': 'إعادة المحاولة',
  'products.loading': 'جارٍ تحميل المنتجات…',

  'products.table.label': 'قائمة المنتجات',
  'products.table.sortBy': 'ترتيب حسب {column}',
} as const;

export type MessageKey = keyof typeof AR;
