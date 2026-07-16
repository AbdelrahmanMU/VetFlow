import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CategoriesApiService } from './categories-api.service';
import { CategoryListStore } from './category-list.store';

describe('CategoryListStore', () => {
  let store: CategoryListStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        CategoriesApiService,
        CategoryListStore,
      ],
    });

    store = TestBed.inject(CategoryListStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function flushCategories(totalCount: number, itemCount = totalCount): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/categories');
    const items = Array.from({ length: Math.min(itemCount, totalCount) }, (_, index) => ({
      id: `${index}`,
      name: `تصنيف ${index}`,
      isActive: true,
    }));
    request.flush({ items, page: 1, pageSize: 25, totalCount });
  }

  it('starts loading and becomes ready when the API answers', () => {
    expect(store.view().kind).toBe('loading');
    TestBed.tick();
    flushCategories(0);
    expect(store.view().kind).toBe('ready');
  });

  it('an empty list without a search term is the new state', () => {
    TestBed.tick();
    flushCategories(0);
    expect(store.emptyKind()).toBe('new');
  });

  it('an empty result with a search term is the no-results-for-search state', () => {
    TestBed.tick();
    flushCategories(0);

    store.setSearch('غير موجود');
    TestBed.tick();
    flushCategories(0);

    expect(store.emptyKind()).toBe('search');
  });

  it('a populated list has no empty state', () => {
    TestBed.tick();
    flushCategories(3);
    expect(store.emptyKind()).toBeNull();
  });

  it('changing the sort resets pagination to the first page', () => {
    TestBed.tick();
    flushCategories(60, 25);

    store.setPage(3);
    TestBed.tick();
    flushCategories(60, 25);
    expect(store.page()).toBe(3);

    store.setSort({ field: 'status', direction: 'desc' });
    TestBed.tick();
    flushCategories(60, 25);

    expect(store.page()).toBe(1);
  });

  it('an API failure is the error state and retry issues a new request', () => {
    TestBed.tick();
    const failing = http.expectOne((candidate) => candidate.url === '/api/v1/categories');
    failing.flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flushCategories(0);
    expect(store.view().kind).toBe('ready');
  });

  it('refresh re-fetches the current view from the server (never optimistic)', () => {
    TestBed.tick();
    flushCategories(1);
    expect(store.view().kind).toBe('ready');

    store.refresh();
    TestBed.tick();
    flushCategories(2);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.totalCount).toBe(2);
    }
  });
});
