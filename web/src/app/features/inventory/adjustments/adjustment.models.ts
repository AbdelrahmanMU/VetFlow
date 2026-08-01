/** Contract types of POST /api/v1/inventory/adjustments (ADR-0015, REQ-INV-010). */

/** Explicit direction — the sign belongs to the server (BR-INV-064). */
export type AdjustmentDirection = 'increase' | 'decrease';

/**
 * The adjustment reason list, and only it (DEC-INV-031, BR-INV-067). `expired` and `contaminated`
 * are absent on purpose: they belong to write-off, and offering them here would merge two lists the
 * owner ruled separately.
 */
export type AdjustmentReason =
  | 'countCorrection'
  | 'initialBalance'
  | 'damaged'
  | 'found'
  | 'lost'
  | 'other';

export const ADJUSTMENT_REASONS: readonly AdjustmentReason[] = [
  'countCorrection',
  'initialBalance',
  'damaged',
  'found',
  'lost',
  'other',
];

export interface AdjustmentPayload {
  readonly batchId: string;
  readonly direction: AdjustmentDirection;
  readonly quantity: number;
  readonly reason: AdjustmentReason;
  /** Optional free-text note (BR-INV-067). */
  readonly reasonNote: string | null;
  /** Optional free-text actor; absence never blocks the operation (BR-INV-066, DEC-INV-030). */
  readonly actorName: string | null;
}

export interface ProductPickerOption {
  readonly id: string;
  readonly name: string;
}

/** One selectable batch of the chosen product — read through the existing batch-viewer endpoint. */
export interface BatchPickerOption {
  readonly batchId: string;
  readonly remainingQuantity: number;
  readonly stockUnitName: string;
  readonly expiryDate: string | null;
}
