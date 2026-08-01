/** Contract types of the purchase-return endpoints (ADR-0015, REQ-PUR-006, DEC-PUR-010). */

/**
 * One original invoice line as the return screen sees it, from
 * `GET /purchase-invoices/{id}/returnable-lines`.
 *
 * There is no price and no line total: a return has **no financial effect at all**
 * (DEC-INV-035), and showing money here would imply a credit that does not exist. There is no
 * batch either — the destination is derived from the original line (BR-PUR-017), so surfacing it
 * would invite a picker the rules forbid.
 */
export interface ReturnableLine {
  readonly purchaseLineItemId: string;
  readonly productId: string;
  readonly productName: string;
  readonly purchaseUnitName: string;
  /** Originally received on this line, in the line's purchase unit. */
  readonly quantity: number;
  /** Already returned across every committed return of this invoice (BR-PUR-016). */
  readonly returnedQuantity: number;
  /** What a new return line may still take (BR-PUR-016). */
  readonly returnableQuantity: number;
}

/** Header of a new return. No supplier (snapshotted server-side) and no reason (BR-INV-067). */
export interface CreatePurchaseReturnPayload {
  readonly purchaseInvoiceId: string;
  readonly returnDate: string;
  readonly notes: string | null;
}

/**
 * One line being returned. Quantities are in the **original line's purchase unit** — the unit the
 * screen shows and BR-PUR-016 caps. The conversion to stock units is the server's (BR-PUR-016أ).
 */
export interface AddReturnLinePayload {
  readonly purchaseLineItemId: string;
  readonly quantity: number;
}
