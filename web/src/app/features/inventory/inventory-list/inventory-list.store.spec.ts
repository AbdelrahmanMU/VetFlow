import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { InventoryApiService } from './inventory-api.service';
import { InventoryListStore } from './inventory-list.store';

describe('InventoryListStore', () => {
  let store: InventoryListStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        InventoryApiService,
        InventoryListStore,
      ],
    });

    store = TestBed.inject(InventoryListStore);
    http = TestBed.inject(HttpTestingController);

    // The category lookup subscribes on construction — answer it once.
    http
      .expectOne((candidate) => candidate.url === '/api/v1/categories')
      .flush({ items: [], page: 1, pageSize: 100, totalCount: 0 });
  });

  afterEach(() => {
    http.verify();
  });

  function flushInventory(totalCount: number, itemCount = totalCount): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/inventory');
    const items = Array.from({ length: Math.min(itemCount, totalCount) }, (_, index) => ({
      productId: `${index}`,
      productName: `منتج ${index}`,
      onHandQuantity: 24,
      stockUnitName: 'شريط',
      batchCount: 1,
      nearestExpiry: null,
    }));
    request.flush({ items, page: 1, pageSize: 25, totalCount });
  }

  it('starts loading and becomes ready when the API answers', () => {
    expect(store.view().kind).toBe('loading');
    TestBed.tick();
    flushInventory(0);
    expect(store.view().kind).toBe('ready');
  });

  it('defaults to the product-name-ascending sort', () => {
    expect(store.sort()).toEqual({ field: 'product', direction: 'asc' });
  });

  it('an empty list without a search term or filters is the new state (empty inventory)', () => {
    TestBed.tick();
    flushInventory(0);
    expect(store.emptyKind()).toBe('new');
  });

  it('an empty result with a search term is the no-results-for-search state', () => {
    TestBed.tick();
    flushInventory(0);

    store.setSearch('غير موجود');
    TestBed.tick();
    flushInventory(0);

    expect(store.emptyKind()).toBe('search');
  });

  it('an empty result with the expiring-soon filter is the filters state (BR-INV-013)', () => {
    TestBed.tick();
    flushInventory(0);

    store.setFilters({ category: null, expiringSoon: true, outOfStock: false });
    TestBed.tick();
    flushInventory(0);

    expect(store.emptyKind()).toBe('filters');
    expect(store.appliedChips().map((chip) => chip.key)).toEqual(['expiringSoon']);
  });

  it('removing a boolean filter chip turns it off and reloads (BR-INV-014)', () => {
    TestBed.tick();
    flushInventory(1);

    store.setFilters({ category: null, expiringSoon: false, outOfStock: true });
    TestBed.tick();
    flushInventory(0);
    expect(store.appliedChips().map((chip) => chip.key)).toEqual(['outOfStock']);

    store.removeFilter('outOfStock');
    TestBed.tick();
    flushInventory(1);
    expect(store.filters().outOfStock).toBe(false);
    expect(store.appliedChips()).toEqual([]);
  });

  it('a populated list has no empty state', () => {
    TestBed.tick();
    flushInventory(3);
    expect(store.emptyKind()).toBeNull();
  });

  it('changing the sort resets pagination to the first page', () => {
    TestBed.tick();
    flushInventory(60, 25);

    store.setPage(3);
    TestBed.tick();
    flushInventory(60, 25);
    expect(store.page()).toBe(3);

    store.setSort({ field: 'nearestExpiry', direction: 'asc' });
    TestBed.tick();
    flushInventory(60, 25);

    expect(store.page()).toBe(1);
  });

  it('an API failure is the error state and retry issues a new request', () => {
    TestBed.tick();
    const failing = http.expectOne((candidate) => candidate.url === '/api/v1/inventory');
    failing.flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flushInventory(0);
    expect(store.view().kind).toBe('ready');
  });
});
