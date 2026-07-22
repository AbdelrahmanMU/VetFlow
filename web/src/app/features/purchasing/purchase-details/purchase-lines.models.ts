/** Contract types of the purchase line-items endpoints (REQ-PUR-004, ADR-0015). */

import { Money } from './purchase-details.models';

export interface PurchaseLine {
  readonly id: string;
  readonly productId: string;
  /** Immutable product-name snapshot captured at add time (BR-PUR-007). */
  readonly productName: string;
  readonly purchaseUnitId: string;
  /** Immutable purchase-unit-name snapshot captured at add time (BR-PUR-007). */
  readonly purchaseUnitName: string;
  readonly quantity: number;
  readonly unitPrice: Money;
  readonly lineTotal: Money;
  /** Live flag: the line's product currently requires an expiry date at receiving (BR-PUR-013). */
  readonly requiresExpiry: boolean;
}

/** JSON body of POST /api/v1/purchase-invoices/{id}/receive (REQ-PUR-005). */
export interface ReceivePurchaseInvoicePayload {
  readonly lines: readonly ReceiveLineExpiry[];
}

/** An expiry date captured for one line at receiving (BR-PUR-013); ISO yyyy-MM-dd. */
export interface ReceiveLineExpiry {
  readonly lineId: string;
  readonly expiryDate: string | null;
}

/** JSON body of POST /api/v1/purchase-invoices/{id}/lines. */
export interface AddPurchaseLinePayload {
  readonly productId: string;
  readonly purchaseUnitId: string;
  readonly quantity: number;
  readonly unitPrice: number;
}

/** A selectable active product for the add-line picker (from the Catalog list API). */
export interface ProductPickerOption {
  readonly id: string;
  readonly name: string;
}

/** A purchase unit of the selected product (from the Catalog details API). */
export interface PurchaseUnitOption {
  readonly unitId: string;
  readonly unitName: string;
  readonly isDefaultPurchaseUnit: boolean;
}
