import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ExpiryMonitoringApiService } from './expiry-monitoring-api.service';
import { ExpiryMonitoringStore } from './expiry-monitoring.store';

describe('ExpiryMonitoringStore', () => {
  let store: ExpiryMonitoringStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ExpiryMonitoringApiService,
        ExpiryMonitoringStore,
      ],
    });

    store = TestBed.inject(ExpiryMonitoringStore);
    http = TestBed.inject(HttpTestingController);

    // The category lookup subscribes on construction — answer it once.
    http
      .expectOne((candidate) => candidate.url === '/api/v1/categories')
      .flush({ items: [], page: 1, pageSize: 100, totalCount: 0 });
  });

  afterEach(() => {
    http.verify();
  });

  function flushExpiry(totalCount: number, itemCount = totalCount): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/inventory/expiry');
    const items = Array.from({ length: Math.min(itemCount, totalCount) }, (_, index) => ({
      productId: `p-${index}`,
      productName: `منتج ${index}`,
      batchId: `b-${index}`,
      remainingQuantity: 10,
      stockUnitName: 'شريط',
      expiryDate: '2026-08-01',
    }));
    request.flush({ items, page: 1, pageSize: 25, totalCount });
  }

  it('starts loading and becomes ready when the API answers', () => {
    expect(store.view().kind).toBe('loading');
    TestBed.tick();
    flushExpiry(0);
    expect(store.view().kind).toBe('ready');
  });

  it('an empty list with no search or filters is the none-empty state', () => {
    TestBed.tick();
    flushExpiry(0);
    expect(store.emptyKind()).toBe('none');
  });

  it('an empty result with a search term is the search-empty state', () => {
    TestBed.tick();
    flushExpiry(0);

    store.setSearch('غير موجود');
    TestBed.tick();
    flushExpiry(0);

    expect(store.emptyKind()).toBe('search');
  });

  it('the expired filter is an applied chip and resets pagination (BR-INV-036)', () => {
    TestBed.tick();
    flushExpiry(60, 25);

    store.setPage(2);
    TestBed.tick();
    flushExpiry(60, 25);
    expect(store.page()).toBe(2);

    store.setFilters({ category: null, expired: true, expiringSoon: false });
    TestBed.tick();
    flushExpiry(0);

    expect(store.page()).toBe(1);
    expect(store.appliedChips().map((chip) => chip.key)).toEqual(['expired']);
  });

  it('a populated list has no empty state', () => {
    TestBed.tick();
    flushExpiry(3);
    expect(store.emptyKind()).toBeNull();
  });

  it('an API failure is the error state and retry re-requests', () => {
    TestBed.tick();
    http.expectOne((candidate) => candidate.url === '/api/v1/inventory/expiry').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flushExpiry(0);
    expect(store.view().kind).toBe('ready');
  });
});
