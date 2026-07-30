/** Contract types of POST /api/v1/sales-invoices (ADR-0015, REQ-SAL-001). */

export interface CreateSalePayload {
  /** Optional free text — a direct cash sale may have no named customer (DEC-SAL-002). */
  readonly customerName: string | null;
  /** Business date as an ISO `yyyy-mm-dd` string (date only, no time). */
  readonly saleDate: string;
  readonly notes: string | null;
}

/** The lightweight result of creating a sales invoice: the new id and the system number (BR-SAL-002). */
export interface CreatedSale {
  readonly id: string;
  readonly number: string;
}
