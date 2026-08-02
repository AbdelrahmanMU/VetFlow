/**
 * Contract types of GET /api/v1/inventory/{productId}/summary (REQ-INV-012, ADR-0015).
 *
 * <b>Owned by Inventory, not by Catalog.</b> The product-details screen displays these
 * numbers; it does not compute, convert or aggregate them. On-hand is the canonical stored
 * balance (BR-INV-008), the batch count is active batches only (BR-INV-009), and the nearest
 * expiry is the minimum across those same batches (BR-INV-010) — all decided by the Inventory
 * module and rendered here verbatim, in the product's stock unit.
 */
export interface ProductInventorySummary {
  readonly productId: string;
  readonly onHandQuantity: number;
  readonly stockUnitName: string;
  readonly batchCount: number;
  /** ISO `yyyy-mm-dd`, or null when no active batch carries an expiry. */
  readonly nearestExpiry: string | null;
  /**
   * False when the product has never been received at all — which reads identically to
   * "received and now at zero" in the numbers, but means something different to the user.
   */
  readonly hasInventoryRecord: boolean;
}
