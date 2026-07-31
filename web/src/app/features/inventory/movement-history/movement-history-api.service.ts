import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { MovementHistoryItem, MovementHistoryRequest } from './movement-history.models';

/**
 * Data access of the inventory movement history (REQ-INV-005) — pagination only. There is no
 * search, no filter and no sort parameter: the list is an unfiltered chronological one in this
 * slice (BR-INV-044), and it is read-only, so this service has no write method (BR-INV-039).
 */
@Injectable()
export class MovementHistoryApiService {
  private readonly api = inject(ApiClient);

  getMovements(request: MovementHistoryRequest): Observable<PagedResult<MovementHistoryItem>> {
    return this.api.get<PagedResult<MovementHistoryItem>>('/inventory/movements', {
      page: request.page,
      pageSize: request.pageSize,
    });
  }
}
