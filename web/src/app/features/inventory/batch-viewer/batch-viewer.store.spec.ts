import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { BatchViewerApiService } from './batch-viewer-api.service';
import { BatchViewerStore } from './batch-viewer.store';

describe('BatchViewerStore', () => {
  let store: BatchViewerStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        BatchViewerApiService,
        BatchViewerStore,
      ],
    });

    store = TestBed.inject(BatchViewerStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function flushBatches(totalCount: number, itemCount = totalCount): void {
    const request = http.expectOne((candidate) => candidate.url === '/api/v1/inventory/p-1/batches');
    const items = Array.from({ length: Math.min(itemCount, totalCount) }, (_, index) => ({
      batchId: `b-${index}`,
      purchaseReference: `PUR-00000${index}`,
      purchaseInvoiceId: `inv-${index}`,
      receiveDate: '2026-07-01T09:00:00Z',
      originalQuantity: 24,
      remainingQuantity: 24,
      stockUnitName: 'شريط',
      unitCostSnapshot: 100,
      expiryDate: null,
      status: 'active',
    }));
    request.flush({ productName: 'منتج', stockUnitName: 'شريط', batches: { items, page: 1, pageSize: 25, totalCount } });
  }

  it('stays loading until a product id is set (BR-INV-019)', () => {
    expect(store.view().kind).toBe('loading');
    http.expectNone(() => true);
  });

  it('defaults to the receive-date-descending order (BR-INV-031)', () => {
    expect(store.sort()).toEqual({ field: 'receiveDate', direction: 'desc' });
  });

  it('becomes ready with the product header when the API answers', () => {
    store.setProductId('p-1');
    TestBed.tick();
    flushBatches(2);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.productName).toBe('منتج');
      expect(view.stockUnitName).toBe('شريط');
      expect(view.items.length).toBe(2);
    }
  });

  it('surfaces a distinct not-found state on 404 (AC-INV-022)', () => {
    store.setProductId('p-1');
    TestBed.tick();
    http.expectOne((candidate) => candidate.url === '/api/v1/inventory/p-1/batches').flush(
      { type: 'about:blank', title: 'Not Found', status: 404 },
      { status: 404, statusText: 'Not Found' },
    );

    expect(store.view().kind).toBe('notFound');
  });

  it('an empty page with no filters is the none-empty state, with filters is the filters state', () => {
    store.setProductId('p-1');
    TestBed.tick();
    flushBatches(0);
    expect(store.emptyKind()).toBe('none');

    store.setFilters({ status: 'depleted', expired: false, expiringSoon: false });
    TestBed.tick();
    flushBatches(0);
    expect(store.emptyKind()).toBe('filters');
    expect(store.appliedChips().map((chip) => chip.key)).toEqual(['status']);
  });

  it('an API failure is the error state and retry re-requests', () => {
    store.setProductId('p-1');
    TestBed.tick();
    http.expectOne((candidate) => candidate.url === '/api/v1/inventory/p-1/batches').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    expect(store.view().kind).toBe('error');

    store.retry();
    TestBed.tick();
    flushBatches(1);
    expect(store.view().kind).toBe('ready');
  });
});
