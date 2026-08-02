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

  const summary = {
    productId: 'p-1',
    onHandQuantity: 34,
    stockUnitName: 'شريط',
    batchCount: 2,
    nearestExpiry: '2026-12-31',
    hasInventoryRecord: true,
  };

  /** The card's read is Inventory's (REQ-INV-012); every test must answer it or verify() trips. */
  function flushInventory(id: string, body: object = summary): void {
    http.expectOne(`/api/v1/inventory/${id}/summary`).flush(body);
  }

  it('starts loading and becomes ready when the product is found', () => {
    store.setId('p-1');
    TestBed.tick();

    http.expectOne('/api/v1/products/p-1').flush(product);
    flushInventory('p-1');

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
    flushInventory('missing', { type: 'about:blank', status: 404 });

    expect(store.view().kind).toBe('notFound');
  });

  it('surfaces the error state on a transport failure', () => {
    store.setId('p-1');
    TestBed.tick();

    http.expectOne('/api/v1/products/p-1').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    flushInventory('p-1');

    expect(store.view().kind).toBe('error');
  });

  describe('the inventory card (REQ-INV-012)', () => {
    it('reads the summary from Inventory and exposes it verbatim', () => {
      store.setId('p-1');
      TestBed.tick();

      http.expectOne('/api/v1/products/p-1').flush(product);
      flushInventory('p-1');

      const view = store.inventoryView();
      expect(view.kind).toBe('ready');
      if (view.kind === 'ready') {
        // Rendered as supplied — the screen sums and converts nothing (BR-INV-008).
        expect(view.summary.onHandQuantity).toBe(34);
        expect(view.summary.batchCount).toBe(2);
        expect(view.summary.stockUnitName).toBe('شريط');
        expect(view.summary.nearestExpiry).toBe('2026-12-31');
      }
    });

    it('keeps the product readable when the stock read fails', () => {
      store.setId('p-1');
      TestBed.tick();

      http.expectOne('/api/v1/products/p-1').flush(product);
      http.expectOne('/api/v1/inventory/p-1/summary').flush(
        { type: 'about:blank', title: 'Internal Server Error', status: 500 },
        { status: 500, statusText: 'Internal Server Error' },
      );

      // The whole point of loading it separately: five loaded cards must not be
      // thrown away because the sixth could not be read.
      expect(store.view().kind).toBe('ready');
      expect(store.inventoryView().kind).toBe('error');
    });

    it('retries only the inventory card, leaving the product alone', () => {
      store.setId('p-1');
      TestBed.tick();

      http.expectOne('/api/v1/products/p-1').flush(product);
      http.expectOne('/api/v1/inventory/p-1/summary').flush(
        { type: 'about:blank', status: 500 },
        { status: 500, statusText: 'Internal Server Error' },
      );
      expect(store.inventoryView().kind).toBe('error');

      store.retryInventory();
      TestBed.tick();

      // Exactly one new request, and it is not the product's — verify() would fail
      // if retrying the card re-fetched the product too.
      flushInventory('p-1');
      expect(store.inventoryView().kind).toBe('ready');
      expect(store.view().kind).toBe('ready');
    });

    it('reports "never received" distinctly from a zero balance', () => {
      store.setId('p-1');
      TestBed.tick();

      http.expectOne('/api/v1/products/p-1').flush(product);
      flushInventory('p-1', {
        ...summary,
        onHandQuantity: 0,
        batchCount: 0,
        nearestExpiry: null,
        hasInventoryRecord: false,
      });

      const view = store.inventoryView();
      if (view.kind === 'ready') {
        expect(view.summary.hasInventoryRecord).toBe(false);
        expect(view.summary.onHandQuantity).toBe(0);
      }
    });
  });
});
