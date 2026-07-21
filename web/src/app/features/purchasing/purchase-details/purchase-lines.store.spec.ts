import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { PurchaseLinesApiService } from './purchase-lines-api.service';
import { PurchaseLinesStore } from './purchase-lines.store';

/**
 * Purchase line-items store (REQ-PUR-004): the reactive list read and the add/remove
 * mutations that refresh the list from the server on success (no optimistic UI —
 * STD-FE-036), reporting success so the page can re-read the header total.
 */
describe('PurchaseLinesStore', () => {
  let store: PurchaseLinesStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), PurchaseLinesApiService, PurchaseLinesStore],
    });

    store = TestBed.inject(PurchaseLinesStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  const line = {
    id: 'l1',
    productId: 'p1',
    productName: 'أموكسيسيلين',
    purchaseUnitId: 'u1',
    purchaseUnitName: 'كرتونة',
    quantity: 3,
    unitPrice: { amount: 100, currency: 'EGP' },
    lineTotal: { amount: 300, currency: 'EGP' },
  };

  it('loads the lines when the invoice id is set', () => {
    store.setId('inv-1');
    TestBed.tick();

    http.expectOne('/api/v1/purchase-invoices/inv-1/lines').flush([line]);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.lines.length).toBe(1);
      expect(view.lines[0].productName).toBe('أموكسيسيلين');
    }
  });

  it('surfaces the error state on a transport failure', () => {
    store.setId('inv-1');
    TestBed.tick();

    http.expectOne('/api/v1/purchase-invoices/inv-1/lines').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(store.view().kind).toBe('error');
  });

  it('POSTs a line then refreshes the list from the server (AC-PUR-008)', () => {
    store.setId('inv-1');
    TestBed.tick();
    http.expectOne('/api/v1/purchase-invoices/inv-1/lines').flush([]);

    let reported: boolean | null = null;
    store.add({ productId: 'p1', purchaseUnitId: 'u1', quantity: 3, unitPrice: 100 }, (ok) => (reported = ok));

    const post = http.expectOne(
      (request) => request.method === 'POST' && request.url === '/api/v1/purchase-invoices/inv-1/lines',
    );
    expect(post.request.body).toEqual({ productId: 'p1', purchaseUnitId: 'u1', quantity: 3, unitPrice: 100 });
    post.flush({ lineId: 'l1' });

    // Success refreshes the list (no optimistic insert).
    TestBed.tick();
    http.expectOne('/api/v1/purchase-invoices/inv-1/lines').flush([line]);

    expect(reported).toBe(true);
    expect(store.saving()).toBe(false);
    const view = store.view();
    expect(view.kind === 'ready' && view.lines.length).toBe(1);
  });

  it('reports failure and does not refresh when the add fails', () => {
    store.setId('inv-1');
    TestBed.tick();
    http.expectOne('/api/v1/purchase-invoices/inv-1/lines').flush([]);

    let reported: boolean | null = null;
    store.add({ productId: 'p1', purchaseUnitId: 'u1', quantity: 0, unitPrice: 100 }, (ok) => (reported = ok));

    http.expectOne((request) => request.method === 'POST').flush(
      { type: 'about:blank', title: 'Bad Request', status: 400 },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(reported).toBe(false);
    expect(store.saving()).toBe(false);
    // No refresh GET is issued on failure (http.verify() in afterEach would flag a stray request).
  });

  it('DELETEs a line then refreshes the list (AC-PUR-010)', () => {
    store.setId('inv-1');
    TestBed.tick();
    http.expectOne('/api/v1/purchase-invoices/inv-1/lines').flush([line]);

    let reported: boolean | null = null;
    store.remove('l1', (ok) => (reported = ok));

    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/v1/purchase-invoices/inv-1/lines/l1')
      .flush(null);

    TestBed.tick();
    http.expectOne('/api/v1/purchase-invoices/inv-1/lines').flush([]);

    expect(reported).toBe(true);
    const view = store.view();
    expect(view.kind === 'ready' && view.lines.length).toBe(0);
  });
});
