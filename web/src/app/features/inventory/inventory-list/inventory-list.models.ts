/** Contract types of GET /api/v1/inventory (ADR-0015, REQ-INV-002). */

export interface InventoryItem {
  /** The product this on-hand row belongs to — the row's navigation target (BR-INV-015). */
  readonly productId: string;
  readonly productName: string;
  /** On-hand quantity in the product's canonical stock unit (BR-INV-008). */
  readonly onHandQuantity: number;
  readonly stockUnitName: string;
  /** Count of active batches — RemainingQuantity > 0 (BR-INV-009). */
  readonly batchCount: number;
  /** Nearest expiry as an ISO `yyyy-mm-dd` string, or null when none (BR-INV-010). */
  readonly nearestExpiry: string | null;
}

/** A category option for the filter — the Catalog list shape ({ id, name }). */
export interface CategoryOption {
  readonly id: string;
  readonly name: string;
}

/**
 * The projection filters (BR-INV-014): category, "expiring soon" (30-day horizon,
 * BR-INV-013) and "out of stock" (BR-INV-011). "Low stock" is deferred (DEC-INV-004)
 * and intentionally absent.
 */
export interface InventoryFilters {
  readonly category: CategoryOption | null;
  readonly expiringSoon: boolean;
  readonly outOfStock: boolean;
}

export const EMPTY_FILTERS: InventoryFilters = {
  category: null,
  expiringSoon: false,
  outOfStock: false,
};

export type InventorySortField = 'product' | 'onHand' | 'nearestExpiry';

export interface InventorySort {
  readonly field: InventorySortField;
  readonly direction: 'asc' | 'desc';
}

export interface InventoryListRequest {
  readonly search: string;
  readonly filters: InventoryFilters;
  readonly sort: InventorySort;
  readonly page: number;
  readonly pageSize: number;
}
