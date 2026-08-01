import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { PurchaseReturnApiService } from './purchase-return-api.service';
import { PurchaseReturnStore } from './purchase-return.store';

describe('PurchaseReturnStore', () => {
  let store: PurchaseReturnStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        PurchaseReturnApiService,
        PurchaseReturnStore,
      ],
    });

    store = TestBed.inject(PurchaseReturnStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function quantities(entries: readonly (readonly [string, number])[]): ReadonlyMap<string, number> {
    return new Map(entries);
  }

  it('marks the invoice unavailable when the returnable read 404s (BR-PUR-015)', () => {
    store.loadLines('invoice-1');

    http
      .expectOne((candidate) => candidate.url === '/api/v1/purchase-invoices/invoice-1/returnable-lines')
      .flush(null, { status: 404, statusText: 'Not Found' });

    expect(store.unavailable()).toBe(true);
    expect(store.lines()).toEqual([]);
  });

  it('creates the draft, adds every chosen line, then commits (BR-PUR-018)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 3], ['line-b', 2]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/purchase-returns')
      .flush({ id: 'ret-1', number: 'PRT-000001' });

    const first = http.expectOne((candidate) => candidate.url === '/api/v1/purchase-returns/ret-1/lines');
    expect(first.request.body).toEqual({ purchaseLineItemId: 'line-a', quantity: 3 });
    first.flush({ id: 'rl-1' });

    const second = http.expectOne((candidate) => candidate.url === '/api/v1/purchase-returns/ret-1/lines');
    expect(second.request.body).toEqual({ purchaseLineItemId: 'line-b', quantity: 2 });
    second.flush({ id: 'rl-2' });

    http.expectOne((candidate) => candidate.url === '/api/v1/purchase-returns/ret-1/commit').flush(null);

    expect(store.submit()).toEqual({ kind: 'saved', number: 'PRT-000001' });
  });

  it('never sends a reason or an amount — a return has neither (BR-INV-067, DEC-INV-035)', () => {
    store.save('invoice-1', '2026-07-31', 'ملاحظة', quantities([['line-a', 1]]));

    const created = http.expectOne((candidate) => candidate.url === '/api/v1/purchase-returns');
    expect(created.request.body).toEqual({
      purchaseInvoiceId: 'invoice-1',
      returnDate: '2026-07-31',
      notes: 'ملاحظة',
    });
    created.flush({ id: 'ret-1', number: 'PRT-000001' });

    const line = http.expectOne((candidate) => candidate.url === '/api/v1/purchase-returns/ret-1/lines');
    // Only the original line and the quantity — no reason, no batch, no price.
    expect(Object.keys(line.request.body as object).sort()).toEqual(['purchaseLineItemId', 'quantity']);
    line.flush({ id: 'rl-1' });

    http.expectOne((candidate) => candidate.url === '/api/v1/purchase-returns/ret-1/commit').flush(null);
  });

  it('skips lines with no quantity and refuses an empty return without calling the API', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 0]]));

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.messageKey).toBe('purchaseReturn.error.noLines');
      expect(state.draftCreated).toBe(false);
    }
    http.expectNone(() => true);
  });

  it('classifies the over-return rejection by its code (BR-PUR-016)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 99]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/purchase-returns')
      .flush({ id: 'ret-1', number: 'PRT-000001' });

    http
      .expectOne((candidate) => candidate.url === '/api/v1/purchase-returns/ret-1/lines')
      .flush(
        { errorCode: 'VTF-PUR-016', status: 409, title: 'x', type: 'y' },
        { status: 409, statusText: 'Conflict' },
      );

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.code).toBe('VTF-PUR-016');
      expect(state.failure.messageKey).toBe('purchaseReturn.error.exceedsReturnable');
      // The draft was created before the rejection — the page states the
      // partial document state (STD-UX-042).
      expect(state.draftCreated).toBe(true);
    }
  });

  it('classifies the batch floor rejection, which is also the over-return guard (BR-INV-061)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 5]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/purchase-returns')
      .flush({ id: 'ret-1', number: 'PRT-000001' });
    http
      .expectOne((candidate) => candidate.url === '/api/v1/purchase-returns/ret-1/lines')
      .flush({ id: 'rl-1' });
    http
      .expectOne((candidate) => candidate.url === '/api/v1/purchase-returns/ret-1/commit')
      .flush(
        { errorCode: 'VTF-INV-061', status: 409, title: 'x', type: 'y' },
        { status: 409, statusText: 'Conflict' },
      );

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.messageKey).toBe('purchaseReturn.error.belowZero');
      expect(state.draftCreated).toBe(true);
    }
  });

  it('classifies a return against a non-received invoice by its code (BR-PUR-015)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 1]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/purchase-returns')
      .flush(
        { errorCode: 'VTF-PUR-015', status: 409, title: 'x', type: 'y' },
        { status: 409, statusText: 'Conflict' },
      );

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.messageKey).toBe('purchaseReturn.error.invoiceNotReceived');
      // The very first step failed — no draft exists and nothing needs stating.
      expect(state.draftCreated).toBe(false);
    }
  });
});
