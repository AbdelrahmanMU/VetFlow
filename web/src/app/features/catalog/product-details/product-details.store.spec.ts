import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ProductDetailsApiService } from './product-details-api.service';
import { ProductDetailsStore } from './product-details.store';

describe('ProductDetailsStore', () => {
  let store: ProductDetailsStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ProductDetailsApiService,
        ProductDetailsStore,
      ],
    });

    store = TestBed.inject(ProductDetailsStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  const product = {
    id: 'p-1',
    internalCode: 'PRD-000001',
    arabicName: 'أموكسيسيلين 500',
    englishName: null,
    size: null,
    concentration: null,
    categoryName: 'أدوية',
    manufacturerName: 'شركة أ',
    natureName: 'دواء',
    status: 'active',
    isSplittable: true,
    isRefrigerated: false,
    hasExpiration: true,
    hasOpenExpiration: false,
    openExpirationPeriodDays: null,
    internalNotes: null,
    hasSellingPrice: true,
    units: [],
  };

  it('starts loading and becomes ready when the product is found', () => {
    store.setId('p-1');
    TestBed.tick();

    http.expectOne('/api/v1/products/p-1').flush(product);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.product.arabicName).toBe('أموكسيسيلين 500');
      expect(view.product.internalCode).toBe('PRD-000001');
    }
  });

  it('surfaces a distinct not-found state on 404', () => {
    store.setId('missing');
    TestBed.tick();

    http.expectOne('/api/v1/products/missing').flush(
      { type: 'about:blank', title: 'Not Found', status: 404 },
      { status: 404, statusText: 'Not Found' },
    );

    expect(store.view().kind).toBe('notFound');
  });

  it('surfaces the error state on a transport failure', () => {
    store.setId('p-1');
    TestBed.tick();

    http.expectOne('/api/v1/products/p-1').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(store.view().kind).toBe('error');
  });
});
