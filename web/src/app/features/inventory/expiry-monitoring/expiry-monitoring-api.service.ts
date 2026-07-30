import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { CategoryOption, ExpiryMonitoringItem, ExpiryMonitoringRequest } from './expiry-monitoring.models';

/** Data access of the expiry monitoring slice — explicit whitelisted parameters (BR-INV-035, STD-API-023). */
@Injectable()
export class ExpiryMonitoringApiService {
  private static readonly LookupPageSize = 100;

  private readonly api = inject(ApiClient);

  getExpiring(request: ExpiryMonitoringRequest): Observable<PagedResult<ExpiryMonitoringItem>> {
    const { filters } = request;
    return this.api.get<PagedResult<ExpiryMonitoringItem>>('/inventory/expiry', {
      search: request.search || undefined,
      category: filters.category?.id,
      expired: filters.expired || undefined,
      expiringSoon: filters.expiringSoon || undefined,
      page: request.page,
      pageSize: request.pageSize,
    });
  }

  /** Category options for the filter — reuses the Catalog categories list. */
  getCategoryOptions(): Observable<PagedResult<CategoryOption>> {
    return this.api.get<PagedResult<CategoryOption>>('/categories', {
      pageSize: ExpiryMonitoringApiService.LookupPageSize,
    });
  }
}
