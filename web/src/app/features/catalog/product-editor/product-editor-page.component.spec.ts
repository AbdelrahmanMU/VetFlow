import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { ProductEditorPageComponent, EditorMode } from './product-editor-page.component';
import { EditProduct } from './product-editor.models';

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

  function setup(mode: EditorMode, id?: string) {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(ProductEditorPageComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('mode', mode);
    if (id) {
      fixture.componentRef.setInput('id', id);
    }
    flushLookups();

    if (mode === 'edit') {
      fixture.detectChanges(); // ngOnInit → load(id)
      http.expectOne((request) => request.url === `/api/v1/products/${id}` && request.method === 'GET').flush(editProduct);
    }

    return fixture;
  }

  function flushLookups(): void {
    const lookups = http.match(
      (request) =>
        request.url.endsWith('/categories') ||
        request.url.endsWith('/manufacturers') ||
        request.url.endsWith('/product-natures') ||
        request.url.endsWith('/units'),
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

  afterEach(() => {
    http.verify();
  });

  describe('create mode', () => {
    it('blocks submit and surfaces errors when the minimum is missing', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;

      component.submit();

      expect(component.submitted()).toBe(true);
      // No possible-duplicate read and no create are issued for an invalid form.
      http.expectNone('/api/v1/products/possible-duplicates');
      http.expectNone((request) => request.method === 'POST');
    });

    it('runs the duplicate check then creates and navigates on the happy path', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      fillValidCreateForm(component);
      expect(component.form.valid).toBe(true);
      component.submit();

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

    it('shows the possible-duplicate warning without creating until confirmed', () => {
      const fixture = setup('create');
      const component = fixture.componentInstance;

      fillValidCreateForm(component);
      component.submit();

      const duplicateCheck = http.expectOne(
        (request) => request.url === '/api/v1/products/possible-duplicates',
      );
      duplicateCheck.flush({
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
      const component = fixture.componentInstance;
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      component.submit();

      // Edit is not audited and never runs the possible-duplicate advisory (create-only).
      http.expectNone('/api/v1/products/possible-duplicates');

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
});
