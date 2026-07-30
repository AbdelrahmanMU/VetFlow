/** Contract types of GET /api/v1/sales-invoices/{id} (ADR-0015, REQ-SAL-002). */

export interface Money {
  readonly amount: number;
  readonly currency: string;
}

/** Two states only (BR-SAL-003); «ملغاة» was not introduced (DEC-SAL-009 — open). */
export type SaleStatus = 'draft' | 'committed';

export interface SaleDetails {
  readonly id: string;
  readonly number: string;
  /** Optional free text (DEC-SAL-002) — «—» when absent. */
  readonly customerName: string | null;
  /** Business date as an ISO `yyyy-mm-dd` string (date only, no time). */
  readonly saleDate: string;
  readonly status: SaleStatus;
  readonly total: Money;
  readonly notes: string | null;
  /** System timestamp as an ISO date-time string. */
  readonly createdAt: string;
}
