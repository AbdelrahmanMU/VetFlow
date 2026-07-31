import { Injectable, inject, signal } from '@angular/core';

import { ApiError } from '../../../core/api/problem-details';
import { AdjustmentApiService } from './adjustment-api.service';
import {
  AdjustmentFailure,
  AdjustmentPayload,
  BatchPickerOption,
  ProductPickerOption,
} from './adjustment.models';

/** Error codes the adjustment path can return — branch on the code, never on message text (STD-FE-037). */
const BelowZeroCode = 'VTF-INV-061';
const ReasonNotAllowedCode = 'VTF-INV-067';
const ConflictCode = 'VTF-INV-068';

export type AdjustmentSubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'saved'; readonly movementId: string }
  | { readonly kind: 'failed'; readonly failure: AdjustmentFailure };

/**
 * Inventory adjustment state (REQ-INV-010): signals for state, RxJS only at the HTTP boundary
 * (STD-FE-012/013).
 *
 * The store holds no copy and makes no rule decision — it classifies the server's error code into
 * one of the documented failures and lets the page choose the sentence (BR-INV-061/067/068).
 */
@Injectable()
export class AdjustmentStore {
  private readonly api = inject(AdjustmentApiService);

  readonly products = signal<readonly ProductPickerOption[]>([]);
  readonly batches = signal<readonly BatchPickerOption[]>([]);
  readonly batchesLoading = signal(false);
  readonly submit = signal<AdjustmentSubmitState>({ kind: 'idle' });

  loadProducts(): void {
    this.api.getActiveProducts().subscribe({
      next: (products) => this.products.set(products),
      error: () => this.products.set([]),
    });
  }

  loadBatches(productId: string | null): void {
    this.batches.set([]);
    if (!productId) {
      return;
    }

    this.batchesLoading.set(true);
    this.api.getBatches(productId).subscribe({
      next: (batches) => {
        this.batches.set(batches);
        this.batchesLoading.set(false);
      },
      error: () => {
        this.batches.set([]);
        this.batchesLoading.set(false);
      },
    });
  }

  save(payload: AdjustmentPayload): void {
    this.submit.set({ kind: 'saving' });
    this.api.adjust(payload).subscribe({
      next: (result) => {
        this.submit.set({ kind: 'saved', movementId: result.movementId });
        // The batch moved, so the picker's remaining quantities are now stale.
        this.loadBatches(null);
      },
      error: (error: unknown) => this.submit.set({ kind: 'failed', failure: classify(error) }),
    });
  }

  reset(): void {
    this.submit.set({ kind: 'idle' });
  }
}

function classify(error: unknown): AdjustmentFailure {
  if (!(error instanceof ApiError)) {
    return 'unknown';
  }

  switch (error.errorCode) {
    case BelowZeroCode:
      return 'belowZero';
    case ConflictCode:
      return 'conflict';
    case ReasonNotAllowedCode:
      return 'reason';
    default:
      return error.status === 404 ? 'notFound' : 'unknown';
  }
}
