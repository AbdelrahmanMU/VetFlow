/** Contract types of the sales line-items endpoints (REQ-SAL-001/002, ADR-0015). */

import { Money } from './sale-details.models';

export interface SaleLine {
  readonly id: string;
  readonly productId: string;
  /** Immutable product-name snapshot captured at add time (BR-SAL-006). */
  readonly productName: string;
  readonly saleUnitId: string;
  /** Immutable sale-unit-name snapshot captured at add time (BR-SAL-006). */
  readonly saleUnitName: string;
  readonly quantity: number;
  /** Immutable snapshot of the catalog sale price (BR-SAL-006, DEC-SAL-003). */
  readonly unitPrice: Money;
  readonly lineTotal: Money;
}

/**
 * JSON body of POST /api/v1/sales-invoices/{id}/lines. There is deliberately no price field: the
 * unit price is the catalog snapshot the server takes, never client input (DEC-SAL-003).
 */
export interface AddSaleLinePayload {
  readonly productId: string;
  readonly saleUnitId: string;
  readonly quantity: number;
}

/** A selectable active product for the add-line picker (from the Catalog list API). */
export interface ProductPickerOption {
  readonly id: string;
  readonly name: string;
}

/** A sale unit of the selected product, with its catalog price (from the Catalog details API). */
export interface SaleUnitOption {
  readonly unitId: string;
  readonly unitName: string;
  readonly isDefaultSaleUnit: boolean;
  /** Null when the catalog defines no price for this unit — such a line is rejected (TS-SAL-006). */
  readonly sellingPrice: Money | null;
}

/** Whether the picked product may be sold in fractional quantities (DEC-SAL-007). */
export interface ProductSaleProfile {
  readonly isSplittable: boolean;
  readonly units: readonly SaleUnitOption[];
}
