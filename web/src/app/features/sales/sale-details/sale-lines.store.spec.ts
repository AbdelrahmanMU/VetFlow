import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ClassifiedFailure } from '../../../core/validation/api-error-mapper';
import { SaleLinesApiService } from './sale-lines-api.service';
import { SaleLinesStore } from './sale-lines.store';

/**
 * Sales line-items store (REQ-SAL-001/002/003): the reactive list read, the add/remove mutations
 * that refresh from the server on success (no optimistic UI — STD-FE-036), and the commit, whose
 * refusal is classified by error code only (STD-FE-037) so the page can name the products
 * (AC-SAL-009) or offer a retry (AC-SAL-012).
 */
describe('SaleLinesStore', () => {
  let store: SaleLinesStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SaleLinesApiService, SaleLinesStore],
    });

    store = TestBed.inject(SaleLinesStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  const line = {
    id: 'l1',
    productId: 'p1',
    productName: 'أموكسيسيلين',
    saleUnitId: 'u1',
    saleUnitName: 'شريط',
    quantity: 2,
    unitPrice: { amount: 45, currency: 'EGP' },
    lineTotal: { amount: 90, currency: 'EGP' },
  };

  function loadLines(lines: unknown[] = [line]): void {
    store.setId('si-1');
    TestBed.tick();
    http.expectOne('/api/v1/sales-invoices/si-1/lines').flush(lines);
  }

  /**
   * A refusal exactly as the API shapes it (ADR-0015 §3): a complete RFC 9457 body. The client
   * only recognises a problem that carries `title` and `status`, so a partial stub would be
   * classified as a transport failure and would not test the classification at all.
   */
  function problem(status: number, errorCode: string, metadata?: Record<string, string>) {
    return {
      type: 'about:blank',
      title: 'Business rule violated',
      status,
      errorCode,
      ...(metadata ? { metadata } : {}),
    };
  }

  it('loads the lines when the invoice id is set', () => {
    loadLines();

    const view = store.view();
    expect(view.kind).toBe('ready');
    if (view.kind === 'ready') {
      expect(view.lines.length).toBe(1);
      expect(view.lines[0].productName).toBe('أموكسيسيلين');
      expect(view.lines[0].saleUnitName).toBe('شريط');
    }
  });

  it('surfaces the error state on a transport failure', () => {
    store.setId('si-1');
    TestBed.tick();

    http.expectOne('/api/v1/sales-invoices/si-1/lines').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(store.view().kind).toBe('error');
  });

  it('POSTs a line without a price then refreshes from the server (AC-SAL-003, DEC-SAL-003)', () => {
    loadLines([]);

    const reports: (ClassifiedFailure | null)[] = [];
    store.add({ productId: 'p1', saleUnitId: 'u1', quantity: 2 }, (failure) => reports.push(failure));

    const post = http.expectOne(
      (request) => request.method === 'POST' && request.url === '/api/v1/sales-invoices/si-1/lines',
    );
    // The price is never client input — the server snapshots the catalog price (BR-SAL-006).
    expect(post.request.body).toEqual({ productId: 'p1', saleUnitId: 'u1', quantity: 2 });
    post.flush({ lineId: 'l1' });

    TestBed.tick();
    http.expectOne('/api/v1/sales-invoices/si-1/lines').flush([line]);

    expect(reports).toEqual([null]);
    expect(store.saving()).toBe(false);
    const view = store.view();
    expect(view.kind === 'ready' && view.lines.length).toBe(1);
  });

  it('reports failure and does not refresh when the add is rejected', () => {
    loadLines([]);

    const reports: (ClassifiedFailure | null)[] = [];
    store.add({ productId: 'p1', saleUnitId: 'u1', quantity: 0 }, (failure) => reports.push(failure));

    http.expectOne((request) => request.method === 'POST').flush(
      { type: 'about:blank', title: 'Bad Request', status: 400 },
      { status: 400, statusText: 'Bad Request' },
    );

    // The failure arrives classified (STD-UX-123) with the dialog's ruled fallback copy.
    expect(reports[0]?.kind).toBe('system');
    expect(reports[0]?.messageKey).toBe('saleDetails.lines.dialog.error');
    expect(store.saving()).toBe(false);
    // No refresh GET is issued on failure (http.verify() in afterEach would flag a stray request).
  });

  it('DELETEs a line then refreshes the list (AC-SAL-003)', () => {
    loadLines();

    const reports: (ClassifiedFailure | null)[] = [];
    store.remove('l1', (failure) => reports.push(failure));

    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/v1/sales-invoices/si-1/lines/l1')
      .flush(null);

    TestBed.tick();
    http.expectOne('/api/v1/sales-invoices/si-1/lines').flush([]);

    expect(reports).toEqual([null]);
    const view = store.view();
    expect(view.kind === 'ready' && view.lines.length).toBe(0);
  });

  it('commits with no body and reports no rejection on success (AC-SAL-007)', () => {
    loadLines();

    const reports: (ClassifiedFailure | null)[] = [];
    store.commit((failure) => reports.push(failure));

    const commit = http.expectOne(
      (request) => request.method === 'POST' && request.url === '/api/v1/sales-invoices/si-1/commit',
    );
    expect(commit.request.body).toEqual({});
    commit.flush(null);

    expect(reports).toEqual([null]);
    expect(store.saving()).toBe(false);
  });

  it('classifies insufficient stock and carries the products the server named (AC-SAL-009)', () => {
    loadLines();

    const reports: (ClassifiedFailure | null)[] = [];
    store.commit((failure) => reports.push(failure));

    http
      .expectOne('/api/v1/sales-invoices/si-1/commit')
      .flush(problem(409, 'VTF-INV-052', { products: 'أموكسيسيلين، سيفترياكسون' }), {
        status: 409,
        statusText: 'Conflict',
      });

    expect(reports[0]?.code).toBe('VTF-INV-052');
    expect(reports[0]?.params).toEqual({ products: 'أموكسيسيلين، سيفترياكسون' });
  });

  it('classifies a concurrency conflict so the page can offer a retry (AC-SAL-012)', () => {
    loadLines();

    const reports: (ClassifiedFailure | null)[] = [];
    store.commit((failure) => reports.push(failure));

    http
      .expectOne('/api/v1/sales-invoices/si-1/commit')
      .flush(problem(409, 'VTF-INV-056'), { status: 409, statusText: 'Conflict' });

    expect(reports[0]?.code).toBe('VTF-INV-056');
    expect(reports[0]?.retryable).toBe(true);
  });

  it('classifies an inexact conversion and names the offending line (AC-SAL-013)', () => {
    loadLines();

    const reports: (ClassifiedFailure | null)[] = [];
    store.commit((failure) => reports.push(failure));

    http
      .expectOne('/api/v1/sales-invoices/si-1/commit')
      .flush(problem(400, 'VTF-SAL-012', { reason: 'conversionNotExact', product: 'أموكسيسيلين' }), {
        status: 400,
        statusText: 'Bad Request',
      });

    expect(reports[0]?.code).toBe('VTF-SAL-012');
    expect(reports[0]?.params?.['product']).toBe('أموكسيسيلين');
  });

  it('falls back to a generic refusal for an unrecognised failure', () => {
    loadLines();

    const reports: (ClassifiedFailure | null)[] = [];
    store.commit((failure) => reports.push(failure));

    http
      .expectOne('/api/v1/sales-invoices/si-1/commit')
      .flush(
        { type: 'about:blank', title: 'Internal Server Error', status: 500 },
        { status: 500, statusText: 'Internal Server Error' },
      );

    expect(reports[0]?.kind).toBe('system');
    expect(reports[0]?.messageKey).toBe('saleDetails.commit.error.other');
    expect(store.saving()).toBe(false);
  });

  it('keeps the lines untouched when a commit is refused (BR-SAL-012)', () => {
    loadLines();

    store.commit(() => undefined);
    http
      .expectOne('/api/v1/sales-invoices/si-1/commit')
      .flush(problem(409, 'VTF-INV-052', { products: 'أموكسيسيلين' }), { status: 409, statusText: 'Conflict' });

    // A refusal changes nothing: no refresh, and the draft still holds every line.
    const view = store.view();
    expect(view.kind === 'ready' && view.lines.length).toBe(1);
  });
});
