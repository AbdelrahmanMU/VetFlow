import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { ProductDetails } from './product-details.models';
import { ProductInventorySummary } from './product-inventory.models';

/** Data access of the product-details slice (screen S2). */
@Injectable()
export class ProductDetailsApiService {
  private readonly api = inject(ApiClient);

  getById(id: string): Observable<ProductDetails> {
    return this.api.get<ProductDetails>(`/products/${id}`);
  }

  /**
   * The inventory card's data (catalog ui.md §4, card 7) read from **Inventory's own public
   * contract** (REQ-INV-012) — not from Catalog's, and not assembled here from batches.
   * A second call rather than a widened product contract: stock is Inventory's fact to
   * report, and the two loads fail independently so a stock outage cannot blank the page.
   */
  getInventorySummary(id: string): Observable<ProductInventorySummary> {
    return this.api.get<ProductInventorySummary>(`/inventory/${id}/summary`);
  }
}
