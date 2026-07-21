import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { ApiClient } from '../../../core/api/api-client';
import { PagedResult } from '../../../core/api/paged-result';
import {
  AddPurchaseLinePayload,
  ProductPickerOption,
  PurchaseLine,
  PurchaseUnitOption,
} from './purchase-lines.models';

/**
 * Data access of the purchase line-items slice (REQ-PUR-004). The line endpoints are
 * Purchasing-owned; the product picker and its units come from the existing Catalog
 * read endpoints (STD-FE-004 mirror-without-importing — minimal local response shapes,
 * no cross-feature import).
 */
@Injectable()
export class PurchaseLinesApiService {
  private static readonly ProductPageSize = 100;

  private readonly api = inject(ApiClient);

  getLines(invoiceId: string): Observable<readonly PurchaseLine[]> {
    return this.api.get<readonly PurchaseLine[]>(`/purchase-invoices/${invoiceId}/lines`);
  }

  addLine(invoiceId: string, payload: AddPurchaseLinePayload): Observable<{ readonly lineId: string }> {
    return this.api.post<{ readonly lineId: string }>(`/purchase-invoices/${invoiceId}/lines`, payload);
  }

  removeLine(invoiceId: string, lineId: string): Observable<void> {
    return this.api.delete<void>(`/purchase-invoices/${invoiceId}/lines/${lineId}`);
  }

  /** Active products only (DEC-PUR-003 — the picker mirrors the Catalog active-only convention). */
  getActiveProducts(): Observable<readonly ProductPickerOption[]> {
    return this.api
      .get<PagedResult<CatalogProductItem>>('/products', {
        status: 'active',
        sort: 'name',
        dir: 'asc',
        pageSize: PurchaseLinesApiService.ProductPageSize,
      })
      .pipe(map((page) => page.items.map((item) => ({ id: item.id, name: item.arabicName }))));
  }

  /** The purchase units of one product (BR-PUR-005 — purchase-role units only). */
  getPurchaseUnits(productId: string): Observable<readonly PurchaseUnitOption[]> {
    return this.api.get<CatalogProductWithUnits>(`/products/${productId}`).pipe(
      map((product) =>
        product.units
          .filter((unit) => unit.isPurchaseUnit)
          .map((unit) => ({
            unitId: unit.unitId,
            unitName: unit.unitName,
            isDefaultPurchaseUnit: unit.isDefaultPurchaseUnit,
          })),
      ),
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
  readonly isPurchaseUnit: boolean;
  readonly isDefaultPurchaseUnit: boolean;
}

interface CatalogProductWithUnits {
  readonly units: readonly CatalogProductUnit[];
}
