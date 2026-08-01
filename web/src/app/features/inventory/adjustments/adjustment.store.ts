import { Injectable, inject, signal } from '@angular/core';

import {
  ApiErrorMapper,
  ClassifiedFailure,
  FailureMessageOverrides,
} from '../../../core/validation/api-error-mapper';
import { AdjustmentApiService } from './adjustment-api.service';
import { AdjustmentPayload, BatchPickerOption, ProductPickerOption } from './adjustment.models';

/**
 * The adjustment screen's ruled contextual wordings (inventory ui.md «الرفض —
 * رسائل صريحة لا صامتة»; BR-INV-061/067/068) — overrides on the shared
 * ValidationRegistry defaults, never a fork of it (STD-UX-110/111).
 */
const ADJUSTMENT_MESSAGES: FailureMessageOverrides = {
  'VTF-INV-061': 'adjustment.error.belowZero',
  'VTF-INV-067': 'adjustment.error.reason',
  'VTF-INV-068': 'adjustment.error.conflict',
  notFound: 'adjustment.error.notFound',
  system: 'adjustment.error.unknown',
};

export type AdjustmentSubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'saved'; readonly movementId: string }
  | { readonly kind: 'failed'; readonly failure: ClassifiedFailure };

/**
 * Inventory adjustment state (REQ-INV-010): signals for state, RxJS only at the HTTP boundary
 * (STD-FE-012/013).
 *
 * The store holds no copy and makes no rule decision — every failure passes through the shared
 * ApiErrorMapper (STD-UX-123) and the page renders the classified message.
 */
@Injectable()
export class AdjustmentStore {
  private readonly api = inject(AdjustmentApiService);
  private readonly mapper = inject(ApiErrorMapper);

  readonly products = signal<readonly ProductPickerOption[]>([]);
  readonly batches = signal<readonly BatchPickerOption[]>([]);
  readonly batchesLoading = signal(false);
  /** Picker-load failures are surfaced with a retry — a failed load never degrades to an empty list (STD-UX-041). */
  readonly productsError = signal(false);
  readonly batchesError = signal(false);
  readonly submit = signal<AdjustmentSubmitState>({ kind: 'idle' });

  loadProducts(): void {
    this.productsError.set(false);
    this.api.getActiveProducts().subscribe({
      next: (products) => this.products.set(products),
      error: () => {
        this.products.set([]);
        this.productsError.set(true);
      },
    });
  }

  loadBatches(productId: string | null): void {
    this.batches.set([]);
    this.batchesError.set(false);
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
        this.batchesError.set(true);
      },
    });
  }

  /** Retry the batches load for the currently chosen product (STD-UX-041). */
  retryBatches(productId: string | null): void {
    this.loadBatches(productId);
  }

  save(payload: AdjustmentPayload): void {
    this.submit.set({ kind: 'saving' });
    this.api.adjust(payload).subscribe({
      next: (result) => {
        this.submit.set({ kind: 'saved', movementId: result.movementId });
        // The batch moved, so the picker's remaining quantities are now stale.
        this.loadBatches(null);
      },
      error: (error: unknown) =>
        this.submit.set({ kind: 'failed', failure: this.mapper.map(error, ADJUSTMENT_MESSAGES) }),
    });
  }

  reset(): void {
    this.submit.set({ kind: 'idle' });
  }
}
