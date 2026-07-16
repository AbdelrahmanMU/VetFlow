import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { ManufacturerListItem, ManufacturerListRequest } from './manufacturer-list.models';

/** Data access of the Manufacturers management feature — the list and its writes (REQ-CAT-013/047/048). */
@Injectable()
export class ManufacturersApiService {
  private readonly api = inject(ApiClient);

  list(request: ManufacturerListRequest): Observable<PagedResult<ManufacturerListItem>> {
    return this.api.get<PagedResult<ManufacturerListItem>>('/manufacturers', {
      search: request.search || undefined,
      sort: request.sort.field,
      dir: request.sort.direction,
      page: request.page,
      pageSize: request.pageSize,
    });
  }

  create(name: string): Observable<{ readonly id: string }> {
    return this.api.post<{ readonly id: string }>('/manufacturers', { name });
  }

  rename(id: string, name: string): Observable<void> {
    return this.api.put<void>(`/manufacturers/${id}`, { name });
  }

  activate(id: string): Observable<void> {
    return this.api.post<void>(`/manufacturers/${id}/activate`, {});
  }

  deactivate(id: string): Observable<void> {
    return this.api.post<void>(`/manufacturers/${id}/deactivate`, {});
  }
}
