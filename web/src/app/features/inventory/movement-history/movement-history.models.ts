/** Contract types of GET /api/v1/inventory/movements (ADR-0015, REQ-INV-005). */

/** The closed set of movement types (BR-INV-065) — never widened on the client. */
export type MovementType =
  | 'receive'
  | 'consume'
  | 'adjustment'
  | 'writeOff'
  | 'purchaseReturn'
  | 'salesReturn';

/** The module that caused the movement (BR-INV-043, DEC-INV-016). */
export type MovementSource = 'purchasing' | 'sales' | 'inventory';

/** What a row's reference opens; `none` means there is no document at all (DEC-INV-036). */
export type MovementReferenceTarget = 'none' | 'purchaseInvoice' | 'salesInvoice';

/**
 * One history row — the seven frozen fields of BR-INV-041 and nothing else. The ledger also
 * carries a reason, a note and an actor name (BR-INV-066/067); the screen deliberately does not
 * show them, because the field list is locked and DEC-INV-038 reopened this design for movement
 * types, not for new columns.
 */
export interface MovementHistoryItem {
  readonly movementId: string;
  /** ISO timestamp of the movement (BR-INV-041 field 1). */
  readonly occurredAt: string;
  readonly type: MovementType;
  readonly productName: string;
  /** The batch's existing stable identity (BR-INV-025). */
  readonly batchId: string;
  /** Signed quantity in the stock unit: positive increases, negative decreases (BR-INV-064). */
  readonly quantity: number;
  readonly stockUnitName: string;
  /** The causing document's number, or null when the operation has no document. */
  readonly referenceLabel: string | null;
  readonly referenceTarget: MovementReferenceTarget;
  readonly referenceId: string | null;
  readonly source: MovementSource;
}

export interface MovementHistoryRequest {
  readonly page: number;
  readonly pageSize: number;
}
