import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { MovementHistoryApiService } from './movement-history-api.service';
import { MovementHistoryItem } from './movement-history.models';
import { MovementHistoryStore } from './movement-history.store';

describe('MovementHistoryStore', () => {
  let store: MovementHistoryStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        MovementHistoryApiService,
        MovementHistoryStore,
      ],
    });

    store = TestBed.inject(MovementHistoryStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function row(index: number, overrides: Partial<MovementHistoryItem> = {}): MovementHistoryItem {
    return {
      movementId: `m-${index}`,
      occurredAt: '2026-07-31T10:00:00+03:00',
      type: 'receive',
      productName: `منتج ${index}`,
      batchId: `b-${index}`,
      quantity: 10,
      stockUnitName: 'شريط',
      referenceLabel: 'PUR-000001',
      referenceTarget: 'purchaseInvoice',
      referenceId: 'inv-1',
      source: 'purchasing',
      ...overrides,
    };
  }

  function flush(totalCount: number, items?: readonly MovementHistoryItem[]): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/inventory/movements');
    const body = items ?? Array.from({ length: totalCount }, (_, index) => row(index));
    request.flush({ items: body, page: 1, pageSize: 25, totalCount });
  }

  it('starts loading and becomes ready when the API answers', () => {
    expect(store.view().kind).toBe('loading');
    TestBed.tick();
    flush(0);
    expect(store.view().kind).toBe('ready');
  });

  it('requests pagination only — no search, filter or sort parameter (BR-INV-044)', () => {
    TestBed.tick();
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/inventory/movements');

    expect(request.request.params.keys().sort()).toEqual(['page', 'pageSize']);
    expect(request.request.params.get('pageSize')).toBe('25');
    request.flush({ items: [], page: 1, pageSize: 25, totalCount: 0 });
  });

  it('an empty ledger is the one and only empty state — there are no filters to blame', () => {
    TestBed.tick();
    flush(0);
    expect(store.isEmpty()).toBe(true);
  });

  it('is not empty when rows come back', () => {
    TestBed.tick();
    flush(2);
    expect(store.isEmpty()).toBe(false);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.totalCount).toBe(2);
      expect(view.items.length).toBe(2);
    }
  });

  it('keeps the signed quantity exactly as the ledger reported it (BR-INV-064)', () => {
    TestBed.tick();
    flush(1, [row(0, { type: 'consume', quantity: -3, source: 'sales' })]);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.items[0].quantity).toBe(-3);
    }
  });

  it('paging refetches for the requested page', () => {
    TestBed.tick();
    flush(30);

    store.setPage(2);
    TestBed.tick();
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/inventory/movements');
    expect(request.request.params.get('page')).toBe('2');
    request.flush({ items: [], page: 2, pageSize: 25, totalCount: 30 });
  });

  it('an error becomes the error state, and retry refetches', () => {
    TestBed.tick();
    http
      .expectOne((candidate) => candidate.url === '/api/v1/inventory/movements')
      .flush('boom', { status: 500, statusText: 'Server Error' });
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flush(1);
    expect(store.view().kind).toBe('ready');
  });
});
