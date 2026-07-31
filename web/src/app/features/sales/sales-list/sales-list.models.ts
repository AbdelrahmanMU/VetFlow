/** Contract types of GET /api/v1/sales-invoices (ADR-0015, REQ-SAL-005). */

export interface Money {
  readonly amount: number;
  readonly currency: string;
}

export type SaleListStatus = 'draft' | 'committed';

export interface SalesListItem {
  readonly id: string;
  readonly number: string;
  /** Optional free text (DEC-SAL-002) — null renders as a dash. */
  readonly customerName: string | null;
  /** Business date as an ISO `yyyy-mm-dd` string (date only, no time). */
  readonly saleDate: string;
  readonly status: SaleListStatus;
  readonly total: Money;
  /** System timestamp as an ISO date-time string. */
  readonly createdAt: string;
}

/** The Sales-owned list filters (BR-SAL-019): status and a sale-date range. */
export interface SalesListFilters {
  readonly status: SaleListStatus | null;
  /** Inclusive lower bound, ISO `yyyy-mm-dd`, or null. */
  readonly dateFrom: string | null;
  /** Inclusive upper bound, ISO `yyyy-mm-dd`, or null. */
  readonly dateTo: string | null;
}

export const EMPTY_FILTERS: SalesListFilters = {
  status: null,
  dateFrom: null,
  dateTo: null,
};

export type SalesListSortField = 'number' | 'saleDate' | 'customer' | 'status' | 'total';

export interface SalesListSort {
  readonly field: SalesListSortField;
  readonly direction: 'asc' | 'desc';
}

export interface SalesListRequest {
  readonly search: string;
  readonly filters: SalesListFilters;
  readonly sort: SalesListSort;
  readonly page: number;
  readonly pageSize: number;
}
