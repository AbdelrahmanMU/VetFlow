import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdjustmentApiService } from '../adjustments/adjustment-api.service';
import { WriteOffPayload } from './write-off.models';
import { WriteOffStore } from './write-off.store';

describe('WriteOffStore', () => {
  let store: WriteOffStore;
  let http: HttpTestingController;

  const payload: WriteOffPayload = {
    batchId: 'batch-1',
    quantity: 8,
    reason: 'expired',
    reasonNote: null,
    actorName: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AdjustmentApiService, WriteOffStore],
    });

    store = TestBed.inject(WriteOffStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function expectWriteOff() {
    return http.expectOne((candidate) => candidate.url === '/api/v1/inventory/write-offs');
  }

  function failedFailure() {
    const state = store.submit();
    if (state.kind !== 'failed') {
      throw new Error(`expected a failed state, got ${state.kind}`);
    }

    return state.failure;
  }

  it('posts the payload with a positive magnitude — the removal sign is the server’s (BR-INV-064)', () => {
    store.save(payload);

    const request = expectWriteOff();
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    expect(request.request.body.quantity).toBeGreaterThan(0);
    request.flush({ movementId: 'mv-1' });

    expect(store.submit()).toEqual({ kind: 'saved', movementId: 'mv-1' });
  });

  it('classifies the below-zero rejection with the write-off wording (BR-INV-061, STD-UX-123)', () => {
    store.save(payload);
    expectWriteOff().flush(
      { errorCode: 'VTF-INV-061', status: 409, title: 'x', type: 'y' },
      { status: 409, statusText: 'Conflict' },
    );

    const failure = failedFailure();
    expect(failure.code).toBe('VTF-INV-061');
    expect(failure.messageKey).toBe('writeOff.error.belowZero');
    expect(failure.retryable).toBe(false);
  });

  it('classifies a concurrency conflict as retryable with the screen’s own key (BR-INV-068, AP-16)', () => {
    store.save(payload);
    expectWriteOff().flush(
      { errorCode: 'VTF-INV-068', status: 409, title: 'x', type: 'y' },
      { status: 409, statusText: 'Conflict' },
    );

    const failure = failedFailure();
    expect(failure.messageKey).toBe('writeOff.error.conflict');
    expect(failure.retryable).toBe(true);
  });

  it('classifies a reason outside the write-off list (BR-INV-067, DEC-INV-031)', () => {
    store.save(payload);
    expectWriteOff().flush(
      { errorCode: 'VTF-INV-067', status: 400, title: 'x', type: 'y' },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(failedFailure().messageKey).toBe('writeOff.error.reason');
  });

  it('classifies a missing batch as not found with the screen’s own key (AP-16)', () => {
    store.save(payload);
    expectWriteOff().flush({}, { status: 404, statusText: 'Not Found' });

    const failure = failedFailure();
    expect(failure.kind).toBe('notFound');
    expect(failure.messageKey).toBe('writeOff.error.notFound');
  });

  it('a failed products load surfaces the error channel — never a silent empty list (STD-UX-041)', () => {
    store.loadProducts();
    http
      .expectOne((candidate) => candidate.url === '/api/v1/products')
      .flush({}, { status: 500, statusText: 'Internal Server Error' });

    expect(store.products()).toEqual([]);
    expect(store.productsError()).toBe(true);

    // The retry clears the flag and re-issues the load.
    store.loadProducts();
    expect(store.productsError()).toBe(false);
    http
      .expectOne((candidate) => candidate.url === '/api/v1/products')
      .flush({ items: [], page: 1, pageSize: 100, totalCount: 0 });
  });

  it('a failed batches load surfaces the error channel with the loading state cleared (STD-UX-041)', () => {
    store.loadBatches('prod-1');
    http
      .expectOne((candidate) => candidate.url === '/api/v1/inventory/prod-1/batches')
      .flush({}, { status: 500, statusText: 'Internal Server Error' });

    expect(store.batches()).toEqual([]);
    expect(store.batchesLoading()).toBe(false);
    expect(store.batchesError()).toBe(true);
  });
});
