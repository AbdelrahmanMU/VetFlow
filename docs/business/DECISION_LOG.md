# Business Decision Log — سجل القرارات العام

> Status: Draft — Business Decision Sprint. Nothing here is auto-approved.
>
> This edition is an **extraction**: it registers only business decisions that
> already exist in project documentation — nothing invented, nothing assumed.
> Each entry cites its source document(s); the source remains the single place
> the decision *lives* (per ADR-0001), this log indexes it. Entries whose
> source is itself `Draft` are marked so and become final only when the owner
> approves that source. New global (cross-module) decisions are appended to
> the matching section.
>
> Conventions (per ADR-0002): scaffolding — IDs, field labels, statuses,
> paths — in English; decision text in Arabic. ID format: `BD-<SEC>-NNN`,
> stable, never renumbered.

---

## Product

### BD-PRD-001 — Single-clinic product
- **Status:** Draft
- **Decision:** VetFlow منصة لإدارة عيادة بيطرية واحدة وفريق من شخصين؛ تعدد الفروع قدرة مستقبلية بعد النسخة الأولى.
- **Reason:** نطاق المنتج المسجل في السياق وخارطة الطريق.
- **Source(s):** `docs/PROJECT_CONTEXT.md` (Draft), `docs/shared/roadmap/ROADMAP.md`
- **Related Module:** Product-wide

### BD-PRD-002 — Arabic-first UI (RTL)
- **Status:** Draft
- **Decision:** واجهة المنتج عربية أولًا وبالاتجاه من اليمين إلى اليسار، بلغة فريق العمل لا ترجمةً لاحقة.
- **Reason:** مبدأ «العربية أولًا» — المستخدمون عرب واللغة جزء من المشكلة التي يحلها المنتج.
- **Source(s):** `CLAUDE.md`, `docs/PROJECT_CONTEXT.md` (Draft), `docs/shared/VISION.md` (Draft)
- **Related Module:** Product-wide

### BD-PRD-003 — Two user roles only (MVP)
- **Status:** Draft
- **Decision:** مستخدما النظام اثنان فقط: الطبيب البيطري (المالك) والكاشير/المساعد.
- **Reason:** حجم فريق العيادة الفعلي كما هو مسجل.
- **Source(s):** `docs/PROJECT_CONTEXT.md` (Draft), `docs/shared/personas.md` (Draft)
- **Related Module:** Product-wide

### BD-PRD-004 — MVP scope is fixed and commercial-only
- **Status:** Draft
- **Decision:** نطاق النسخة الأولى ثابت (١٥ وحدة) ويغطي الدورة التجارية للعيادة فقط؛ الجانب الطبي مؤجَّل عمدًا إلى ما بعدها.
- **Reason:** عقد نطاق مسجل في خارطة الطريق؛ تغييره قرار مالك.
- **Source(s):** `docs/shared/roadmap/releases/mvp.md`, `docs/PROJECT_CONTEXT.md` (Draft)
- **Related Module:** Product-wide

### BD-PRD-005 — Future capabilities are additive
- **Status:** Draft
- **Decision:** الوحدات المستقبلية (خدمات طبية، مواعيد، فروع، مزامنة، …) تُضاف فوق النظام دون إعادة هيكلة ودون كسر ما اعتاد عليه المستخدم.
- **Reason:** مبدأ «النمو بالإضافة» وشرط معماري مسجل في ADR-0001.
- **Source(s):** `docs/architecture/decisions/ADR-0001-repository-structure.md`, `docs/PROJECT_CONTEXT.md` (Draft), `docs/shared/VISION.md` (Draft)
- **Related Module:** Product-wide

### BD-PRD-006 — MVP completion yardstick
- **Status:** Draft
- **Decision:** مقياس اكتمال النسخة الأولى: يوم عمل كامل للعيادة — من فتح الجلسة النقدية إلى إقفالها — يُدار داخل النظام دون سجلات ورقية موازية.
- **Reason:** معيار النجاح المصاغ في وثيقة الرؤية.
- **Source(s):** `docs/shared/VISION.md` (Draft)
- **Related Module:** Product-wide

### BD-PRD-007 — Initial business-event list exists (names only)
- **Status:** Draft
- **Decision:** قائمة أولية من ١٩ حدث عمل معرَّفة بالأسماء فقط، وتُستكمل (وصف، مصدر، مستهلكون، بيانات) أثناء توثيق كل وحدة.
- **Reason:** تعليمات المالك عند تأسيس المستودع (أسماء فقط، بلا افتراضات).
- **Source(s):** `docs/shared/events.md` (Draft)
- **Related Module:** Cross-module

## Catalog

### BD-CAT-001 — Products module renamed to Catalog
- **Status:** Draft
- **Decision:** وحدة «المنتجات» اسمها المعتمد «الكتالوج» (`catalog/`).
- **Reason:** مراجعة المالك لهيكل المستودع قبل الالتزام الأول.
- **Source(s):** `docs/architecture/decisions/ADR-0001-repository-structure.md`, `docs/modules/_INDEX.md`
- **Related Module:** `catalog`

### BD-CAT-002 — Pricing is a Catalog capability, not a module
- **Status:** Draft
- **Decision:** التسعير قدرة ضمن وحدة الكتالوج وليست وحدة مستقلة (أُزيلت وحدة Pricing المستقلة).
- **Reason:** التسعير ليس سير عمل تجاري مستقلًا بذاته (قرار مالك مسجَّل).
- **Source(s):** `docs/modules/catalog/decisions.md`, `docs/architecture/decisions/ADR-0001-repository-structure.md`
- **Related Module:** `catalog`

### BD-CAT-003 — Categories is its own MVP module
- **Status:** Draft
- **Decision:** تصنيف المنتجات وحدة مستقلة ضمن نطاق النسخة الأولى.
- **Reason:** واردة كوحدة مستقلة في عقد النطاق وفهرس الوحدات.
- **Source(s):** `docs/shared/roadmap/releases/mvp.md`, `docs/modules/_INDEX.md`
- **Related Module:** `categories`

### BD-CAT-004 — Initial Catalog events
- **Status:** Draft
- **Decision:** أحداث الكتالوج الأولية: `ProductAdded`، `ProductUpdated`، `ProductPriceChanged` (أسماء فقط).
- **Reason:** القائمة الأولية لأحداث العمل.
- **Source(s):** `docs/shared/events.md` (Draft)
- **Related Module:** `catalog`

## Purchasing

### BD-PUR-001 — Purchasing from tracked suppliers
- **Status:** Draft
- **Decision:** الشراء يتم من موردين مسجلين؛ «المشتريات» و«الموردون» وحدتان مستقلتان ضمن النسخة الأولى.
- **Reason:** عقد نطاق النسخة الأولى وفهرس الوحدات.
- **Source(s):** `docs/shared/roadmap/releases/mvp.md`, `docs/modules/_INDEX.md`
- **Related Module:** `purchasing`, `suppliers`

### BD-PUR-002 — Purchase returns exist
- **Status:** Draft
- **Decision:** إرجاع المشتريات إلى المورد حدث عمل معترف به (`PurchaseReturned`)؛ قواعده تُوثَّق لاحقًا.
- **Reason:** وارد في القائمة الأولية لأحداث العمل.
- **Source(s):** `docs/shared/events.md` (Draft)
- **Related Module:** `purchasing`

## Sales

### BD-SAL-001 — Sales to customers from clinic stock
- **Status:** Draft
- **Decision:** البيع للعملاء من مخزون العيادة؛ «المبيعات» وحدة ضمن النسخة الأولى.
- **Reason:** عقد نطاق النسخة الأولى ودورة العمل اليومية الموثقة.
- **Source(s):** `docs/shared/roadmap/releases/mvp.md`, `docs/business/domain-overview.md` (Draft)
- **Related Module:** `sales`

### BD-SAL-002 — Sale refunds exist
- **Status:** Draft
- **Decision:** استرجاع البيع حدث عمل معترف به (`SaleRefunded`)؛ قواعده تُوثَّق لاحقًا.
- **Reason:** وارد في القائمة الأولية لأحداث العمل.
- **Source(s):** `docs/shared/events.md` (Draft)
- **Related Module:** `sales`

### BD-SAL-003 — Customer data is minimal in MVP
- **Status:** Draft
- **Decision:** بيانات العملاء في النسخة الأولى بحد أدنى («Customers (minimal)»)؛ ملفات أصحاب الحيوانات الكاملة خارج النطاق.
- **Reason:** نطاق مسجل في السياق وعقد النسخة الأولى.
- **Source(s):** `docs/PROJECT_CONTEXT.md` (Draft), `docs/shared/roadmap/releases/mvp.md`
- **Related Module:** `customers`

## Inventory

### BD-INV-001 — Inventory domain split into three modules
- **Status:** Draft
- **Decision:** مجال المخزون ثلاث وحدات: المخزون (`inventory/`)، الدفعات (`batch/`)، المراقبة (`monitoring/`)؛ الحدود الدقيقة بينها تُحسم أثناء التوثيق.
- **Reason:** مراجعة المالك لهيكل المستودع.
- **Source(s):** `docs/architecture/decisions/ADR-0001-repository-structure.md`, `docs/modules/inventory/overview.md`
- **Related Module:** `inventory`, `batch`, `monitoring`

### BD-INV-002 — Single source of truth for stock
- **Status:** Draft
- **Decision:** المخزون لا يُمسَك في مكانين؛ ما يعرضه النظام هو الحقيقة لأن كل حركة مسجلة.
- **Reason:** مبدأ «مصدر واحد للحقيقة» و«الثقة في الأرقام» في الرؤية.
- **Source(s):** `docs/shared/VISION.md` (Draft)
- **Related Module:** `inventory`

### BD-INV-003 — Stock adjustments are recorded operations
- **Status:** Draft
- **Decision:** تعديل المخزون عملية مسجلة (`StockAdjusted`)؛ قواعده الدقيقة تُوثَّق مع الوحدة.
- **Reason:** وارد في قائمة الأحداث والمسرد.
- **Source(s):** `docs/shared/events.md` (Draft), `docs/shared/GLOSSARY.md` (Draft)
- **Related Module:** `inventory`

## Batch

### BD-BAT-001 — Stock is held in expiry-dated batches
- **Status:** Draft
- **Decision:** المخزون بدفعات، ولكل دفعة تاريخ صلاحية خاص بها — هذه خصوصية المجال الأساسية.
- **Reason:** حقيقة مجال مسجلة (دواء بيطري ومستلزمات).
- **Source(s):** `docs/business/domain-overview.md` (Draft), `docs/shared/GLOSSARY.md` (Draft)
- **Related Module:** `batch`

### BD-BAT-002 — Batch receipt and batch expiry are business events
- **Status:** Draft
- **Decision:** استلام الدفعة وانتهاء صلاحيتها حدثا عمل معترف بهما (`BatchReceived`، `BatchExpired`).
- **Reason:** القائمة الأولية لأحداث العمل.
- **Source(s):** `docs/shared/events.md` (Draft)
- **Related Module:** `batch`

### BD-BAT-003 — An expired batch must not be sold
- **Status:** Draft
- **Decision:** تاريخ الصلاحية هو التاريخ الذي لا يجوز بعده بيع الدفعة.
- **Reason:** تعريف المسرد؛ يحتاج تثبيتًا صريحًا من المالك عند توثيق وحدة الدفعات.
- **Source(s):** `docs/shared/GLOSSARY.md` (Draft)
- **Related Module:** `batch`, `sales`

## Monitoring

### BD-MON-001 — Alert before loss, not after
- **Status:** Draft
- **Decision:** المراقبة تنبّه قبل وقوع الضرر لا بعده — النظام يراقب الحدود الدنيا وتواريخ الصلاحية.
- **Reason:** مبدأ «التنبيه قبل الخسارة» في الرؤية.
- **Source(s):** `docs/shared/VISION.md` (Draft), `docs/shared/GLOSSARY.md` (Draft)
- **Related Module:** `monitoring`

### BD-MON-002 — Two initial monitored conditions
- **Status:** Draft
- **Decision:** الحالتان المراقَبتان مبدئيًا: انخفاض المخزون تحت الحد الأدنى (`LowStockDetected`) واقتراب انتهاء الصلاحية (`ExpiryApproaching`)؛ سلوك الحدود يُوثَّق مع الوحدة.
- **Reason:** القائمة الأولية لأحداث العمل.
- **Source(s):** `docs/shared/events.md` (Draft)
- **Related Module:** `monitoring`

## Cash

### BD-CSH-001 — Cash is managed in daily sessions
- **Status:** Draft
- **Decision:** النقدية تُدار بجلسات يومية: فتح في بداية اليوم (`CashSessionOpened`) وإقفال في نهايته (`CashSessionClosed`)؛ التعريف الدقيق للجلسة يُوثَّق مع الوحدة.
- **Reason:** دورة العمل اليومية الموثقة وقائمة الأحداث.
- **Source(s):** `docs/shared/events.md` (Draft), `docs/business/domain-overview.md` (Draft), `docs/shared/GLOSSARY.md` (Draft)
- **Related Module:** `cash`

### BD-CSH-002 — Daily reconciliation; differences need a recorded explanation
- **Status:** Draft
- **Decision:** الإقفال اليومي بمطابقة؛ أي فرق نقدي يجب أن يكون له تفسير مسجَّل.
- **Reason:** معيار نجاح مصاغ في الرؤية.
- **Source(s):** `docs/shared/VISION.md` (Draft)
- **Related Module:** `cash`

### BD-CSH-003 — Expenses are separate from purchasing
- **Status:** Draft
- **Decision:** المصروفات وحدة مستقلة عن المشتريات؛ المصروف إنفاق خارج الشراء من الموردين، وحدُّه الدقيق يُوثَّق مع الوحدة.
- **Reason:** وحدتان منفصلتان في عقد النطاق؛ تعريف المسرد.
- **Source(s):** `docs/shared/roadmap/releases/mvp.md`, `docs/shared/GLOSSARY.md` (Draft)
- **Related Module:** `expenses`, `cash`

## Reporting

### BD-REP-001 — Reports give the owner the aggregate picture
- **Status:** Draft
- **Decision:** التقارير تجمع الصورة اليومية للمالك (بيع، شراء، مخزون، نقدية، مصروفات)؛ وحدة ضمن النسخة الأولى.
- **Reason:** عقد النطاق ودورة العمل الموثقة وهدف المالك في الرؤية.
- **Source(s):** `docs/shared/roadmap/releases/mvp.md`, `docs/business/domain-overview.md` (Draft), `docs/shared/VISION.md` (Draft)
- **Related Module:** `reporting`

### BD-REP-002 — Reports are read-only
- **Status:** Draft
- **Decision:** التقرير عرض للقراءة فقط فوق النشاط المسجل — لا يُنشئ ولا يعدّل حركة.
- **Reason:** تعريف المسرد.
- **Source(s):** `docs/shared/GLOSSARY.md` (Draft)
- **Related Module:** `reporting`

## Settings

### BD-SET-001 — Settings is an MVP module for clinic-level configuration
- **Status:** Draft
- **Decision:** «الإعدادات» وحدة ضمن النسخة الأولى للإعداد على مستوى العيادة؛ نطاقها الدقيق يُحسم عند توثيقها.
- **Reason:** أُضيفت بمراجعة المالك لهيكل المستودع؛ النطاق مُعلَّم TODO في المسرد.
- **Source(s):** `docs/architecture/decisions/ADR-0001-repository-structure.md`, `docs/shared/GLOSSARY.md` (Draft)
- **Related Module:** `settings`

## Security

### BD-SEC-001 — Every operation leaves an audit trail
- **Status:** Draft
- **Decision:** لا حركة بلا سجل: كل عملية تُسجَّل ولها أثر تدقيق يجيب عن «من، ماذا، متى»؛ سجل التدقيق وحدة ضمن النسخة الأولى.
- **Reason:** المبدأ الأول في الرؤية؛ وحدة `audit-log` في عقد النطاق.
- **Source(s):** `docs/shared/VISION.md` (Draft), `docs/shared/roadmap/releases/mvp.md`
- **Related Module:** `audit-log`

### BD-SEC-002 — Clear role boundaries (owner vs assistant)
- **Status:** Draft
- **Decision:** حدود واضحة بين الأدوار: ما يراه ويفعله المالك غير ما يراه ويفعله المساعد؛ توزيع الصلاحيات التفصيلي يُحسم عند توثيق وحدة الإعدادات.
- **Reason:** مبدأ مسجل في الرؤية والشخصيات؛ التفاصيل TODO مسجلة.
- **Source(s):** `docs/shared/VISION.md` (Draft), `docs/shared/personas.md` (Draft)
- **Related Module:** `settings`, `audit-log`

### BD-SEC-003 — Assistant actions are attributed by name
- **Status:** Draft
- **Decision:** كل حركة يسجلها المساعد تظهر في سجل التدقيق باسمه.
- **Reason:** توصيف الشخصيات المبني على مبدأ الأثر.
- **Source(s):** `docs/shared/personas.md` (Draft)
- **Related Module:** `audit-log`

### BD-SEC-004 — The owner's data is his, with a local backup always
- **Status:** Draft
- **Decision:** بيانات المالك ملكه، محفوظة وآمنة ولها نسخة احتياطية محلية دائمًا؛ النسخ الاحتياطي المحلي وحدة ضمن النسخة الأولى.
- **Reason:** فلسفة المنتج في الرؤية؛ وحدة `backup` في عقد النطاق.
- **Source(s):** `docs/shared/VISION.md` (Draft), `docs/shared/roadmap/releases/mvp.md`
- **Related Module:** `backup`

## Documentation

### BD-DOC-001 — Documentation-First development
- **Status:** Draft
- **Decision:** لا تنفيذ قبل اعتماد وثائق الوحدة المعنية؛ دورة حياة الوثيقة: `Draft → Review → Approved`.
- **Reason:** قاعدة سير العمل الأساسية للمشروع.
- **Source(s):** `CLAUDE.md`, `.claude/rules/workflow.md`
- **Related Module:** Process-wide

### BD-DOC-002 — Repository structure per ADR-0001
- **Status:** Draft (source ADR: Accepted)
- **Decision:** مستودع واحد؛ وثائق كل وحدة في مجلد خاص بطقم ملفات قياسي من ثمانية ملفات (`docs/modules/_TEMPLATE/`)؛ الوثائق العابرة للوحدات في `docs/shared/`.
- **Reason:** بنية مثبتة في ADR-0001 المعتمد.
- **Source(s):** `docs/architecture/decisions/ADR-0001-repository-structure.md`
- **Related Module:** Process-wide

### BD-DOC-003 — Hybrid documentation language
- **Status:** Draft (source ADR: Accepted)
- **Decision:** الوثائق الهندسية بالإنجليزية؛ وثائق العمل والمنتج بالعربية؛ أسماء الملفات والحالات والمعرّفات الثابتة تبقى لاتينية.
- **Reason:** قرار المالك المثبت في ADR-0002 المعتمد.
- **Source(s):** `docs/architecture/decisions/ADR-0002-documentation-language.md`
- **Related Module:** Process-wide

### BD-DOC-004 — One place per decision (routing rule)
- **Status:** Draft (source ADR: Accepted)
- **Decision:** القرار يعيش في مكان واحد فقط: عسير العكس (> يوم) → ADR؛ عابر للوحدات → هذا السجل؛ خاص بوحدة → `decisions.md` الخاص بها. الربط بدل التكرار.
- **Reason:** قاعدة التوجيه في ADR-0001 وقواعد سير العمل.
- **Source(s):** `docs/architecture/decisions/ADR-0001-repository-structure.md`, `.claude/rules/workflow.md`
- **Related Module:** Process-wide

### BD-DOC-005 — Business decisions belong to the owner and are final
- **Status:** Draft
- **Decision:** قرارات العمل ملك المالك ونهائية؛ لا اختراع متطلبات — عند نقص المعلومة: سؤال المالك وتسجيل الإجابة.
- **Reason:** قاعدة غير قابلة للتفاوض في دستور المشروع.
- **Source(s):** `CLAUDE.md`, `docs/PROJECT_CONTEXT.md` (Draft)
- **Related Module:** Process-wide

### BD-DOC-006 — The glossary is the single EN↔AR bridge
- **Status:** Draft (source ADR: Accepted)
- **Decision:** `docs/shared/GLOSSARY.md` هو المصدر الوحيد لمقابلة المصطلح العربي المعتمد بالمعرّف الإنجليزي المستخدم في الكود والوثائق الهندسية.
- **Reason:** مثبت في ADR-0002.
- **Source(s):** `docs/architecture/decisions/ADR-0002-documentation-language.md`
- **Related Module:** Process-wide

### BD-DOC-007 — Stable IDs are never renumbered
- **Status:** Draft
- **Decision:** المعرّفات الثابتة (`ADR-NNNN`، `REQ-`، `BR-`، `WF-`، `AC-`، `TS-`، `BD-`) لا يُعاد ترقيمها أبدًا؛ الـADR لا يُحذف بل يُستبدَل بالإحالة (Superseded).
- **Reason:** قواعد التسمية وفهرس الـADRs وقوالب الوحدة.
- **Source(s):** `.claude/rules/naming.md`, `docs/architecture/decisions/_INDEX.md`, `docs/modules/_TEMPLATE/`
- **Related Module:** Process-wide
