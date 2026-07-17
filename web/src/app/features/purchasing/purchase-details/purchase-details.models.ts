/** Contract types of GET /api/v1/purchase-invoices/{id} (ADR-0015, REQ-PUR-002). */

export interface Money {
  readonly amount: number;
  readonly currency: string;
}

export type PurchaseStatus = 'draft' | 'received' | 'cancelled';

export interface PurchaseDetails {
  readonly id: string;
  readonly number: string;
  readonly supplierName: string;
  readonly supplierInvoiceReference: string | null;
  /** Business date as an ISO `yyyy-mm-dd` string (date only, no time). */
  readonly invoiceDate: string;
  readonly status: PurchaseStatus;
  readonly total: Money;
  readonly notes: string | null;
  /** System timestamp as an ISO date-time string. */
  readonly createdAt: string;
}
