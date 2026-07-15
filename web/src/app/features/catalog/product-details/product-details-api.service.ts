import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { ProductDetails } from './product-details.models';

/** Data access of the product-details slice (screen S2). */
@Injectable()
export class ProductDetailsApiService {
  private readonly api = inject(ApiClient);

  getById(id: string): Observable<ProductDetails> {
    return this.api.get<ProductDetails>(`/products/${id}`);
  }
}
