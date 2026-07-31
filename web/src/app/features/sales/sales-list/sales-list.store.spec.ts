import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { SalesListApiService } from './sales-list-api.service';
import { SalesListStore } from './sales-list.store';

describe('SalesListStore', () => {
  let store: SalesListStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        SalesListApiService,
        SalesListStore,
      ],
    });

    store = TestBed.inject(SalesListStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function flushSales(totalCount: number, itemCount = totalCount): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/sales-invoices');
    const items = Array.from({ length: Math.min(itemCount, totalCount) }, (_, index) => ({
      id: `${index}`,
      number: `SAL-00000${index}`,
      customerName: index % 2 === 0 ? `عميل ${index}` : null,
      saleDate: '2026-07-01',
      status: 'draft',
      total: { amount: 100, currency: 'EGP' },
      createdAt: '2026-07-01T09:00:00+00:00',
    }));
    request.flush({ items, page: 1, pageSize: 25, totalCount });
  }

  it('starts loading and becomes ready when the API answers (TS-SAL-024)', () => {
    expect(store.view().kind).toBe('loading');
    TestBed.tick();
    flushSales(0);
    expect(store.view().kind).toBe('ready');
  });

  it('defaults to the newest-sale-first sort (BR-SAL-019)', () => {
    expect(store.sort()).toEqual({ field: 'saleDate', direction: 'desc' });
  });

  it('an empty list without a search term or filters is the new state (TS-SAL-025)', () => {
    TestBed.tick();
    flushSales(0);
    expect(store.emptyKind()).toBe('new');
  });

  it('an empty result with a search term is the no-results-for-search state', () => {
    TestBed.tick();
    flushSales(0);

    store.setSearch('غير موجود');
    TestBed.tick();
    flushSales(0);

    expect(store.emptyKind()).toBe('search');
  });

  it('an empty result with an applied filter is the filters state (TS-SAL-027)', () => {
    TestBed.tick();
    flushSales(0);

    store.setFilters({ status: 'committed', dateFrom: null, dateTo: null });
    TestBed.tick();
    flushSales(0);

    expect(store.emptyKind()).toBe('filters');
  });

  it('a populated list has no empty state', () => {
    TestBed.tick();
    flushSales(3);
    expect(store.emptyKind()).toBeNull();
  });

  it('changing the sort resets pagination to the first page (TS-SAL-028)', () => {
    TestBed.tick();
    flushSales(60, 25);

    store.setPage(3);
    TestBed.tick();
    flushSales(60, 25);
    expect(store.page()).toBe(3);

    store.setSort({ field: 'total', direction: 'asc' });
    TestBed.tick();
    flushSales(60, 25);

    expect(store.page()).toBe(1);
  });

  it('an API failure is the error state and retry issues a new request', () => {
    TestBed.tick();
    const failing = http.expectOne((candidate) => candidate.url === '/api/v1/sales-invoices');
    failing.flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flushSales(0);
    expect(store.view().kind).toBe('ready');
  });
});
