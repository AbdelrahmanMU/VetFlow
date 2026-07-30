import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { BatchViewerRequest, BatchViewerResult } from './batch-viewer.models';

/** Data access of the batch viewer slice — explicit whitelisted parameters (BR-INV-026/027, STD-API-023). */
@Injectable()
export class BatchViewerApiService {
  private readonly api = inject(ApiClient);

  getBatches(request: BatchViewerRequest): Observable<BatchViewerResult> {
    const { filters } = request;
    return this.api.get<BatchViewerResult>(`/inventory/${request.productId}/batches`, {
      status: filters.status ?? undefined,
      expired: filters.expired || undefined,
      expiringSoon: filters.expiringSoon || undefined,
      sort: request.sort.field,
      dir: request.sort.direction,
      page: request.page,
      pageSize: request.pageSize,
    });
  }
}
