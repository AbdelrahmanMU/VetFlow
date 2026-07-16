import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { PurchasesApiService } from './purchases-api.service';
import { PurchaseListStore } from './purchase-list.store';

describe('PurchaseListStore', () => {
  let store: PurchaseListStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        PurchasesApiService,
        PurchaseListStore,
      ],
    });

    store = TestBed.inject(PurchaseListStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function flushPurchases(totalCount: number, itemCount = totalCount): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/purchase-invoices');
    const items = Array.from({ length: Math.min(itemCount, totalCount) }, (_, index) => ({
      id: `${index}`,
      number: `PUR-00000${index}`,
      supplierName: `مورد ${index}`,
      supplierInvoiceReference: null,
      invoiceDate: '2026-07-01',
      status: 'draft',
      total: { amount: 100, currency: 'EGP' },
      createdAt: '2026-07-01T09:00:00+00:00',
    }));
    request.flush({ items, page: 1, pageSize: 25, totalCount });
  }

  it('starts loading and becomes ready when the API answers', () => {
    expect(store.view().kind).toBe('loading');
    TestBed.tick();
    flushPurchases(0);
    expect(store.view().kind).toBe('ready');
  });

  it('defaults to the newest-invoice-first sort (BR-PUR-004)', () => {
    expect(store.sort()).toEqual({ field: 'invoiceDate', direction: 'desc' });
  });

  it('an empty list without a search term or filters is the new state', () => {
    TestBed.tick();
    flushPurchases(0);
    expect(store.emptyKind()).toBe('new');
  });

  it('an empty result with a search term is the no-results-for-search state', () => {
    TestBed.tick();
    flushPurchases(0);

    store.setSearch('غير موجود');
    TestBed.tick();
    flushPurchases(0);

    expect(store.emptyKind()).toBe('search');
  });

  it('an empty result with an applied filter is the filters state', () => {
    TestBed.tick();
    flushPurchases(0);

    store.setFilters({ status: 'received', dateFrom: null, dateTo: null });
    TestBed.tick();
    flushPurchases(0);

    expect(store.emptyKind()).toBe('filters');
  });

  it('a populated list has no empty state', () => {
    TestBed.tick();
    flushPurchases(3);
    expect(store.emptyKind()).toBeNull();
  });

  it('changing the sort resets pagination to the first page', () => {
    TestBed.tick();
    flushPurchases(60, 25);

    store.setPage(3);
    TestBed.tick();
    flushPurchases(60, 25);
    expect(store.page()).toBe(3);

    store.setSort({ field: 'total', direction: 'asc' });
    TestBed.tick();
    flushPurchases(60, 25);

    expect(store.page()).toBe(1);
  });

  it('an API failure is the error state and retry issues a new request', () => {
    TestBed.tick();
    const failing = http.expectOne((candidate) => candidate.url === '/api/v1/purchase-invoices');
    failing.flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flushPurchases(0);
    expect(store.view().kind).toBe('ready');
  });
});
