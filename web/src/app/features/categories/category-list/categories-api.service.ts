import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { CategoryListItem, CategoryListRequest } from './category-list.models';

/** Data access of the Categories module — the management list and its writes (REQ-CTG-001..004). */
@Injectable()
export class CategoriesApiService {
  private readonly api = inject(ApiClient);

  list(request: CategoryListRequest): Observable<PagedResult<CategoryListItem>> {
    return this.api.get<PagedResult<CategoryListItem>>('/categories', {
      search: request.search || undefined,
      sort: request.sort.field,
      dir: request.sort.direction,
      page: request.page,
      pageSize: request.pageSize,
    });
  }

  create(name: string): Observable<{ readonly id: string }> {
    return this.api.post<{ readonly id: string }>('/categories', { name });
  }

  rename(id: string, name: string): Observable<void> {
    return this.api.put<void>(`/categories/${id}`, { name });
  }

  activate(id: string): Observable<void> {
    return this.api.post<void>(`/categories/${id}/activate`, {});
  }

  deactivate(id: string): Observable<void> {
    return this.api.post<void>(`/categories/${id}/deactivate`, {});
  }
}
