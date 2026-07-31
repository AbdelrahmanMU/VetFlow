import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { AdjustmentPayload, BatchPickerOption, ProductPickerOption } from './adjustment.models';

interface CatalogProductItem {
  readonly id: string;
  readonly arabicName: string;
}

interface BatchViewerResponse {
  readonly batches: PagedResult<{
    readonly batchId: string;
    readonly remainingQuantity: number;
    readonly stockUnitName: string;
    readonly expiryDate: string | null;
  }>;
}

/**
 * Data access of the inventory adjustment slice (REQ-INV-010). The product picker and the batch
 * list come from **existing** read endpoints — the batch viewer's own query serves the batch
 * picker, so no endpoint was added for the form (STD-FE-004 mirror-without-importing: minimal local
 * response shapes, no cross-feature import).
 */
@Injectable()
export class AdjustmentApiService {
  private static readonly ProductPageSize = 100;
  private static readonly BatchPageSize = 100;

  private readonly api = inject(ApiClient);

  adjust(payload: AdjustmentPayload): Observable<{ readonly movementId: string }> {
    return this.api.post<{ readonly movementId: string }>('/inventory/adjustments', payload);
  }

  /** Active products only — the approved picker convention (DEC-PUR-006). */
  getActiveProducts(): Observable<readonly ProductPickerOption[]> {
    return this.api
      .get<PagedResult<CatalogProductItem>>('/products', {
        status: 'active',
        sort: 'name',
        dir: 'asc',
        pageSize: AdjustmentApiService.ProductPageSize,
      })
      .pipe(map((result) => result.items.map((item) => ({ id: item.id, name: item.arabicName }))));
  }

  /**
   * Every batch of the product, active and depleted alike (BR-INV-019). Depleted ones are kept
   * deliberately: an adjustment can add back to a batch that reached zero, and hiding them would
   * silently make that impossible.
   */
  getBatches(productId: string): Observable<readonly BatchPickerOption[]> {
    return this.api
      .get<BatchViewerResponse>(`/inventory/${productId}/batches`, {
        pageSize: AdjustmentApiService.BatchPageSize,
      })
      .pipe(
        map((result) =>
          result.batches.items.map((batch) => ({
            batchId: batch.batchId,
            remainingQuantity: batch.remainingQuantity,
            stockUnitName: batch.stockUnitName,
            expiryDate: batch.expiryDate,
          })),
        ),
      );
  }
}
