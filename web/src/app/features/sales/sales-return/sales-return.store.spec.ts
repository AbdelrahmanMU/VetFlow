import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { SalesReturnApiService } from './sales-return-api.service';
import { SalesReturnStore } from './sales-return.store';

describe('SalesReturnStore', () => {
  let store: SalesReturnStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        SalesReturnApiService,
        SalesReturnStore,
      ],
    });

    store = TestBed.inject(SalesReturnStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function quantities(entries: readonly (readonly [string, number])[]): ReadonlyMap<string, number> {
    return new Map(entries);
  }

  it('marks the invoice unavailable when the returnable read 404s (BR-SAL-015)', () => {
    store.loadLines('invoice-1');

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-invoices/invoice-1/returnable-lines')
      .flush(null, { status: 404, statusText: 'Not Found' });

    expect(store.unavailable()).toBe(true);
    expect(store.lines()).toEqual([]);
  });

  it('creates the draft, adds every chosen line, then commits (BR-SAL-018)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 3], ['line-b', 2]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns')
      .flush({ id: 'ret-1', number: 'SRT-000001' });

    const first = http.expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/lines');
    expect(first.request.body).toEqual({ salesLineItemId: 'line-a', quantity: 3 });
    first.flush({ id: 'rl-1' });

    const second = http.expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/lines');
    expect(second.request.body).toEqual({ salesLineItemId: 'line-b', quantity: 2 });
    second.flush({ id: 'rl-2' });

    http.expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/commit').flush(null);

    expect(store.submit()).toEqual({ kind: 'saved', number: 'SRT-000001' });
  });

  it('never sends a reason, a batch or an amount — a return has none of them (BR-INV-067, BR-SAL-013, DEC-INV-035)', () => {
    store.save('invoice-1', '2026-07-31', 'ملاحظة', quantities([['line-a', 1]]));

    const created = http.expectOne((candidate) => candidate.url === '/api/v1/sales-returns');
    expect(created.request.body).toEqual({
      salesInvoiceId: 'invoice-1',
      returnDate: '2026-07-31',
      notes: 'ملاحظة',
    });
    created.flush({ id: 'ret-1', number: 'SRT-000001' });

    const line = http.expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/lines');
    // Only the original line and the quantity — no reason, no batch, no price. The batch is the
    // sharp one here: the destinations are read from the consumption trace server-side, and Sales
    // may not hold a batch reference at all.
    expect(Object.keys(line.request.body as object).sort()).toEqual(['quantity', 'salesLineItemId']);
    line.flush({ id: 'rl-1' });

    http.expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/commit').flush(null);
  });

  it('skips lines with no quantity and refuses an empty return without calling the API', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 0]]));

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.messageKey).toBe('salesReturn.error.noLines');
      expect(state.draftCreated).toBe(false);
    }
    http.expectNone(() => true);
  });

  it('classifies the over-return rejection by its code (BR-SAL-016)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 99]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns')
      .flush({ id: 'ret-1', number: 'SRT-000001' });

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/lines')
      .flush(
        { errorCode: 'VTF-SAL-016', status: 409, title: 'x', type: 'y' },
        { status: 409, statusText: 'Conflict' },
      );

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.code).toBe('VTF-SAL-016');
      expect(state.failure.messageKey).toBe('salesReturn.error.exceedsReturnable');
      // The draft was created before the rejection — the page states the
      // partial document state (STD-UX-042).
      expect(state.draftCreated).toBe(true);
    }
  });

  it('classifies a fractional return of an indivisible product by its code (BR-SAL-016, DEC-SAL-007)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 2.5]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns')
      .flush({ id: 'ret-1', number: 'SRT-000001' });

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/lines')
      .flush(
        { errorCode: 'VTF-SAL-017', status: 400, title: 'x', type: 'y' },
        { status: 400, statusText: 'Bad Request' },
      );

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.messageKey).toBe('salesReturn.error.lineComposition');
      expect(state.draftCreated).toBe(true);
    }
  });

  it('classifies an unusable consumption trace by its code (BR-SAL-017)', () => {
    // The destination cannot be known, so nothing is put back — the user is told the return did
    // not happen rather than being shown a success that moved a guessed quantity.
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 1]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns')
      .flush({ id: 'ret-1', number: 'SRT-000001' });
    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/lines')
      .flush({ id: 'rl-1' });
    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/commit')
      .flush(
        { errorCode: 'VTF-SAL-020', status: 409, title: 'x', type: 'y' },
        { status: 409, statusText: 'Conflict' },
      );

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.messageKey).toBe('salesReturn.error.traceUnusable');
      expect(state.draftCreated).toBe(true);
    }
  });

  it('classifies a concurrent batch change as retryable (BR-INV-068)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 5]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns')
      .flush({ id: 'ret-1', number: 'SRT-000001' });
    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/lines')
      .flush({ id: 'rl-1' });
    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/commit')
      .flush(
        { errorCode: 'VTF-INV-068', status: 409, title: 'x', type: 'y' },
        { status: 409, statusText: 'Conflict' },
      );

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.messageKey).toBe('salesReturn.error.conflict');
      // The concurrency conflict is retryable (STD-UX-033, DEC-INV-023).
      expect(state.failure.retryable).toBe(true);
      expect(state.draftCreated).toBe(true);
    }
  });

  it('classifies a return against a draft invoice by its code (BR-SAL-015)', () => {
    store.save('invoice-1', '2026-07-31', null, quantities([['line-a', 1]]));

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns')
      .flush(
        { errorCode: 'VTF-SAL-015', status: 409, title: 'x', type: 'y' },
        { status: 409, statusText: 'Conflict' },
      );

    const state = store.submit();
    expect(state.kind).toBe('failed');
    if (state.kind === 'failed') {
      expect(state.failure.messageKey).toBe('salesReturn.error.invoiceNotCommitted');
      // The very first step failed — no draft exists and nothing needs stating.
      expect(state.draftCreated).toBe(false);
    }
  });
});
