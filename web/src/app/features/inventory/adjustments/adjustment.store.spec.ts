import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdjustmentApiService } from './adjustment-api.service';
import { AdjustmentPayload } from './adjustment.models';
import { AdjustmentStore } from './adjustment.store';

describe('AdjustmentStore', () => {
  let store: AdjustmentStore;
  let http: HttpTestingController;

  const payload: AdjustmentPayload = {
    batchId: 'batch-1',
    direction: 'decrease',
    quantity: 4,
    reason: 'lost',
    reasonNote: null,
    actorName: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AdjustmentApiService, AdjustmentStore],
    });

    store = TestBed.inject(AdjustmentStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function expectAdjust() {
    return http.expectOne((candidate) => candidate.url === '/api/v1/inventory/adjustments');
  }

  it('posts the payload with the direction and a positive magnitude (BR-INV-064)', () => {
    store.save(payload);

    const request = expectAdjust();
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    // The magnitude is never signed on the wire — the server owns the sign.
    expect(request.request.body.quantity).toBeGreaterThan(0);
    request.flush({ movementId: 'mv-1' });

    expect(store.submit()).toEqual({ kind: 'saved', movementId: 'mv-1' });
  });

  function failedFailure() {
    const state = store.submit();
    if (state.kind !== 'failed') {
      throw new Error(`expected a failed state, got ${state.kind}`);
    }

    return state.failure;
  }

  it('classifies the below-zero rejection with its ruled wording (BR-INV-061, STD-UX-123)', () => {
    store.save(payload);
    expectAdjust().flush(
      { errorCode: 'VTF-INV-061', status: 409, title: 'x', type: 'y' },
      { status: 409, statusText: 'Conflict' },
    );

    const failure = failedFailure();
    expect(failure.code).toBe('VTF-INV-061');
    expect(failure.messageKey).toBe('adjustment.error.belowZero');
    expect(failure.retryable).toBe(false);
  });

  it('classifies a concurrency conflict as retryable (BR-INV-068, STD-UX-033)', () => {
    store.save(payload);
    expectAdjust().flush(
      { errorCode: 'VTF-INV-068', status: 409, title: 'x', type: 'y' },
      { status: 409, statusText: 'Conflict' },
    );

    const failure = failedFailure();
    expect(failure.messageKey).toBe('adjustment.error.conflict');
    expect(failure.retryable).toBe(true);
  });

  it('classifies a reason outside the adjustment list (BR-INV-067)', () => {
    store.save(payload);
    expectAdjust().flush(
      { errorCode: 'VTF-INV-067', status: 400, title: 'x', type: 'y' },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(failedFailure().messageKey).toBe('adjustment.error.reason');
  });

  it('classifies a missing batch as not found', () => {
    store.save(payload);
    expectAdjust().flush({}, { status: 404, statusText: 'Not Found' });

    const failure = failedFailure();
    expect(failure.kind).toBe('notFound');
    expect(failure.messageKey).toBe('adjustment.error.notFound');
  });

  it('loads the batches of the chosen product and clears them when it is cleared', () => {
    store.loadBatches('prod-1');
    http
      .expectOne((candidate) => candidate.url === '/api/v1/inventory/prod-1/batches')
      .flush({
        productName: 'منتج',
        stockUnitName: 'شريط',
        batches: {
          items: [
            { batchId: 'b-1', remainingQuantity: 10, stockUnitName: 'شريط', expiryDate: null },
            // A depleted batch is offered too: an adjustment can add back to it (BR-INV-019).
            { batchId: 'b-2', remainingQuantity: 0, stockUnitName: 'شريط', expiryDate: '2026-09-01' },
          ],
          page: 1,
          pageSize: 100,
          totalCount: 2,
        },
      });

    expect(store.batches().length).toBe(2);
    expect(store.batchesLoading()).toBe(false);

    store.loadBatches(null);
    expect(store.batches()).toEqual([]);
  });

  it('a failed picker load surfaces its error channel — never a silent empty list (STD-UX-041)', () => {
    store.loadProducts();
    http
      .expectOne((candidate) => candidate.url === '/api/v1/products')
      .flush({}, { status: 500, statusText: 'Internal Server Error' });
    expect(store.products()).toEqual([]);
    expect(store.productsError()).toBe(true);

    store.loadBatches('prod-1');
    http
      .expectOne((candidate) => candidate.url === '/api/v1/inventory/prod-1/batches')
      .flush({}, { status: 500, statusText: 'Internal Server Error' });
    expect(store.batchesError()).toBe(true);
    expect(store.batchesLoading()).toBe(false);
  });
});
