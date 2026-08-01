/** Contract types of the sales-return endpoints (ADR-0015, REQ-SAL-004, DEC-SAL-010). */

/**
 * One original sale line as the return screen sees it, from
 * `GET /sales-invoices/{id}/returnable-lines`.
 *
 * There is no unit price and no line total, though the sale line itself has both: a return has
 * **no financial effect at all** (DEC-INV-035) and cash refunds stay out of scope (DEC-SAL-001),
 * so an amount here would imply a refund that does not exist. There is no batch either — the
 * destinations are read from the recorded consumption at commit (BR-SAL-017), and Sales holds no
 * batch reference at all (BR-SAL-013).
 */
export interface ReturnableSaleLine {
  readonly salesLineItemId: string;
  readonly productId: string;
  readonly productName: string;
  readonly saleUnitName: string;
  /** Originally sold on this line, in the line's sale unit («الكمّية المباعة»). */
  readonly quantity: number;
  /** Already returned across every committed return of this invoice (BR-SAL-016). */
  readonly returnedQuantity: number;
  /** What a new return line may still take («المتبقّي القابل للإرجاع» — BR-SAL-016). */
  readonly returnableQuantity: number;
}

/** Header of a new return. No customer (snapshotted server-side) and no reason (BR-INV-067). */
export interface CreateSalesReturnPayload {
  readonly salesInvoiceId: string;
  readonly returnDate: string;
  readonly notes: string | null;
}

/**
 * One line being returned. Quantities are in the **original line's sale unit** — the unit the
 * screen shows and BR-SAL-016 caps. Converting to stock units, and splitting across the batches the
 * goods actually left, is the server's (BR-SAL-017).
 */
export interface AddSalesReturnLinePayload {
  readonly salesLineItemId: string;
  readonly quantity: number;
}
