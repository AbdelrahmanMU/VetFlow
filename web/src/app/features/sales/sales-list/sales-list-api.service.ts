import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { SalesListItem, SalesListRequest } from './sales-list.models';

/** Data access of the sales list (BR-SAL-019, STD-API-023) — explicit whitelisted parameters. */
@Injectable()
export class SalesListApiService {
  private readonly api = inject(ApiClient);

  getSales(request: SalesListRequest): Observable<PagedResult<SalesListItem>> {
    const { filters } = request;
    return this.api.get<PagedResult<SalesListItem>>('/sales-invoices', {
      search: request.search || undefined,
      status: filters.status ?? undefined,
      dateFrom: filters.dateFrom ?? undefined,
      dateTo: filters.dateTo ?? undefined,
      sort: request.sort.field,
      dir: request.sort.direction,
      page: request.page,
      pageSize: request.pageSize,
    });
  }
}
