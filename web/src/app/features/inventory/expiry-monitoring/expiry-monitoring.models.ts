/** Contract types of GET /api/v1/inventory/expiry (ADR-0015, REQ-INV-004). */

export interface ExpiryMonitoringItem {
  readonly productId: string;
  readonly productName: string;
  /** The batch's existing stable identity (BR-INV-025). */
  readonly batchId: string;
  /** Remaining quantity in the product's stock unit (BR-INV-034). */
  readonly remainingQuantity: number;
  readonly stockUnitName: string;
  /** Expiry date as an ISO `yyyy-mm-dd` string — always present in this projection (BR-INV-033). */
  readonly expiryDate: string;
}

/** A category option for the filter — the Catalog list shape ({ id, name }). */
export interface CategoryOption {
  readonly id: string;
  readonly name: string;
}

/**
 * The expiry monitoring filters (BR-INV-035): category, "expired" and "expiring soon"
 * (30-day horizon, BR-INV-013). Both are batch-level derived filters, not statuses (DEC-INV-012).
 */
export interface ExpiryMonitoringFilters {
  readonly category: CategoryOption | null;
  readonly expired: boolean;
  readonly expiringSoon: boolean;
}

export const EMPTY_EXPIRY_FILTERS: ExpiryMonitoringFilters = {
  category: null,
  expired: false,
  expiringSoon: false,
};

export interface ExpiryMonitoringRequest {
  readonly search: string;
  readonly filters: ExpiryMonitoringFilters;
  readonly page: number;
  readonly pageSize: number;
}
