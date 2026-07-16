import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ManufacturersApiService } from './manufacturers-api.service';
import { ManufacturerListStore } from './manufacturer-list.store';

describe('ManufacturerListStore', () => {
  let store: ManufacturerListStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ManufacturersApiService,
        ManufacturerListStore,
      ],
    });

    store = TestBed.inject(ManufacturerListStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function flushManufacturers(totalCount: number, itemCount = totalCount): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/manufacturers');
    const items = Array.from({ length: Math.min(itemCount, totalCount) }, (_, index) => ({
      id: `${index}`,
      name: `شركة ${index}`,
      isActive: true,
    }));
    request.flush({ items, page: 1, pageSize: 25, totalCount });
  }

  it('starts loading and becomes ready when the API answers', () => {
    expect(store.view().kind).toBe('loading');
    TestBed.tick();
    flushManufacturers(0);
    expect(store.view().kind).toBe('ready');
  });

  it('an empty list without a search term is the new state', () => {
    TestBed.tick();
    flushManufacturers(0);
    expect(store.emptyKind()).toBe('new');
  });

  it('an empty result with a search term is the no-results-for-search state', () => {
    TestBed.tick();
    flushManufacturers(0);

    store.setSearch('غير موجود');
    TestBed.tick();
    flushManufacturers(0);

    expect(store.emptyKind()).toBe('search');
  });

  it('a populated list has no empty state', () => {
    TestBed.tick();
    flushManufacturers(3);
    expect(store.emptyKind()).toBeNull();
  });

  it('changing the sort resets pagination to the first page', () => {
    TestBed.tick();
    flushManufacturers(60, 25);

    store.setPage(3);
    TestBed.tick();
    flushManufacturers(60, 25);
    expect(store.page()).toBe(3);

    store.setSort({ field: 'status', direction: 'desc' });
    TestBed.tick();
    flushManufacturers(60, 25);

    expect(store.page()).toBe(1);
  });

  it('an API failure is the error state and retry issues a new request', () => {
    TestBed.tick();
    const failing = http.expectOne((candidate) => candidate.url === '/api/v1/manufacturers');
    failing.flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flushManufacturers(0);
    expect(store.view().kind).toBe('ready');
  });

  it('refresh re-fetches the current view from the server (never optimistic)', () => {
    TestBed.tick();
    flushManufacturers(1);
    expect(store.view().kind).toBe('ready');

    store.refresh();
    TestBed.tick();
    flushManufacturers(2);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.totalCount).toBe(2);
    }
  });
});
