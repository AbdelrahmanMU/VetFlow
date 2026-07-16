import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import {
  CategoryOption,
  CreateProductPayload,
  CreatedProduct,
  EditProduct,
  LookupOption,
  ManufacturerOption,
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

  /** The management list already carries {id, name, isActive}, so the editor filters
   *  active/inactive client-side (REQ-CTG-005) — no separate options endpoint needed. */
  categoryOptions(): Observable<PagedResult<CategoryOption>> {
    return this.api.get<PagedResult<CategoryOption>>('/categories', {
      pageSize: ProductEditorApiService.LookupPageSize,
    });
  }

  /** The management list already carries {id, name, isActive}, so the editor filters
   *  active/inactive client-side (REQ-CAT-048) — no separate options endpoint needed. */
  manufacturerOptions(): Observable<PagedResult<ManufacturerOption>> {
    return this.api.get<PagedResult<ManufacturerOption>>('/manufacturers', {
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
