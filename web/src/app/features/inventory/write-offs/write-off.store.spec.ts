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

  it('posts a positive quantity and no direction — a write-off only removes', () => {
    store.save(payload);

    const request = expectWriteOff();
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    expect('direction' in request.request.body).toBe(false);
    request.flush({ movementId: 'mv-9' });

    expect(store.submit()).toEqual({ kind: 'saved', movementId: 'mv-9' });
  });

  it('classifies a write-off beyond the batch (BR-INV-061)', () => {
    store.save(payload);
    expectWriteOff().flush(
      { errorCode: 'VTF-INV-061', status: 409, title: 'x', type: 'y' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(store.submit()).toEqual({ kind: 'failed', failure: 'belowZero' });
  });

  it('classifies an adjustment-only reason as a reason failure (DEC-INV-031)', () => {
    store.save({ ...payload, reason: 'other' });
    expectWriteOff().flush(
      { errorCode: 'VTF-INV-067', status: 400, title: 'x', type: 'y' },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(store.submit()).toEqual({ kind: 'failed', failure: 'reason' });
  });

  it('offers expired batches for selection — that is the point of R9', () => {
    store.loadBatches('prod-1');
    http
      .expectOne((candidate) => candidate.url === '/api/v1/inventory/prod-1/batches')
      .flush({
        productName: 'منتج',
        stockUnitName: 'شريط',
        batches: {
          items: [{ batchId: 'b-1', remainingQuantity: 8, stockUnitName: 'شريط', expiryDate: '2020-01-01' }],
          page: 1,
          pageSize: 100,
          totalCount: 1,
        },
      });

    expect(store.batches().length).toBe(1);
    expect(store.batches()[0].expiryDate).toBe('2020-01-01');
  });
});
