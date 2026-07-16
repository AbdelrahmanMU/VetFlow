/** Contract types of GET /api/v1/purchase-invoices (ADR-0015, REQ-PUR-001). */

export interface Money {
  readonly amount: number;
  readonly currency: string;
}

export type PurchaseStatus = 'draft' | 'received' | 'cancelled';

export interface PurchaseListItem {
  readonly id: string;
  readonly number: string;
  readonly supplierName: string;
  readonly supplierInvoiceReference: string | null;
  /** Business date as an ISO `yyyy-mm-dd` string (date only, no time). */
  readonly invoiceDate: string;
  readonly status: PurchaseStatus;
  readonly total: Money;
  /** System timestamp as an ISO date-time string. */
  readonly createdAt: string;
}

/** The Purchasing-owned list filters (BR-PUR-004): status and an invoice-date range. */
export interface PurchaseFilters {
  readonly status: PurchaseStatus | null;
  /** Inclusive lower bound, ISO `yyyy-mm-dd`, or null. */
  readonly dateFrom: string | null;
  /** Inclusive upper bound, ISO `yyyy-mm-dd`, or null. */
  readonly dateTo: string | null;
}

export const EMPTY_FILTERS: PurchaseFilters = {
  status: null,
  dateFrom: null,
  dateTo: null,
};

export type PurchaseSortField = 'number' | 'invoiceDate' | 'supplier' | 'status' | 'total';

export interface PurchaseSort {
  readonly field: PurchaseSortField;
  readonly direction: 'asc' | 'desc';
}

export interface PurchaseListRequest {
  readonly search: string;
  readonly filters: PurchaseFilters;
  readonly sort: PurchaseSort;
  readonly page: number;
  readonly pageSize: number;
}
