/** Contract types of POST /api/v1/purchase-invoices (ADR-0015, REQ-PUR-003). */

export interface CreatePurchasePayload {
  readonly supplierName: string;
  readonly supplierInvoiceReference: string | null;
  /** Business date as an ISO `yyyy-mm-dd` string (date only, no time). */
  readonly invoiceDate: string;
  readonly notes: string | null;
}

/** The lightweight result of creating a purchase invoice: the new id and the
 *  system-generated number (BR-PUR-002). */
export interface CreatedPurchase {
  readonly id: string;
  readonly number: string;
}
