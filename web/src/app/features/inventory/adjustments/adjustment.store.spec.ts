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

  it('classifies the below-zero rejection so the page can say what happened (BR-INV-061)', () => {
    store.save(payload);
    expectAdjust().flush(
      { errorCode: 'VTF-INV-061', status: 409, title: 'x', type: 'y' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(store.submit()).toEqual({ kind: 'failed', failure: 'belowZero' });
  });

  it('classifies a concurrency conflict as retryable (BR-INV-068)', () => {
    store.save(payload);
    expectAdjust().flush(
      { errorCode: 'VTF-INV-068', status: 409, title: 'x', type: 'y' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(store.submit()).toEqual({ kind: 'failed', failure: 'conflict' });
  });

  it('classifies a reason outside the adjustment list (BR-INV-067)', () => {
    store.save(payload);
    expectAdjust().flush(
      { errorCode: 'VTF-INV-067', status: 400, title: 'x', type: 'y' },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(store.submit()).toEqual({ kind: 'failed', failure: 'reason' });
  });

  it('classifies a missing batch as not found', () => {
    store.save(payload);
    expectAdjust().flush({}, { status: 404, statusText: 'Not Found' });

    expect(store.submit()).toEqual({ kind: 'failed', failure: 'notFound' });
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
});
