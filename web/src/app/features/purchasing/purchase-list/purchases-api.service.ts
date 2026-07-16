import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { PurchaseListItem, PurchaseListRequest } from './purchase-list.models';

/** Data access of the purchase list slice — explicit whitelisted parameters (BR-PUR-004, STD-API-023). */
@Injectable()
export class PurchasesApiService {
  private readonly api = inject(ApiClient);

  getPurchases(request: PurchaseListRequest): Observable<PagedResult<PurchaseListItem>> {
    const { filters } = request;
    return this.api.get<PagedResult<PurchaseListItem>>('/purchase-invoices', {
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
