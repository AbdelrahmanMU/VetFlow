import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { ProductCreatePageComponent } from './product-create-page.component';

describe('ProductCreatePageComponent', () => {
  let http: HttpTestingController;

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(ProductCreatePageComponent);
    http = TestBed.inject(HttpTestingController);
    flushLookups();
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

  function fillValidForm(component: ProductCreatePageComponent): void {
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

  it('blocks submit and surfaces errors when the minimum is missing', () => {
    const fixture = setup();
    const component = fixture.componentInstance;

    component.submit();

    expect(component.submitted()).toBe(true);
    // No possible-duplicate read and no create are issued for an invalid form.
    http.expectNone('/api/v1/products/possible-duplicates');
    http.expectNone((request) => request.method === 'POST');
  });

  it('runs the duplicate check then creates and navigates on the happy path', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fillValidForm(component);
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
    create.flush({ id: 'new-1', internalCode: 'PRD-000001' });

    expect(navigate).toHaveBeenCalledWith(['/catalog/products', 'new-1']);
  });

  it('shows the possible-duplicate warning without creating until confirmed', () => {
    const fixture = setup();
    const component = fixture.componentInstance;

    fillValidForm(component);
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
