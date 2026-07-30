import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import { Money } from './sale-details.models';
import { AddSaleLinePayload, ProductPickerOption, ProductSaleProfile, SaleLine } from './sale-lines.models';

/**
 * Data access of the sales line-items slice (REQ-SAL-001/002/003). The line and commit endpoints
 * are Sales-owned; the product picker and its sale units come from the existing Catalog read
 * endpoints (STD-FE-004 mirror-without-importing — minimal local response shapes, no cross-feature
 * import). Nothing here knows anything about batches (BR-SAL-013).
 */
@Injectable()
export class SaleLinesApiService {
  private static readonly ProductPageSize = 100;

  private readonly api = inject(ApiClient);

  getLines(invoiceId: string): Observable<readonly SaleLine[]> {
    return this.api.get<readonly SaleLine[]>(`/sales-invoices/${invoiceId}/lines`);
  }

  addLine(invoiceId: string, payload: AddSaleLinePayload): Observable<{ readonly lineId: string }> {
    return this.api.post<{ readonly lineId: string }>(`/sales-invoices/${invoiceId}/lines`, payload);
  }

  removeLine(invoiceId: string, lineId: string): Observable<void> {
    return this.api.delete<void>(`/sales-invoices/${invoiceId}/lines/${lineId}`);
  }

  /** Commit the sale (REQ-SAL-003) — the single stock-consuming action. No body: the invoice has everything. */
  commit(invoiceId: string): Observable<void> {
    return this.api.post<void>(`/sales-invoices/${invoiceId}/commit`, {});
  }

  /** Active products only — the approved picker convention (DEC-PUR-006). */
  getActiveProducts(): Observable<readonly ProductPickerOption[]> {
    return this.api
      .get<PagedResult<CatalogProductItem>>('/products', {
        status: 'active',
        sort: 'name',
        dir: 'asc',
        pageSize: SaleLinesApiService.ProductPageSize,
      })
      .pipe(map((page) => page.items.map((item) => ({ id: item.id, name: item.arabicName }))));
  }

  /**
   * The sale units of one product with their catalog prices, plus whether the product may be sold
   * in fractions (BR-SAL-004, DEC-SAL-007). Sale-role units only.
   */
  getSaleProfile(productId: string): Observable<ProductSaleProfile> {
    return this.api.get<CatalogProductWithUnits>(`/products/${productId}`).pipe(
      map((product) => ({
        isSplittable: product.isSplittable,
        units: product.units
          .filter((unit) => unit.isSaleUnit)
          .map((unit) => ({
            unitId: unit.unitId,
            unitName: unit.unitName,
            isDefaultSaleUnit: unit.isDefaultSaleUnit,
            sellingPrice: unit.sellingPrice ?? null,
          })),
      })),
    );
  }
}

/** Minimal shapes of the Catalog responses this slice consumes (no cross-feature import). */
interface CatalogProductItem {
  readonly id: string;
  readonly arabicName: string;
}

interface CatalogProductUnit {
  readonly unitId: string;
  readonly unitName: string;
  readonly isSaleUnit: boolean;
  readonly isDefaultSaleUnit: boolean;
  readonly sellingPrice: Money | null;
}

interface CatalogProductWithUnits {
  readonly isSplittable: boolean;
  readonly units: readonly CatalogProductUnit[];
}
