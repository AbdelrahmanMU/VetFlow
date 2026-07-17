import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { PurchaseDetailsApiService } from './purchase-details-api.service';
import { PurchaseDetailsStore } from './purchase-details.store';

describe('PurchaseDetailsStore', () => {
  let store: PurchaseDetailsStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        PurchaseDetailsApiService,
        PurchaseDetailsStore,
      ],
    });

    store = TestBed.inject(PurchaseDetailsStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  const invoice = {
    id: 'pi-1',
    number: 'PUR-000001',
    supplierName: 'مورد الإمداد',
    supplierInvoiceReference: 'S-42',
    invoiceDate: '2026-05-20',
    status: 'draft',
    total: { amount: 4250.75, currency: 'EGP' },
    notes: 'ملاحظة',
    createdAt: '2026-05-20T09:00:00Z',
  };

  it('starts loading and becomes ready when the invoice is found', () => {
    store.setId('pi-1');
    TestBed.tick();

    http.expectOne('/api/v1/purchase-invoices/pi-1').flush(invoice);

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.invoice.number).toBe('PUR-000001');
      expect(view.invoice.supplierName).toBe('مورد الإمداد');
      expect(view.invoice.total.amount).toBe(4250.75);
    }
  });

  it('surfaces a distinct not-found state on 404 (AC-PUR-005)', () => {
    store.setId('missing');
    TestBed.tick();

    http.expectOne('/api/v1/purchase-invoices/missing').flush(
      { type: 'about:blank', title: 'Not Found', status: 404 },
      { status: 404, statusText: 'Not Found' },
    );

    expect(store.view().kind).toBe('notFound');
  });

  it('surfaces the error state on a transport failure', () => {
    store.setId('pi-1');
    TestBed.tick();

    http.expectOne('/api/v1/purchase-invoices/pi-1').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(store.view().kind).toBe('error');
  });
});
