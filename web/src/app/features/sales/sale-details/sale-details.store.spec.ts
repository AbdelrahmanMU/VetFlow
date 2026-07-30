import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { SaleDetailsApiService } from './sale-details-api.service';
import { SaleDetailsStore } from './sale-details.store';

/**
 * Sale-details store (REQ-SAL-002, AC-SAL-006): the four data-view states, with a missing
 * invoice surfaced as its own not-found state — distinct from a transport error.
 */
describe('SaleDetailsStore', () => {
  let store: SaleDetailsStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SaleDetailsApiService, SaleDetailsStore],
    });

    store = TestBed.inject(SaleDetailsStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  const invoice = {
    id: 'si-1',
    number: 'SAL-000001',
    customerName: 'عيادة النور',
    saleDate: '2026-07-30',
    status: 'draft',
    total: { amount: 320.5, currency: 'EGP' },
    notes: 'ملاحظة',
    createdAt: '2026-07-30T09:00:00Z',
  };

  it('starts loading and becomes ready when the invoice is found', () => {
    store.setId('si-1');
    TestBed.tick();

    http.expectOne('/api/v1/sales-invoices/si-1').flush(invoice);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.invoice.number).toBe('SAL-000001');
      expect(view.invoice.status).toBe('draft');
      expect(view.invoice.total.amount).toBe(320.5);
    }
  });

  it('keeps the optional customer as null so the screen can render «—» (DEC-SAL-002)', () => {
    store.setId('si-1');
    TestBed.tick();

    http.expectOne('/api/v1/sales-invoices/si-1').flush({ ...invoice, customerName: null, notes: null });

    const view = store.view();
    expect(view.kind === 'ready' && view.invoice.customerName).toBeNull();
  });

  it('surfaces a distinct not-found state on 404 (AC-SAL-006)', () => {
    store.setId('missing');
    TestBed.tick();

    http.expectOne('/api/v1/sales-invoices/missing').flush(
      { type: 'about:blank', title: 'Not Found', status: 404 },
      { status: 404, statusText: 'Not Found' },
    );

    expect(store.view().kind).toBe('notFound');
  });

  it('surfaces the error state on a transport failure', () => {
    store.setId('si-1');
    TestBed.tick();

    http.expectOne('/api/v1/sales-invoices/si-1').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(store.view().kind).toBe('error');
  });

  it('re-reads the invoice on retry, so a commit shows the new status (BR-SAL-011)', () => {
    store.setId('si-1');
    TestBed.tick();
    http.expectOne('/api/v1/sales-invoices/si-1').flush(invoice);

    store.retry();
    TestBed.tick();
    http.expectOne('/api/v1/sales-invoices/si-1').flush({ ...invoice, status: 'committed' });

    const view = store.view();
    expect(view.kind === 'ready' && view.invoice.status).toBe('committed');
  });
});
