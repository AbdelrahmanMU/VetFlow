# VetFlow Roadmap

> Direction and sequence only. Scope contracts live in `releases/`.
> Status: Placeholder — sequencing beyond the MVP is the owner's decision.

## Now — MVP

See [`releases/mvp.md`](releases/mvp.md).

### Pre-Pilot direction — owner ruling (2026-07-31)

**بعد اكتمال Epic 2 «عمليات المخزون»، الاتجاه هو التحضير لجاهزية التجربة الأولى
(Pilot Readiness) لا توسيع النطاق.**

**لا تُضاف أيّ قدرة جديدة قبل التجربة الأولى إلا إذا كانت لازمة للتشغيل الناجح.**

- **القرار للمالك وحده:** «لازمة للتشغيل الناجح» معيارٌ يحكم به المالك، لا يُستنتج.
  عند الشكّ في قدرة مقترحة: تُعرَض على المالك ولا تُنفَّذ.
- **النطاق المجمَّد هو نطاق النسخة الأولى** المسجَّل في
  [`releases/mvp.md`](releases/mvp.md) — هذا القرار يقيّد **الإضافة**، ولا يوسّع
  النطاق ولا يقلّصه.
- **ما هي التجربة الأولى ومتى تبدأ:** معرَّفان في
  [`ADR-0020`](../../architecture/decisions/ADR-0020-schema-evolution-safety.md)
  (§When the Pilot begins · §Pilot Transition Checklist) — لا يُعاد تعريفهما هنا.
- **لا يشمل هذا القرار** إصلاح العيوب، ولا سداد الدين التقني المسجَّل، ولا إكمال
  Epic 2 نفسه (C5/C6) — فتلك أعمال قائمة لا قدرات جديدة.

**نطاق أعمال جاهزية التجربة الأولى — توجيه المالك (2026-07-31، مع اعتماد Epic 2):**
اكتمل Epic 2 «عمليات المخزون» واعتُمد. **الهدف التالي ليس Epic تنفيذٍ آخر** بل تجهيز
المشروع تشغيليًا. تتركّز أعمال Pilot Readiness على:

1. التحقّق من صحّة النشر (Deployment validation).
2. التحقّق الفعلي من النسخ الاحتياطي والاستعادة (Backup & Restore verification).
3. تهيئة قاعدة بيانات نظيفة للتشغيل (Clean database setup).
4. اختبار الدخان (Smoke testing).
5. التحضير لاختبار قبول المستخدم (UAT preparation).
6. تقييم نهائي **Go/No-Go**.

**الغاية جاهزية تشغيلية، لا وظائف إضافية** — ولا يُوسَّع التنفيذ بعد Epic 2
(تأكيدٌ لقاعدة هذا القسم أعلاه، لا قاعدة جديدة).

**الفهرسة:** `BD-PRD-008` في [`docs/business/DECISION_LOG.md`](../../business/DECISION_LOG.md).

## Later (order not decided)

Medical Services · Animal Records · Appointments · Vaccinations ·
Prescriptions · Laboratory · Imaging · Multi-Branch · Cloud Synchronization
