import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { CategoryOption, InventoryItem, InventoryListRequest } from './inventory-list.models';

/** Data access of the inventory projection slice — explicit whitelisted parameters (BR-INV-014, STD-API-023). */
@Injectable()
export class InventoryApiService {
  private static readonly LookupPageSize = 100;

  private readonly api = inject(ApiClient);

  getInventory(request: InventoryListRequest): Observable<PagedResult<InventoryItem>> {
    const { filters } = request;
    return this.api.get<PagedResult<InventoryItem>>('/inventory', {
      search: request.search || undefined,
      category: filters.category?.id,
      expiringSoon: filters.expiringSoon || undefined,
      outOfStock: filters.outOfStock || undefined,
      sort: request.sort.field,
      dir: request.sort.direction,
      page: request.page,
      pageSize: request.pageSize,
    });
  }

  /** Category options for the filter — reuses the Catalog categories list (DEC-INV-006). */
  getCategoryOptions(): Observable<PagedResult<CategoryOption>> {
    return this.api.get<PagedResult<CategoryOption>>('/categories', {
      pageSize: InventoryApiService.LookupPageSize,
    });
  }
}
