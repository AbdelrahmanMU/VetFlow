/** Contract types of GET /api/v1/inventory/{productId}/batches (ADR-0015, REQ-INV-003). */

/** Derived batch status — Active/Depleted only (BR-INV-021, DEC-INV-011). */
export type BatchStatus = 'active' | 'depleted';

export interface BatchViewerItem {
  /** The batch's existing stable identity — no new field (BR-INV-025, DEC-INV-009). */
  readonly batchId: string;
  /** The owning purchase invoice number, e.g. PUR-000001 (BR-INV-024). */
  readonly purchaseReference: string;
  /** The owning invoice id — the navigation target /purchases/:id (BR-INV-024, DEC-INV-010). */
  readonly purchaseInvoiceId: string;
  /** Receive timestamp as an ISO string (BR-INV-020). */
  readonly receiveDate: string;
  /** Original received quantity in the product's stock unit — immutable (BR-INV-022). */
  readonly originalQuantity: number;
  /** Remaining quantity as stored, in the product's stock unit (BR-INV-022). */
  readonly remainingQuantity: number;
  readonly stockUnitName: string;
  /** Unit-cost snapshot — a frozen historical value (BR-INV-022). */
  readonly unitCostSnapshot: number;
  /** Expiry date as an ISO `yyyy-mm-dd` string, or null when none (BR-INV-023). */
  readonly expiryDate: string | null;
  readonly status: BatchStatus;
}

/** The batch viewer response: product header + paged batch rows (REQ-INV-003). */
export interface BatchViewerResult {
  readonly productName: string;
  readonly stockUnitName: string;
  readonly batches: {
    readonly items: readonly BatchViewerItem[];
    readonly page: number;
    readonly pageSize: number;
    readonly totalCount: number;
  };
}

/**
 * The batch viewer filters (BR-INV-026): batch status (Active/Depleted), "expired"
 * and "expiring soon" (30-day horizon, BR-INV-013). "Expired" is a filter, never a
 * status (DEC-INV-012).
 */
export interface BatchViewerFilters {
  readonly status: BatchStatus | null;
  readonly expired: boolean;
  readonly expiringSoon: boolean;
}

export const EMPTY_BATCH_FILTERS: BatchViewerFilters = {
  status: null,
  expired: false,
  expiringSoon: false,
};

export type BatchViewerSortField = 'receiveDate' | 'expiryDate' | 'remainingQuantity';

export interface BatchViewerSort {
  readonly field: BatchViewerSortField;
  readonly direction: 'asc' | 'desc';
}

export interface BatchViewerRequest {
  readonly productId: string;
  readonly filters: BatchViewerFilters;
  readonly sort: BatchViewerSort;
  readonly page: number;
  readonly pageSize: number;
}
