import { Injectable, inject, signal } from '@angular/core';

import { ApiClient } from '../../../core/api/api-client';
import {
  ApiErrorMapper,
  ClassifiedFailure,
  FailureMessageOverrides,
} from '../../../core/validation/api-error-mapper';
import { AdjustmentApiService } from '../adjustments/adjustment-api.service';
import { BatchPickerOption, ProductPickerOption } from '../adjustments/adjustment.models';
import { WriteOffPayload } from './write-off.models';

/**
 * The write-off screen's ruled contextual wordings (inventory ui.md §إهلاك
 * مخزون; BR-INV-061/067/068) — overrides on the shared ValidationRegistry
 * defaults, never a fork of it (STD-UX-110/111). Key ownership is the
 * screen's own (validation gap AP-16 cleanup).
 */
const WRITE_OFF_MESSAGES: FailureMessageOverrides = {
  'VTF-INV-061': 'writeOff.error.belowZero',
  'VTF-INV-067': 'writeOff.error.reason',
  'VTF-INV-068': 'writeOff.error.conflict',
  notFound: 'writeOff.error.notFound',
  system: 'writeOff.error.unknown',
};

export type WriteOffSubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'saved'; readonly movementId: string }
  | { readonly kind: 'failed'; readonly failure: ClassifiedFailure };

/**
 * Write-off state (REQ-INV-011). The product and batch pickers are the <b>same reads</b> the
 * adjustment screen uses, so {@link AdjustmentApiService} is reused rather than copied — the only
 * genuinely different call is the write-off POST itself.
 *
 * Expired batches are offered deliberately: refusing them here would leave the stranded stock R9
 * described with no way out (DEC-INV-021 governs selling, not disposal).
 *
 * The store holds no copy and makes no rule decision — every failure passes through the shared
 * ApiErrorMapper (STD-UX-123) and the page renders the classified message.
 */
@Injectable()
export class WriteOffStore {
  private readonly api = inject(ApiClient);
  private readonly pickers = inject(AdjustmentApiService);
  private readonly mapper = inject(ApiErrorMapper);

  readonly products = signal<readonly ProductPickerOption[]>([]);
  readonly batches = signal<readonly BatchPickerOption[]>([]);
  readonly batchesLoading = signal(false);
  /** Picker-load failures are surfaced with a retry — a failed load never degrades to an empty list (STD-UX-041). */
  readonly productsError = signal(false);
  readonly batchesError = signal(false);
  readonly submit = signal<WriteOffSubmitState>({ kind: 'idle' });

  loadProducts(): void {
    this.productsError.set(false);
    this.pickers.getActiveProducts().subscribe({
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
    this.pickers.getBatches(productId).subscribe({
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

  save(payload: WriteOffPayload): void {
    this.submit.set({ kind: 'saving' });
    this.api.post<{ readonly movementId: string }>('/inventory/write-offs', payload).subscribe({
      next: (result) => {
        this.submit.set({ kind: 'saved', movementId: result.movementId });
        // The batch moved, so the picker's remaining quantities are now stale.
        this.loadBatches(null);
      },
      error: (error: unknown) =>
        this.submit.set({ kind: 'failed', failure: this.mapper.map(error, WRITE_OFF_MESSAGES) }),
    });
  }

  reset(): void {
    this.submit.set({ kind: 'idle' });
  }
}
