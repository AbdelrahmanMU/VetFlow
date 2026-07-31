/** Contract types of POST /api/v1/inventory/write-offs (ADR-0015, REQ-INV-011). */

/**
 * The write-off reason list, and only it (DEC-INV-031, BR-INV-067). `countCorrection`,
 * `initialBalance` and `found` are absent by design — they belong to adjustments, and «موجود» on a
 * write-off would be a contradiction in terms.
 */
export type WriteOffReason = 'expired' | 'damaged' | 'lost' | 'contaminated' | 'other';

export const WRITE_OFF_REASONS: readonly WriteOffReason[] = [
  'expired',
  'damaged',
  'lost',
  'contaminated',
  'other',
];

/** There is no direction: a write-off only ever removes stock. */
export interface WriteOffPayload {
  readonly batchId: string;
  readonly quantity: number;
  readonly reason: WriteOffReason;
  readonly reasonNote: string | null;
  readonly actorName: string | null;
}
