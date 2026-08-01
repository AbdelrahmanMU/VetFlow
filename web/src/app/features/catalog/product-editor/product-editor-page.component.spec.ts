import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { ProductEditorPageComponent, EditorMode } from './product-editor-page.component';
import { CategoryOption, EditProduct, ManufacturerOption } from './product-editor.models';

/**
 * Product editor S3 (catalog ui.md §5, DEC-CAT-031) on the validation
 * foundation (validation-and-guidance.md): per-rule messages through
 * `vf-form-field` (STD-UX-017/120), the clickable Validation Summary on the
 * qualifying long form (STD-UX-023/129), one sentence per cross-row unit rule
 * (BR-CAT-016/024/025), classified failures (STD-UX-123) with server field
 * errors projected inline (STD-UX-019), surfaced lookup failures with retry
 * (STD-UX-041), and the possible-duplicate advisory through the shared
 * debounced/cancelling/cached async check with its failure surfaced and never
 * blocking (BR-CAT-042, STD-UX-101/102).
 */
describe('ProductEditorPageComponent', () => {
  let http: HttpTestingController;

  const editProduct: EditProduct = {
    id: 'p-1',
    internalCode: 'PRD-000009',
    arabicName: 'أموكسيسيلين 500',
    englishName: null,
    size: null,
    concentration: null,
    categoryId: 'cat-1',
    manufacturerId: 'man-1',
    natureId: 'nat-1',
    isSplittable: false,
    isRefrigerated: false,
    hasExpiration: false,
    hasOpenExpiration: false,
    openExpirationPeriodDays: null,
    internalNotes: null,
    units: [
      {
        unitId: 'unit-carton',
        position: 0,
        quantityInNextUnit: 10,
        isPurchaseUnit: true,
        isSaleUnit: false,
        barcode: null,
        sellingPrice: null,
        isStorageUnit: false,
        isDefaultSaleUnit: false,
        isDefaultPurchaseUnit: true,
      },
      {
        unitId: 'unit-box',
        position: 1,
        quantityInNextUnit: null,
        isPurchaseUnit: false,
        isSaleUnit: true,
        barcode: '6221000000010',
        sellingPrice: { amount: 25, currency: 'EGP' },
        isStorageUnit: true,
        isDefaultSaleUnit: true,
        isDefaultPurchaseUnit: false,
      },
    ],
  };

  function setup(
    mode: EditorMode,
    id?: string,
    categories: readonly CategoryOption[] = [],
    manufacturers: readonly ManufacturerOption[] = [],
  ) {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(ProductEditorPageComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('mode', mode);
    if (id) {
      fixture.componentRef.setInput('id', id);
    }
    flushLookups(categories, manufacturers);

    if (mode === 'edit') {
      fixture.detectChanges(); // ngOnInit → load(id)
      http.expectOne((request) => request.url === `/api/v1/products/${id}` && request.method === 'GET').flush(editProduct);
    }

    return fixture;
  }

  function flushLookups(
    categories: readonly CategoryOption[] = [],
    manufacturers: readonly ManufacturerOption[] = [],
  ): void {
    // Categories and manufacturers carry the active flag (REQ-CTG-005 / REQ-CAT-048);
    // the other lookups are plain.
    for (const request of http.match((request) => request.url.endsWith('/categories'))) {
      request.flush({ items: categories, page: 1, pageSize: 100, totalCount: categories.length });
    }
    for (const request of http.match((request) => request.url.endsWith('/manufacturers'))) {
      request.flush({ items: manufacturers, page: 1, pageSize: 100, totalCount: manufacturers.length });
    }
    const lookups = http.match(
      (request) => request.url.endsWith('/product-natures') || request.url.endsWith('/units'),
    );
    for (const request of lookups) {
      request.flush({ items: [], page: 1, pageSize: 100, totalCount: 0 });
    }
  }

  function fillValidCreateForm(component: ProductEditorPageComponent): void {
    const { controls } = component.form;
    controls.arabicName.setValue('أموكسيسيلين 500');
    controls.categoryId.setValue('cat-1');
    controls.manufacturerId.setValue('man-1');
    controls.natureId.setValue('nat-1');

    const [purchaseRow, saleRow] = component.form.controls.units.controls;
    purchaseRow.controls.unitId.setValue('unit-carton');
    purchaseRow.controls.isPurchaseUnit.setValue(true);
    saleRow.controls.unitId.setValue('unit-box');
    saleRow.controls.isSaleUnit.setValue(true);

    controls.storageUnitId.setValue('unit-box');
    controls.defaultSaleUnitId.setValue('unit-box');
    controls.defaultPurchaseUnitId.setValue('unit-carton');
  }

  function submitForm(fixture: ComponentFixture<ProductEditorPageComponent>): void {
    fixture.detectChanges();
    const form = (fixture.nativeElement as HTMLElement).querySelector('form');
    if (!form) {
      throw new Error('form not rendered');
    }

    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();
  }

  afterEach(() => {
    http.verify();
  });

  describe('create mode — submit guidance', () => {
    it('blocks submit, shows per-field and per-rule errors, the summary, and focuses the first invalid field (STD-UX-012/017/023/070)', async () => {
      const fixture = setup('create');

      submitForm(fixture);

      // Nothing is probed and nothing is written when the form is invalid.
      http.expectNone((request) => request.url === '/api/v1/products/possible-duplicates');
      http.expectNone((request) => request.method === 'POST');

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('هذا الحقل مطلوب.');
      // The cross-row unit rules speak one sentence each (BR-CAT-024/025) —
      // the empty-profile sentence stays silent while rows exist.
      expect(text).toContain('حدّد وحدة شراء واحدة على الأقل.');
      expect(text).toContain('حدّد وحدة بيع واحدة على الأقل.');
      expect(text).not.toContain('أضف وحدة واحدة على الأقل.');
      // The qualifying long form renders the navigational summary (STD-UX-023).
      expect(text).toContain('أكمل الحقول التالية ثم أعد الحفظ:');
      const summaryLinks = (fixture.nativeElement as HTMLElement).querySelectorAll('.vf-summary-link');
      expect(summaryLinks.length).toBeGreaterThan(0);

      // The shared guidance focuses the first invalid control one tick later.
      await new Promise((resolve) => setTimeout(resolve, 0));
      const firstInvalid = (fixture.nativeElement as HTMLElement).querySelector('.vf-field--invalid input');
      expect(document.activeElement).toBe(firstInvalid);
    });

    it('each violated rule keeps its own sentence — max length never renders the required copy (STD-UX-017)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      fixture.detectChanges();

      component.form.controls.arabicName.setValue('م'.repeat(301));
      component.form.controls.arabicName.markAsTouched();
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('يجب ألّا يتجاوز هذا الحقل');
    });

    it('the open-expiration period is required and positive only while the capability is on (BR-CAT-036)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      fixture.detectChanges();

      const days = component.form.controls.openExpirationPeriodDays;
      expect(days.valid).toBe(true);

      component.form.controls.hasOpenExpiration.setValue(true);
      expect(days.hasError('required')).toBe(true);

      days.setValue(0);
      expect(days.hasError('positive')).toBe(true);

      days.setValue(30);
      expect(days.valid).toBe(true);

      component.form.controls.hasOpenExpiration.setValue(false);
      expect(days.value).toBeNull();
      expect(days.valid).toBe(true);
    });

    it('capability checkboxes carry an explicit id/for label association and bind the form control (STD-UX-093)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      fixture.detectChanges();

      const checkbox = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
        '.capabilities input[type="checkbox"]',
      );
      if (!checkbox) {
        throw new Error('capability checkbox not rendered');
      }

      expect(checkbox.id).not.toBe('');
      expect(checkbox.closest('label')?.getAttribute('for')).toBe(checkbox.id);

      expect(component.form.controls.isSplittable.value).toBe(false);
      checkbox.click();
      expect(component.form.controls.isSplittable.value).toBe(true);
    });
  });

  describe('create mode — the possible-duplicate advisory (fake timers)', () => {
    beforeEach(() => {
      vi.useFakeTimers();
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('runs the debounced check then creates and navigates on the happy path (STD-UX-101)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      fillValidCreateForm(component);
      expect(component.form.valid).toBe(true);
      submitForm(fixture);

      // Nothing fires before the debounce pause (STD-UX-101).
      http.expectNone((request) => request.url === '/api/v1/products/possible-duplicates');
      vi.advanceTimersByTime(300);

      const duplicateCheck = http.expectOne(
        (request) => request.url === '/api/v1/products/possible-duplicates',
      );
      duplicateCheck.flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });

      const create = http.expectOne(
        (request) => request.url === '/api/v1/products' && request.method === 'POST',
      );
      expect(create.request.body.arabicName).toBe('أموكسيسيلين 500');
      expect(create.request.body.units.length).toBe(2);
      // Create carries the per-unit selling price (the create write side).
      expect('sellingPrice' in create.request.body.units[1]).toBe(true);
      create.flush({ id: 'new-1', internalCode: 'PRD-000001' });

      expect(navigate).toHaveBeenCalledWith(['/catalog/products', 'new-1']);
    });

    it('shows the possible-duplicate warning without creating until confirmed (BR-CAT-042)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;

      fillValidCreateForm(component);
      submitForm(fixture);
      vi.advanceTimersByTime(300);

      http
        .expectOne((request) => request.url === '/api/v1/products/possible-duplicates')
        .flush({
          items: [
            {
              id: 'existing-1',
              arabicName: 'أموكسيسيلين 500',
              englishName: null,
              size: null,
              concentration: null,
              manufacturerName: 'شركة أ',
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 1,
        });

      expect(component.dialogVisible()).toBe(true);
      expect(component.duplicates().length).toBe(1);
      // The warning never blocks, but nothing is created until the user decides.
      http.expectNone((request) => request.method === 'POST');
    });

    it('caches the outcome per probed value — a retried save issues no second check (STD-UX-102)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      fillValidCreateForm(component);
      submitForm(fixture);
      vi.advanceTimersByTime(300);
      http
        .expectOne((request) => request.url === '/api/v1/products/possible-duplicates')
        .flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });

      // The create itself fails; the classified banner renders (STD-UX-123).
      http
        .expectOne((request) => request.method === 'POST')
        .flush(
          { type: 'about:blank', title: 'Internal Server Error', status: 500 },
          { status: 500, statusText: 'Internal Server Error' },
        );
      fixture.detectChanges();
      const banner = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
      expect(banner?.textContent).toContain('تعذّر حفظ المنتج. لم يُحفظ أيّ تغيير');
      expect(banner?.getAttribute('role')).toBe('alert');

      // Retry with the same values: the cached advisory answers instantly.
      submitForm(fixture);
      http.expectNone((request) => request.url === '/api/v1/products/possible-duplicates');
      http
        .expectOne((request) => request.method === 'POST')
        .flush({ id: 'new-1', internalCode: 'PRD-000001' });
    });

    it('surfaces a failed duplicate check, pauses once, and never blocks the save (BR-CAT-042, AC-UX-01)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      fillValidCreateForm(component);
      submitForm(fixture);
      vi.advanceTimersByTime(300);
      http
        .expectOne((request) => request.url === '/api/v1/products/possible-duplicates')
        .flush(
          { type: 'about:blank', title: 'Internal Server Error', status: 500 },
          { status: 500, statusText: 'Internal Server Error' },
        );
      fixture.detectChanges();

      // The advisory failure is explicit — a warning, not an error, and no create yet.
      const notice = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
      expect(notice?.textContent).toContain('تعذّر التحقق من وجود منتج مشابه');
      expect(notice?.getAttribute('role')).toBe('status');
      expect(component.saving()).toBe(false);
      http.expectNone((request) => request.method === 'POST');

      // The user's next save is the explicit choice to continue without it.
      submitForm(fixture);
      http.expectNone((request) => request.url === '/api/v1/products/possible-duplicates');
      http
        .expectOne((request) => request.method === 'POST')
        .flush({ id: 'new-1', internalCode: 'PRD-000001' });
      expect(navigate).toHaveBeenCalledWith(['/catalog/products', 'new-1']);
    });

    it('a VTF-VAL-001 field error projects inline onto its field — not a banner (STD-UX-019/020)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      fillValidCreateForm(component);
      // Whitespace-only passes the client `required` but trims to empty in the
      // payload — the one real client-side escape to VTF-VAL-001.
      component.form.controls.arabicName.setValue('   ');
      submitForm(fixture);
      vi.advanceTimersByTime(300);
      http
        .expectOne((request) => request.url === '/api/v1/products/possible-duplicates')
        .flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });

      http
        .expectOne((request) => request.method === 'POST')
        .flush(
          {
            type: 'about:blank',
            title: 'Bad Request',
            status: 400,
            errorCode: 'VTF-VAL-001',
            errors: { arabicName: ['server text (never rendered)'] },
          },
          { status: 400, statusText: 'Bad Request' },
        );
      fixture.detectChanges();

      expect((fixture.nativeElement as HTMLElement).querySelector('vf-banner')).toBeNull();
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('راجع قيمة هذا الحقل.');
    });

    it('a business rejection renders its own mapped sentence, never a generic one (STD-UX-036/123)', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      fillValidCreateForm(component);
      submitForm(fixture);
      vi.advanceTimersByTime(300);
      http
        .expectOne((request) => request.url === '/api/v1/products/possible-duplicates')
        .flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });

      http
        .expectOne((request) => request.method === 'POST')
        .flush(
          {
            type: 'about:blank',
            title: 'Conflict',
            status: 409,
            errorCode: 'VTF-CAT-020',
          },
          { status: 409, statusText: 'Conflict' },
        );
      fixture.detectChanges();

      const banner = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
      expect(banner?.textContent).toContain('وحدة المخزون يجب أن تكون إحدى وحدات ملف الوحدات.');

      // The rejection banner never survives the edit that addresses it (STD-UX-035).
      component.form.controls.storageUnitId.setValue('unit-carton');
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('vf-banner')).toBeNull();
    });
  });

  describe('lookup failures (STD-UX-041)', () => {
    it('a failed categories lookup surfaces with retry instead of an empty list', () => {
      TestBed.configureTestingModule({
        providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
      });
      const fixture = TestBed.createComponent(ProductEditorPageComponent);
      http = TestBed.inject(HttpTestingController);
      fixture.componentRef.setInput('mode', 'create');

      http
        .expectOne((request) => request.url.endsWith('/categories'))
        .flush(
          { type: 'about:blank', title: 'Internal Server Error', status: 500 },
          { status: 500, statusText: 'Internal Server Error' },
        );
      for (const request of http.match(
        (request) =>
          request.url.endsWith('/manufacturers') ||
          request.url.endsWith('/product-natures') ||
          request.url.endsWith('/units'),
      )) {
        request.flush({ items: [], page: 1, pageSize: 100, totalCount: 0 });
      }
      fixture.detectChanges();

      const banner = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
      expect(banner?.textContent).toContain('تعذّر تحميل قائمة التصنيفات.');

      const retry = banner?.querySelector<HTMLButtonElement>('.retry-link');
      retry?.click();
      http
        .expectOne((request) => request.url.endsWith('/categories'))
        .flush({ items: [], page: 1, pageSize: 100, totalCount: 0 });
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('تعذّر تحميل قائمة التصنيفات.');
    });
  });

  describe('edit mode', () => {
    it('loads the product and prefills the form', () => {
      const fixture = setup('edit', 'p-1');
      const component = fixture.componentInstance;

      expect(component.loadState()).toBe('ready');
      expect(component.form.controls.arabicName.value).toBe('أموكسيسيلين 500');
      expect(component.form.controls.categoryId.value).toBe('cat-1');
      expect(component.form.controls.units.length).toBe(2);
      // Distinguished-unit flags drive the role prefill.
      expect(component.form.controls.storageUnitId.value).toBe('unit-box');
      expect(component.form.controls.defaultSaleUnitId.value).toBe('unit-box');
      expect(component.form.controls.defaultPurchaseUnitId.value).toBe('unit-carton');
      expect(component.form.valid).toBe(true);
    });

    it('surfaces a distinct not-found state on a 404 load', () => {
      TestBed.configureTestingModule({
        providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
      });
      const fixture = TestBed.createComponent(ProductEditorPageComponent);
      http = TestBed.inject(HttpTestingController);
      fixture.componentRef.setInput('mode', 'edit');
      fixture.componentRef.setInput('id', 'missing');
      flushLookups();
      fixture.detectChanges();

      http.expectOne('/api/v1/products/missing').flush(
        { type: 'about:blank', title: 'Not Found', status: 404 },
        { status: 404, statusText: 'Not Found' },
      );

      expect(fixture.componentInstance.loadState()).toBe('notFound');
    });

    it('saves via PUT without a selling price or a duplicate check', () => {
      const fixture = setup('edit', 'p-1');
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      submitForm(fixture);

      // Edit is not audited and never runs the possible-duplicate advisory (create-only).
      http.expectNone((request) => request.url === '/api/v1/products/possible-duplicates');

      const update = http.expectOne(
        (request) => request.url === '/api/v1/products/p-1' && request.method === 'PUT',
      );
      expect(update.request.body.arabicName).toBe('أموكسيسيلين 500');
      expect(update.request.body.units.length).toBe(2);
      // Price editing is the deferred audited path — PUT carries no per-unit price.
      expect('sellingPrice' in update.request.body.units[1]).toBe(false);
      update.flush(null, { status: 204, statusText: 'No Content' });

      expect(navigate).toHaveBeenCalledWith(['/catalog/products', 'p-1']);
    });
  });

  describe('category active-only integration (REQ-CTG-005 / DEC-CTG-002)', () => {
    it('offers only active categories when creating a new product (TS-CTG-006)', () => {
      const fixture = setup('create', undefined, [
        { id: 'cat-active', name: 'مضادات حيوية', isActive: true },
        { id: 'cat-inactive', name: 'تصنيف مُلغى', isActive: false },
      ]);

      const values = fixture.componentInstance.categorySelectOptions().map((option) => option.value);
      expect(values).toEqual(['cat-active']);
    });

    it('shows an active current category selected and untagged in edit mode', () => {
      const fixture = setup('edit', 'p-1', [
        { id: 'cat-1', name: 'مضادات حيوية', isActive: true },
        { id: 'cat-2', name: 'تصنيف نشط آخر', isActive: true },
      ]);
      const current = fixture.componentInstance.categorySelectOptions().find((option) => option.value === 'cat-1');

      expect(current).toBeDefined();
      expect(current!.label).toBe('مضادات حيوية');
      expect(current!.label).not.toContain('غير نشط');
    });

    it('keeps a deactivated current category visible, marked inactive, and offers active ones too (TS-CTG-007)', () => {
      const fixture = setup('edit', 'p-1', [
        { id: 'cat-1', name: 'تصنيف مُلغى', isActive: false },
        { id: 'cat-2', name: 'تصنيف نشط', isActive: true },
      ]);
      const options = fixture.componentInstance.categorySelectOptions();

      const current = options.find((option) => option.value === 'cat-1');
      expect(current).toBeDefined();
      expect(current!.label).toContain('غير نشط');
      expect(options.some((option) => option.value === 'cat-2')).toBe(true);
    });

    it('saves the unchanged inactive category on PUT without forcing a change (TS-CTG-007)', () => {
      const fixture = setup('edit', 'p-1', [{ id: 'cat-1', name: 'تصنيف مُلغى', isActive: false }]);
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      submitForm(fixture);

      const update = http.expectOne((request) => request.url === '/api/v1/products/p-1' && request.method === 'PUT');
      expect(update.request.body.categoryId).toBe('cat-1');
      update.flush(null, { status: 204, statusText: 'No Content' });
    });

    it('drops the inactive category once the user picks an active one', () => {
      const fixture = setup('edit', 'p-1', [
        { id: 'cat-1', name: 'تصنيف مُلغى', isActive: false },
        { id: 'cat-2', name: 'تصنيف نشط', isActive: true },
      ]);
      const component = fixture.componentInstance;
      expect(component.categorySelectOptions().some((option) => option.value === 'cat-1')).toBe(true);

      component.form.controls.categoryId.setValue('cat-2');

      expect(component.categorySelectOptions().some((option) => option.value === 'cat-1')).toBe(false);
    });
  });

  describe('manufacturer active-only integration (REQ-CAT-048 / DEC-CAT-032)', () => {
    it('offers only active manufacturers when creating a new product', () => {
      const fixture = setup('create', undefined, [], [
        { id: 'man-active', name: 'شركة نشطة', isActive: true },
        { id: 'man-inactive', name: 'شركة مُلغاة', isActive: false },
      ]);

      const values = fixture.componentInstance.manufacturerSelectOptions().map((option) => option.value);
      expect(values).toEqual(['man-active']);
    });

    it('shows an active current manufacturer selected and untagged in edit mode', () => {
      const fixture = setup('edit', 'p-1', [], [
        { id: 'man-1', name: 'شركة نشطة', isActive: true },
        { id: 'man-2', name: 'شركة نشطة أخرى', isActive: true },
      ]);
      const current = fixture.componentInstance.manufacturerSelectOptions().find((option) => option.value === 'man-1');

      expect(current).toBeDefined();
      expect(current!.label).toBe('شركة نشطة');
      expect(current!.label).not.toContain('غير نشط');
    });

    it('keeps a deactivated current manufacturer visible, marked inactive, and offers active ones too', () => {
      const fixture = setup('edit', 'p-1', [], [
        { id: 'man-1', name: 'شركة مُلغاة', isActive: false },
        { id: 'man-2', name: 'شركة نشطة', isActive: true },
      ]);
      const options = fixture.componentInstance.manufacturerSelectOptions();

      const current = options.find((option) => option.value === 'man-1');
      expect(current).toBeDefined();
      expect(current!.label).toContain('غير نشط');
      expect(options.some((option) => option.value === 'man-2')).toBe(true);
    });

    it('saves the unchanged inactive manufacturer on PUT without forcing a change', () => {
      const fixture = setup('edit', 'p-1', [], [{ id: 'man-1', name: 'شركة مُلغاة', isActive: false }]);
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      submitForm(fixture);

      const update = http.expectOne((request) => request.url === '/api/v1/products/p-1' && request.method === 'PUT');
      expect(update.request.body.manufacturerId).toBe('man-1');
      update.flush(null, { status: 204, statusText: 'No Content' });
    });

    it('drops the inactive manufacturer once the user picks an active one', () => {
      const fixture = setup('edit', 'p-1', [], [
        { id: 'man-1', name: 'شركة مُلغاة', isActive: false },
        { id: 'man-2', name: 'شركة نشطة', isActive: true },
      ]);
      const component = fixture.componentInstance;
      expect(component.manufacturerSelectOptions().some((option) => option.value === 'man-1')).toBe(true);

      component.form.controls.manufacturerId.setValue('man-2');

      expect(component.manufacturerSelectOptions().some((option) => option.value === 'man-1')).toBe(false);
    });
  });
});
