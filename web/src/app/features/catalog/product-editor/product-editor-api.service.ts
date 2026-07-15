import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import {
  CreateProductPayload,
  CreatedProduct,
  EditProduct,
  LookupOption,
  PossibleDuplicate,
  UpdateProductPayload,
} from './product-editor.models';

/** Data access of the product editor: the create/edit writes and their supporting reads. */
@Injectable()
export class ProductEditorApiService {
  private static readonly LookupPageSize = 100;

  private readonly api = inject(ApiClient);

  create(payload: CreateProductPayload): Observable<CreatedProduct> {
    return this.api.post<CreatedProduct>('/products', payload);
  }

  /** Loads a product for editing (GET /products/{id}); 404 surfaces as ApiError. */
  load(id: string): Observable<EditProduct> {
    return this.api.get<EditProduct>(`/products/${id}`);
  }

  /** Non-audited unified edit (PUT /products/{id}); 204 on success, 404 if gone. */
  update(id: string, payload: UpdateProductPayload): Observable<void> {
    return this.api.put<void>(`/products/${id}`, payload);
  }

  possibleDuplicates(arabicName: string, manufacturerId: string): Observable<PagedResult<PossibleDuplicate>> {
    return this.api.get<PagedResult<PossibleDuplicate>>('/products/possible-duplicates', {
      arabicName,
      manufacturerId,
    });
  }

  categoryOptions(): Observable<PagedResult<LookupOption>> {
    return this.api.get<PagedResult<LookupOption>>('/categories', {
      pageSize: ProductEditorApiService.LookupPageSize,
    });
  }

  manufacturerOptions(): Observable<PagedResult<LookupOption>> {
    return this.api.get<PagedResult<LookupOption>>('/manufacturers', {
      pageSize: ProductEditorApiService.LookupPageSize,
    });
  }

  natureOptions(): Observable<PagedResult<LookupOption>> {
    return this.api.get<PagedResult<LookupOption>>('/product-natures', {
      pageSize: ProductEditorApiService.LookupPageSize,
    });
  }

  unitOptions(): Observable<PagedResult<LookupOption>> {
    return this.api.get<PagedResult<LookupOption>>('/units', {
      pageSize: ProductEditorApiService.LookupPageSize,
    });
  }
}
