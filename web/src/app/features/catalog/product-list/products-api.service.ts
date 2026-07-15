import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { LookupOption, ProductListItem, ProductListRequest } from './product-list.models';

/** Data access of the product list slice — explicit whitelisted parameters (STD-API-023). */
@Injectable()
export class ProductsApiService {
  private static readonly LookupPageSize = 100;

  private readonly api = inject(ApiClient);

  getProducts(request: ProductListRequest): Observable<PagedResult<ProductListItem>> {
    const { filters } = request;
    return this.api.get<PagedResult<ProductListItem>>('/products', {
      search: request.search || undefined,
      categoryId: filters.category?.id,
      manufacturerId: filters.manufacturer?.id,
      natureId: filters.nature?.id,
      status: filters.status ?? undefined,
      refrigerated: filters.refrigerated ?? undefined,
      hasExpiration: filters.hasExpiration ?? undefined,
      splittable: filters.splittable ?? undefined,
      hasSellingPrice: filters.hasSellingPrice ?? undefined,
      sort: request.sort.field,
      dir: request.sort.direction,
      page: request.page,
      pageSize: request.pageSize,
    });
  }

  getCategoryOptions(): Observable<PagedResult<LookupOption>> {
    return this.api.get<PagedResult<LookupOption>>('/categories', {
      pageSize: ProductsApiService.LookupPageSize,
    });
  }

  getManufacturerOptions(): Observable<PagedResult<LookupOption>> {
    return this.api.get<PagedResult<LookupOption>>('/manufacturers', {
      pageSize: ProductsApiService.LookupPageSize,
    });
  }

  getNatureOptions(): Observable<PagedResult<LookupOption>> {
    return this.api.get<PagedResult<LookupOption>>('/product-natures', {
      pageSize: ProductsApiService.LookupPageSize,
    });
  }
}
