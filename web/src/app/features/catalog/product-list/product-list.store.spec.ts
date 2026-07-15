import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { EMPTY_FILTERS, LookupOption } from './product-list.models';
import { ProductListStore } from './product-list.store';
import { ProductsApiService } from './products-api.service';

describe('ProductListStore', () => {
  let store: ProductListStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ProductsApiService,
        ProductListStore,
      ],
    });

    store = TestBed.inject(ProductListStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    flushLookups();
    http.verify();
  });

  function flushLookups(): void {
    for (const request of http.match((candidate) => !candidate.url.includes('/products'))) {
      request.flush({ items: [], page: 1, pageSize: 100, totalCount: 0 });
    }
  }

  function flushProducts(totalCount: number): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/products');
    request.flush({ items: [], page: 1, pageSize: 25, totalCount });
  }

  it('starts loading and becomes ready when the API answers', () => {
    expect(store.view().kind).toBe('loading');
    TestBed.tick();
    flushProducts(0);
    expect(store.view().kind).toBe('ready');
  });

  it('an empty catalog without search or filters is the new-catalog state', () => {
    TestBed.tick();
    flushProducts(0);
    expect(store.emptyKind()).toBe('new');
  });

  it('an empty result with a search term is the no-results-for-search state', () => {
    TestBed.tick();
    flushProducts(0);

    store.setSearch('غير موجود');
    TestBed.tick();
    flushProducts(0);

    expect(store.emptyKind()).toBe('search');
  });

  it('an empty result with applied filters is the no-results-for-filters state', () => {
    TestBed.tick();
    flushProducts(0);

    store.setFilters({ ...EMPTY_FILTERS, refrigerated: true });
    TestBed.tick();
    flushProducts(0);

    expect(store.emptyKind()).toBe('filters');
  });

  it('changing filters resets pagination to the first page', () => {
    TestBed.tick();
    flushProducts(60);

    store.setPage(3);
    TestBed.tick();
    flushProducts(60);
    expect(store.page()).toBe(3);

    store.setFilters({ ...EMPTY_FILTERS, status: 'active' });
    TestBed.tick();
    flushProducts(60);

    expect(store.page()).toBe(1);
  });

  it('an API failure is the error state and retry issues a new request', () => {
    TestBed.tick();
    const failing = http.expectOne((candidate) => candidate.url === '/api/v1/products');
    failing.flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flushProducts(0);
    expect(store.view().kind).toBe('ready');
  });

  it('applied filter chips describe every active filter', () => {
    TestBed.tick();
    flushProducts(0);

    const category: LookupOption = { id: '11111111-1111-1111-1111-111111111111', name: 'أدوية' };
    store.setFilters({ ...EMPTY_FILTERS, category, hasSellingPrice: false });
    TestBed.tick();
    flushProducts(0);

    const labels = store.appliedChips().map((chip) => chip.label);
    expect(labels).toContain('التصنيف: أدوية');
    expect(labels).toContain('له سعر بيع: لا');
  });
});
